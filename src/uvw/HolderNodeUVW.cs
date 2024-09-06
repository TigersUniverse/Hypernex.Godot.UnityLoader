using System.Linq;
using Godot;
using Godot.Collections;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class HolderNode
    {
        [Export]
        public Dictionary<long, NodePath> hashToNodePath = new Dictionary<long, NodePath>();
        public System.Collections.Generic.Dictionary<long, HolderNode> hashToNode => hashToNodePath.ToDictionary(x => x.Key, x => GetNode<HolderNode>(x.Value));
        [Export]
        public Vector3 rootBonePosition = Vector3.Zero;
        [Export]
        public Quaternion rootBoneRotation = Quaternion.Identity;
        [Export]
        public Vector3 rootBoneScale = Vector3.One;
    }
}