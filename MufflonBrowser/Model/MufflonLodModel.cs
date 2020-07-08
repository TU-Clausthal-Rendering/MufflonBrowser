using System;
using System.Collections.Generic;
using System.IO;

namespace MufflonBrowser.Model
{
    public class MufflonLodModel
    {
        public static readonly uint LOD_MAGIC = 'L' | ('O' << 8) | ('D' << 16) | ('_' << 24);

        public MufflonLodModel(BinaryReader reader)
        {
            Offset = (ulong)reader.BaseStream.Position;
            if (reader.ReadUInt32() != LOD_MAGIC)
                throw new Exception("Invalid LoD magic constant");
            Triangles = reader.ReadUInt32();
            Quads = reader.ReadUInt32();
            Spheres = reader.ReadUInt32();
            Vertices = reader.ReadUInt32();
            Edges = reader.ReadUInt32();
            var vertexAttribCount = reader.ReadUInt32();
            var faceAttribCount = reader.ReadUInt32();
            var sphereAttribCount = reader.ReadUInt32();
            VertexAttributes = new List<MufflonAttributeModel>((int)vertexAttribCount);
            FaceAttributes = new List<MufflonAttributeModel>((int)faceAttribCount);
            SphereAttributes = new List<MufflonAttributeModel>((int)sphereAttribCount);
            // Skip to the attributes
            // TODO: if this is compressed we have to properly read the compressed size...
            reader.BaseStream.Seek((long)VertexDataSize, SeekOrigin.Current);
            for (uint i = 0u; i < vertexAttribCount; ++i)
                VertexAttributes.Add(new MufflonAttributeModel(reader));
            reader.BaseStream.Seek((long)FaceDataSize, SeekOrigin.Current);
            for (uint i = 0u; i < faceAttribCount; ++i)
                FaceAttributes.Add(new MufflonAttributeModel(reader));
            reader.BaseStream.Seek((long)SphereDataSize, SeekOrigin.Current);
            for (uint i = 0u; i < sphereAttribCount; ++i)
                SphereAttributes.Add(new MufflonAttributeModel(reader));
        }

        public uint Triangles { get; }
        public uint Quads { get; }
        public uint Spheres { get; }
        public uint Vertices { get; } 
        public uint Edges { get; }
        public List<MufflonAttributeModel> VertexAttributes { get; }
        public List<MufflonAttributeModel> FaceAttributes { get; }
        public List<MufflonAttributeModel> SphereAttributes { get; }
        public int VertexAttributeCount { get => (VertexAttributes == null) ? 0 : VertexAttributes.Count; } 
        public int FaceAttributeCount { get => (FaceAttributes == null) ? 0 : FaceAttributes.Count; }
        public int SphereAttributeCount { get => (SphereAttributes == null) ? 0 : SphereAttributes.Count; }
        public ulong Offset { get; }
        // TODO: these are only valid if no compression has been applied
        public ulong VertexDataSize { get => 8ul * 4ul * Vertices; }
        public ulong FaceDataSize { get => 3ul * 4ul * Triangles + 4ul * 4ul * Quads + 2ul * (Triangles + Quads); }
        public ulong SphereDataSize { get => 4ul * 4ul * Spheres + 2ul * Spheres; }
        public ulong DataSize { get => VertexDataSize + FaceDataSize + SphereDataSize; }
        public ulong AttributeSize
        {
            get
            {
                ulong sum = 0ul;
                foreach (var attr in VertexAttributes)
                    sum += attr.TotalSize;
                foreach (var attr in FaceAttributes)
                    sum += attr.TotalSize;
                foreach (var attr in SphereAttributes)
                    sum += attr.TotalSize;
                return sum;
            }
        }
        public ulong TotalSize { get => DataSize + AttributeSize + 9ul * 4ul; }
    }
}
