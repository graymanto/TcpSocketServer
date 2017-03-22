using System;
using System.Text;
using System.Threading;

namespace TcpSocketServer.Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var server = new SocketServer();
            server.AddTcpListener(8181);

            server.Messages.Subscribe(m =>
            {
                var message = Encoding.UTF8.GetString(m);
                Console.WriteLine("Received message {0}", message);
            });

            server.Start();
            Console.WriteLine("Server is running on port 8181");

            server.BroadcastMessage(Encoding.UTF8.GetBytes("Server is running.")).Wait();

            Console.CancelKeyPress += (s, ea) =>
            {
                Console.WriteLine("Cancel requested.");
                Environment.Exit(0);
            };

            Thread.CurrentThread.Join();
        }
    }
}
