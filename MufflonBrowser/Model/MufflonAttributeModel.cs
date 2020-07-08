using MufflonBrowser.Util;
using System;
using System.IO;

namespace MufflonBrowser.Model
{
    public class MufflonAttributeModel
    {
        private static readonly uint ATTRIBUTE_MAGIC = 'A' | ('t' << 8) | ('t' << 16) | ('r' << 24);

        public enum AttributeType : uint
        {
            INT_8 = 0,
            UINT_8 = 1,
            INT_16 = 2,
            UINT_16 = 3,
            INT_32 = 4,
            UINT_32 = 5,
            INT_64 = 6,
            UINT_64 = 7,
            FLOAT_32 = 8,
            FLOAT_64 = 9,
            VEC2_UINT_8 = 10,
            VEC3_UINT_8 = 11,
            VEC4_UINT_8 = 12,
            VEC2_INT_32 = 13,
            VEC3_INT_32 = 14,
            VEC4_INT_32 = 15,
            VEC2_FLOAT_32 = 16,
            VEC3_FLOAT_32 = 17,
            VEC4_FLOAT_32 = 18,
            VEC4_UINT_32 = 19
        }

        public MufflonAttributeModel(BinaryReader reader)
        {
            if (reader.ReadUInt32() != ATTRIBUTE_MAGIC)
                throw new Exception("Invalid attribute magic constant");
            Name = MufflonFileUtil.ReadString(reader);
            MetaInfo = MufflonFileUtil.ReadString(reader);
            MetaFlags = reader.ReadUInt32();
            Type = (AttributeType)reader.ReadUInt32();
            DataSize = reader.ReadUInt64();
        }

        public string Name { get; }
        public string MetaInfo { get; }
        public uint MetaFlags { get; }
        public AttributeType Type { get; }
        // Data size is the size of the (not parsed) attribute data itself
        public ulong DataSize { get; }
        // Total size is the size of the attribute data as well as the header
        public ulong TotalSize { get => 5u * 4ul + 8ul + (ulong)(Name.Length + MetaInfo.Length) + DataSize; }
    }
}
