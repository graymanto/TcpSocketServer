using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Net.Sockets;
using System.IO;
using System.Reactive.Concurrency;
using System.Diagnostics;
using TcpSocketServer.Extensions;

namespace TcpSocketServer
{
    public class SocketServer : IDisposable
    {
        private readonly List<TcpConnectionListener> _tcpListeners =
             new List<TcpConnectionListener>();

        private readonly CompositeDisposable _tcpSubscriptions = new CompositeDisposable();

        private ConcurrentDictionary<TcpClient, bool> _connectedClients =
            new ConcurrentDictionary<TcpClient, bool>();

        private readonly Subject<byte[]> _messages = new Subject<byte[]>();
        private readonly Subject<TcpClient> _tcpConnectionErrors = new Subject<TcpClient>();
        private readonly NetworkBeacon _beacon = new NetworkBeacon();

        private bool _started = false;

        public IObservable<byte[]> Messages
        {
            get
            {
                return _messages;
            }
        }

        public IObservable<TcpClient> TcpConnectionErrors
        {
            get
            {
                return _tcpConnectionErrors;
            }
        }

        public void AddTcpListener(IPAddress address, int port)
        {
            var listener = new TcpConnectionListener(address, port);
            _tcpListeners.Add(listener);

            _tcpSubscriptions.Add(listener
                .Connections
                .Subscribe(OnNewTcpConnection));
        }

        private void OnNewTcpConnection(TcpClient client)
        {
            Debug.WriteLine("Client connected from {0}", client.Client.LocalEndPoint.AddressFamily);
            _connectedClients[client] = true;
            ProcessClientMessage(client);
        }

        public void AddTcpListener(int port)
        {
            AddTcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            if (_started)
                return;

            _tcpListeners.ForEach(l =>
            {
                l.Start();
            });

            _beacon.Start();

            _started = true;
        }

        public void Stop()
        {
            _tcpListeners.ForEach(l =>
            {
                l.Stop();
            });

            _connectedClients.Keys.ForEach(c => c.Dispose());
            _connectedClients.Clear();

            _beacon.Stop();

            _started = false;
        }

        public async Task BroadcastMessage(byte[] message)
        {
            foreach (var client in _connectedClients.Keys)
            {
                await SendMessageFromClient(client, message);
            }
        }

        private void ProcessClientMessage(TcpClient client)
        {
            Scheduler.Default.ScheduleAsync(
                async (ctrl, ct) =>
                {
                    var stream = client.GetStream();
                    do
                    {
                        try
                        {
                            var length = await stream.ReadInt64Header(ct);
                            if (length == 0)
                            {
                                Debug.WriteLine("Connection closed when reading from client");
                                TcpConnectionError(client);
                                return;
                            }

                            var bytes = new byte[length];
                            var bytesRead = await stream.ReadIntoByteBuffer(bytes, ct);

                            if (bytesRead == 0)
                            {
                                Debug.WriteLine("Connection closed when reading from client");
                                TcpConnectionError(client);
                                return;
                            }

                            _messages.OnNext(bytes);
                        }
                        catch (TaskCanceledException e)
                        {
                            Debug.WriteLine("Connection error when reading from client, task cancelled {0}", e);
                            TcpConnectionError(client);
                            break;
                        }
                        catch (IOException e)
                        {
                            Debug.WriteLine("Connection error when reading from client, {0}", e);
                            TcpConnectionError(client);
                            break;
                        }
                    }
                    while (stream.DataAvailable);
                });
        }

        private async Task SendMessageFromClient(TcpClient client, byte[] message)
        {
            Int64 messageLength = message.Length;

            var header = BitConverter.GetBytes(
                IPAddress.HostToNetworkOrder(messageLength));

            var networkStream = client.GetStream();
            try
            {
                await networkStream.WriteAsync(header, 0, sizeof(Int64));
                await networkStream.WriteAsync(message, 0, message.Length);

            }
            catch (IOException e)
            {
                // TODO: validate this is always broken connection

                Debug.WriteLine("Error writing to network {0}", e);
                TcpConnectionError(client);
            }
        }

        private void TcpConnectionError(TcpClient client)
        {
            _tcpConnectionErrors.OnNext(client);
            RemoveTcpClient(client);
        }

        private void RemoveTcpClient(TcpClient client)
        {
            bool dummy;
            _connectedClients.TryRemove(client, out dummy);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tcpConnectionErrors.OnCompleted();
                    _tcpConnectionErrors.Dispose();
                    _messages.OnCompleted();
                    _messages.Dispose();
                    _tcpSubscriptions.Dispose();
                    _tcpListeners.ForEach(l => l.Dispose());
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
