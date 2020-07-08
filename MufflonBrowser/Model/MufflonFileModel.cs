using System;
using System.Collections.Generic;

namespace MufflonBrowser.Model
{
    public class MufflonFileModel
    {
        private readonly List<UInt64> objectJumpTable = new List<UInt64>();

        public string FilePath { get; private set; }
        public IReadOnlyList<UInt64> ObjectJumpTable { get => objectJumpTable; }
        public UInt64 AnimationHeaderStart { get; private set; }
        public UInt64 ObjectHeaderStart { get; private set; }
        public UInt64 InstanceStart { get; private set; }
        public MufflonSceneModel Scene { get; private set; }

        public MufflonFileModel(string filePath, List<UInt64> objectJumpTable, UInt64 animHeaderStart,
            UInt64 objHeaderStart, UInt64 instStart, MufflonSceneModel scene)
        {
            FilePath = filePath;
            this.objectJumpTable = objectJumpTable;
            AnimationHeaderStart = animHeaderStart;
            ObjectHeaderStart = objHeaderStart;
            InstanceStart = instStart;
            Scene = scene;
        }
    }
}
