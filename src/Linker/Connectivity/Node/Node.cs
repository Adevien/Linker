using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;

namespace Linker
{
    public class Node
    {
        public event LinkEventHandler PortalLinked;

        public event LinkEventHandler PortalUnlinked;

        public event LinkReceiveEventHandler PortalReceived;

        public event LinkExceptionHandler Error;

        private readonly string _nodeName;

        private readonly List<Link> _connections = new List<Link>();

        private int _nextPipeId;

        private volatile bool _shouldKeepRunning;
        private volatile bool _isRunning;

        public bool IsRunning { get => _isRunning; set => _isRunning = value; }

        public Node(string nodeName) => _nodeName = nodeName;

        public void Start()
        {
            _shouldKeepRunning = true;
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        public void Send(byte[] message)
        {
            lock (_connections)
            {
                foreach (var client in _connections)
                {
                    client.SendAndEnqueue(message);
                }
            }
        }

        public void SendButExclude(byte[] message, int excluded)
        {
            lock (_connections)
            {
                foreach (var client in _connections)
                {
                   if(client.Id != excluded)
                        client.SendAndEnqueue(message);
                }
            }
        }

        public void SendTo(byte[] message, int id)
        {
            lock (_connections)
            {
                if(_connections[id] != null)
                {
                    _connections[id].SendAndEnqueue(message);
                }
            }
        }

        public void Stop()
        {
            _shouldKeepRunning = false;

            lock (_connections)
            {
                foreach (var client in _connections.ToArray())
                {
                    client.Close();
                }
            }

            Portal dummy = new Portal(_nodeName);
            dummy.Start();
            dummy.WaitForConnection(TimeSpan.FromSeconds(2));
            dummy.Stop();
            dummy.WaitForDisconnection(TimeSpan.FromSeconds(2));
        }

        private void ListenSync()
        {
            IsRunning = true;
            while (_shouldKeepRunning)
            {
                WaitForConnection(_nodeName);
            }
            IsRunning = false;
        }

        private void WaitForConnection(string pipeName)
        {
            NamedPipeServerStream handshakePipe = null;
            NamedPipeServerStream dataPipe = null;
            Link connection = null;

            var connectionPipeName = GetNextConnectionPipeName(pipeName);

            try
            {
                handshakePipe = PipeServerFactory.CreateAndConnectPipe(pipeName);
                var handshakeWrapper = new StreamWrapper(handshakePipe);
                handshakeWrapper.Write(Encoding.UTF8.GetBytes(connectionPipeName));
                handshakeWrapper.WaitForPipeDrain();
                handshakeWrapper.Close();

                dataPipe = PipeServerFactory.CreatePipe(connectionPipeName);
                dataPipe.WaitForConnection();

                connection = LinkFactory.CreateConnection(dataPipe);
                connection.ReceiveData += ClientOnReceiveMessage;
                connection.Unlinked += ClientOnDisconnected;
                connection.LinkingError += ConnectionOnError;
                connection.Open();

                lock (_connections)
                {
                    _connections.Add(connection);
                }

                ClientOnConnected(connection);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Named pipe is broken or disconnected: {0}", e);

                Cleanup(handshakePipe);
                Cleanup(dataPipe);

                ClientOnDisconnected(connection);
            }
        }

        private void ClientOnConnected(Link connection) => PortalLinked?.Invoke(connection);

        private void ClientOnReceiveMessage(Link connection, byte[] message) => PortalReceived?.Invoke(connection, message);

        private void ClientOnDisconnected(Link connection)
        {
            if (connection == null)
                return;

            lock (_connections)
            {
                _connections.Remove(connection);
            }

            PortalUnlinked?.Invoke(connection);
        }

        private void ConnectionOnError(Link connection, Exception exception) => OnError(exception);

        private void OnError(Exception exception) => Error?.Invoke(exception);

        private string GetNextConnectionPipeName(string pipeName) => string.Format("{0}_{1}", pipeName, ++_nextPipeId);

        private static void Cleanup(NamedPipeServerStream pipe)
        {
            if (pipe == null) return;
            using NamedPipeServerStream x = pipe;
            x.Close();
        }
    }
}
