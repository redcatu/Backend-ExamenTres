using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Examen3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContabilidadesController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        // Regla de Negocio: Definimos un tiempo máximo de espera (Timeout) de 5 segundos
        private readonly TimeSpan _timeoutLimit = TimeSpan.FromSeconds(5);

        public ContabilidadesController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet("Reporte/{codigoFactura}")]
        public async Task<IActionResult> GetReporteCombinado(int codigoFactura)
        {
            // --- 1. PREPARACIÓN DE DATOS ---
            List<FacturaDto> todasLasFacturas = new List<FacturaDto>();
            List<PagoDto> todosLosPagos = new List<PagoDto>();
            List<string> errores = new List<string>();

            string urlFacturas = "https://programacionweb2examen3-production.up.railway.app/api/Facturas/Listar";
            string urlPagos = "https://programacionweb2examen3-production.up.railway.app/api/Pagos/Listar";

            // Opciones de JSON para ser tolerante a mayúsculas/minúsculas
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // --- 2. CONSUMO DE SERVICIOS CON VALIDACIONES Y TIMEOUT ---

            // A) Obtener Facturas
            try
            {
                // Usamos CancellationTokenSource para aplicar el Timeout
                using var cts = new CancellationTokenSource(_timeoutLimit);

                var response = await _httpClient.GetAsync(urlFacturas, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    // VALIDACIÓN NULL CHECK: Si deserializa a null, asignamos lista vacía
                    todasLasFacturas = JsonSerializer.Deserialize<List<FacturaDto>>(content, jsonOptions) ?? new List<FacturaDto>();
                }
                else
                {
                    errores.Add($"Facturas API respondió con código: {response.StatusCode}");
                }
            }
            catch (TaskCanceledException) // Esto captura el TIMEOUT
            {
                errores.Add("El servicio de Facturas tardó demasiado (Timeout).");
            }
            catch (Exception ex)
            {
                errores.Add($"Error crítico en Facturas: {ex.Message}");
            }

            // B) Obtener Pagos
            try
            {
                using var cts = new CancellationTokenSource(_timeoutLimit);

                var response = await _httpClient.GetAsync(urlPagos, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    todosLosPagos = JsonSerializer.Deserialize<List<PagoDto>>(content, jsonOptions) ?? new List<PagoDto>();
                }
                else
                {
                    errores.Add($"Pagos API respondió con código: {response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                errores.Add("El servicio de Pagos tardó demasiado (Timeout).");
            }
            catch (Exception ex)
            {
                errores.Add($"Error crítico en Pagos: {ex.Message}");
            }

            // --- 3. REGLAS DE NEGOCIO Y ORQUESTACIÓN ---

            // VALIDACIÓN: ¿Existe la factura solicitada?
            var factura = todasLasFacturas.FirstOrDefault(f => f.Codigo == codigoFactura);

            if (factura == null)
            {
                // Si fallaron los servicios y no tenemos datos, devolvemos error 500 o 503
                if (errores.Any() && !todasLasFacturas.Any())
                {
                    return StatusCode(503, new { Mensaje = "Servicios externos no disponibles", Detalles = errores });
                }
                // Si los servicios funcionaron pero el ID no existe, es un 404
                return NotFound(new { Mensaje = $"No se encontró ninguna factura con el código {codigoFactura}" });
            }

            // FILTRADO (Lógica de relación 1 a muchos)
            var pagosAsociados = todosLosPagos.Where(p => p.FacturaCodigo == codigoFactura).ToList();

            // CÁLCULOS (Reglas de negocio)
            decimal totalPagadoReal = pagosAsociados.Sum(p => p.MontoPagado);
            decimal saldoPendiente = factura.MontoTotal - totalPagadoReal;

            // Lógica de estado: A veces la factura dice "Pagada: false", pero la suma de pagos dice que ya está cubierta.
            // Nosotros mandamos la verdad calculada.
            string estadoFinanciero = "";
            if (saldoPendiente <= 0) estadoFinanciero = "Pagada Totalmente";
            else if (totalPagadoReal > 0) estadoFinanciero = "Pago Parcial";
            else estadoFinanciero = "Sin Pagos";

            // --- 4. RESPUESTA COMBINADA (DTO FINAL) ---
            var respuesta = new
            {
                Factura = new
                {
                    Id = factura.Codigo,
                    Cliente = factura.ClienteCi,
                    Fecha = factura.Fecha.ToString("yyyy-MM-dd"),
                    MontoOriginal = factura.MontoTotal
                },
                EstadoDeCuenta = new
                {
                    TotalPagado = totalPagadoReal,
                    SaldoRestante = saldoPendiente < 0 ? 0 : saldoPendiente, // No mostrar saldo negativo si pagó de más
                    Estado = estadoFinanciero,
                    BanderaPagadaEnOrigen = factura.Pagada // Para comparar
                },
                DetallePagos = pagosAsociados.Select(p => new {
                    Fecha = p.FechaPago.ToString("yyyy-MM-dd"),
                    Monto = p.MontoPagado
                }),
                Metadata = new
                {
                    Exito = !errores.Any(),
                    Errores = errores // Aquí van los mensajes si un servicio falló
                }
            };

            return Ok(respuesta);
        }
    }

    // CLASES DTO (Las mismas de antes)
    public class FacturaDto
    {
        public int Codigo { get; set; }
        public int ClienteCi { get; set; }
        public DateTime Fecha { get; set; }
        public decimal MontoTotal { get; set; }
        public bool Pagada { get; set; }
    }

    public class PagoDto
    {
        public int Codigo { get; set; }
        public int FacturaCodigo { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal MontoPagado { get; set; }
    }
}