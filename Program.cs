using k8s;
using KubeManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços aos contentores.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----- CONFIGURAÇÃO DO KUBERNETES -----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<KubernetesService>();
builder.Services.AddScoped<ProxmoxService>();

// ----- CONFIGURAÇÃO DO PROXMOX -----
builder.Services.AddHttpClient<AuthService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

builder.Services.AddHttpClient<ProxmoxService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });
// -----------------------------------

var app = builder.Build();

// Configurar o pipeline de pedidos HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseDefaultFiles(); 
app.UseStaticFiles();  

// ── TRATAMENTO GLOBAL DE EXCEÇÕES ──
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

app.MapControllers();

app.Run();
