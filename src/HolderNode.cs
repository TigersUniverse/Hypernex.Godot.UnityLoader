using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Godot;
using Godot.Collections;
using Hypernex.Game;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class HolderNode : Node3D, IEntity
    {
        [Export]
        public bool hasSetup = false;

        [Export]
        public long fileId;
        [Export]
        public long gameObjectFileId;
        public long? parentFileId;
        [Export]
        public Array<NodePath> components;

        public AssetTypeValueField assetField;
        public AssetExternal assetTransformField;
        public AssetExternal assetAnimatorField;

        [Export]
        public Mesh mesh;
        [Export]
        public Array<MeshInfo> meshes = new Array<MeshInfo>();

        [Export]
        public bool hasRigidbody = false;
        [Export]
        public float rigidbodyMass = 1f;
        [Export]
        public Array<ShapeInfo> shapes = new Array<ShapeInfo>();

        [Export]
        public Array<AudioInfo> audios = new Array<AudioInfo>();

        public long? rootBoneFileId = null;
        [Export]
        public int rootBoneIndex = -1;
        [Export]
        public Array<NodePath> rootBonePaths = new Array<NodePath>();
        [Export]
        public Array<long> boneFileIds = new Array<long>();
        [Export]
        public Array<Transform3D> bindPoses = new Array<Transform3D>();
        [Export]
        public Dictionary<NodePath, int> skeletonBoneMap = new Dictionary<NodePath, int>();

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

        public bool Enabled
        {
            get => CanProcess();
            set
            {
                Visible = value;
                ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            }
        }

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

        public static void GetTransformGlobal(Node3D src, Array<Transform3D> xforms)
        {
            xforms.Add(src.Transform);
            var n = src.GetParentOrNull<Node3D>();
            if (IsInstanceValid(n))
            {
                GetTransformGlobal(n, xforms);
            }
        }

        public static Transform3D GetTransformGlobal(Node3D src)
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
            foreach (var kvp in humanBoneAxes)
            {
                var bone = GetNodeOrNull<Node3D>(kvp.Key);
                bone.Transform = kvp.Value.xform;
            }
            foreach (var info in meshes)
            {
                Mesh m = IsInstanceValid(info.mesh) ? info.mesh : mesh;
                if (!IsInstanceValid(m))
                    continue;
                MeshInstance3D meshInst = new MeshInstance3D();
                meshInst.CastShadow = info.shadows;
                meshInst.Visible = info.visible;
                meshInst.Mesh = m;
                if (info.subMeshCount == 0)
                {
                    if (info.materials.Count != 0)
                    {
                        for (int i = 0; i < m.GetSurfaceCount(); i++)
                        {
                            meshInst.SetSurfaceOverrideMaterial(i, info.materials[i % info.materials.Count]);
                        }
                    }
                }
                else
                {
                    SurfaceTool st = new SurfaceTool();
                    ArrayMesh final = null;
                    for (int i = 0; i < info.subMeshCount; i++)
                    {
                        st.CreateFrom(mesh, i + info.firstSubmesh);
                        final = st.Commit(final);
                    }
                    meshInst.Mesh = final;
                    if (info.materials.Count != 0)
                        for (int i = 0; i < info.subMeshCount; i++)
                            meshInst.SetSurfaceOverrideMaterial(i, info.materials[i % info.materials.Count]);
                }
                meshInst.SetMeta("mesh_static", info.subMeshCount != 0);
                // components.Add(meshInst);
                if (rootBoneFileId.HasValue)
                    Parent.rootBonePaths.Add(Parent.GetPathTo(reader.GetNodeByTransformComponentId(rootBoneFileId.Value)));
                if (boneFileIds.Count != 0)
                {
                    Skeleton3D skel = new Skeleton3D();
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
                            // meshInst.Transform = GetTransformGlobal(this).AffineInverse() * GetTransformGlobal(bone);
                            break;
                        }
                    }
                    if (!rootBoneFileId.HasValue)
                    {
                        skel.AddBone("Root");
                        var boneNode = Parent;
                        if (IsInstanceValid(boneNode))
                        {
                            var xform = GetTransformGlobal(this).AffineInverse() * GetTransformGlobal(boneNode);
                            if (IsInstanceValid(rootAxes))
                                xform = xform.Scaled(rootAxes.scale);
                            skel.SetBoneRest(0, xform);
                        }
                    }

                    Dictionary<string, string> renames = new Dictionary<string, string>();
                    skeletonBoneMap.Clear();
                    for (int i = 0; i < boneFileIds.Count; i++)
                    {
                        var id = boneFileIds[i];
                        var boneNode = reader.GetNodeByTransformComponentId(id);
                        if (!IsInstanceValid(boneNode))
                        {
                            continue;
                        }
                        if (rootBoneFileId.HasValue && !reader.GetNodeByTransformComponentId(rootBoneFileId.Value).IsAncestorOf(boneNode) && reader.GetNodeByTransformComponentId(rootBoneFileId.Value) != boneNode)
                            continue;
                        var xform = boneNode.Transform;
                        var pose = xform;
                        string bonename = string.Empty;
                        if (Parent.humanBoneAxes.ContainsKey(Parent.GetPathTo(boneNode)))
                        {
                            bonename = Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].humanBoneName;
                            pose = Parent.humanBoneAxes[Parent.GetPathTo(boneNode)].xform;
                        }
                        int boneIdx = skel.AddBone(boneNode.Name);
                        if (!string.IsNullOrEmpty(bonename))
                            renames.Add(boneNode.Name, bonename);
                        var path = GetPathTo(boneNode);
                        skeletonBoneMap.Add(path, boneIdx);
                        var boneParent = reader.GetNodeByTransformComponentId(boneNode.parentFileId.GetValueOrDefault());
                        if (boneNode.parentFileId.HasValue && IsInstanceValid(boneParent))
                        {
                            int idx = skel.FindBone(boneParent.Name);
                            if (idx != boneIdx)
                            {
                                if (idx == -1 && !rootBoneFileId.HasValue)
                                    idx = 0;
                                skel.SetBoneParent(boneIdx, idx);
                                boneIdx = skel.FindBone(boneNode.Name);
                                skeletonBoneMap[path] = boneIdx;
                            }
                        }
                        if (rootBoneFileId.HasValue && id == rootBoneFileId.Value)
                        {
                            rootBone = boneIdx;
                        }

                        var binds = new Array<Transform3D>();
                        var curNode = boneNode;
                        while (true)
                        {
                            var idx = boneFileIds.IndexOf(curNode.fileId);
                            if (idx == -1)
                                break;
                            if (idx >= bindPoses.Count)
                                break;
                            binds.Add(bindPoses[idx]);
                            if (!IsInstanceValid(curNode.Parent))
                                break;
                            curNode = curNode.Parent;
                        }
                        if (binds.Count > 0)
                        {
                            var bind = binds[0].AffineInverse();
                            if (binds.Count > 1)
                            {
                                bind = (binds[0] * binds[1].AffineInverse()).AffineInverse();
                            }
                            if (BundleReader.flipZ)
                                xform = Transform3D.FlipZ * bind * Transform3D.FlipZ;
                            else
                                xform = bind;
                        }
                        skel.SetBoneRest(boneIdx, xform);
                        skel.SetBonePose(boneIdx, xform);
                    }
                    rootBoneIndex = rootBone;

                    foreach (var kvp in renames)
                    {
                        if (skel.FindBone(kvp.Key) != -1 && skel.FindBone(kvp.Value) == -1)
                            skel.SetBoneName(skel.FindBone(kvp.Key), kvp.Value);
                    }

                    AddChild(skel, true);
                    skel.AddChild(meshInst, true);
                    meshInst.Skin = skel.CreateSkinFromRestTransforms();
                    meshInst.Skeleton = meshInst.GetPathTo(skel);
                    if (IsInstanceValid(Parent.GetComponent<AnimationPlayer>()))
                    {
                        AddComponent(skel);
                        // components.Add(skel);
                        // Parent.components.Add(skel);
                    }
                    else
                    {
                        AddComponent(skel);
                        // components.Add(skel);
                    }
                    // skel.ShowRestOnly = true; // debug
                }
                else
                {
                    AddChild(meshInst);
                }
                AddComponent(meshInst);
            }
            if (hasRigidbody)
            {
                // components.Add(new RigidBody3D());
                this.AddNewComponent<RigidBody3D>();
            }
            foreach (var info in shapes)
            {
                CollisionObject3D body = info.shapeTrigger ? new Area3D() : hasRigidbody ? this.GetComponent<RigidBody3D>() : new StaticBody3D();
                CollisionShape3D collision = new CollisionShape3D();
                collision.Shape = info.shape;
                // components.Add(body);
                if (!hasRigidbody)
                {
                    AddChild(body);
                    AddComponent(body);
                }
                // components.Add(collision);
                body.AddChild(collision);
                AddComponent(collision);
                collision.Position = info.shapeCenter;
                collision.Disabled = !info.shapeEnabled;
            }
            foreach (var info in audios)
            {
                AudioStreamPlayer3D player = new AudioStreamPlayer3D();
                // components.Add(player);
                AddChild(player);
                AddComponent(player);
                player.AttenuationFilterDb = 0f;
                player.AttenuationModel = info.attenuation;
                player.VolumeDb = info.audioVolumeDb;
                player.MaxDistance = info.audioMaxDistance;
                player.PanningStrength = info.audioPan;
                player.Stream = info.audioStream;
                player.Autoplay = info.autoPlayAudio;
                player.SetMeta("audio_loop", info.loopAudio);
            }
            if (assetAnimatorField.info != null)
            {
                muscles = new float[HumanTrait.MuscleName.Length];
                AnimationPlayer tree = reader.GetAnimPlayer(assetAnimatorField.file, assetAnimatorField.baseField, this);
                // components.Add(tree);
                AddChild(tree);
                AddComponent(tree);
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
                foreach (var kvp in boneToNode)
                {
                    var bone = GetNode<HolderNode>(kvp.Value);
                    if (indexes.ContainsKey(bone) && !string.IsNullOrEmpty(kvp.Key))
                    {
                        skel.SetBoneName(indexes[bone], kvp.Key);
                    }
                }
                components.Add(skel);
                AddChild(skel);
                */
            }
        }

        public Node AddComponent(Node value)
        {
            components ??= new Array<NodePath>();
            components.Add(GetPathTo(value));
            value.SetMeta(IEntity.TypeName, value.GetPathTo(this));
            return value;
        }

        public Node GetComponent(System.Type type)
        {
            if (components == null)
                return null;
            return components.Select(x => GetNodeOrNull(x)).FirstOrDefault(x => IsInstanceValid(x) && (x.GetType() == type || type.IsAssignableFrom(x.GetType())));
        }

        public Node[] GetComponents(System.Type type)
        {
            if (components == null)
                return System.Array.Empty<Node>();
            return components.Select(x => GetNodeOrNull(x)).Where(x => IsInstanceValid(x) && (x.GetType() == type || type.IsAssignableFrom(x.GetType()))).ToArray();
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
            var rb = this.GetComponent<RigidBody3D>();
            if (IsInstanceValid(rb))
            {
                rb.TopLevel = true;
                rb.GlobalTransform = GlobalTransform;
                AddChild(new PhysNode());
            }
        }

        public override void _Ready()
        {
            foreach (MeshInstance3D comp in this.GetComponents<MeshInstance3D>())
            {
                if (comp.HasMeta("mesh_static") && comp.GetMeta("mesh_static").AsBool())
                    comp.GlobalTransform = Transform3D.Identity;
            }
            foreach (AudioStreamPlayer3D comp in this.GetComponents<AudioStreamPlayer3D>())
            {
                if (comp.Autoplay)
                    comp.Play();
                if (comp.HasMeta("audio_loop") && comp.GetMeta("audio_loop").AsBool())
                    comp.Finished += () => comp.Play();
            }
            var anim = this.GetComponent<AnimationPlayer>();
            if (IsInstanceValid(anim) || this.GetComponents<Skeleton3D>().Length != 0 || rootBonePaths.Count != 0)
            {
                AddChild(new SkinnedNode());
            }
            foreach (var probe in this.GetComponents<ReflectionProbe>())
            {
                probe.GlobalBasis = Basis.Identity;
            }

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
    }
}