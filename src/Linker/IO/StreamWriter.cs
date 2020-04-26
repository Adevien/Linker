using System;
using System.IO.Pipes;

namespace Linker
{
    public class StreamWriter
    {
        public PipeStream BaseStream { get; private set; }

        public StreamWriter(PipeStream stream)
        {
            BaseStream = stream;
        }

        private void WriteLength(int len)
        {
            var lenbuf = BitConverter.GetBytes(len);
            BaseStream.Write(lenbuf, 0, lenbuf.Length);
        }

        private void WriteDataObject(byte[] data) => BaseStream.Write(data, 0, data.Length);

        private void Flush() => BaseStream.Flush();

        public void WriteData(byte[] data)
        {
            WriteLength(data.Length);
            WriteDataObject(data);
            Flush();
        }

        public void WaitForPipeDrain() => BaseStream.WaitForPipeDrain();
    }
}