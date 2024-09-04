// https://gitlab.com/lox9973/uvw.js/-/tree/master/engine/animation/binding
using System;
using Godot;

namespace Hypernex.GodotVersion.UnityLoader
{
    public abstract partial class PropertyBinding
    {
        public abstract string Set(uint attribute, float[] values, uint offset, bool apply);
    }

    public partial class TransformBinding : PropertyBinding
    {
        private HolderNode node;

        public TransformBinding(HolderNode node)
        {
            // GD.PrintS("Transform", node.Name);
            this.node = node;
        }

        public override string Set(uint attribute, float[] values, uint offset, bool apply)
        {
            switch (attribute)
            {
                case 1: // position
                    if (apply)
                        node.Position = new Vector3(values[offset], values[offset+1], values[offset+2] * BundleReader.zFlipper);
                    return Node3D.PropertyName.Position;
                case 2: // rotation
                    if (apply)
                        node.Quaternion = new Quaternion(values[offset], values[offset+1], values[offset+2] * BundleReader.zFlipper, values[offset+3] * BundleReader.zFlipper);
                    return Node3D.PropertyName.Quaternion;
                case 3: // scale
                    if (apply)
                        node.Scale = new Vector3(values[offset], values[offset+1], values[offset+2]);
                    return Node3D.PropertyName.Scale;
                case 4: // euler
                    if (apply)
                        node.RotationDegrees = new Vector3(values[offset], values[offset+1], values[offset+2] * BundleReader.zFlipper); // TODO: zflipper might not work here
                    return Node3D.PropertyName.RotationDegrees;
            }
            return string.Empty;
        }
    }

    public partial class MuscleBinding : PropertyBinding
    {
        private HolderNode node;

        public MuscleBinding(HolderNode node)
        {
            // GD.PrintS("Muscle", node.Name);
            this.node = node;
            if (this.node.muscles.Length != HumanTrait.MuscleName.Length)
                this.node.muscles = new float[HumanTrait.MuscleName.Length];
        }

        public override string Set(uint attribute, float[] values, uint offset, bool apply)
        {
            var value = values[offset];
            var index = attribute;
            if (index < 42)
            {
                var type = Mathf.FloorToInt(index / 7);
                index %= 7;
                switch (type)
                {
                    case 1: // Root
                        // TODO
                        break;
                }
                return string.Empty;
            }
            index -= 42;
            if (index < HumanTrait.MuscleName.Length)
            {
                // if (!Mathf.IsZeroApprox(value))
                //     GD.PrintS(node.Name, offset, index, value);
                if (apply)
                    node.muscles[index] = value;
                // node.SetMeta($"muscle_{index}", value);
                // return "metadata/muscle_" + index;
                return HolderNode.PropertyName.muscles;// + "/" + index;
                // return HumanTrait.MuscleName[index];
            }
            return string.Empty;
        }
    }
}