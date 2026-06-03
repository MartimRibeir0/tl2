using k8s;
using KubeManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços aos contentores.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----- CONFIGURAÇÃO DO KUBERNETES -----
// Vai procurar a configuração automaticamente no ~/.kube/config (gerada pelo MicroK8s)
var kubeConfig = KubernetesClientConfiguration.BuildDefaultConfig();
builder.Services.AddSingleton<IKubernetes>(new Kubernetes(kubeConfig));
builder.Services.AddScoped<KubernetesService>();
// --------------------------------------

// ----- CONFIGURAÇÃO DO PROXMOX -----
builder.Services.AddHttpClient<ProxmoxService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Ignorar erros de SSL para ambientes de teste (Proxmox usa self-signed por padrão)
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

// ── ADICIONE ESTAS DUAS LINHAS ──
app.UseDefaultFiles(); // Procura automaticamente por ficheiros index.html
app.UseStaticFiles();  // Permite servir a pasta estática "wwwroot"
// ────────────────────────────────

app.MapControllers();

app.Run();

