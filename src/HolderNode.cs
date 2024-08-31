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

        public void Setup()
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
                if (subMeshCount != 0)
                    meshInst.GlobalTransform = Transform3D.Identity;
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
                // if (autoPlayAudio)
                    // player.Play();
                // if (loopAudio)
                //     player.Finished += () => player.Play();
            }
        }

        public override void _Ready()
        {
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