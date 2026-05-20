namespace KubeManager.API.Models;

public record CreatePodDto(string Namespace, string Name, string Image);
public record CreateDeploymentDto(string Namespace, string Name, string Image, int Replicas = 1);
public record CreateNamespaceDto(string Name);
public record CreateServiceDto(string Namespace, string Name, string AppLabel, int Port, int TargetPort);