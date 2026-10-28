using System.Net.Sockets;
using k8s;
using k8s.Models;
using KubePodCreator.Controllers;

namespace KubePodCreator.Services;

public class PodCreatorService : IDisposable
{
    private readonly Kubernetes client = new(KubernetesClientConfiguration.InClusterConfig());

    public async Task ListPodsAsync()
    {
        var pods = await client.CoreV1.ListNamespacedPodAsync("default");
        foreach (var pod in pods.Items)
            Console.WriteLine($"Pod: {pod.Metadata.Name}");
    }

    public async Task<IpPortDto> CreatePodAndService()
    {
        string name = $"game-server-{Guid.NewGuid():N}";
        await client.CoreV1.CreateNamespacedPodAsync(DefinePod(name), "default");
        var service = await client.CoreV1.CreateNamespacedServiceAsync(DefineService(name), "default");

        // Let Kubernetes choose a port, then expose it through Docker Desktop's LoadBalancer.
        int port = service.Spec.Ports[0].NodePort
            ?? throw new InvalidOperationException("Kubernetes did not assign a NodePort.");
        service.Spec.Type = "LoadBalancer";
        service.Spec.Ports[0].Port = port;
        await client.CoreV1.ReplaceNamespacedServiceAsync(service, name, "default");

        await WaitForServerAsync(name, port);
        Console.WriteLine($"Created {name}: localhost:{port} -> game-server:7777");
        service.Spec.
        return new IpPortDto(name, "127.0.0.1", port);
    }

    private static V1Pod DefinePod(string name) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = name,
            Labels = new Dictionary<string, string> { ["app"] = name, ["demo"] = "pod-creator" }
        },
        Spec = new V1PodSpec
        {
            Containers = [new V1Container
            {
                Name = "game-server",
                Image = "game-server:1.0",
                ImagePullPolicy = "Never"
            }]
        }
    };

    private static V1Service DefineService(string name) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = name,
            Labels = new Dictionary<string, string> { ["demo"] = "pod-creator" }
        },
        Spec = new V1ServiceSpec
        {
            Selector = new Dictionary<string, string> { ["app"] = name },
            Type = "NodePort",
            Ports = [new V1ServicePort { Port = 7777, TargetPort = 7777 }]
        }
    };

    // A LoadBalancer status alone does not guarantee that its traffic route is ready.
    private async Task WaitForServerAsync(string name, int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (true)
        {
            var service = await client.CoreV1.ReadNamespacedServiceAsync(name, "default", cancellationToken: timeout.Token);
            string? host = service.Status?.LoadBalancer?.Ingress?.FirstOrDefault()?.Ip;
            if (host != null && await ServerRespondsAsync(host, port)) return;
            await Task.Delay(500, timeout.Token);
        }
    }

    private static async Task<bool> ServerRespondsAsync(string host, int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var socket = new TcpClient();
        try
        {
            await socket.ConnectAsync(host, port, timeout.Token);
            using var stream = socket.GetStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            using var reader = new StreamReader(stream);
            await writer.WriteLineAsync("ready?");
            return await reader.ReadLineAsync(timeout.Token) == "ready?";
        }
        catch (Exception error) when (error is SocketException or IOException or OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose() => client.Dispose();
}
