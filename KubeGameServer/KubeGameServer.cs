using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Any, 7777);
listener.Start();
Console.WriteLine("Game server is listening on TCP port 7777.");
while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleClient(client);
}

static async Task HandleClient(TcpClient client)
{
    using (client)
    using (var stream = client.GetStream())
    using (var reader = new StreamReader(stream))
    using (var writer = new StreamWriter(stream) { AutoFlush = true })
    {
        try
        {
            while (await reader.ReadLineAsync() is { } message)
            {
                Console.WriteLine($"Received: {message}");
                await writer.WriteLineAsync(message);
            }
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Connection closed: {exception.Message}");
        }
    }
}
