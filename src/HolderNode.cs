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

        public HolderNode Parent => GetParent<HolderNode>();

        public Transform3D GetTransformSkeleton(HolderNode node)
        {
            if (node.GetParent() is not HolderNode)
                return Transform3D.Identity;
            if (node.Parent == this)
                return Transform3D.Identity;
            return node.Transform * GetTransformSkeleton(node.Parent);
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
                    AddChild(skel);
                    skel.AddChild(meshInst);
                    // skel.Scale = Vector3.One * Parent.avatarScale;
                    // skel.Basis = Transform.Inverse().Basis;
                    Parent.Setup(reader);
                    skel.MotionScale = Parent.avatarScale;
                    GD.PrintErr(Name + " " + Parent.avatarScale + " " + avatarScale);
                    // meshInst.Basis = Transform.Basis;//.Scaled(Vector3.One * Parent.avatarScale);
                    // meshInst.Scale = Vector3.One / Parent.avatarScale;
                    int rootBone = -1;
                    Axes rootAxes = null;
                    foreach (var kvp in Parent.humanBoneAxes)
                    {
                        var bone = Parent.GetNode<HolderNode>(kvp.Key);
                        // bone.Transform = kvp.Value.xform;
                        if (kvp.Value.humanBoneName == HumanTrait.BoneName[0])
                        {
                            rootAxes = kvp.Value;
                            var rot = kvp.Value.rotation;
                            rot = Quaternion.Identity;
                            // bone.Basis = bone.Basis.Scaled(new Vector3(1f, 1f, -1f) * kvp.Value.scale);
                            bone.ForceUpdateTransform();
                            skel.ForceUpdateTransform();
                            meshInst.ForceUpdateTransform();
                        }
                    }

                    skeletonBoneMap.Clear();
                    if (rootBoneFileId.HasValue)
                    {
                        var rootNode = reader.GetNodeByTransformComponentId(rootBoneFileId.Value);
                        // int boneIdx = skel.AddBone(rootNode.Name);
                        var path = Parent.GetPathTo(rootNode);
                        GD.PrintS("Root", path);
                        // rootNode.Scale *= Vector3.One * Parent.avatarScale;
                        // rootNode.ForceUpdateTransform();
                        // skeletonBoneMap.Add(path, boneIdx);
                        if (Parent.humanBoneAxes.ContainsKey(path))
                        {
                            // rootNode.Transform = Parent.humanBoneAxes[path].xform;
                            GD.PrintS("Root", Parent.humanBoneAxes[path].scale);
                        }
                        // skel.Reparent(rootNode);
                    }
                    foreach (var id in boneFileIds)
                    {
                        // if (rootBoneFileId.HasValue && id == rootBoneFileId.Value)
                        //     continue;
                        var boneNode = reader.GetNodeByTransformComponentId(id);
                        if (!IsInstanceValid(boneNode))
                        {
                            continue;
                        }
                        var path2 = Parent.GetPathTo(boneNode);
                        if (Parent.humanBoneAxes.ContainsKey(path2))
                        {
                            // if (Parent.humanBoneAxes[path2].humanBoneName == HumanTrait.BoneName[0])
                            //     continue;
                            // if (Parent.humanBoneAxes[path2].humanBoneName == HumanTrait.BoneName[7])
                            //     continue;
                        }
                        var xform = boneNode.Transform;
                        string bonename = boneNode.Name;
                        if (Parent.humanBoneAxes.ContainsKey(Parent.GetPathTo(boneNode)))
                            bonename = Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].humanBoneName;
                        // xform = GetTransformSkeleton(boneNode);
                        int boneIdx = skel.AddBone(boneNode.Name);
                        // skel.SetBoneParent(boneIdx, rootBone);
                        var path = GetPathTo(boneNode);
                        skeletonBoneMap.Add(path, boneIdx);
                        var boneParent = reader.GetNodeByTransformComponentId(boneNode.parentFileId.GetValueOrDefault());
                        if (boneNode.parentFileId.HasValue && IsInstanceValid(boneParent))
                        {
                            int idx = skel.FindBone(boneParent.Name);
                            if (idx != boneIdx)
                            {
                                // if (idx == -1)
                                //     idx = rootBone;
                                skel.SetBoneParent(boneIdx, idx);
                            }
                        }
                        // GD.Print($"{skel.GetBoneName(boneIdx)} {skel.GetBoneParent(boneIdx)}");
                        // xform.Origin *= Vector3.One * Parent.avatarScale;
                        if (rootBoneFileId.HasValue && id == rootBoneFileId.Value)
                        {
                            if (IsInstanceValid(rootAxes))
                                xform = rootAxes.xform;
                            rootBone = boneIdx;
                            // skel.SetBoneEnabled(boneIdx, false);
                            // continue;
                        }
                        skel.SetBoneRest(boneIdx, xform);
                        // skel.SetBoneRest(boneIdx, Transform3D.Identity);
                        // skel.SetBoneRest(boneIdx, xform.Inverse());
                        if (Parent.humanBoneAxes.ContainsKey(Parent.GetPathTo(boneNode)))
                        {
                            // skel.SetBoneName(boneIdx, Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].humanBoneName);
                            // boneNode.Transform = Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].xform;
                            // skel.SetBoneRest(boneIdx, Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].xform);
                        }
                        else
                        {
                            // GD.PrintErr(Parent.GetPathTo(boneNode));
                        }
                        // skel.SetBonePose(boneIdx, xform);
                        /*
                        var bone = new BoneAttachment3D();
                        boneNode.AddChild(bone);
                        bone.SetUseExternalSkeleton(true);
                        bone.SetExternalSkeleton(bone.GetPathTo(skel));
                        bone.BoneIdx = boneIdx;
                        bone.Transform = xform;
                        bone.OverridePose = true;
                        */
                    }
                    // GD.PrintErr(string.Join(", ", Parent.humanBoneAxes.Keys));
                    // skel.ResetBonePoses();
                    // skel.LocalizeRests();
                    // skel.ForceUpdateAllBoneTransforms();
                    for (int i = 0; i < skel.GetBoneCount(); i++)
                        skel.ForceUpdateBoneChildTransform(i);
                    skel.ForceUpdateTransform();
                    meshInst.ForceUpdateTransform();
                    meshInst.Skeleton = meshInst.GetPathTo(skel);
                    meshInst.Skin = skel.CreateSkinFromRestTransforms();
                    // skel.SetBonePose(rootBone, Transform3D.Identity);
                    foreach (var kvp in Parent.humanBoneAxes)
                    {
                        var bone = Parent.GetNode<HolderNode>(kvp.Key);
                        if (kvp.Value.humanBoneName == HumanTrait.BoneName[0])
                        {
                            // skel.Scale = Vector3.One * Parent.avatarScale;
                            // bone.Scale = Vector3.One * Parent.avatarScale;
                        }
                    }
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

            /*
            var im = new MeshInstance3D();
            im.Mesh = new SphereMesh()
            {
                Radius = 0.025f,
                Height = 0.05f
            };
            AddChild(im);
            im.GlobalBasis = Basis.Identity;
            */
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
                // skel.ResetBonePoses();
                foreach (var kvp in skeletonBoneMap)
                {
                    // break;
                    var node = GetNode<HolderNode>(kvp.Key);
                    int par = skel.GetBoneParent(kvp.Value);
                    var xform = node.Transform;
                    // skel.SetBonePose(kvp.Value, xform);
                    var gxform = skel.GlobalTransform.Inverse() * node.GlobalTransform;
                    // gxform.Basis = gxform.Basis.Scaled(Vector3.One / gxform.Basis.Scale);
                    skel.SetBoneGlobalPose(kvp.Value, gxform);
                    // GetNode<HolderNode>(kvp.Key).Transform = skel.GetBonePose(kvp.Value);
                    // skel.SetBonePosePosition(kvp.Value, xform.Origin);
                    // skel.SetBonePoseRotation(kvp.Value, xform.Basis.GetRotationQuaternion());
                }
                // skel.ResetBonePoses();
            }
        }
    }
}