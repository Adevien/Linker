using CESIL.SHARED;
using Linker;
using System;

namespace CESIL.WORKER
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (args is null)
            {

            }

            new Client();
        }

    }

    public class Client
    {
        private readonly Portal pipeClient = new Portal("CESIL_PIPE");

        private readonly StreamProcessor _processor = new StreamProcessor();

        readonly bool isRunning = true;

        public Client()
        {
            pipeClient.Received += OnServerMessage;
            pipeClient.Unlinked += OnDisconnected;
            pipeClient.Error += PipeClient_Error;
            pipeClient.Linked += PipeClient_Linked;
            pipeClient.Start();

            _processor.Register<TestClass>(OnTestClassReceived);

            while (isRunning)
            {
                var input = Console.ReadLine();

                if (input == "exit")
                {
                    isRunning = false;
                }
                else
                {


                    _processor.Send(pipeClient, new TestClass() { Text = input });


                }
            }
        }

        private void PipeClient_Linked()
        {
            Console.WriteLine($"Connected with success");
        }

        private void OnTestClassReceived(TestClass obj)
        {
            Console.WriteLine($"RECEIVED FROM SERVER: {obj.Text}");
        }

        private void PipeClient_Error(Exception exception)
        {
            Console.WriteLine(exception.Message);
        }

        private void OnServerMessage(Link connection, byte[] message)
        {
            _processor.ReadPacket(message);
        }

        private void OnDisconnected(Link connection)
        {
            Console.WriteLine("WE DISCONNECTED FROM SERVER");
        }

    }
}
