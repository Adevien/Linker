using System;
using System.IO;
using System.IO.Pipes;

namespace Linker
{
    public class StreamReader
    {
        public PipeStream BaseStream { get; private set; }

        public bool IsConnected { get; private set; }

        public StreamReader(PipeStream stream)
        {
            BaseStream = stream;
            IsConnected = stream.IsConnected;
        }

        private int ReadDataLength()
        {
            const int lengthSize = sizeof(int);
            var dataLength = new byte[lengthSize];
            var dataBytes = BaseStream.Read(dataLength, 0, lengthSize);

            if (dataBytes == 0) { IsConnected = false; return 0; }
            if (dataBytes != lengthSize) { throw new IOException(string.Format("Expected {0} bytes but read {1}", lengthSize, dataBytes)); }

            return BitConverter.ToInt32(dataLength, 0);
        }

        private byte[] ReadData(int length)
        {
            byte[] data = new byte[length];
            BaseStream.Read(data, 0, length);
            return data;
        }

        public byte[] ReadAction()
        {
            int length = ReadDataLength();
            return length == 0 ? default : ReadData(length);
        }
    }
}
