using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class HumanTrait
    {
        public static string[] BoneName = new string[] {
            "Hips",
            "LeftUpperLeg", "RightUpperLeg",
            "LeftLowerLeg", "RightLowerLeg",
            "LeftFoot", "RightFoot",
            "Spine",
            "Chest",
            "Neck",
            "Head",
            "LeftShoulder", "RightShoulder",
            "LeftUpperArm", "RightUpperArm",
            "LeftLowerArm", "RightLowerArm",
            "LeftHand", "RightHand",
            "LeftToes", "RightToes",
            "LeftEye", "RightEye",
            "Jaw",
            "Left Thumb Proximal", "Left Thumb Intermediate", "Left Thumb Distal",
            "Left Index Proximal", "Left Index Intermediate", "Left Index Distal",
            "Left Middle Proximal", "Left Middle Intermediate", "Left Middle Distal",
            "Left Ring Proximal", "Left Ring Intermediate", "Left Ring Distal",
            "Left Little Proximal", "Left Little Intermediate", "Left Little Distal",
            "Right Thumb Proximal", "Right Thumb Intermediate", "Right Thumb Distal",
            "Right Index Proximal", "Right Index Intermediate", "Right Index Distal",
            "Right Middle Proximal", "Right Middle Intermediate", "Right Middle Distal",
            "Right Ring Proximal", "Right Ring Intermediate", "Right Ring Distal",
            "Right Little Proximal", "Right Little Intermediate", "Right Little Distal",
            "UpperChest",
        };
        public static string[] MuscleName = new string[] {
            "Spine Front-Back", "Spine Left-Right", "Spine Twist Left-Right",
            "Chest Front-Back", "Chest Left-Right", "Chest Twist Left-Right",
            "UpperChest Front-Back", "UpperChest Left-Right", "UpperChest Twist Left-Right",
            "Neck Nod Down-Up", "Neck Tilt Left-Right", "Neck Turn Left-Right",
            "Head Nod Down-Up", "Head Tilt Left-Right", "Head Turn Left-Right",
            "Left Eye Down-Up", "Left Eye In-Out",
            "Right Eye Down-Up", "Right Eye In-Out",
            "Jaw Close", "Jaw Left-Right",
            "Left Upper Leg Front-Back", "Left Upper Leg In-Out", "Left Upper Leg Twist In-Out",
            "Left Lower Leg Stretch", "Left Lower Leg Twist In-Out",
            "Left Foot Up-Down", "Left Foot Twist In-Out", "Left Toes Up-Down",
            "Right Upper Leg Front-Back", "Right Upper Leg In-Out", "Right Upper Leg Twist In-Out",
            "Right Lower Leg Stretch", "Right Lower Leg Twist In-Out",
            "Right Foot Up-Down", "Right Foot Twist In-Out", "Right Toes Up-Down",
            "Left Shoulder Down-Up", "Left Shoulder Front-Back", "Left Arm Down-Up",
            "Left Arm Front-Back", "Left Arm Twist In-Out",
            "Left Forearm Stretch", "Left Forearm Twist In-Out",
            "Left Hand Down-Up", "Left Hand In-Out",
            "Right Shoulder Down-Up", "Right Shoulder Front-Back", "Right Arm Down-Up",
            "Right Arm Front-Back", "Right Arm Twist In-Out",
            "Right Forearm Stretch", "Right Forearm Twist In-Out",
            "Right Hand Down-Up", "Right Hand In-Out",
            "Left Thumb 1 Stretched", "Left Thumb Spread", "Left Thumb 2 Stretched", "Left Thumb 3 Stretched",
            "Left Index 1 Stretched", "Left Index Spread", "Left Index 2 Stretched", "Left Index 3 Stretched",
            "Left Middle 1 Stretched", "Left Middle Spread", "Left Middle 2 Stretched", "Left Middle 3 Stretched",
            "Left Ring 1 Stretched", "Left Ring Spread", "Left Ring 2 Stretched", "Left Ring 3 Stretched",
            "Left Little 1 Stretched", "Left Little Spread", "Left Little 2 Stretched", "Left Little 3 Stretched",
            "Right Thumb 1 Stretched", "Right Thumb Spread", "Right Thumb 2 Stretched", "Right Thumb 3 Stretched",
            "Right Index 1 Stretched", "Right Index Spread", "Right Index 2 Stretched", "Right Index 3 Stretched",
            "Right Middle 1 Stretched", "Right Middle Spread", "Right Middle 2 Stretched", "Right Middle 3 Stretched",
            "Right Ring 1 Stretched", "Right Ring Spread", "Right Ring 2 Stretched", "Right Ring 3 Stretched",
            "Right Little 1 Stretched", "Right Little Spread", "Right Little 2 Stretched", "Right Little 3 Stretched",
        };

        public static int[][] MuscleFromBone = new int[][] {
            new int[] {-1,-1,-1},
            new int[] {23,22,21}, new int[] {31,30,29},
            new int[] {25,-1,24}, new int[] {33,-1,32},
            new int[] {-1,27,26}, new int[] {-1,35,34},
            new int[] {2,1,0},
            new int[] {5,4,3},
            new int[] {11,10,9},
            new int[] {14,13,12},
            new int[] {-1,38,37}, new int[] {-1,47,46},
            new int[] {41,40,39}, new int[] {50,49,48},
            new int[] {43,-1,42}, new int[] {52,-1,51},
            new int[] {-1,45,44}, new int[] {-1,54,53},
            new int[] {-1,-1,28}, new int[] {-1,-1,36},
            new int[] {-1,16,15}, new int[] {-1,18,17},
            new int[] {-1,20,19},
            new int[] {-1,56,55}, new int[] {-1,-1,57}, new int[] {-1,-1,58},
            new int[] {-1,60,59}, new int[] {-1,-1,61}, new int[] {-1,-1,62},
            new int[] {-1,64,63}, new int[] {-1,-1,65}, new int[] {-1,-1,66},
            new int[] {-1,68,67}, new int[] {-1,-1,69}, new int[] {-1,-1,70},
            new int[] {-1,72,71}, new int[] {-1,-1,73}, new int[] {-1,-1,74},
            new int[] {-1,76,75}, new int[] {-1,-1,77}, new int[] {-1,-1,78},
            new int[] {-1,80,79}, new int[] {-1,-1,81}, new int[] {-1,-1,82},
            new int[] {-1,84,83}, new int[] {-1,-1,85}, new int[] {-1,-1,86},
            new int[] {-1,88,87}, new int[] {-1,-1,89}, new int[] {-1,-1,90},
            new int[] {-1,92,91}, new int[] {-1,-1,93}, new int[] {-1,-1,94},
            new int[] {8,7,6},
        };
        public static float[] MuscleDefaultMax = new float[] { // HumanTrait.GetMuscleDefaultMax
            40, 40, 40, 40, 40, 40, 20, 20, 20, 40, 40, 40, 40, 40, 40,
            15, 20, 15, 20, 10, 10,
            50, 60, 60, 80, 90, 50, 30, 50,
            50, 60, 60, 80, 90, 50, 30, 50,
            30, 15, 100, 100, 90, 80, 90, 80, 40,
            30, 15, 100, 100, 90, 80, 90, 80, 40,
            20, 25, 35, 35, 50, 20, 45, 45, 50, 7.5f, 45, 45, 50, 7.5f, 45, 45, 50, 20, 45, 45,
            20, 25, 35, 35, 50, 20, 45, 45, 50, 7.5f, 45, 45, 50, 7.5f, 45, 45, 50, 20, 45, 45,
        };
        public static float[] MuscleDefaultMin = new float[] { // HumanTrait.GetMuscleDefaultMin
            -40,-40,-40,-40,-40,-40,-20,-20,-20,-40,-40,-40,-40,-40,-40,
            -10,-20,-10,-20,-10,-10,
            -90,-60,-60,-80,-90,-50,-30,-50,
            -90,-60,-60,-80,-90,-50,-30,-50,
            -15,-15,-60,-100,-90,-80,-90,-80,-40,
            -15,-15,-60,-100,-90,-80,-90,-80,-40,
            -20,-25,-40,-40,-50,-20,-45,-45,-50,-7.5f,-45,-45,-50,-7.5f,-45,-45,-50,-20,-45,-45,
            -20,-25,-40,-40,-50,-20,-45,-45,-50,-7.5f,-45,-45,-50,-7.5f,-45,-45,-50,-20,-45,-45,
        };

        public static HumanBodyBones[] BoneIndexToMono = new HumanBodyBones[] {
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightToes,
            HumanBodyBones.LeftEye,
            HumanBodyBones.RightEye,
            HumanBodyBones.Jaw,
            HumanBodyBones.LeftThumbProximal,
            HumanBodyBones.LeftThumbIntermediate,
            HumanBodyBones.LeftThumbDistal,
            HumanBodyBones.LeftIndexProximal,
            HumanBodyBones.LeftIndexIntermediate,
            HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal,
            HumanBodyBones.LeftMiddleIntermediate,
            HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal,
            HumanBodyBones.LeftRingIntermediate,
            HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal,
            HumanBodyBones.LeftLittleIntermediate,
            HumanBodyBones.LeftLittleDistal,
            HumanBodyBones.RightThumbProximal,
            HumanBodyBones.RightThumbIntermediate,
            HumanBodyBones.RightThumbDistal,
            HumanBodyBones.RightIndexProximal,
            HumanBodyBones.RightIndexIntermediate,
            HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal,
            HumanBodyBones.RightMiddleIntermediate,
            HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal,
            HumanBodyBones.RightRingIntermediate,
            HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal,
            HumanBodyBones.RightLittleIntermediate,
            HumanBodyBones.RightLittleDistal,
        };

        public static string[] BoneIndexToName => BoneIndexToMono.Select(x => BoneName[(int)x]).ToArray();

        public static int LeftUpperLeg = 1;
        public static int RightUpperLeg = 2;
        public static int LeftLowerLeg = 3;
        public static int RightLowerLeg = 4;

        public static int LeftUpperArm = 13;
        public static int RightUpperArm = 14;
        public static int LeftLowerArm = 15;
        public static int RightLowerArm = 16;

        public static int LeftFoot = 5;
        public static int RightFoot = 6;

        public static int LeftHand = 17;
        public static int RightHand = 18;

        public static int[][] BoneFallbacksIds = new int[][]
        {
            new int[] {54, 8},
            new int[] {8, 7},
            new int[] {9, 10},
            new int[] {11, LeftUpperArm},
            new int[] {12, RightUpperArm},
        };

        public static int[] WeightIndexes = new int[]
        {
            0, 1, 2,
        };

        public static void ApplyBones(HolderNode node)
        {
            if (node.muscles.Length == 0)
                return;

            var BoneFallbacks = BoneFallbacksIds.Select(x => (new float[] { x[0], x[1] }).Concat(WeightIndexes.Select(y => (MuscleFromBone[x[0]][y] == -1 ? 0 : MuscleDefaultMax[MuscleFromBone[x[0]][y]]) / MuscleDefaultMax[MuscleFromBone[x[1]][y]])).ToArray()).ToArray();

            var twistSplits = new Variant[BoneName.Length][];
            Array.Fill(twistSplits, new Variant[3]);
            // I know it looks stupid to do this every frame, because it is
            {
                var nullv = new Variant();

                twistSplits[LeftUpperArm] = [node.twists[0], Quaternion.Identity, nullv];
                twistSplits[LeftLowerArm] = [node.twists[1], Quaternion.Identity, twistSplits[LeftUpperArm][1]];
                twistSplits[LeftHand] = [nullv, nullv, twistSplits[LeftLowerArm][1]];

                twistSplits[RightUpperArm] = [node.twists[0], Quaternion.Identity, nullv];
                twistSplits[RightLowerArm] = [node.twists[1], Quaternion.Identity, twistSplits[RightUpperArm][1]];
                twistSplits[RightHand] = [nullv, nullv, twistSplits[RightLowerArm][1]];

                twistSplits[LeftUpperLeg] = [node.twists[2], Quaternion.Identity, nullv];
                twistSplits[LeftLowerLeg] = [node.twists[3], Quaternion.Identity, twistSplits[LeftUpperLeg][1]];
                twistSplits[LeftHand] = [nullv, nullv, twistSplits[LeftLowerLeg][1]];

                twistSplits[RightUpperLeg] = [node.twists[2], Quaternion.Identity, nullv];
                twistSplits[RightLowerLeg] = [node.twists[3], Quaternion.Identity, twistSplits[RightUpperLeg][1]];
                twistSplits[RightHand] = [nullv, nullv, twistSplits[RightLowerLeg][1]];
            }

            var swingTwists = new float[BoneName.Length][];
            for (int i = 0; i < BoneName.Length; i++)
            {
                swingTwists[i] = new float[3];
                for (int j = 0; j < 3; j++)
                {
                    if (MuscleFromBone[i][j] == -1)
                        swingTwists[i][j] = 0f;
                    else
                        swingTwists[i][j] = node.muscles[MuscleFromBone[i][j]];
                }
            }
            foreach (var item in BoneFallbacks)
            {
                var src = (int)item[0];
                var dst = (int)item[1];
                if (node.boneToNode.ContainsKey(BoneName[src]) && node.humanBoneAxes.ContainsKey(node.boneToNode[BoneName[src]]))
                    continue;
                for (int j = 0; j < 3; j++)
                {
                    swingTwists[dst][j] += swingTwists[src][j] * item[2 + j];
                }
            }
            for (int i = 0; i < BoneName.Length; i++)
            {
                var hName = BoneName[i];
                if (!node.boneToNode.ContainsKey(hName))
                    continue;
                var bone = node.GetNode<HolderNode>(node.boneToNode[hName]);
                if (i == 0)
                {
                    // bone.Position = node.rootBonePosition;
                    // bone.Quaternion = node.rootBoneRotation;
                    // bone.Scale = node.rootBoneScale;
                    continue;
                }
                if (!node.humanBoneAxes.ContainsKey(node.boneToNode[hName]))
                    continue;
                var limits = node.humanBoneAxes[node.boneToNode[hName]];
                int j3 = i;
                var invPostQ = Invert(limits.postQ).Normalized();
                float x = swingTwists[j3][0];
                float y = swingTwists[j3][1];
                float z = swingTwists[j3][2];
                x *= Mathf.DegToRad(x >= 0 ? limits.max.X : -limits.min.X) * limits.sgn.X;
                y *= Mathf.DegToRad(y >= 0 ? limits.max.Y : -limits.min.Y) * limits.sgn.Y;
                z *= Mathf.DegToRad(z >= 0 ? limits.max.Z : -limits.min.Z) * limits.sgn.Z;
                Variant weight = twistSplits[j3][0];
                float weightReal = 1f;
                if (weight.VariantType != Variant.Type.Nil)
                    weightReal = weight.AsSingle();
                Variant pushQ = twistSplits[j3][1];
                Variant popQ = twistSplits[j3][2];
                var preQ = limits.preQ.Normalized();
                // preQ = invPostQ.Inverse().Normalized();
                
                if (popQ.VariantType != Variant.Type.Nil)
                    preQ = popQ.AsQuaternion().Normalized() * preQ;

                var localQ = preQ.Normalized() * SwingTwist(x * weightReal, y, z).Normalized() * invPostQ;
                bone.Quaternion = FlipZ(localQ).Normalized();
            }
        }

        public Quaternion GetMassQ(HolderNode node)
        {
            var leftUpperArm = node.GetNode<HolderNode>(node.boneToNode[BoneName[LeftUpperArm]]);
            var rightUpperArm = node.GetNode<HolderNode>(node.boneToNode[BoneName[RightUpperArm]]);
            var leftUpperLeg = node.GetNode<HolderNode>(node.boneToNode[BoneName[LeftUpperLeg]]);
            var rightUpperLeg = node.GetNode<HolderNode>(node.boneToNode[BoneName[RightUpperLeg]]);
            // var x = rightUpperArm.GlobalPosition + leftUpperArm.GlobalPosition
            return default;
        }

        public static Quaternion Invert(Quaternion q)
        {
            if (BundleReader.flipZ)
                return q.Inverse();
            var dot = q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3];
            var invDot = Mathf.IsZeroApprox(dot) ? 0f : (1f / dot);
            return new Quaternion(-q[0] * invDot, -q[1] * invDot, -q[2] * invDot, q[3] * invDot);
        }

        public static Quaternion FlipZ(Quaternion q)
        {
            if (!BundleReader.flipZ)
                return q;
            // return new Quaternion(-q.Z, -q.Y, q.X, q.W);
            return new Quaternion(q.X, q.Y, -q.Z, -q.W);
        }

        public static Quaternion SwingTwist(float x, float y, float z)
        {
            var yz = float.Hypot(y, z);
            // var yz = Mathf.Sqrt(y*y + z*z);
            var sinc = Mathf.Abs(yz) < 1e-8f ? 0.5f : (Mathf.Sin(yz/2)/yz);
            var swingW = Mathf.Cos(yz/2);
            var twistW = Mathf.Cos(x/2);
            var twistX = Mathf.Sin(x/2);
            return new Quaternion(swingW * twistX, (z * twistX + y * twistW) * sinc, (z * twistW - y * twistX) * sinc, swingW * twistW);
        }
    }
}