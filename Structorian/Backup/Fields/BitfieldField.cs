using System;
using System.IO;

namespace Structorian.Engine.Fields
{
    class BitfieldField: StructField
    {
        public BitfieldField(StructDef structDef) : base(structDef, "size", true)
        {
        }

        public override void LoadData(BinaryReader reader, StructInstance instance)
        {
            int size = GetExpressionAttribute("size").EvaluateInt(instance);
            long baseOffset = reader.BaseStream.Position;
            uint baseValue;
            switch(size)
            {
                case 1: baseValue = reader.ReadByte(); break;
                case 2: baseValue = reader.ReadUInt16(); break;
                case 4: baseValue = reader.ReadUInt32(); break;
                default:
                    throw new LoadDataException("Invalid bitfield size " + size);
            }
            BitfieldReader bitFieldReader = new BitfieldReader(baseValue, size, baseOffset);
            
            foreach(StructField field in ChildFields)
            {
                if (field is IntBasedField)
                {
                    IntBasedField intBasedField = (IntBasedField) field;
                    int? bit = intBasedField.GetIntAttribute("bit");
                    if (bit.HasValue)
                        bitFieldReader.SetBits(bit.Value, bit.Value);
                    else
                    {
                        int? fromBit = intBasedField.GetIntAttribute("frombit");
                        int? toBit = intBasedField.GetIntAttribute("tobit");
                        bitFieldReader.SetBits(fromBit.Value, toBit.Value);
                    }
                }
                field.LoadData(bitFieldReader, instance);
            }
        }

        public override int GetDataSize()
        {
            Expression expr = GetExpressionAttribute("size");
            if (expr.IsConstant)
                return expr.EvaluateInt(null);
            return base.GetDataSize();
        }

        private class BitfieldReader: BinaryReader
        {
            private readonly uint _baseValue;
            private readonly int _size;
            private uint _curValue;
            private int _fromBit;
            private int _toBit;

            public BitfieldReader(uint baseValue, int size, long baseOffset) 
                : base(new BitfieldMemoryStream(baseOffset))
            {
                _baseValue = baseValue;
                _size = size;
            }
            
            public void SetBits(int fromBit, int toBit)
            {
                _fromBit = fromBit;
                _toBit = toBit;
                _curValue = (uint) ((_baseValue >> _fromBit) & ((1 << (_toBit - _fromBit + 1)) - 1));
            }

            public override byte ReadByte()
            {
                return (byte) _curValue;
            }

            public override sbyte ReadSByte()
            {
                return (sbyte) _curValue;
            }

            public override short ReadInt16()
            {
                return (short) _curValue;
            }

            public override ushort ReadUInt16()
            {
                return (ushort) _curValue;
            }

            public override int ReadInt32()
            {
                return (int) _curValue;
            }

            public override uint ReadUInt32()
            {
                return _curValue;
            }
        }

        private class BitfieldMemoryStream: MemoryStream
        {
            private long _baseOffset;

            public BitfieldMemoryStream(long baseOffset)
            {
                _baseOffset = baseOffset;
            }

            public override long Position
            {
                get { return _baseOffset; }
                set { throw new NotImplementedException(); }
            }
        }
    }
}
