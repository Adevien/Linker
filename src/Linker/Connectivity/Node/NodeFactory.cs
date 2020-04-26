using System.IO.Pipes;

namespace Linker
{
    static class PipeServerFactory
    {
        public static NamedPipeServerStream CreateAndConnectPipe(string pipeName)
        {
            var pipe = CreatePipe(pipeName);
            pipe.WaitForConnection();
            return pipe;
        }

        public static NamedPipeServerStream CreatePipe(string pipeName) => new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    }
}
