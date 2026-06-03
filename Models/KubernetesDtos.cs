namespace KubeManager.API.Models;

public record CreatePodDto(string Namespace, string Name, string Image);
public record CreateDeploymentDto(
    string Namespace, 
    string Name, 
    string Image, 
    int Replicas = 1, 
    Dictionary<string, string>? EnvVars = null,
    string? CpuLimit = null,
    string? MemLimit = null
);
public record CreateNamespaceDto(string Name);
public record CreateServiceDto(
    string Namespace, 
    string Name, 
    string AppLabel, 
    int Port, 
    int TargetPort, 
    string Type = "ClusterIP",
    int? NodePort = null
);
public record CreateIngressDto(string Namespace, string Name, string Host, string ServiceName, int Port);
public record ScaleDeploymentDto(string Namespace, string Name, int Replicas);
