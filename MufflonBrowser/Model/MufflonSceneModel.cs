using System.Collections.Generic;

namespace MufflonBrowser.Model
{
    public class MufflonSceneModel
    {
        private readonly List<string> materials = new List<string>();
        private readonly List<string> bones = new List<string>();
        private readonly List<MufflonObjectModel> objects = new List<MufflonObjectModel>();
        private readonly List<MufflonInstanceModel> instances = new List<MufflonInstanceModel>();

        public IReadOnlyList<string> Materials { get => materials; }
        public IReadOnlyList<string> Bones { get => bones; }
        public IReadOnlyList<MufflonObjectModel> Objects { get => objects; }
        public IReadOnlyList<MufflonInstanceModel> Instances { get => instances; }

        public MufflonSceneModel(List<string> materials, List<string> bones,
            List<MufflonObjectModel> objects, List<MufflonInstanceModel> instances)
        {
            this.materials = materials;
            this.bones = bones;
            this.objects = objects;
            this.instances = instances;
        }
    }
}
