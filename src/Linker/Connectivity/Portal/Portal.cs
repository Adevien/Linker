using System;
using System.Text;
using System.Threading;

namespace Linker
{
    public class Portal
    {
        public event LinkedHandler Linked;

        public event LinkReceiveEventHandler Received;

        public event LinkEventHandler Unlinked;

        public event LinkExceptionHandler Error;

        private readonly string _nodeName;

        private Link _link;

        private readonly AutoResetEvent _linked = new AutoResetEvent(false);
        private readonly AutoResetEvent _unlinked = new AutoResetEvent(false);

        public Portal(string nodeName) => _nodeName = nodeName;

        public void Start()
        {
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        public void Send(byte[] data)
        {
            if (_link != null)
                _link.SendAndEnqueue(data);
        }

        public void Stop()
        {
            if (_link != null)
                _link.Close();
        }

        public void WaitForConnection() => _linked.WaitOne();

        public void WaitForConnection(int millisecondsTimeout) => _linked.WaitOne(millisecondsTimeout);

        public void WaitForConnection(TimeSpan timeout) => _linked.WaitOne(timeout);

        public void WaitForDisconnection() => _unlinked.WaitOne();

        public void WaitForDisconnection(int millisecondsTimeout) => _unlinked.WaitOne(millisecondsTimeout);

        public void WaitForDisconnection(TimeSpan timeout) => _unlinked.WaitOne(timeout);

        private void ListenSync()
        {
            var handshake = PortalFactory.Connect(_nodeName);
            var dataPipeName = handshake.Read();
            handshake.Close();

            var dataPipe = PortalFactory.CreateAndConnectPipe(Encoding.UTF8.GetString(dataPipeName));

            _link = LinkFactory.CreateConnection(dataPipe);
            _link.Unlinked += UnlinkedEvent;
            _link.ReceiveData += ReceiveEvent;
            _link.LinkingError += LinkingErrorEvent;
            _link.Open();

            _linked.Set();

            if (_link.IsConnected)
                Linked?.Invoke();
        }

        private void UnlinkedEvent(Link connection)
        {
            Unlinked?.Invoke(connection);

            _unlinked.Set();
        }

        private void ReceiveEvent(Link connection, byte[] data) => Received?.Invoke(connection, data);

        private void LinkingErrorEvent(Link link, Exception exception) => OnError(exception);

        private void OnError(Exception exception) => Error?.Invoke(exception);

    }
}
