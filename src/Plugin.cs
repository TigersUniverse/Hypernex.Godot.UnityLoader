using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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

        private IntPtr TracyResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName.Equals("TracyClient"))
            {
                return NativeLibrary.Load(Path.GetFullPath(Path.Combine(assembly.Location, "..", "runtimes", libraryName)));
            }
            return IntPtr.Zero;
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
                        foreach (var scr in component["LocalScripts.Array"])
                        {
                            WorldScript wScript = new WorldScript();
                            wScript.Name = scr["Name"].AsString;
                            wScript.Language = (NexboxLanguage)scr["Language"].AsInt;
                            wScript.Contents = scr["Script"].AsString;
                            node.components.Add(wScript);
                            node.AddChild(wScript, true);
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
                    case "Hypernex.CCK.Unity.LocalScript":
                    {
                        WorldScript script = new WorldScript();
                        script.Name = component["NexboxScript.Name"].AsString;
                        script.Language = (NexboxLanguage)component["NexboxScript.Language"].AsInt;
                        script.Contents = component["NexboxScript.Script"].AsString;
                        return script;
                    }
                    case "Hypernex.CCK.Unity.NetworkSyncDescriptor":
                    {
                        NetworkSyncDescriptor script = new NetworkSyncDescriptor();
                        script.InstanceHostOnly = component["InstanceHostOnly"].AsBool;
                        script.CanSteal = component["CanSteal"].AsBool;
                        script.AlwaysSync = component["AlwaysSync"].AsBool;
                        return script;
                    }
                    case "Hypernex.CCK.Unity.VideoPlayerDescriptor":
                    {
                        VideoPlayer vid = new VideoPlayer();
                        foreach (var item in component["VideoOutputs.Array"])
                        {
                            var info = manager.GetExtAsset(fileInst, item);
                            if (info.info == null)
                                continue;
                            HolderNode other = reader.GetNodeById(info.baseField["m_GameObject.m_PathID"].AsLong);
                        }
                        var audioInfo = manager.GetExtAsset(fileInst, component["AudioOutput"]);
                        if (audioInfo.info != null)
                        {
                            HolderNode other = reader.GetNodeById(audioInfo.baseField["m_PathID"].AsLong);
                            if (GodotObject.IsInstanceValid(other))
                            {
                                vid.audioPlayer3d = "../" + node.GetPathTo(other);
                            }
                        }
                        return vid;
                    }
                    case "TMPro.TextMeshPro":
                    {
                        Label3D label = new Label3D();
                        label.Visible = component["m_Enabled"].AsBool;
                        label.Modulate = BundleReader.GetColor(component["m_fontColor"]);
                        label.Text = component["m_text"].AsString;
                        label.PixelSize = 0.1f;
                        label.FontSize = Mathf.RoundToInt(component["m_fontSize"].AsFloat);
                        return label;
                    }
                }
                return null;
            };
            return reader;
        }
    }
}