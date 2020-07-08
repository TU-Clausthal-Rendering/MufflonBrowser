using MufflonBrowser.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace MufflonBrowser.IO
{
    public class MufflonFileExporter
    {
        public delegate void ReportProgress(string status, bool indeterminate, int current, int max);

        public static void ExportFile(string fileName, MufflonFileModel mufflonFile,
            ReportProgress progress)
        {
            using BinaryReader reader = new BinaryReader(File.OpenRead(mufflonFile.FilePath));
            using BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None));
            progress("writing materials and animations", true, 1, 1);
            // We simply copy material and bone information (for now)
            CopyFileUntilObjectJumpTable(reader, writer, mufflonFile.ObjectHeaderStart,
                out long instanceSectionStreamPos, out long objectCountStreamPos, progress);

            var newObjectIds = ExportObjects(reader, writer, mufflonFile.Scene.Objects,
                mufflonFile.ObjectJumpTable, mufflonFile.InstanceStart,
                out uint remainingObjectCount, progress);

            // Depending on the number of instances we did or didn't parse them,
            // so now we either need to copy them from the input file or can just
            // export them directly
            var instanceSectionOffset = (ulong)writer.BaseStream.Position;
            uint remainingInstanceCount;
            long instanceCountStreamPos;
            if (mufflonFile.Scene.Instances == null)
                remainingInstanceCount = CopyInstances(reader, writer, mufflonFile.Scene.Objects,
                    newObjectIds, out instanceCountStreamPos, progress);
            else
                remainingInstanceCount = ExportInstances(writer, mufflonFile.Scene.Instances,
                    mufflonFile.Scene.Objects, newObjectIds, out instanceCountStreamPos, progress);

            // Fill in the missing counts/section positions
            writer.BaseStream.Seek(instanceSectionStreamPos, SeekOrigin.Begin);
            writer.Write(instanceSectionOffset);
            writer.BaseStream.Seek(objectCountStreamPos, SeekOrigin.Begin);
            writer.Write(remainingObjectCount);
            writer.BaseStream.Seek(instanceCountStreamPos, SeekOrigin.Begin);
            writer.Write(remainingInstanceCount);
        }

        /**
         * Copies the data from the reader into the writer up until the beginning of the object jump table.
         * Returns the stream positions for the next section and object count to be filled in later.
         */
        private static void CopyFileUntilObjectJumpTable(BinaryReader reader, BinaryWriter writer,
            UInt64 objHeaderStart, out long nextSectionStreamPos, out long objectCountStreamPos,
            ReportProgress progress)
        {
            progress("writing materials and animations", true, 1, 1);
            // We simply copy material and bone information (for now)
            writer.Write(reader.ReadBytes((int)objHeaderStart));
            reader.BaseStream.Seek((long)objHeaderStart, SeekOrigin.Begin);
            writer.Write(reader.ReadUInt32());  // Object header magic
                                                // Store the state for when we know how big the object section is
            nextSectionStreamPos = writer.BaseStream.Position;
            writer.Write(reader.ReadUInt64());  // Next section position
            writer.Write(reader.ReadUInt32());  // Flags
            objectCountStreamPos = writer.BaseStream.Position;
            writer.Write(0u);
        }

        /**
         * Writes the object jump table and copies the remaining objects to the writer.
         * Also creates a map of new object IDs for the remaining objects so that
         * they have contiguous IDs.
         */
        private static uint[] ExportObjects(BinaryReader reader, BinaryWriter writer,
            IReadOnlyList<MufflonObjectModel> objects, IReadOnlyList<UInt64> objJumpTable,
            UInt64 previousInstanceSectionStart, out uint remainingObjectCount, ReportProgress progress)
        {
            // Write the jump table
            // First we have to count the number of remaining objects because we need to compute the stream positions
            // We also track the new object IDs (because the have to be contiguous)
            progress("writing objects", false, 1, objects.Count);
            uint[] newObjectIds = new uint[objects.Count];
            remainingObjectCount = 0u;
            for (int i = 0; i < objects.Count; ++i)
                if (objects[i].Retain)
                    newObjectIds[i] = remainingObjectCount++;
            // Now we can write the jump table itself
            // Because we don't actually have the object data lying around we use the
            // old jump table to compute the respective sizes
            long currOffset = writer.BaseStream.Position + remainingObjectCount * 8;
            for (int i = 0; i < objects.Count; ++i)
            {
                if (i % 1000 == 0)
                    progress(null, false, i + 1, objects.Count);
                if (objects[i].Retain)
                {
                    writer.Write(currOffset);
                    var start = objJumpTable[i];
                    // Make sure that we don't go past the end of the jump table, the instance section
                    // provides the last end point
                    var end = (i + 1 < objects.Count) ? objJumpTable[i + 1] : previousInstanceSectionStart;
                    currOffset += (long)(end - start);
                }
            }

            // Now we can write the actual objects
            for (int i = 0; i < objects.Count; ++i)
            {
                if (i % 100 == 0)
                    progress(null, false, i + 1, objects.Count);
                reader.BaseStream.Seek((long)objJumpTable[i], SeekOrigin.Begin);
                // Copy over object or skip it
                if (objects[i].Retain)
                {
                    objects[i].WriteWithoutLods(writer);
                    // Copy over Lods
                    for (int l = 0; l < objects[i].LodCount; ++l)
                    {
                        reader.BaseStream.Seek((long)objects[i].Lods[l].Offset, SeekOrigin.Begin);
                        writer.Write(reader.ReadBytes((int)objects[i].Lods[l].TotalSize));
                    }
                };
            }

            return newObjectIds;
        }

        /**
         * Re-reads the instances from the reader and exports them with modified object ID.
         * Instances whose objects have been removed will not be written.
         * Returns the number of remaining instances.
         */
        private static uint CopyInstances(BinaryReader reader, BinaryWriter writer, 
            IReadOnlyList<MufflonObjectModel> objects, uint[] newObjectIds,
            out long instanceCountStreamPos, ReportProgress progress)
        {
            progress("writing instances", false, 1, 1);
            writer.Write(reader.ReadUInt32());  // Instance header magic
            instanceCountStreamPos = writer.BaseStream.Position;
            writer.Write(0u);

            // If we never parsed the instances we have to read them now
            uint remainingInstanceCount = 0u;
            var oldInstanceCount = reader.ReadUInt32();
            for (uint i = 0u; i < oldInstanceCount; ++i)
            {
                if (i % 100000 == 0)
                    progress(null, false, (int)i + 1, (int)oldInstanceCount);
                var instance = new MufflonInstanceModel(reader);
                if (objects[(int)instance.ObjectId].Retain)
                {
                    instance.ObjectId = newObjectIds[instance.ObjectId];
                    instance.Write(writer);
                    remainingInstanceCount += 1u;
                }
            }

            return remainingInstanceCount;
        }

        /**
         * Writes the instances to the writer with modified object ID.
         * Only instances with remaining objects will be written.
         * Returns the number of remaining instances.
         */
        private static uint ExportInstances(BinaryWriter writer, IReadOnlyList<MufflonInstanceModel> instances,
            IReadOnlyList<MufflonObjectModel> objects, uint[] newObjectIds,
            out long instanceCountStreamPos, ReportProgress progress)
        {
            progress("writing instances", false, 1, 1);
            writer.Write(MufflonFileImporter.INSTANCE_MAGIC);  // Instance header magic
            instanceCountStreamPos = writer.BaseStream.Position;
            writer.Write(0u);

            uint remainingInstanceCount = 0u;
            for (int i = 0; i < instances.Count; ++i)
            {
                if (i % 100000 == 0)
                    progress(null, false, i + 1, instances.Count);
                if (objects[(int)instances[i].ObjectId].Retain)
                {
                    instances[i].WriteWithDifferentObjectId(writer, newObjectIds[instances[i].ObjectId]);
                    remainingInstanceCount += 1u;
                }
            }
            return remainingInstanceCount;
        }
    }
}
