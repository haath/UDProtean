# UDProtean

[![Build Status](https://devops.gmantaos.com/buildStatus/icon?job=UDProtean)](https://devops.gmantaos.com/job/UDProtean/)



## Usage

### Getting Started

To get started quickly, you can register an event handler for incoming messages.

#### Server

```csharp
UDPServer server = new UDPServer(5000);

server.OnData += (endPoint, data) =>
{

};

server.Start();
```

#### Client

```csharp
UDPClient client = new UDPClient("127.0.0.1", 5000);

// Performs the handshake to establish the connection
client.connect();

client.Send("hello world");
```

### Server client behaviors

However, the point of this library is to treat each client as maintained end-to-end connection. For this purpose you want to define a class which implements `UDPClientBehavior`.

```csharp
class EchoClient
{
	protected override void OnOpen()
	{
		Console.WriteLine("Client connected from: " + EndPoint.Address);
	}

	protected override void OnClose()
	{
		Console.WriteLine("Client disconnected");
	}

	protected override void OnData(byte[] data)
	{
		Console.WriteLine("Received {0} bytes of data", data.Length);		

		// Echo it back
		Send(data);
	}

	protected override void OnError(Exception ex)
	{
	}
}
```

And then simply start it like before.

```csharp
UDPServer<EchoClient> server = new UDPServer<EchoClient>(5000);
server.Start();
```