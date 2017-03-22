using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpSocketSender
{
    public class TcpSender
    {
        private readonly TcpClient _client;

        public async Task Connect(string host, int port)
        {
            await _client.ConnectAsync(host, port);
        }

        public async Task Send(string message)
        {
            await Send(Encoding.UTF8.GetBytes(message));
        }

        public async Task Send(byte[] message)
        {
            var outStream = _client.GetStream();

            Int64 length = message.Length;

            var header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));
            try
            {
                await outStream.WriteAsync(header, 0, header.Length);
                await outStream.WriteAsync(message, 0, message.Length);
            }
            catch (IOException e)
            {
                Console.WriteLine("Unable to write message {0}", e);
            }
        }
    }
}
