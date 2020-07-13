using MufflonBrowser.Model;
using MufflonBrowser.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace MufflonBrowser.IO
{
    public class MufflonFileImporter
    {
        public static readonly uint MATERIALS_HEADER_MAGIC = 'M' | ('a' << 8) | ('t' << 16) | ('s' << 24);
        public static readonly uint BONE_ANIMATION_HEADER_MAGIC = 'B' | ('o' << 8) | ('n' << 16) | ('e' << 24);
        public static readonly uint OBJECTS_HEADER_MAGIC = 'O' | ('b' << 8) | ('j' << 16) | ('s' << 24);
        public static readonly uint INSTANCE_MAGIC = 'I' | ('n' << 8) | ('s' << 16) | ('t' << 24);

        public delegate void ReportProgress(string status, bool indeterminate, int current, int max);

        /**
         * Reads the file provided and parses it as a mufflon file.
         * Throws if file does not exist or the file cannot be parsed
         * (e.g. failed header checks or invalid object IDs).
         */
        public static MufflonFileModel LoadFile(string filePath, ReportProgress progress)
        {
            if (!File.Exists(filePath))
                throw new Exception("File does not exist");

            using BinaryReader reader = new BinaryReader(File.OpenRead(filePath));
            // March through the files and parse the different sections.
            // Each section will report the start of the next section as well
            var materials = ReadMaterials(reader, out ulong animHeaderStart, progress);

            reader.BaseStream.Seek((long)animHeaderStart, SeekOrigin.Begin);
            var bones = ReadAnimationBones(reader, out ulong objHeaderStart, progress);

            reader.BaseStream.Seek((long)objHeaderStart, SeekOrigin.Begin);
            var objJumpTable = ReadObjectJumpTable(reader, out ulong instStart, progress);
            // No need to seek because readObjects does it for us
            var objects = ReadObjects(reader, objJumpTable, progress);

            reader.BaseStream.Seek((long)instStart, SeekOrigin.Begin);
            var instances = ReadInstances(reader, objects, progress);
            return new MufflonFileModel(filePath, objJumpTable, animHeaderStart, objHeaderStart,
                instStart, new MufflonSceneModel(materials, bones, objects, instances));
        }

        /**
         * Reads the material names from the reader.
         * Expects the reader to be at the beginning of the materials section.
         */
        private static List<string> ReadMaterials(BinaryReader reader, out UInt64 nextSectionStart,
            ReportProgress progress)
        {
            progress("reading materials", false, 1, 1);
            // Ensure that magic constant matches expectation (materials are NOT optional)
            if (reader.ReadUInt32() != MATERIALS_HEADER_MAGIC)
                throw new Exception("Invalid materials header magic constant");
            nextSectionStart = reader.ReadUInt64();
            var numMaterials = reader.ReadUInt32();
            var materials = new List<string>((int)numMaterials);
            for (uint i = 0u; i < numMaterials; ++i)
            {
                if (i % 100 == 0)
                    progress(null, false, (int)i + 1, (int)numMaterials);
                materials.Add(MufflonFileUtil.ReadString(reader));
            }
            return materials;
        }

        /**
         * Reads the animation bones from the reader.
         * Expects the reader's stream position to be at the beginning of the animation section,
         * if one exists. If no animation section is present this returns null and the
         * current stream position as the next section.
         */
        private static List<string> ReadAnimationBones(BinaryReader reader, out UInt64 nextSectionStart,
            ReportProgress progress)
        {
            progress("reading animation bones", false, 1, 1);
            // Ensure that magic constant matches expectation (animation bones ARE optional)
            if (reader.ReadUInt32() != BONE_ANIMATION_HEADER_MAGIC)
            {
                nextSectionStart = (UInt64)(reader.BaseStream.Position - 4L);
                return null;
            }
            nextSectionStart = reader.ReadUInt64();
            _ = reader.ReadUInt32();            // Number of bones
            _ = reader.ReadUInt32();            // Frame count
            // TODO: we don't actually store bone names in the file, only the dual quaternions...
            return new List<string>();
        }

        /**
         * Reads the object's jump table from the reader.
         * Expects the reader's stream position to be at the beginning of the objects section.
         */
        private static List<UInt64> ReadObjectJumpTable(BinaryReader reader, out UInt64 nextSectionStart,
            ReportProgress progress)
        {
            progress("reading object jump table", false, 1, 1);
            // Ensure that magic constant matches expectation (objects are NOT optional)
            if (reader.ReadUInt32() != OBJECTS_HEADER_MAGIC)
                throw new Exception("Invalid objects header magic constant");
            nextSectionStart = reader.ReadUInt64();
            _ = reader.ReadUInt32();         // TODO: compression flags
            var numObjects = reader.ReadUInt32();

            // For objects we have a jump table instead of a direct list of names
            var objectJumpTable = new List<UInt64>((int)numObjects);
            for (uint i = 0u; i < numObjects; ++i)
            {
                if (i % 100 == 0)
                    progress(null, false, (int)i + 1, (int)numObjects);
                objectJumpTable.Add(reader.ReadUInt64());
            }
            return objectJumpTable;
        }

        /**
         * Reads all objects from the reader.
         * Does not make any assumptions about the reader's stream position and instead utilizes
         * the provided jump table.
         */
        private static List<MufflonObjectModel> ReadObjects(BinaryReader reader, List<UInt64> objJumpTable,
            ReportProgress progress)
        {
            progress("reading objects", false, 0, objJumpTable.Count);
            var objects = new List<MufflonObjectModel>(objJumpTable.Count);
            for (int i = 0; i < objJumpTable.Count; ++i)
            {
                if (i % 100 == 0)
                    progress(null, false, i + 1, objJumpTable.Count);
                reader.BaseStream.Seek((long)objJumpTable[i], SeekOrigin.Begin);
                objects.Add(new MufflonObjectModel(reader));
            }
            return objects;
        }

        /**
         * Reads all instances from the reader.
         * Expects the reader's stream position to be at the beginning of the instance section.
         * If the number of instances exceeds 100,000, no instance will actually be
         * loaded except for their object ID to avoid blowing up memory.
         * Every instance also increases the instance counter of the object it references
         * via its object ID.
         */
        private static List<MufflonInstanceModel> ReadInstances(BinaryReader reader, List<MufflonObjectModel> objects,
                                                                ReportProgress progress)
        {
            progress("reading instances", false, 1, 1);
            // Ensure that magic constant matches expectation (objects are NOT optional)
            if (reader.ReadUInt32() != INSTANCE_MAGIC)
                throw new Exception("Invalid instance magic constant");
            var numInstances = reader.ReadUInt32();



            // We only parse instances up to a certain limit due to memory constraints (mainly the names...)
            if (numInstances > 100000)
            {
                /*for (uint i = 0u; i < numInstances; ++i)
                {
                    if (i % 100000 == 0)
                        progress(null, false, (int)i + 1, (int)numInstances);
                    var objId = (int)MufflonInstanceModel.OnlyParseObjectIdAndSeekPastRest(reader);
                    if (objId >= objects.Count)
                        throw new Exception("Found instance with object ID out of bounds");
                    objects[objId].InstanceCount += 1;
                }*/
                return null;
            }

            // To avoid excessive reallocations we first parse all instances and then
            // add them to the respective objects
            var instances = new List<MufflonInstanceModel>((int)numInstances);
            for (uint i = 0u; i < numInstances; ++i)
            {
                if (i % 10000 == 0)
                    progress(null, false, (int)i + 1, (int)numInstances);
                instances.Add(new MufflonInstanceModel(reader));
            }

            foreach (var obj in objects)
                obj.Instances = new List<MufflonInstanceModel>();
            foreach(var inst in instances)
            {
                var objId = (int)inst.ObjectId;
                if (objId >= objects.Count)
                    throw new Exception("Found instance with object ID out of bounds");
                objects[objId].Instances.Add(inst);
            }

            return instances;
        }
    }
}
