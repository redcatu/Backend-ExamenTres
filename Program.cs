var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. CONFIGURACIÓN DE PUERTO (PARA RAILWAY)
// -----------------------------------------------------------------------------
// Lee el puerto dinámico de Railway ($PORT) o usa 8080 como fallback.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// -----------------------------------------------------------------------------
// 2. SERVICIOS
// -----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // Mantienes esto para el consumo de API externas.

var app = builder.Build();

// -----------------------------------------------------------------------------
// 3. PIPELINE (MIDDLEWARES)
// -----------------------------------------------------------------------------

// Configuramos Swagger y SwaggerUI para que estén activos
// (Es común dejarlos activos en producción en Railway para testing).
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // Se puede deshabilitar en Railway si usas un proxy o CDN.

app.UseAuthorization();

app.MapControllers();

// Endpoint de prueba para verificar que la app funciona.
app.MapGet("/", () => "API ExamenTresBE funcionando! (Sin DB)");

app.Run();