using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace TcpSocketServer
{
    public class TcpConnectionListener : IDisposable
    {
        private readonly TcpListener _listener;

        private IDisposable _connectionSubscription;

        private readonly Subject<TcpClient> _connections = new Subject<TcpClient>();

        public TcpConnectionListener(IPAddress address, int tcpListenerPort)
        {
            _listener = new TcpListener(address, tcpListenerPort);
        }

        public TcpConnectionListener(int tcpListenerPort) : this(IPAddress.Any, tcpListenerPort)
        {
        }

        public IObservable<TcpClient> Connections
        {
            get { return _connections; }
        }

        public void Start()
        {
            _listener.Start();

            _connectionSubscription =
                Observable.FromAsync(_listener.AcceptTcpClientAsync)
                .Where(o => o != null && o.Connected)
                .Retry()
                .Repeat()
                .Finally(OnSubscriptionFinished)
                .Subscribe(_connections.OnNext);
        }

        public void Stop()
        {
            _listener.Stop();
            _connectionSubscription.Dispose();
            _connectionSubscription = null;
        }

        private void OnSubscriptionFinished()
        {
            _listener.Stop();

            _connectionSubscription?.Dispose();

            Debug.WriteLine("Tcp listener subscription finished.");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _listener.Stop();
                    _connectionSubscription?.Dispose();
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
