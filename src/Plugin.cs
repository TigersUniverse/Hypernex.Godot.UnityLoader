using System.Collections.Generic;
using Godot;
using Hypernex.CCK;
using Hypernex.CCK.GodotVersion;
using Hypernex.CCK.GodotVersion.Classes;

namespace Hypernex.GodotVersion.UnityLoader
{
    public class Plugin : HypernexPlugin
    {
        public override string PluginName => "UnitySceneProvider";

        public override string PluginCreator => "TigersUniverse";

        public override string PluginVersion => "0.0.0.0";

        public override void OnPluginLoaded()
        {
            Init.WorldProvider = UnitySceneProvider;
            // Init.AvatarProvider = UnitySceneProvider;
        }

        public ISceneProvider UnitySceneProvider()
        {
            BundleReader reader = new BundleReader();
            reader.typeMappings = (_, typeName, node, manager, fileInst, component) =>
            {
                switch (typeName)
                {
                    case "Hypernex.CCK.Unity.World":
                    {
                        var world = new WorldDescriptor();
                        List<NodePath> paths = new List<NodePath>();
                        foreach (var spawn in component["SpawnPoints.Array"])
                        {
                            Node other = reader.GetNodeById(spawn["m_PathID"].AsLong);
                            paths.Add("../" + node.GetPathTo(other));
                        }
                        world.StartPositions = paths.ToArray();
                        return world;
                    }
                    case "Hypernex.CCK.Unity.Avatar":
                    {
                        var avi = new AvatarDescriptor();
                        foreach (var anims in component["Animators.Array"])
                        {
                            var animCtrl = manager.GetExtAsset(fileInst, anims["AnimatorController"]);
                            if (animCtrl.info == null)
                                continue;
                            var library = reader.GetAnimationLibraryUseExistingTOS(animCtrl, node);
                            if (node.GetComponent<AnimationPlayer>() != null)
                            {
                                node.GetComponent<AnimationPlayer>().AddAnimationLibrary(animCtrl.baseField["m_Name"].AsString, library);
                            }
                        }
                        var skel = node.GetComponent<Skeleton3D>();
                        if (skel == null)
                        {
                            return null;
                        }
                        var eyes = new Node3D();
                        node.AddChild(eyes);
                        avi.Skeleton = "../" + node.GetPathTo(skel);
                        // avi.Eyes = "../" + node.GetPathTo(eyes);
                        var xform = HolderNode.GetTransformGlobal(skel);
                        // node.Scale /= xform.Basis.Scale;
                        // avi.Eyes = "../" + node.boneToNode[HumanBodyBones.Head.ToString()];
                        return avi;
                    }
                    case "Hypernex.CCK.Unity.GrabbableDescriptor":
                    {
                        var desc = new GrabbableDescriptor();
                        // TODO: properties
                        node.GetComponent<RigidBody3D>()?.AddChild(desc);
                        return desc;
                    }
                    case "Hypernex.CCK.Unity.RespawnableDescriptor":
                    {
                        var desc = new RespawnableDescriptor();
                        desc.LowestPointRespawnThreshold = component["LowestPointRespawnThreshold"].AsFloat;
                        node.GetComponent<RigidBody3D>()?.AddChild(desc);
                        return desc;
                    }
                    case "kTools.Mirrors.Mirror":
                    {
                        foreach (var item in component["m_Renderers.Array"])
                        {
                            var info = manager.GetExtAsset(fileInst, item);
                            if (info.info == null)
                                continue;
                            Mirror mirror = new Mirror();
                            mirror.RotationDegrees = new Vector3(0f, 180f, 0f);
                            HolderNode other = reader.GetNodeById(info.baseField["m_GameObject.m_PathID"].AsLong);
                            mirror.existingMesh = other.GetComponent<MeshInstance3D>();
                            return mirror;
                        }
                        return null;
                    }
                }
                return null;
            };
            return reader;
        }
    }
}