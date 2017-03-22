using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;

namespace TcpSocketServer
{
    /// <summary>
    /// Listens on broadcast address for client pings and sends a response message. This
    /// allows clients to automatically locate the server on the network.
    /// </summary>
    public class NetworkBeacon
    {
        private IDisposable _listeningSubscription;
        private UdpClient _client;
        private const int _locationPort = 9789;
        private const int _responsePort = 9790;
        private readonly IPEndPoint _endPoint = new IPEndPoint(IPAddress.Any, _locationPort);
        private const string _locationRequestMessage = "HOST_LOC_REQ";

        public void Start()
        {
            _client = new UdpClient(_endPoint);
            _client.EnableBroadcast = true;

            _listeningSubscription =
                Observable.FromAsync(_client.ReceiveAsync)
                .Retry()
                .Repeat()
                .Subscribe(RespondToRequest);
        }

        public void Stop()
        {
            _client.Dispose();
            _listeningSubscription.Dispose();
        }

        private async void RespondToRequest(UdpReceiveResult incoming)
        {
            var message = Encoding.UTF8.GetString(incoming.Buffer);

            if (message == _locationRequestMessage)
            {
                await SendResponse(incoming.RemoteEndPoint.Address);
            }
        }

        private async Task SendResponse(IPAddress dest)
        {
            var endpoint = new IPEndPoint(dest, _responsePort);
            var client = new UdpClient();

            var message = "UDP_HOST_VERIFY";
            var bytes = Encoding.UTF8.GetBytes(message);

            await client.SendAsync(bytes, bytes.Length, endpoint);
        }
    }
}
