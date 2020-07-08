using System.IO;

namespace MufflonBrowser.Util
{
    public class MufflonFileUtil
    {
        public static string ReadString(BinaryReader reader)
        {
            var length = reader.ReadUInt32();
            return new string(reader.ReadChars((int)length));
        }

        public static void Write(BinaryWriter writer, string str)
        {
            writer.Write((uint)str.Length);
            foreach (var chr in str)
                writer.Write(chr);
        }
    }
}
