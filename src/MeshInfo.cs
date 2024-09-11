using Godot;
using Godot.Collections;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class MeshInfo : Resource
    {
        [Export]
        public bool visible = true;
        [Export]
        public Mesh mesh = null;
        [Export]
        public ushort firstSubmesh = 0;
        [Export]
        public ushort subMeshCount = 0;
        [Export]
        public GeometryInstance3D.ShadowCastingSetting shadows = GeometryInstance3D.ShadowCastingSetting.On;
        [Export]
        public Array<Material> materials = new Array<Material>();
    }
}