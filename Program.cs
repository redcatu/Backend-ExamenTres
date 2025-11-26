var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. CONFIGURACIÓN DE PUERTO (PARA RAILWAY)
// -----------------------------------------------------------------------------
// Lee el puerto dinámico de Railway ($PORT) o usa 8080 como fallback.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// -----------------------------------------------------------------------------
// 2. CONFIGURACIÓN DE CORS (Permitir peticiones desde cualquier origen)
// -----------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod();
    });
});

// -----------------------------------------------------------------------------
// 3. SERVICIOS
// -----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // Mantiene esto para el consumo de API externas.

var app = builder.Build();

// -----------------------------------------------------------------------------
// 4. PIPELINE (MIDDLEWARES)
// -----------------------------------------------------------------------------

// Aplicar CORS antes de usar controladores
app.UseCors("AllowAll");

// Configuramos Swagger y SwaggerUI para que estén activos
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); 

app.UseAuthorization();

app.MapControllers();

// Endpoint de prueba para verificar que la app funciona.
app.MapGet("/", () => "API ExamenTresBE funcionando! (Sin DB)");

app.Run();