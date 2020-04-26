using System.IO.Pipes;

namespace Linker
{
    public class StreamWrapper
    {
        public PipeStream BaseStream { get; private set; }

        public bool IsConnected { get { return BaseStream.IsConnected && _reader.IsConnected; } }

        public bool CanRead { get { return BaseStream.CanRead; } }

        public bool CanWrite { get { return BaseStream.CanWrite; } }

        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public StreamWrapper(PipeStream stream)
        {
            BaseStream = stream;
            _reader = new StreamReader(BaseStream);
            _writer = new StreamWriter(BaseStream);
        }

        public byte[] Read() => _reader.ReadAction();

        public void Write(byte[] data) => _writer.WriteData(data);

        public void WaitForPipeDrain() => _writer.WaitForPipeDrain();

        public void Close() => BaseStream.Close();

    }
}
