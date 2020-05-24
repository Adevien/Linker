using Linker;
using SamplesShared;
using System;
using System.Collections.Generic;

namespace NodeTest
{
    class Program
    {
        static void Main(string[] args) => new Server();

        public class Server
        {
            private readonly Node _server = new Node("TEST_PIPE");

            private readonly ISet<string> _clients = new HashSet<string>();

            private readonly StreamProcessor _processor = new StreamProcessor();

            private readonly bool isRunning = true;

            public Server()
            {
                _server.PortalLinked += OnClientConnected;
                _server.PortalUnlinked += OnClientDisconnected;
                _server.PortalReceived += Server_ClientMessage;
                _server.Error += Server_Error;
                _server.Start();

                // Enable if you don't want while loops (for windows service)
                //_server.UseRecursion = true;

                _processor.Register<TestClass>(OnTestClass);

                while (isRunning)
                {
                    var input = Console.ReadLine();

                    if (input == "exit")
                        isRunning = false;
                    else  _processor.SendToAll(_server, new TestClass() { Text = input });              
                }
            }

            private void Server_Error(Exception exception) => Console.WriteLine(exception.Message);

            private void OnTestClass(TestClass obj) => Console.WriteLine(obj.Text);

            private void Server_ClientMessage(Link connection, byte[] message) => _processor.ReadPacket(message);

            private void OnClientConnected(Link connection)
            {
                _clients.Add(connection.Name);
                Console.WriteLine(connection.Name + " connected!");
            }

            private void OnClientDisconnected(Link connection)
            {
                _clients.Remove(connection.Name);
                Console.WriteLine(connection.Name + " disconnected!");
            }
        }
    }
}
