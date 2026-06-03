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

    public async Task CreateDeploymentAsync(string ns, string name, string image, int replicas = 1, Dictionary<string, string>? envVars = null, string? cpuLimit = null, string? memLimit = null)
    {
        var container = new V1Container 
        { 
            Name = name, 
            Image = image,
            Env = envVars?.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList(),
            Resources = new V1ResourceRequirements
            {
                Limits = new Dictionary<string, ResourceQuantity>()
            }
        };

        if (!string.IsNullOrEmpty(cpuLimit)) container.Resources.Limits["cpu"] = new ResourceQuantity(cpuLimit);
        if (!string.IsNullOrEmpty(memLimit)) container.Resources.Limits["memory"] = new ResourceQuantity(memLimit);

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
                        Containers = new List<V1Container> { container }
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

    public async Task CreateServiceAsync(string ns, string name, string appLabel, int port, int targetPort, string type = "ClusterIP", int? nodePort = null)
    {
        var svc = new V1Service
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1ServiceSpec
            {
                Type = type,
                Selector = new Dictionary<string, string> { { "app", appLabel } },
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort { Port = port, TargetPort = targetPort, NodePort = nodePort }
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

    // ── EVENTS ─────────────────────────────────────────────
    public async Task<object> GetEventsAsync(string ns = "default")
    {
        var events = await _client.CoreV1.ListNamespacedEventAsync(ns);
        return events.Items
            .OrderByDescending(e => e.LastTimestamp ?? e.EventTime ?? DateTime.MinValue)
            .Select(e => new
            {
                name = e.Metadata.Name,
                type = e.Type,
                reason = e.Reason,
                message = e.Message,
                source = e.Source?.Component,
                count = e.Count,
                lastSeen = e.LastTimestamp ?? e.FirstTimestamp
            });
    }

    // ── METRICS ───────────────────────────────────────────
    public async Task<object?> GetRealUsageAsync()
    {
        try 
        {
            var metrics = await _client.CustomObjects.GetClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes", "");
            var items = ((System.Text.Json.JsonElement)metrics).GetProperty("items");
            
            long usedCpu = 0;
            long usedMem = 0;

            foreach (var item in items.EnumerateArray())
            {
                var usage = item.GetProperty("usage");
                usedCpu += ParseCpu(usage.GetProperty("cpu").GetString()!);
                usedMem += ParseMemory(usage.GetProperty("memory").GetString()!);
            }

            return new { usedCpu, usedMem };
        }
        catch { return null; }
    }

    // ── SCALING ───────────────────────────────────────────
    public async Task ScaleDeploymentAsync(string ns, string name, int replicas)
    {
        var patch = new V1Patch($"{{\"spec\": {{\"replicas\": {replicas}}}}}", V1Patch.PatchType.MergePatch);
        await _client.AppsV1.PatchNamespacedDeploymentScaleAsync(patch, name, ns);
    }

    // ── LOGS ──────────────────────────────────────────────
    public async Task<string> GetPodLogsAsync(string ns, string name)
    {
        try
        {
            var stream = await _client.CoreV1.ReadNamespacedPodLogAsync(name, ns);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            return "Erro ao recuperar logs ou o pod ainda não está pronto.";
        }
    }

    // ── YAML EDITOR ───────────────────────────────────────
    public async Task<string> GetYamlAsync(string resource, string ns, string name)
    {
        object? obj = resource.ToLower() switch
        {
            "pods" => await _client.CoreV1.ReadNamespacedPodAsync(name, ns),
            "deployments" => await _client.AppsV1.ReadNamespacedDeploymentAsync(name, ns),
            "services" => await _client.CoreV1.ReadNamespacedServiceAsync(name, ns),
            "namespaces" => await _client.CoreV1.ReadNamespaceAsync(name),
            "ingresses" => await _client.NetworkingV1.ReadNamespacedIngressAsync(name, ns),
            _ => throw new Exception("Recurso não suportado para YAML")
        };

        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Serialize(obj);
    }

    public async Task UpdateYamlAsync(string resource, string ns, string name, string yaml)
    {
        var type = resource.ToLower() switch
        {
            "pods" => typeof(V1Pod),
            "deployments" => typeof(V1Deployment),
            "services" => typeof(V1Service),
            "namespaces" => typeof(V1Namespace),
            "ingresses" => typeof(V1Ingress),
            _ => throw new Exception("Recurso não suportado para YAML")
        };

        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
            
        var obj = deserializer.Deserialize(yaml, type);
        
        if (resource == "pods") await _client.CoreV1.ReplaceNamespacedPodAsync((V1Pod)obj, name, ns);
        else if (resource == "deployments") await _client.AppsV1.ReplaceNamespacedDeploymentAsync((V1Deployment)obj, name, ns);
        else if (resource == "services") await _client.CoreV1.ReplaceNamespacedServiceAsync((V1Service)obj, name, ns);
        else if (resource == "namespaces") await _client.CoreV1.ReplaceNamespaceAsync((V1Namespace)obj, name);
        else if (resource == "ingresses") await _client.NetworkingV1.ReplaceNamespacedIngressAsync((V1Ingress)obj, name, ns);
    }

    // ── DASHBOARD / CLUSTER INFO ───────────────────────────
    public async Task<object> GetClusterInfoAsync()
    {
        var nodes = await _client.CoreV1.ListNodeAsync();
        var pods = await _client.CoreV1.ListPodForAllNamespacesAsync();
        var namespaces = await _client.CoreV1.ListNamespaceAsync();
        var deployments = await _client.AppsV1.ListDeploymentForAllNamespacesAsync();

        long totalCpuMillicores = 0;
        long totalMemoryBytes = 0;

        foreach (var node in nodes.Items)
        {
            if (node.Status.Capacity != null)
            {
                if (node.Status.Capacity.TryGetValue("cpu", out var cpuCap)) 
                    totalCpuMillicores += ParseCpu(cpuCap.ToString());
                if (node.Status.Capacity.TryGetValue("memory", out var memCap)) 
                    totalMemoryBytes += ParseMemory(memCap.ToString());
            }
        }

        // Tentar obter uso real
        var realUsage = await GetRealUsageAsync();
        double cpuUsagePercent = 0;
        double memUsagePercent = 0;

        if (realUsage != null)
        {
            var used = (dynamic)realUsage;
            cpuUsagePercent = totalCpuMillicores > 0 ? (double)used.usedCpu / totalCpuMillicores * 100 : 0;
            memUsagePercent = totalMemoryBytes > 0 ? (double)used.usedMem / totalMemoryBytes * 100 : 0;
        }
        else
        {
            // Fallback: Calcular ALOCAÇÃO (Soma dos requests dos pods)
            long allocatedCpu = 0;
            long allocatedMem = 0;

            foreach (var pod in pods.Items)
            {
                if (pod.Status.Phase == "Running" || pod.Status.Phase == "Pending")
                {
                    foreach (var container in pod.Spec.Containers)
                    {
                        if (container.Resources?.Requests != null)
                        {
                            if (container.Resources.Requests.TryGetValue("cpu", out var cpuReq))
                                allocatedCpu += ParseCpu(cpuReq.ToString());
                            if (container.Resources.Requests.TryGetValue("memory", out var memReq))
                                allocatedMem += ParseMemory(memReq.ToString());
                        }
                    }
                }
            }

            cpuUsagePercent = totalCpuMillicores > 0 ? (double)allocatedCpu / totalCpuMillicores * 100 : 0;
            memUsagePercent = totalMemoryBytes > 0 ? (double)allocatedMem / totalMemoryBytes * 100 : 0;
            
            // Se ainda for 0, mas existirem nós, podemos usar a diferença entre Capacity e Allocatable
            // como uma estimativa do que o sistema está a reservar para si mesmo.
            if (cpuUsagePercent == 0 && totalCpuMillicores > 0)
            {
                long systemReservedCpu = 0;
                foreach(var node in nodes.Items) {
                    if (node.Status.Capacity != null && node.Status.Allocatable != null) {
                        node.Status.Capacity.TryGetValue("cpu", out var cap);
                        node.Status.Allocatable.TryGetValue("cpu", out var alloc);
                        systemReservedCpu += (ParseCpu(cap.ToString()) - ParseCpu(alloc.ToString()));
                    }
                }
                cpuUsagePercent = (double)systemReservedCpu / totalCpuMillicores * 100;
            }
        }

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
            totalMem = $"{Math.Round(totalMemoryBytes / 1024.0 / 1024.0 / 1024.0, 1)} GB",
            hasRealMetrics = realUsage != null
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