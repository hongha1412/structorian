using System;
using System.IO;
using System.Text;

namespace Structorian.Engine.Fields
{
    public class BlobCell: ValueCell
    {
        private readonly Stream _baseStream;
        private readonly int _baseSize;
        private readonly BlobDecoder _decoder;
        private readonly int _decodedSize;

        public BlobCell(StructField def, Stream baseStream, int offset, int baseSize, BlobDecoder decoder, int decodedSize) 
            : base(def, null, offset)
        {
            _baseStream = baseStream;
            _baseSize = baseSize;
            _decoder = decoder;
            _decodedSize = decodedSize;
        }

        private void BuildDisplayValue()
        {
            using(var s = DataStream)
            {
                int bytesToDisplay = Math.Min(16, (int) s.Length);
                byte[] data = new byte[bytesToDisplay];
                s.Read(data, 0, bytesToDisplay);
                var bytesBuilder = new StringBuilder();
                for (int i = 0; i < bytesToDisplay; i++)
                {
                    if (bytesBuilder.Length > 0)
                        bytesBuilder.Append(' ');
                    bytesBuilder.Append(data[i].ToString("X2"));
                }
                _displayValue = bytesBuilder.ToString();
            }
        }

        public override string Value
        {
            get
            {
                if (_displayValue == null)
                    BuildDisplayValue();
                return _displayValue;
            }
        }

        public Stream DataStream
        {
            get
            {
                byte[] data = new byte[_baseSize];
                _baseStream.Position = Offset;
                _baseStream.Read(data, 0, _baseSize);
                if (_decoder != null)
                {
                    data = _decoder.Decode(data, _decodedSize);
                }
                return new MemoryStream(data);
            }
        }
    }
}
