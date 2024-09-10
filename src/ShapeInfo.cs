using Godot;
using Godot.Collections;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class ShapeInfo : Resource
    {
        [Export]
        public bool shapeEnabled = true;
        [Export]
        public bool shapeTrigger = false;
        [Export]
        public Shape3D shape;
        [Export]
        public Vector3 shapeCenter = Vector3.Zero;
    }
}