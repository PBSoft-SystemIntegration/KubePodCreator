using System.Net.Http.Json;
using System.Net.Sockets;

using var http = new HttpClient();
string apiUrl = args.FirstOrDefault() ?? "http://localhost:8080";
var response = await http.PostAsync($"{apiUrl}/PodCreator/CreateGameServer", null);
response.EnsureSuccessStatusCode();
var server = await response.Content.ReadFromJsonAsync<IpPortDto>()
    ?? throw new InvalidOperationException("The API did not return a server.");

Console.WriteLine($"Created: {server.ServiceName}");
Console.WriteLine($"Connecting to {server.IpAddress}:{server.Port}");

using var client = new TcpClient();
await client.ConnectAsync(server.IpAddress, server.Port);
using var stream = client.GetStream();
using var reader = new StreamReader(stream);
using var writer = new StreamWriter(stream) { AutoFlush = true };
Console.WriteLine("Enter a message. The server echoes it back. An empty line exits.");
while (Console.ReadLine() is { Length: > 0 } message)
{
    await writer.WriteLineAsync(message);
    Console.WriteLine($"Server: {await reader.ReadLineAsync()}");
}
Console.WriteLine($"Cleanup: kubectl delete pod,service {server.ServiceName}");
