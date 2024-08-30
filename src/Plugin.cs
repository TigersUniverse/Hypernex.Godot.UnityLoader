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
                }
                return null;
            };
            return reader;
        }
    }
}