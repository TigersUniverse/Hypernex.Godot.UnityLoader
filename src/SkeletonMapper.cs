using System;
using System.Collections.Generic;
using Godot;
using Hypernex.Game;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class SkeletonMapper : SkeletonModifier3D
    {
        public Skeleton3D skel;
        public Skeleton3D hSkel;
        [Export]
        public NodePath target;
        public Dictionary<string, Transform3D> binds = new Dictionary<string, Transform3D>();

        public override void _Ready()
        {
            skel = GetParent<Skeleton3D>();
            HolderNode holder = GetNodeOrNull<HolderNode>(target);
            hSkel = holder.GetComponent<Skeleton3D>();
            hSkel.SkeletonUpdated += Updated;
        }

        public override void _ProcessModification()
        {
            foreach (var kvp in binds)
            {
                int idx = skel.FindBone(kvp.Key);
                if (idx == -1)
                    continue;
                skel.SetBonePose(idx, kvp.Value);
            }
        }

        private void Updated()
        {
            for (int i = 0; i < hSkel.GetBoneCount(); i++)
            {
                string name = hSkel.GetBoneName(i);
                binds[name] = hSkel.GetBonePose(i);
                continue;
                int idx = hSkel.FindBone(name);
                if (idx == -1)
                    continue;
                hSkel.SetBonePose(idx, skel.GetBonePose(i));
            }
        }
    }
}