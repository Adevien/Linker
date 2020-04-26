using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;

namespace Linker
{
    public class Link
    {
        public readonly int Id;

        public readonly string Name;

        public bool IsConnected { get { return _streamWrapper.IsConnected; } }

        public event LinkEventHandler Unlinked;

        public event LinkReceiveEventHandler ReceiveData;

        public event LinkExceptionEventHandler LinkingError;

        private readonly StreamWrapper _streamWrapper;

        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);

        private readonly Queue<byte[]> _writeQueue = new Queue<byte[]>();

        private bool _notifiedSucceeded;

        internal Link(int id, string name, PipeStream nodeStream)
        {
            Id = id;
            Name = name;
            _streamWrapper = new StreamWrapper(nodeStream);
        }

        public void Open()
        {
            var readWorker = new Worker();
            readWorker.Succeeded += OnSucceeded;
            readWorker.Error += OnError;
            readWorker.DoWork(ReadPipe);

            var writeWorker = new Worker();
            writeWorker.Succeeded += OnSucceeded;
            writeWorker.Error += OnError;
            writeWorker.DoWork(WritePipe);
        }

        public void SendAndEnqueue(byte[] data)
        {
            _writeQueue.Enqueue(data);
            _writeSignal.Set();
        }

        public void Close() => CloseImpl();

        private void CloseImpl()
        {
            _streamWrapper.Close();
            _writeSignal.Set();
        }

        private void OnSucceeded()
        {
            if (_notifiedSucceeded)
                return;

            _notifiedSucceeded = true;

            Unlinked?.Invoke(this);
        }

        private void OnError(Exception exception) => LinkingError?.Invoke(this, exception);

        private void ReadPipe()
        {
            while (IsConnected && _streamWrapper.CanRead)
            {
                var obj = _streamWrapper.Read();
                if (obj == null)
                {
                    CloseImpl();
                    return;
                }
                ReceiveData?.Invoke(this, obj);
            }
        }

        private void WritePipe()
        {
            while (IsConnected && _streamWrapper.CanWrite)
            {
                _writeSignal.WaitOne();
                while (_writeQueue.Count > 0)
                {
                    _streamWrapper.Write(_writeQueue.Dequeue());
                    _streamWrapper.WaitForPipeDrain();
                }
            }
        }
    }

    public delegate void LinkedHandler();

    public delegate void LinkEventHandler(Link link);
    
    public delegate void LinkReceiveEventHandler(Link link, byte[] data);

    public delegate void LinkExceptionEventHandler(Link link, Exception exception);

    public delegate void LinkExceptionHandler(Exception exception);
}
