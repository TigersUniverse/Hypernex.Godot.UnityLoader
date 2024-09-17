using System.Linq;
using Godot;
using Hypernex.Game;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class SkinnedNode : Node
    {
        public HolderNode parent;
        public AnimationPlayer anim;
        public Skeleton3D[] skels;

        public override void _Ready()
        {
            parent = GetParent<HolderNode>();
            anim = parent.GetComponent<AnimationPlayer>();
            skels = parent.GetComponents<Skeleton3D>();
        }

        public override void _Process(double delta)
        {
            if (IsInstanceValid(anim) && !anim.IsPlaying())
            {
                // anim.Play(anim.GetAnimationList().First());
            }
            foreach (var rootPath in parent.rootBonePaths)
            {
                var root = GetNodeOrNull<Node3D>(rootPath);
                if (IsInstanceValid(root))
                {
                    root.Position = parent.rootBonePosition * new Vector3(1f, 1f, BundleReader.zFlipper);
                    root.Quaternion = HumanTrait.FlipZ(parent.rootBoneRotation);
                    if (!parent.rootBoneScale.IsZeroApprox())
                        root.Scale = parent.rootBoneScale;
                }
            }
            foreach (var skel in skels)
            {
                if (IsInstanceValid(skel))
                {
                    if (IsInstanceValid(anim) || parent.rootBonePaths.Count != 0)
                    {
                        skel.TopLevel = true;
                        parent.Transform = skel.Transform;
                        // parent.Position = skel.Position;
                        // parent.Rotation = skel.Rotation + new Vector3(0f, Mathf.DegToRad(180f), 0f);
                        // parent.Scale = skel.Scale;
                        foreach (var kvp in parent.humanBoneAxes)
                        {
                            if (string.IsNullOrEmpty(kvp.Value.humanBoneName))
                                continue;
                            var bone = parent.GetNode<HolderNode>(kvp.Key);
                            if (bone == parent)
                                continue;
                            int idx = skel.FindBone(kvp.Value.humanBoneName);
                            if (idx != -1)
                            {
                                var xform = skel.GetBonePose(idx);
                                bone.Transform = xform;
                            }
                            else
                            {
                                bone.Transform = kvp.Value.xform;
                            }
                        }
                    }
                    else
                    {
                        foreach (var kvp in parent.skeletonBoneMap)
                        {
                            var node = parent.GetNode<HolderNode>(kvp.Key);
                            var xform = node.Transform;
                            skel.SetBonePose(kvp.Value, xform);
                        }
                    }
                }
            }
            // return;
            foreach (var rootPath in parent.rootBonePaths)
            {
                var root = GetNodeOrNull<Node3D>(rootPath);
                if (IsInstanceValid(root))
                {
                    root.Position = parent.rootBonePosition * new Vector3(1f, 1f, BundleReader.zFlipper);
                    root.Quaternion = HumanTrait.FlipZ(parent.rootBoneRotation);
                    if (!parent.rootBoneScale.IsZeroApprox())
                        root.Scale = parent.rootBoneScale;
                }
            }
            return;
            if (IsInstanceValid(anim))
            {
                // System.Array.Fill(muscles, 0f);
                HumanTrait.ApplyBones(parent);
            }
        }
    }
}