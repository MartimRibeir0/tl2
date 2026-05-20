using k8s;
using KubeManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços aos contentores.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----- CONFIGURAÇÃO DO KUBERNETES -----
// Vai procurar a configuração automaticamente no ~/.kube/config (gerada pelo Minikube)
var kubeConfig = KubernetesClientConfiguration.BuildDefaultConfig();
builder.Services.AddSingleton<IKubernetes>(new Kubernetes(kubeConfig));
builder.Services.AddScoped<KubernetesService>();
// --------------------------------------

var app = builder.Build();

// Configurar o pipeline de pedidos HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

