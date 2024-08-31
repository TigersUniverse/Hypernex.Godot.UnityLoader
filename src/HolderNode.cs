using System.Collections.Generic;
using System.Linq;
using AssetsTools.NET;
using Godot;
using Godot.Collections;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class HolderNode : Node3D
    {
        [Export]
        public long fileId;
        [Export]
        public long gameObjectFileId;
        public long? parentFileId;
        [Export]
        public Array<Node> components = new Array<Node>();

        public AssetTypeValueField assetField;

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

        public void Setup(BundleReader reader)
        {
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
                AddChild(meshInst);
                if (boneFileIds.Count != 0)
                {
                    Skeleton3D skel = new Skeleton3D();
                    components.Add(skel);
                    AddChild(skel);
                    meshInst.Skeleton = meshInst.GetPathTo(skel);
                    if (rootBoneFileId.HasValue)
                    {
                        var rootNode = reader.GetNodeByComponentId(rootBoneFileId.Value);
                    }
                    foreach (var id in boneFileIds)
                    {
                        var boneNode = reader.GetNodeByComponentId(id);
                        if (!IsInstanceValid(boneNode))
                        {
                            continue;
                        }
                        var xform = boneNode.Transform;
                        int boneIdx = skel.AddBone(boneNode.Name);
                        var boneParent = reader.GetNodeByComponentId(boneNode.parentFileId.GetValueOrDefault());
                        if (boneNode.parentFileId.HasValue && IsInstanceValid(boneParent))
                        {
                            int idx = skel.FindBone(boneParent.Name);
                            if (idx != boneIdx)
                                skel.SetBoneParent(boneIdx, idx);
                        }
                        skel.SetBoneRest(boneIdx, xform);
                        skel.SetBonePose(boneIdx, xform);
                    }
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
        }
    }
}