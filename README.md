# Tcp Socket Server for .Net Core

This is a simple tcp socket server for .net core.

## Usage

The following examples demonstrate the usage of the library. For further examples see the example app.

### Server

```csharp
var server = new SocketServer();
server.AddTcpListener(8181);

server.Messages.Subscribe(m =>
{
   var message = Encoding.UTF8.GetString(m);
   Console.WriteLine("Received message {0}", message);
});

server.Start();
```
### Client

```csharp
var client = new TcpSender();
client.Connect("host", 8181);
client.Send("Ping");
```


