using JetBrains.Annotations;
using MufflonBrowser.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MufflonBrowser.Model
{
    public class MufflonObjectModel : INotifyPropertyChanged
    {
        public struct BoundingBox
        {
            public BoundingBox(float minX, float minY, float minZ,
                float maxX, float maxY, float maxZ)
            {
                this.minX = minX;
                this.minY = minY;
                this.minZ = minZ;
                this.maxX = maxX;
                this.maxY = maxY;
                this.maxZ = maxZ;
            }
            public float minX, minY, minZ;
            public float maxX, maxY, maxZ;
        }

        private static readonly uint OBJECT_MAGIC = 'O' | ('b' << 8) | ('j' << 16) | ('_' << 24);

        public MufflonObjectModel(BinaryReader reader)
        {
            if (reader.ReadUInt32() != OBJECT_MAGIC)
                throw new Exception("Invalid object magic constant");
            Name = MufflonFileUtil.ReadString(reader);
            Flags = reader.ReadUInt32();
            Keyframe = reader.ReadUInt32();
            PreviousObjectId = reader.ReadUInt32();
            Aabb = new BoundingBox(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            // Read the LoDs
            var lodCount = reader.ReadUInt32();
            var lodJumpTable = new List<UInt64>((int)lodCount);
            for (uint l = 0u; l < lodCount; ++l)
                lodJumpTable.Add(reader.ReadUInt64());

            // Perform look-ahead because not every file version has material indices...
            var matIndicesCount = reader.ReadUInt32();
            if(matIndicesCount == MufflonLodModel.LOD_MAGIC)
            {
                // Put the data back into the stream
                reader.BaseStream.Seek(-4L, SeekOrigin.Current);
                MaterialIndices = null;
            } else
            {
                MaterialIndices = new List<ushort>((int)matIndicesCount);
                for (uint i = 0u; i < matIndicesCount; ++i)
                    MaterialIndices.Add(reader.ReadUInt16());
            }

            // Read in the LoDs (follow directly after object header)
            Lods = new List<MufflonLodModel>((int)lodCount);
            foreach (var lodOffset in lodJumpTable)
            {
                reader.BaseStream.Seek((long)lodOffset, SeekOrigin.Begin);
                Lods.Add(new MufflonLodModel(reader));
            }

            InstanceCount = 0;
        }

        private bool m_retain = true;

        public string Name { get; set; }
        public uint Flags { get; set; }
        public uint Keyframe { get; set; }
        public uint PreviousObjectId { get; set; }
        public BoundingBox Aabb { get; set; }
        public List<ushort> MaterialIndices { get; set; }
        public List<MufflonLodModel> Lods { get; set; }
        public int MaterialIndicesCount { get => (MaterialIndices == null ? 0 : MaterialIndices.Count); }
        public int LodCount { get => (Lods == null ? 0 : Lods.Count); }
        public int InstanceCount { get; set; }

        public bool Retain
        {
            get => m_retain;
            set
            {
                if (value == m_retain) return;
                m_retain = value;
                OnPropertyChanged(nameof(Retain));
            }
        }

        /**
         * Writes the object information right until the beginning of the first LoD.
         * If material indices were present they will get written too, otherwise not.
         */
        public void WriteWithoutLods(BinaryWriter writer)
        {
            writer.Write(OBJECT_MAGIC);
            MufflonFileUtil.Write(writer, Name);
            writer.Write(Flags);
            writer.Write(Keyframe);
            writer.Write(PreviousObjectId);
            writer.Write(Aabb.minX);
            writer.Write(Aabb.minY);
            writer.Write(Aabb.minZ);
            writer.Write(Aabb.maxX);
            writer.Write(Aabb.maxY);
            writer.Write(Aabb.maxZ);
            writer.Write(LodCount);
            ulong lodOffset = (ulong)(writer.BaseStream.Position + 8L * LodCount);
            if(MaterialIndices != null)
                lodOffset += 4UL + 2UL * (ulong)MaterialIndicesCount;
            foreach (var lod in Lods)
            {
                writer.Write(lodOffset);
                lodOffset += lod.TotalSize;
            }
            // TODO: this is dependent on the file version, so maybe
            // add the option?
            if(MaterialIndices != null)
            {
                writer.Write((uint)MaterialIndicesCount);
                foreach (var matIdx in MaterialIndices)
                    writer.Write(matIdx);
            }
        }


        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
