using k8s;
using k8s.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using KubeManager.API.Models;

namespace KubeManager.API.Services;

public class KubernetesService
{
    private IKubernetes? _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;
    private readonly string _clustersPath;

    // Cache de clientes para evitar reconstrução e vazamentos
    private static readonly ConcurrentDictionary<string, IKubernetes> _clientCache = new ConcurrentDictionary<string, IKubernetes>();

    // Cache simples para o dashboard (5 segundos)
    private static readonly ConcurrentDictionary<string, (DateTime Expiry, object Data)> _dashboardCache = new();

    public KubernetesService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _httpContextAccessor = httpContextAccessor;
        _clustersPath = Path.Combine(AppContext.BaseDirectory, "clusters");
        if (!Directory.Exists(_clustersPath)) Directory.CreateDirectory(_clustersPath);

        InitializeClient();
    }

    private void InitializeClient()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var clusterName = context.Request.Headers["X-K8s-Cluster"].ToString();
        if (string.IsNullOrEmpty(clusterName)) return;

        // Tentar obter do cache
        if (_clientCache.TryGetValue(clusterName, out var cachedClient))
        {
            _client = cachedClient;
            return;
        }

        try
        {
            var filePath = Path.Combine(_clustersPath, $"{clusterName}.yaml");
            if (File.Exists(filePath))
            {
                var kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(filePath);

                // Configuração correta de SSL sem vazar callbacks globais
                var client = new Kubernetes(kubeConfig);
                _clientCache.TryAdd(clusterName, client);
                _client = client;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar cluster {clusterName}: {ex.Message}");
        }
    }

    // ── NODES ──────────────────────────────────────────────
    public async Task<object> GetNodesAsync()
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var nodes = await _client.CoreV1.ListNodeAsync();
            return nodes.Items.Select(n => new {
                name = n.Metadata.Name,
                status = n.Status.Conditions?.LastOrDefault(c => c.Type == "Ready")?.Status,
                roles = n.Metadata.Labels?.Where(l => l.Key.StartsWith("node-role.kubernetes.io/")).Select(l => l.Key.Replace("node-role.kubernetes.io/", "")) ?? Array.Empty<string>(),
                cpu = n.Status.Capacity?.ContainsKey("cpu") == true ? n.Status.Capacity["cpu"].ToString() : "N/A",
                memory = n.Status.Capacity?.ContainsKey("memory") == true ? n.Status.Capacity["memory"].ToString() : "N/A",
                k8sVersion = n.Status.NodeInfo?.KubeletVersion ?? "Unknown"
            });
        }
        catch { return Array.Empty<object>(); }
    }

    // ── NAMESPACES ─────────────────────────────────────────
    public async Task<object> GetNamespacesAsync()
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var ns = await _client.CoreV1.ListNamespaceAsync();
            return ns.Items.Select(n => new { name = n.Metadata.Name, status = n.Status.Phase, created = n.Metadata.CreationTimestamp });
        }
        catch { return Array.Empty<object>(); }
    }

    public async Task CreateNamespaceAsync(string name)
    {
        if (_client == null) return;
        try
        {
            var ns = new V1Namespace { Metadata = new V1ObjectMeta { Name = name } };
            await _client.CoreV1.CreateNamespaceAsync(ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar namespace: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteNamespaceAsync(string name)
    {
        if (_client == null) return;
        try { await _client.CoreV1.DeleteNamespaceAsync(name); } catch { }
    }

    // ── PODS ───────────────────────────────────────────────
    public async Task<object> GetPodsAsync(string? ns = null)
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var pods = string.IsNullOrEmpty(ns) || ns == "all"
                ? await _client.CoreV1.ListPodForAllNamespacesAsync()
                : await _client.CoreV1.ListNamespacedPodAsync(ns);
            return pods.Items.Select(p => new { name = p.Metadata.Name, @namespace = p.Metadata.NamespaceProperty, status = p.Status.Phase, ip = p.Status.PodIP, node = p.Spec.NodeName, containers = p.Spec.Containers.Select(c => c.Name), created = p.Metadata.CreationTimestamp });
        }
        catch { return Array.Empty<object>(); }
    }

    public async Task CreatePodAsync(string ns, string name, string image, int? containerPort = null)
    {
        if (_client == null) return;
        try
        {
            var pod = new V1Pod
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    NamespaceProperty = ns,
                    Labels = new Dictionary<string, string> { { "app", name } }
                },
                Spec = new V1PodSpec
                {
                    Containers = new List<V1Container> {
                        new V1Container {
                            Name = name,
                            Image = image,
                            Ports = containerPort.HasValue ? new List<V1ContainerPort> { new V1ContainerPort { ContainerPort = containerPort.Value } } : null
                        }
                    }
                }
            };
            await _client.CoreV1.CreateNamespacedPodAsync(pod, ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar pod: {ex.Message}");
            throw; // Re-throw to allow controller to return error
        }
    }

    public async Task DeletePodAsync(string ns, string name)
    {
        if (_client == null) return;
        try { await _client.CoreV1.DeleteNamespacedPodAsync(name, ns); } catch { }
    }

    // ── DEPLOYMENTS ────────────────────────────────────────
    public async Task<object> GetDeploymentsAsync(string? ns = null)
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var deps = string.IsNullOrEmpty(ns) || ns == "all"
                ? await _client.AppsV1.ListDeploymentForAllNamespacesAsync()
                : await _client.AppsV1.ListNamespacedDeploymentAsync(ns);
            return deps.Items.Select(d => new { name = d.Metadata.Name, @namespace = d.Metadata.NamespaceProperty, replicas = d.Spec.Replicas, available = d.Status.AvailableReplicas, image = d.Spec.Template.Spec.Containers.FirstOrDefault()?.Image, created = d.Metadata.CreationTimestamp });
        }
        catch { return Array.Empty<object>(); }
    }

    public async Task CreateDeploymentAsync(string ns, string name, string image, int replicas = 1, int? containerPort = null, Dictionary<string, string>? envVars = null, string? cpuLimit = null, string? memLimit = null)
    {
        if (_client == null) return;
        try
        {
            var container = new V1Container
            {
                Name = name,
                Image = image,
                Env = envVars?.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList(),
                Resources = new V1ResourceRequirements { Limits = new Dictionary<string, ResourceQuantity>() },
                Ports = containerPort.HasValue ? new List<V1ContainerPort> { new V1ContainerPort { ContainerPort = containerPort.Value } } : null
            };
            if (!string.IsNullOrEmpty(cpuLimit)) container.Resources.Limits["cpu"] = new ResourceQuantity(cpuLimit);
            if (!string.IsNullOrEmpty(memLimit)) container.Resources.Limits["memory"] = new ResourceQuantity(memLimit);
            var dep = new V1Deployment { Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns }, Spec = new V1DeploymentSpec { Replicas = replicas, Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { { "app", name } } }, Template = new V1PodTemplateSpec { Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { { "app", name } } }, Spec = new V1PodSpec { Containers = new List<V1Container> { container } } } } };
            await _client.AppsV1.CreateNamespacedDeploymentAsync(dep, ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na operação: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteDeploymentAsync(string ns, string name)
    {
        if (_client == null) return;
        try { await _client.AppsV1.DeleteNamespacedDeploymentAsync(name, ns); } catch { }
    }

    public async Task ScaleDeploymentAsync(string ns, string name, int replicas)
    {
        if (_client == null) return;
        try { var patch = new V1Patch($"{{\"spec\": {{\"replicas\": {replicas}}}}}", V1Patch.PatchType.MergePatch); await _client.AppsV1.PatchNamespacedDeploymentScaleAsync(patch, name, ns); } catch { }
    }

    // ── SERVICES ───────────────────────────────────────────
    public async Task<object> GetServicesAsync(string? ns = null)
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var svcsTask = string.IsNullOrEmpty(ns) || ns == "all"
                ? _client.CoreV1.ListServiceForAllNamespacesAsync()
                : _client.CoreV1.ListNamespacedServiceAsync(ns);

            var epsTask = string.IsNullOrEmpty(ns) || ns == "all"
                ? _client.CoreV1.ListEndpointsForAllNamespacesAsync()
                : _client.CoreV1.ListNamespacedEndpointsAsync(ns);

            await Task.WhenAll(svcsTask, epsTask);
            var svcs = await svcsTask;
            var allEps = await epsTask;

            return svcs.Items.Select(s => {
                var eps = allEps.Items.FirstOrDefault(e => e.Metadata.Name == s.Metadata.Name && e.Metadata.NamespaceProperty == s.Metadata.NamespaceProperty);
                int activeEndpoints = eps?.Subsets?.Sum(sub => sub.Addresses?.Count ?? 0) ?? 0;

                return new
                {
                    name = s.Metadata.Name,
                    @namespace = s.Metadata.NamespaceProperty,
                    type = s.Spec.Type,
                    clusterIP = s.Spec.ClusterIP,
                    ports = s.Spec.Ports?.Select(p => $"{p.Port}:{p.TargetPort.Value ?? p.TargetPort.ToString()}/{p.Protocol} (NP:{p.NodePort})"),
                    endpoints = activeEndpoints,
                    created = s.Metadata.CreationTimestamp
                };
            });
        }
        catch { return Array.Empty<object>(); }
    }

    public async Task CreateServiceAsync(string ns, string name, string appLabel, List<ServicePortDto> ports, string type = "ClusterIP")
    {
        if (_client == null) return;
        try
        {
            var svcPorts = ports.Select(p => new V1ServicePort
            {
                Port = p.Port,
                TargetPort = p.TargetPort,
                NodePort = p.NodePort,
                Protocol = p.Protocol,
                Name = p.Name
            }).ToList();

            var svc = new V1Service
            {
                Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
                Spec = new V1ServiceSpec
                {
                    Type = type,
                    Selector = new Dictionary<string, string> { { "app", appLabel } },
                    Ports = svcPorts
                }
            };
            await _client.CoreV1.CreateNamespacedServiceAsync(svc, ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na operação: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteServiceAsync(string ns, string name)
    {
        if (_client == null) return;
        try { await _client.CoreV1.DeleteNamespacedServiceAsync(name, ns); } catch { }
    }

    // ── INGRESSES ──────────────────────────────────────────
    public async Task<object> GetIngressesAsync(string? ns = null)
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var ingresses = string.IsNullOrEmpty(ns) || ns == "all"
                ? await _client.NetworkingV1.ListIngressForAllNamespacesAsync()
                : await _client.NetworkingV1.ListNamespacedIngressAsync(ns);
            return ingresses.Items.Select(i => new { name = i.Metadata.Name, @namespace = i.Metadata.NamespaceProperty, hosts = i.Spec.Rules?.Select(r => r.Host), address = i.Status.LoadBalancer?.Ingress?.FirstOrDefault()?.Ip ?? i.Status.LoadBalancer?.Ingress?.FirstOrDefault()?.Hostname, created = i.Metadata.CreationTimestamp });
        }
        catch { return Array.Empty<object>(); }
    }

    public async Task CreateIngressAsync(string ns, string name, string host, string serviceName, int port, string path = "/", string pathType = "Prefix", string? tlsSecret = null)
    {
        if (_client == null) return;
        try
        {
            var ingress = new V1Ingress
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    NamespaceProperty = ns,
                    Annotations = new Dictionary<string, string> { { "kubernetes.io/ingress.class", "nginx" } }
                },
                Spec = new V1IngressSpec
                {
                    IngressClassName = "nginx",
                    Rules = new List<V1IngressRule> {
                        new V1IngressRule {
                            Host = host,
                            Http = new V1HTTPIngressRuleValue {
                                Paths = new List<V1HTTPIngressPath> {
                                    new V1HTTPIngressPath {
                                        Path = path,
                                        PathType = pathType,
                                        Backend = new V1IngressBackend {
                                            Service = new V1IngressServiceBackend {
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
            if (!string.IsNullOrEmpty(tlsSecret))
            {
                ingress.Spec.Tls = new List<V1IngressTLS> {
                    new V1IngressTLS { Hosts = new List<string> { host }, SecretName = tlsSecret }
                };
            }
            await _client.NetworkingV1.CreateNamespacedIngressAsync(ingress, ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na operação: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteIngressAsync(string ns, string name)
    {
        if (_client == null) return;
        try { await _client.NetworkingV1.DeleteNamespacedIngressAsync(name, ns); } catch { }
    }

    // ── EVENTS ─────────────────────────────────────────────
    public async Task<object> GetEventsAsync(string? ns = null)
    {
        if (_client == null) return Array.Empty<object>();
        try
        {
            var events = string.IsNullOrEmpty(ns) || ns == "all"
                ? await _client.CoreV1.ListEventForAllNamespacesAsync()
                : await _client.CoreV1.ListNamespacedEventAsync(ns);
            return events.Items.OrderByDescending(e => e.LastTimestamp ?? e.EventTime ?? DateTime.MinValue).Select(e => new { name = e.Metadata.Name, type = e.Type, @namespace = e.Metadata.NamespaceProperty, reason = e.Reason, message = e.Message, source = e.Source?.Component, count = e.Count, lastSeen = e.LastTimestamp ?? e.FirstTimestamp });
        }
        catch { return Array.Empty<object>(); }
    }

    // ── LOGS ──────────────────────────────────────────────
    public async Task<string> GetPodLogsAsync(string ns, string name)
    {
        if (_client == null) return "Offline";
        try { var stream = await _client.CoreV1.ReadNamespacedPodLogAsync(name, ns); using var reader = new StreamReader(stream); return await reader.ReadToEndAsync(); } catch { return "Erro logs"; }
    }

    // ── YAML EDITOR ───────────────────────────────────────
    public async Task<string> GetYamlAsync(string resource, string ns, string name)
    {
        if (_client == null) return "{}";
        try
        {
            object obj = resource.ToLower() switch { "pods" => await _client.CoreV1.ReadNamespacedPodAsync(name, ns), "deployments" => await _client.AppsV1.ReadNamespacedDeploymentAsync(name, ns), "services" => await _client.CoreV1.ReadNamespacedServiceAsync(name, ns), "namespaces" => await _client.CoreV1.ReadNamespaceAsync(name), "ingresses" => await _client.NetworkingV1.ReadNamespacedIngressAsync(name, ns), _ => throw new Exception("NA") };
            return new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance).Build().Serialize(obj);
        }
        catch { return "Erro YAML"; }
    }

    public async Task UpdateYamlAsync(string resource, string ns, string name, string yaml)
    {
        if (_client == null) return;
        try
        {
            var type = resource.ToLower() switch { "pods" => typeof(V1Pod), "deployments" => typeof(V1Deployment), "services" => typeof(V1Service), "namespaces" => typeof(V1Namespace), "ingresses" => typeof(V1Ingress), _ => throw new Exception("NA") };
            var obj = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build().Deserialize(yaml, type);
            if (obj == null) return;

            if (resource == "pods") await _client.CoreV1.ReplaceNamespacedPodAsync((V1Pod)obj, name, ns);
            else if (resource == "deployments") await _client.AppsV1.ReplaceNamespacedDeploymentAsync((V1Deployment)obj, name, ns);
            else if (resource == "services") await _client.CoreV1.ReplaceNamespacedServiceAsync((V1Service)obj, name, ns);
            else if (resource == "namespaces") await _client.CoreV1.ReplaceNamespaceAsync((V1Namespace)obj, name);
            else if (resource == "ingresses") await _client.NetworkingV1.ReplaceNamespacedIngressAsync((V1Ingress)obj, name, ns);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na operação: {ex.Message}");
            throw;
        }
    }

    // ── DASHBOARD ──────────────────────────────────────────
    public async Task<object> GetClusterInfoAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        var clusterName = context?.Request.Headers["X-K8s-Cluster"].ToString() ?? "default";

        // Verificar cache (5 segundos)
        if (_dashboardCache.TryGetValue(clusterName, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            return cached.Data;
        }

        if (_client == null) return new { totalNodes = 0, readyNodes = 0, totalPods = 0, runningPods = 0, totalNamespaces = 0, totalDeployments = 0, cpuUsage = 0, memUsage = 0, totalCpu = "N/A", totalMem = "N/A", hasRealMetrics = false };

        try
        {
            // Chamadas paralelas para fluidez máxima
            var nodesTask = _client.CoreV1.ListNodeAsync();
            var podsTask = _client.CoreV1.ListPodForAllNamespacesAsync();
            var nsTask = _client.CoreV1.ListNamespaceAsync();
            var depsTask = _client.AppsV1.ListDeploymentForAllNamespacesAsync();

            await Task.WhenAll(nodesTask, podsTask, nsTask, depsTask);

            var nodes = await nodesTask;
            var pods = await podsTask;
            var namespaces = await nsTask;
            var deployments = await depsTask;

            long totalCpu = 0; long totalMem = 0;
            foreach (var node in nodes.Items)
            {
                if (node.Status.Capacity != null)
                {
                    if (node.Status.Capacity.TryGetValue("cpu", out var c)) totalCpu += ParseCpu(c.ToString());
                    if (node.Status.Capacity.TryGetValue("memory", out var m)) totalMem += ParseMemory(m.ToString());
                }
            }

            int cpuUsagePct = 0;
            int memUsagePct = 0;
            bool hasRealMetrics = false;

            try
            {
                var metrics = await _client.CustomObjects.GetClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes", "");
                string? metricsJson = metrics?.ToString();
                if (!string.IsNullOrEmpty(metricsJson))
                {
                    using var doc = JsonDocument.Parse(metricsJson);
                    if (doc.RootElement.TryGetProperty("items", out var items))
                    {
                        long usedCpu = 0; long usedMem = 0;
                        foreach (var item in items.EnumerateArray())
                        {
                            if (item.TryGetProperty("usage", out var usage))
                            {
                                usedCpu += ParseCpu(usage.GetProperty("cpu").GetString() ?? "0");
                                usedMem += ParseMemory(usage.GetProperty("memory").GetString() ?? "0");
                            }
                        }
                        if (totalCpu > 0) cpuUsagePct = (int)((usedCpu * 100) / totalCpu);
                        if (totalMem > 0) memUsagePct = (int)((usedMem * 100) / totalMem);
                        hasRealMetrics = true;
                    }
                }
            }
            catch { }

            var result = new
            {
                totalNodes = nodes.Items.Count,
                readyNodes = nodes.Items.Count(n => n.Status.Conditions?.Any(c => c.Type == "Ready" && c.Status == "True") == true),
                runningPods = pods.Items.Count(p => p.Status.Phase == "Running"),
                totalPods = pods.Items.Count,
                totalNamespaces = namespaces.Items.Count,
                totalDeployments = deployments.Items.Count,
                cpuUsage = cpuUsagePct,
                memUsage = memUsagePct,
                totalCpu = $"{totalCpu / 1000.0} Cores",
                totalMem = $"{Math.Round(totalMem / 1024.0 / 1024.0 / 1024.0, 1)} GB",
                hasRealMetrics = hasRealMetrics
            };

            _dashboardCache[clusterName] = (DateTime.UtcNow.AddSeconds(5), result);
            return result;
        }
        catch
        {
            return new { totalNodes = 0, readyNodes = 0, totalPods = 0, runningPods = 0, totalNamespaces = 0, totalDeployments = 0, cpuUsage = 0, memUsage = 0, totalCpu = "Erro", totalMem = "Erro", hasRealMetrics = false };
        }
    }

    private long ParseCpu(string cpu)
    {
        if (string.IsNullOrEmpty(cpu)) return 0;
        if (cpu.EndsWith("m")) return long.Parse(cpu.TrimEnd('m'));
        if (cpu.EndsWith("n")) return long.Parse(cpu.TrimEnd('n')) / 1000000;
        if (cpu.EndsWith("u")) return long.Parse(cpu.TrimEnd('u')) / 1000;
        if (double.TryParse(cpu, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val)) return (long)(val * 1000);
        return 0;
    }

    private long ParseMemory(string mem)
    {
        if (string.IsNullOrEmpty(mem)) return 0;
        string numericPart = new string(mem.TakeWhile(char.IsDigit).ToArray());
        if (!long.TryParse(numericPart, out long val)) return 0;

        if (mem.EndsWith("Ki")) return val * 1024;
        if (mem.EndsWith("Mi")) return val * 1024 * 1024;
        if (mem.EndsWith("Gi")) return val * 1024 * 1024 * 1024;
        if (mem.EndsWith("Ti")) return val * 1024L * 1024 * 1024 * 1024;
        return val;
    }
}