using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MARC4J.Net
{
    public class InputStream : IDisposable
    {
        private Stream _stream;
        private long _markedPosition = 0;

        public InputStream(Stream stream)
        {
            _stream = stream;
        }
        public InputStream(byte[] data)
        {
            _stream = new MemoryStream(data);
        }
        public Stream BaseStream
        {
            get { return _stream; }
        }

        public void Mark(int readLimit = 0)
        {
            _markedPosition = _stream.Position;
        }

        public int Read(byte[] data)
        {
            return _stream.Read(data, 0, data.Length);
        }

        public int ReadByte()
        {
            return _stream.ReadByte();
        }

        public void Reset()
        {
            _stream.Position = _markedPosition;
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
            }
        }
    }
}
