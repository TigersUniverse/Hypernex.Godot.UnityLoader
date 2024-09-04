using Godot;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class Axes : Resource
    {
        [Export]
        public string humanBoneName;
        [Export]
        public Quaternion preQ;
        [Export]
        public Quaternion postQ;
        [Export]
        public Vector3 sgn;
        [Export]
        public float length;
        [Export]
        public Vector3 maxRad;
        public Vector3 max => maxRad * (180f / Mathf.Pi);
        [Export]
        public Vector3 minRad;
        public Vector3 min => minRad * (180f / Mathf.Pi);
        [Export]
        public Vector3 position = Vector3.Zero;
        [Export]
        public Quaternion rotation = Quaternion.Identity;
        [Export]
        public Vector3 scale = Vector3.One;
        public Transform3D xform => new Transform3D(new Basis(rotation).Scaled(scale), position);
    }
}