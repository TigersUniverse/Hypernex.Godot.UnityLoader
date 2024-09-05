using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Godot;
using Godot.Collections;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class HolderNode : Node3D
    {
        [Export]
        public bool hasSetup = false;

        [Export]
        public long fileId;
        [Export]
        public long gameObjectFileId;
        public long? parentFileId;
        [Export]
        public Array<Node> components = new Array<Node>();

        public AssetTypeValueField assetField;
        public AssetExternal assetTransformField;
        public AssetExternal assetAnimatorField;

        [Export]
        public Mesh mesh;
        [Export]
        public bool shapeEnabled = true;
        [Export]
        public bool shapeTrigger = false;
        [Export]
        public Shape3D shape;
        [Export]
        public Vector3 shapeCenter = Vector3.Zero;
        [Export]
        public ushort firstSubmesh = 0;
        [Export]
        public ushort subMeshCount = 0;
        [Export]
        public Array<Material> materials = new Array<Material>();
        public long? rootBoneFileId = null;
        [Export]
        public int rootBoneIndex = -1;
        [Export]
        public Array<long> boneFileIds = new Array<long>();
        [Export]
        public Dictionary<NodePath, int> skeletonBoneMap = new Dictionary<NodePath, int>();

        [Export]
        public AudioStream audioStream;
        [Export]
        public bool autoPlayAudio = false;
        [Export]
        public bool loopAudio = false;
        [Export]
        public AudioStreamPlayer3D.AttenuationModelEnum attenuation;
        [Export]
        public float audioMaxDistance;
        [Export]
        public float audioVolumeDb;
        [Export]
        public float audioPan;

        [Export]
        public Dictionary<string, NodePath> boneToNode = new Dictionary<string, NodePath>();
        [Export]
        public Dictionary<NodePath, Axes> humanBoneAxes = new Dictionary<NodePath, Axes>();
        [Export]
        public float[] muscles = new float[0];
        [Export]
        public float[] twists = new float[0];
        [Export]
        public float avatarScale = 1f;

        public HolderNode Parent => GetParentOrNull<HolderNode>();

        public void GetTransformPath(Node3D src, NodePath path, Array<Transform3D> xforms)
        {
            if (path.GetNameCount() <= 0)
                return;
            string part = path.GetName(0);
            var n = src.GetNodeOrNull<Node3D>(part);
            if (IsInstanceValid(n) && path.GetNameCount() > 1)
            {
                if (n.IsAncestorOf(src))
                    xforms.Add(src.Transform.AffineInverse());
                else
                    xforms.Add(src.Transform);
                string[] paths = new string[path.GetNameCount() - 1];
                for (int i = 0; i < paths.Length; i++)
                {
                    paths[i] = path.GetName(i+1);
                }
                string path2 = string.Join('/', paths);
                GetTransformPath(n, path2, xforms);
            }
            // else
            //     xforms.Add(src.Transform.AffineInverse());
        }

        public void GetTransformGlobal(Node3D src, Array<Transform3D> xforms)
        {
            xforms.Add(src.Transform);
            var n = src.GetParentOrNull<Node3D>();
            if (IsInstanceValid(n))
            {
                GetTransformGlobal(n, xforms);
            }
        }

        public Transform3D GetTransformGlobal(Node3D src)
        {
            // A
            // B
            // C
            var xforms = new Array<Transform3D>();
            GetTransformGlobal(src, xforms);
            // xforms.Reverse();
            Transform3D ret = xforms[0];
            for (int i = 1; i < xforms.Count; i++)
            {
                ret = xforms[i] * ret;
            }
            return ret;
        }

        public Transform3D GetTransformPath(Node3D src, Node3D dst)
        {
            var xforms = new Array<Transform3D>();
            GetTransformPath(src, src.GetPathTo(dst), xforms);
            xforms.Reverse();
            Transform3D ret = Transform3D.Identity;
            for (int i = 0; i < xforms.Count; i++)
            {
                ret = xforms[i] * ret;
            }
            return ret;
        }

        public void Setup(BundleReader reader)
        {
            if (hasSetup)
                return;
            hasSetup = true;
            if (IsInstanceValid(mesh))
            {
                MeshInstance3D meshInst = new MeshInstance3D();
                meshInst.Mesh = mesh;
                if (subMeshCount == 0)
                {
                    if (materials.Count != 0)
                    {
                        for (int i = 0; i < mesh.GetSurfaceCount(); i++)
                        {
                            meshInst.SetSurfaceOverrideMaterial(i, materials[i % materials.Count]);
                        }
                    }
                }
                else
                {
                    SurfaceTool st = new SurfaceTool();
                    ArrayMesh final = null;
                    for (int i = 0; i < subMeshCount; i++)
                    {
                        st.CreateFrom(mesh, i + firstSubmesh);
                        final = st.Commit(final);
                    }
                    meshInst.Mesh = final;
                    if (materials.Count != 0)
                        for (int i = 0; i < subMeshCount; i++)
                            meshInst.SetSurfaceOverrideMaterial(i, materials[i % materials.Count]);
                }
                components.Add(meshInst);
                if (boneFileIds.Count != 0)
                {
                    Skeleton3D skel = new Skeleton3D();
                    components.Add(skel);
                    // AddChild(skel, true);
                    // skel.AddChild(meshInst, true);
                    Parent.Setup(reader);
                    skel.MotionScale = Parent.avatarScale;
                    int rootBone = -1;
                    Axes rootAxes = null;
                    foreach (var kvp in Parent.humanBoneAxes)
                    {
                        var bone = Parent.GetNode<HolderNode>(kvp.Key);
                        if (IsInstanceValid(bone) && kvp.Value.humanBoneName == HumanTrait.BoneName[0])
                        {
                            rootAxes = kvp.Value;
                            break;
                        }
                    }
                    if (!rootBoneFileId.HasValue)
                    {
                        // var boneNode = reader.GetNodeByTransformComponentId(rootBoneFileId.Value);
                        skel.AddBone("Root");
                        var boneNode = Parent;
                        if (IsInstanceValid(boneNode))
                        {
                            var xform = GetTransformGlobal(this).AffineInverse() * GetTransformGlobal(boneNode);
                            if (IsInstanceValid(rootAxes))
                                xform = xform.Scaled(rootAxes.scale);
                            skel.SetBoneRest(0, xform);
                            skel.SetBoneEnabled(0, false);
                        }
                    }
                    if (IsInstanceValid(rootAxes))
                        meshInst.Scale = Vector3.One / rootAxes.scale;
                    // skel.SetBoneRest(0, scl.Scaled(Vector3.One / Scale));
                    /*
                    // Scale = Vector3.One;
                    // Transform = Transform3D.Identity;
                    // meshInst.Basis = Basis;
                    if (IsInstanceValid(rootAxes))
                    {
                        // Transform = rootAxes.xform;
                        meshInst.Transform = rootAxes.xform.Inverse();
                        skel.Transform = rootAxes.xform;
                        // Basis = Basis.Identity;
                        // meshInst.Transform = Transform3D.Identity;
                    }
                    skel.Scale = Vector3.One / scl;
                    meshInst.Scale = scl;
                    */

                    skeletonBoneMap.Clear();
                    foreach (var id in boneFileIds)
                    {
                        var boneNode = reader.GetNodeByTransformComponentId(id);
                        if (!IsInstanceValid(boneNode))
                        {
                            continue;
                        }
                        // if (rootBoneFileId.HasValue && !reader.GetNodeByTransformComponentId(rootBoneFileId.Value).IsAncestorOf(boneNode) && reader.GetNodeByTransformComponentId(rootBoneFileId.Value) != boneNode)
                        //     continue;
                        var xform = boneNode.Transform;
                        string bonename = boneNode.Name;
                        if (Parent.humanBoneAxes.ContainsKey(Parent.GetPathTo(boneNode)))
                            bonename = Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].humanBoneName;
                        int boneIdx = skel.AddBone(boneNode.Name);
                        var path = GetPathTo(boneNode);
                        skeletonBoneMap.Add(path, boneIdx);
                        var boneParent = reader.GetNodeByTransformComponentId(boneNode.parentFileId.GetValueOrDefault());
                        if (boneNode.parentFileId.HasValue && IsInstanceValid(boneParent))
                        {
                            int idx = skel.FindBone(boneParent.Name);
                            if (idx != boneIdx)
                            {
                                if (idx == -1)
                                    idx = 0;
                                skel.SetBoneParent(boneIdx, idx);
                            }
                        }
                        if (rootBoneFileId.HasValue && id == rootBoneFileId.Value)
                        {
                            xform = GetTransformGlobal(this).AffineInverse() * GetTransformGlobal(boneNode);
                            if (IsInstanceValid(rootAxes))
                                xform = xform.Scaled(rootAxes.scale);
                            rootBone = boneIdx;
                            skel.SetBoneEnabled(boneIdx, false);
                        }
                        skel.SetBoneRest(boneIdx, xform);
                        skel.SetBonePose(boneIdx, xform);
                    }
                    rootBoneIndex = rootBone;

                    AddChild(skel, true);
                    skel.AddChild(meshInst, true);
                    meshInst.Skin = skel.CreateSkinFromRestTransforms();
                    meshInst.Skeleton = meshInst.GetPathTo(skel);
                    // skel.ShowRestOnly = true; // debug
                }
                else
                {
                    AddChild(meshInst);
                }
            }
            if (IsInstanceValid(shape))
            {
                CollisionObject3D body = shapeTrigger ? new Area3D() : new StaticBody3D();
                CollisionShape3D collision = new CollisionShape3D();
                collision.Shape = shape;
                components.Add(body);
                AddChild(body);
                components.Add(collision);
                body.AddChild(collision);
                collision.Position = shapeCenter;
                collision.Disabled = !shapeEnabled;
            }
            if (IsInstanceValid(audioStream))
            {
                AudioStreamPlayer3D player = new AudioStreamPlayer3D();
                components.Add(player);
                AddChild(player);
                player.AttenuationFilterDb = 0f;
                player.AttenuationModel = attenuation;
                player.VolumeDb = audioVolumeDb;
                player.MaxDistance = audioMaxDistance;
                player.PanningStrength = audioPan;
                player.Stream = audioStream;
            }
            if (assetAnimatorField.info != null)
            {
                // GD.Print("Animator found!");
                muscles = new float[HumanTrait.MuscleName.Length];
                AnimationPlayer tree = reader.GetAnimPlayer(assetAnimatorField.file, assetAnimatorField.baseField, this);
                components.Add(tree);
                AddChild(tree);
                /*
                Skeleton3D skel = new Skeleton3D();
                Dictionary<HolderNode, int> indexes = new Dictionary<HolderNode, int>();
                foreach (var boneKvp in hashToNode)
                {
                    if (!IsInstanceValid(boneKvp.Value))
                        continue;
                    int idx = skel.AddBone(boneKvp.Value.Name + "_" + GetPathTo(boneKvp.Value).GetNameCount());
                    indexes.Add(boneKvp.Value, idx);
                    if (IsInstanceValid(boneKvp.Value.Parent) && indexes.ContainsKey(boneKvp.Value.Parent))
                        skel.SetBoneParent(idx, indexes[boneKvp.Value.Parent]);
                }
                foreach (var kvp in humanBoneAxes)
                {
                    var bone = GetNode<HolderNode>(kvp.Key);
                    if (indexes.ContainsKey(bone) && !string.IsNullOrEmpty(kvp.Value.humanBoneName))
                    {
                        skel.SetBoneName(indexes[bone], kvp.Value.humanBoneName);
                    }
                }
                components.Add(skel);
                AddChild(skel);
                */
            }
        }

        public T GetComponent<T>() where T : Node
        {
            return components.FirstOrDefault(x => x is T) as T;
        }

        public HolderNode FindChildTransform(string name)
        {
            return GetChildren().FirstOrDefault(x => x is HolderNode && x.Name == name.ValidateNodeName()) as HolderNode;
        }

        public HolderNode FindChildTransform(string name, string parentName)
        {
            return FindChildren("*", owned: false).FirstOrDefault(x => x is HolderNode && x.Name == name.ValidateNodeName() && x.GetParent().Name == parentName.ValidateNodeName()) as HolderNode;
        }

        public override void _EnterTree()
        {
            var skel = GetComponent<Skeleton3D>();
            // if (IsInstanceValid(skel))
            //     skel.ResetBonePoses();
        }

        public override void _Ready()
        {
            foreach (MeshInstance3D comp in components.Where(x => x is MeshInstance3D))
            {
                if (subMeshCount != 0)
                    comp.GlobalTransform = Transform3D.Identity;
            }
            foreach (AudioStreamPlayer3D comp in components.Where(x => x is AudioStreamPlayer3D))
            {
                if (autoPlayAudio)
                    comp.Play();
                if (loopAudio)
                    comp.Finished += () => comp.Play();
            }
            var anim = GetComponent<AnimationPlayer>();
            if (IsInstanceValid(anim))
                anim.Play(anim.GetAnimationList().First());

            // GD.PrintS(GlobalTransform.Origin, GetTransformGlobal(this).Origin);

            return;
            // /*
            var im = new MeshInstance3D();
            im.Mesh = new SphereMesh()
            {
                Radius = 0.025f,
                Height = 0.05f
            };
            AddChild(im);
            im.GlobalBasis = Basis.Identity;
            // */
        }

        public override void _Process(double delta)
        {
            var anim = GetComponent<AnimationPlayer>();
            if (IsInstanceValid(anim) && !anim.IsPlaying())
            {
                // anim.Play(anim.GetAnimationList().First());
                // GD.PrintS(Name, "[", string.Join(", ", muscles), "]");
            }
            var skel = GetComponent<Skeleton3D>();
            if (IsInstanceValid(anim))
            {
                // System.Array.Fill(muscles, 0f);
                HumanTrait.ApplyBones(this);
            }
            if (IsInstanceValid(skel))
            {
                if (IsInstanceValid(anim))
                {
                    // for (int i = 0; i < skel.GetBoneCount(); i++)
                    {
                        // var gxform = skel.GlobalTransform * skel.GetBoneGlobalPose(i);
                        foreach (var kvp in humanBoneAxes)
                        {
                            break;
                            var bone = GetNode<HolderNode>(kvp.Key);
                            int idx = skel.FindBone(kvp.Value.humanBoneName);
                            if (idx != -1)
                            {
                                var xform = skel.GetBonePose(idx);
                                bone.Transform = xform;
                                // var gxform = skel.GlobalTransform * skel.GetBoneGlobalPose(idx);
                                // bone.GlobalTransform = gxform;
                            }
                        }
                    }
                }
                else
                {
                    skel.ResetBonePoses();
                    foreach (var kvp in skeletonBoneMap)
                    {
                        // break;
                        var node = GetNode<HolderNode>(kvp.Key);
                        int par = skel.GetBoneParent(kvp.Value);
                        var xform = node.Transform;
                        skel.SetBonePose(kvp.Value, xform);
                        var gxform = skel.GlobalTransform.Inverse() * node.GlobalTransform;
                        // skel.SetBoneGlobalPose(kvp.Value, gxform);
                    }
                    // skel.ResetBonePoses();
                }
            }
        }
    }
}