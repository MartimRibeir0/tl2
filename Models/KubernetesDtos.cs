namespace KubeManager.API.Models;

public record CreatePodDto(
    string Namespace, 
    string Name, 
    string Image, 
    int? ContainerPort = null,
    Dictionary<string, string>? Labels = null,
    Dictionary<string, string>? EnvVars = null,
    string? CpuLimit = null,
    string? MemLimit = null
);
public record CreateDeploymentDto(
    string Namespace, 
    string Name, 
    string Image, 
    int Replicas = 1, 
    int? ContainerPort = null,
    Dictionary<string, string>? EnvVars = null,
    string? CpuLimit = null,
    string? MemLimit = null,
    string? ImagePullPolicy = "IfNotPresent",
    string? UpdateStrategy = "RollingUpdate"
);
public record CreateNamespaceDto(string Name);
public record ServicePortDto(int Port, int TargetPort, string? Name = null, string Protocol = "TCP", int? NodePort = null);
public record CreateServiceDto(
    string Namespace, 
    string Name, 
    string AppLabel, 
    List<ServicePortDto> Ports,
    string Type = "ClusterIP",
    Dictionary<string, string>? CustomLabels = null
);
public record CreateIngressDto(
    string Namespace, 
    string Name, 
    string Host, 
    string ServiceName, 
    int Port, 
    string Path = "/", 
    string PathType = "Prefix", 
    string? TlsSecret = null,
    Dictionary<string, string>? Annotations = null
);
public record CreateSecretDto(string Namespace, string Name, string Key, string Cert);
public record ScaleDeploymentDto(string Namespace, string Name, int Replicas);
