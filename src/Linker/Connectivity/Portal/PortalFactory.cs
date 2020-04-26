using System.IO.Pipes;

namespace Linker
{
    static class PortalFactory
    {
        public static StreamWrapper Connect(string nodeName) => new StreamWrapper(CreateAndConnectPipe(nodeName));

        public static NamedPipeClientStream CreateAndConnectPipe(string nodeName)
        {
            var pipe = CreatePipe(nodeName);
            pipe.Connect();
            return pipe;
        }

        private static NamedPipeClientStream CreatePipe(string nodeName) => new NamedPipeClientStream(".", nodeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    }
}
