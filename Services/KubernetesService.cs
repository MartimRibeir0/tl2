using k8s;
using k8s.Models;

namespace KubeManager.API.Services;

public class KubernetesService
{
    private readonly IKubernetes _client;

    public KubernetesService(IKubernetes client)
    {
        _client = client;
    }

    // ── NODES ──────────────────────────────────────────────
    public async Task<object> GetNodesAsync()
    {
        var nodes = await _client.CoreV1.ListNodeAsync();
        return nodes.Items.Select(n => new
        {
            name = n.Metadata.Name,
            status = n.Status.Conditions?.LastOrDefault(c => c.Type == "Ready")?.Status,
            roles = n.Metadata.Labels?
                     .Where(l => l.Key.StartsWith("node-role.kubernetes.io/"))
                     .Select(l => l.Key.Replace("node-role.kubernetes.io/", "")) ?? Array.Empty<string>(),
            cpu = n.Status.Capacity != null && n.Status.Capacity.ContainsKey("cpu") ? n.Status.Capacity["cpu"].ToString() : "N/A",
            memory = n.Status.Capacity != null && n.Status.Capacity.ContainsKey("memory") ? n.Status.Capacity["memory"].ToString() : "N/A",
            k8sVersion = n.Status.NodeInfo?.KubeletVersion ?? "Unknown"
        });
    }

    // ── NAMESPACES ─────────────────────────────────────────
    public async Task<object> GetNamespacesAsync()
    {
        var ns = await _client.CoreV1.ListNamespaceAsync();
        return ns.Items.Select(n => new
        {
            name = n.Metadata.Name,
            status = n.Status.Phase,
            created = n.Metadata.CreationTimestamp
        });
    }

    public async Task CreateNamespaceAsync(string name)
    {
        var ns = new V1Namespace
        {
            Metadata = new V1ObjectMeta { Name = name }
        };
        await _client.CoreV1.CreateNamespaceAsync(ns);
    }

    public async Task DeleteNamespaceAsync(string name)
        => await _client.CoreV1.DeleteNamespaceAsync(name);

    // ── PODS ───────────────────────────────────────────────
    public async Task<object> GetPodsAsync(string ns = "default")
    {
        var pods = await _client.CoreV1.ListNamespacedPodAsync(ns);
        return pods.Items.Select(p => new
        {
            name = p.Metadata.Name,
            @namespace = p.Metadata.NamespaceProperty,
            status = p.Status.Phase,
            ip = p.Status.PodIP,
            node = p.Spec.NodeName,
            containers = p.Spec.Containers.Select(c => c.Name),
            created = p.Metadata.CreationTimestamp
        });
    }

    public async Task CreatePodAsync(string ns, string name, string image)
    {
        var pod = new V1Pod
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1PodSpec
            {
                Containers = new List<V1Container>
                {
                    new V1Container { Name = name, Image = image }
                }
            }
        };
        await _client.CoreV1.CreateNamespacedPodAsync(pod, ns);
    }

    public async Task DeletePodAsync(string ns, string name)
        => await _client.CoreV1.DeleteNamespacedPodAsync(name, ns);

    // ── DEPLOYMENTS ────────────────────────────────────────
    public async Task<object> GetDeploymentsAsync(string ns = "default")
    {
        var deps = await _client.AppsV1.ListNamespacedDeploymentAsync(ns);
        return deps.Items.Select(d => new
        {
            name = d.Metadata.Name,
            @namespace = d.Metadata.NamespaceProperty,
            replicas = d.Spec.Replicas,
            available = d.Status.AvailableReplicas,
            image = d.Spec.Template.Spec.Containers.FirstOrDefault()?.Image,
            created = d.Metadata.CreationTimestamp
        });
    }

    public async Task CreateDeploymentAsync(string ns, string name, string image, int replicas = 1)
    {
        var dep = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1DeploymentSpec
            {
                Replicas = replicas,
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string> { { "app", name } }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string, string> { { "app", name } }
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container { Name = name, Image = image }
                        }
                    }
                }
            }
        };
        await _client.AppsV1.CreateNamespacedDeploymentAsync(dep, ns);
    }

    public async Task DeleteDeploymentAsync(string ns, string name)
        => await _client.AppsV1.DeleteNamespacedDeploymentAsync(name, ns);

    // ── SERVICES ───────────────────────────────────────────
    public async Task<object> GetServicesAsync(string ns = "default")
    {
        var svcs = await _client.CoreV1.ListNamespacedServiceAsync(ns);
        return svcs.Items.Select(s => new
        {
            name = s.Metadata.Name,
            @namespace = s.Metadata.NamespaceProperty,
            type = s.Spec.Type,
            clusterIP = s.Spec.ClusterIP,
            ports = s.Spec.Ports?.Select(p => $"{p.Port}:{p.TargetPort}"),
            created = s.Metadata.CreationTimestamp
        });
    }

    public async Task CreateServiceAsync(string ns, string name, string appLabel, int port, int targetPort)
    {
        var svc = new V1Service
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1ServiceSpec
            {
                Selector = new Dictionary<string, string> { { "app", appLabel } },
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort { Port = port, TargetPort = targetPort }
                }
            }
        };
        await _client.CoreV1.CreateNamespacedServiceAsync(svc, ns);
    }

    public async Task DeleteServiceAsync(string ns, string name)
        => await _client.CoreV1.DeleteNamespacedServiceAsync(name, ns);

    // ── INGRESSES ──────────────────────────────────────────
    public async Task<object> GetIngressesAsync(string ns = "default")
    {
        var ingresses = await _client.NetworkingV1.ListNamespacedIngressAsync(ns);
        return ingresses.Items.Select(i => new
        {
            name = i.Metadata.Name,
            @namespace = i.Metadata.NamespaceProperty,
            hosts = i.Spec.Rules?.Select(r => r.Host),
            address = i.Status.LoadBalancer?.Ingress?.FirstOrDefault()?.Ip ?? i.Status.LoadBalancer?.Ingress?.FirstOrDefault()?.Hostname,
            created = i.Metadata.CreationTimestamp
        });
    }

    public async Task CreateIngressAsync(string ns, string name, string host, string serviceName, int port)
    {
        var ingress = new V1Ingress
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1IngressSpec
            {
                Rules = new List<V1IngressRule>
                {
                    new V1IngressRule
                    {
                        Host = host,
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths = new List<V1HTTPIngressPath>
                            {
                                new V1HTTPIngressPath
                                {
                                    Path = "/",
                                    PathType = "Prefix",
                                    Backend = new V1IngressBackend
                                    {
                                        Service = new V1IngressServiceBackend
                                        {
                                            Name = serviceName,
                                            Port = new V1ServiceBackendPort { Number = port }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        await _client.NetworkingV1.CreateNamespacedIngressAsync(ingress, ns);
    }

    public async Task DeleteIngressAsync(string ns, string name)
        => await _client.NetworkingV1.DeleteNamespacedIngressAsync(name, ns);

    // ── DASHBOARD / CLUSTER INFO ───────────────────────────
    public async Task<object> GetClusterInfoAsync()
    {
        var nodes = await _client.CoreV1.ListNodeAsync();
        var pods = await _client.CoreV1.ListPodForAllNamespacesAsync();
        var namespaces = await _client.CoreV1.ListNamespaceAsync();
        var deployments = await _client.AppsV1.ListDeploymentForAllNamespacesAsync();

        // Cálculo de Recursos (CPU e Memória)
        long totalCpuMillicores = 0;
        long allocatableCpuMillicores = 0;
        long totalMemoryBytes = 0;
        long allocatableMemoryBytes = 0;

        foreach (var node in nodes.Items)
        {
            if (node.Status.Capacity != null)
            {
                if (node.Status.Capacity.TryGetValue("cpu", out var cpuCap)) 
                    totalCpuMillicores += ParseCpu(cpuCap.ToString());
                if (node.Status.Capacity.TryGetValue("memory", out var memCap)) 
                    totalMemoryBytes += ParseMemory(memCap.ToString());
            }

            if (node.Status.Allocatable != null)
            {
                if (node.Status.Allocatable.TryGetValue("cpu", out var cpuAlloc)) 
                    allocatableCpuMillicores += ParseCpu(cpuAlloc.ToString());
                if (node.Status.Allocatable.TryGetValue("memory", out var memAlloc)) 
                    allocatableMemoryBytes += ParseMemory(memAlloc.ToString());
            }
        }

        // Estimativa simples de uso: Capacity - Allocatable (na verdade Allocatable é o que sobra, 
        // mas em sistemas como Minikube, Capacity costuma ser o total do host e Allocatable o que o K8s pode usar)
        // Para uma dashboard mais real, calculamos a % de alocação.
        double cpuUsagePercent = totalCpuMillicores > 0 ? 100 - ((double)allocatableCpuMillicores / totalCpuMillicores * 100) : 0;
        double memUsagePercent = totalMemoryBytes > 0 ? 100 - ((double)allocatableMemoryBytes / totalMemoryBytes * 100) : 0;

        return new
        {
            totalNodes = nodes.Items.Count,
            readyNodes = nodes.Items.Count(n =>
                n.Status.Conditions?.Any(c => c.Type == "Ready" && c.Status == "True") == true),
            totalPods = pods.Items.Count,
            runningPods = pods.Items.Count(p => p.Status.Phase == "Running"),
            totalNamespaces = namespaces.Items.Count,
            totalDeployments = deployments.Items.Count,
            cpuUsage = Math.Round(cpuUsagePercent, 1),
            memUsage = Math.Round(memUsagePercent, 1),
            totalCpu = $"{totalCpuMillicores / 1000.0} Cores",
            totalMem = $"{Math.Round(totalMemoryBytes / 1024.0 / 1024.0 / 1024.0, 1)} GB"
        };
    }

    private long ParseCpu(string cpu)
    {
        if (cpu.EndsWith("m")) return long.Parse(cpu.TrimEnd('m'));
        return (long)(double.Parse(cpu) * 1000);
    }

    private long ParseMemory(string mem)
    {
        if (mem.EndsWith("Ki")) return long.Parse(mem.Replace("Ki", "")) * 1024;
        if (mem.EndsWith("Mi")) return long.Parse(mem.Replace("Mi", "")) * 1024 * 1024;
        if (mem.EndsWith("Gi")) return long.Parse(mem.Replace("Gi", "")) * 1024 * 1024 * 1024;
        return long.Parse(mem);
    }
}