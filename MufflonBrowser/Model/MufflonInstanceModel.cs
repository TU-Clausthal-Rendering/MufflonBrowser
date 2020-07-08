using MufflonBrowser.Util;
using System.IO;

namespace MufflonBrowser.Model
{
    public class MufflonInstanceModel
    {
        public struct TransformationMatrix
        {
            public TransformationMatrix(float m00, float m01, float m02, float m03,
                float m10, float m11, float m12, float m13,
                float m20, float m21, float m22, float m23)
            {
                this.m00 = m00;
                this.m01 = m01;
                this.m02 = m02;
                this.m03 = m03;
                this.m10 = m10;
                this.m11 = m11;
                this.m12 = m12;
                this.m13 = m13;
                this.m20 = m20;
                this.m21 = m21;
                this.m22 = m22;
                this.m23 = m23;
            }

            public float m00, m01, m02, m03;
            public float m10, m11, m12, m13;
            public float m20, m21, m22, m23;
        }

        public MufflonInstanceModel(BinaryReader reader)
        {
            Name = MufflonFileUtil.ReadString(reader);
            ObjectId = reader.ReadUInt32();
            Keyframe = reader.ReadUInt32();
            PreviousInstanceId = reader.ReadUInt32();
            Transformation = new TransformationMatrix(
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
            );
        }

        public static uint OnlyParseObjectIdAndSeekPastRest(BinaryReader reader)
        {
            var nameLength = reader.ReadUInt32();
            reader.BaseStream.Seek((long)nameLength, SeekOrigin.Current);
            var objectId = reader.ReadUInt32();
            reader.BaseStream.Seek(14L * 4L, SeekOrigin.Current);
            return objectId;
        }

        public string Name { get; set; }
        public uint ObjectId { get; set; }
        public uint Keyframe { get; set; }
        public uint PreviousInstanceId { get; set; }
        public TransformationMatrix Transformation { get; set; }

        // Instead of using the stored object ID we write the one provided without modifying the stored one.
        public void WriteWithDifferentObjectId(BinaryWriter writer, uint objId)
        {
            MufflonFileUtil.Write(writer, Name);
            writer.Write(objId);
            writer.Write(Keyframe);
            writer.Write(PreviousInstanceId);
            writer.Write(Transformation.m00);
            writer.Write(Transformation.m01);
            writer.Write(Transformation.m02);
            writer.Write(Transformation.m03);
            writer.Write(Transformation.m10);
            writer.Write(Transformation.m11);
            writer.Write(Transformation.m12);
            writer.Write(Transformation.m13);
            writer.Write(Transformation.m20);
            writer.Write(Transformation.m21);
            writer.Write(Transformation.m22);
            writer.Write(Transformation.m23);
        }

        public void Write(BinaryWriter writer)
        {
            this.WriteWithDifferentObjectId(writer, ObjectId);
        }
    }
}
