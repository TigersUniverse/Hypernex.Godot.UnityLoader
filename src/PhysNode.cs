using Godot;
using Hypernex.Game;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class PhysNode : Node
    {
        public HolderNode parent;
        public RigidBody3D rb;

        public override void _Ready()
        {
            parent = GetParent<HolderNode>();
            rb = parent.GetComponent<RigidBody3D>();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsInstanceValid(rb))
            {
                parent.GlobalTransform = rb.GlobalTransform;
            }
        }
    }
}