using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using Fmod5Sharp;
using Godot;
using Hypernex.CCK.GodotVersion;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class BundleReader : ISceneProvider
    {
        public static Dictionary<string, List<Resource>> loadedResources = new Dictionary<string, List<Resource>>();
        public PackedScene scene;
        public string zippath;

        public Node3D root;
        public List<HolderNode> allNodes;

        public Func<BundleReader, string, HolderNode, AssetsManager, AssetsFileInstance, AssetTypeValueField, Node> typeMappings = (_, _, _, _, _, _) => null;

        public Dictionary<AssetInfo, Resource> assets = new Dictionary<AssetInfo, Resource>();
        public AssetsManager mgr;
        public BundleFileInstance bundleFile;

        public struct AssetInfo
        {
            public int fileId;
            public long pathId;

            public AssetInfo(AssetTypeValueField field)
            {
                fileId = field["m_FileID"].AsInt;
                pathId = field["m_PathID"].AsLong;
            }
        }

        public void Unload()
        {
            scene = null;
            bundleFile = null;
            mgr?.UnloadAll(true);
            mgr = null;
            if (loadedResources.TryGetValue(zippath, out var resources))
            {
                foreach (var res in resources)
                {
                    if (res is Mesh || res is Texture || res is Shader || res is Material || res is Image)
                        RenderingServer.FreeRid(res.GetRid());
                }
                resources.Clear();
                loadedResources.Remove(zippath);
            }
        }

        public void Dispose()
        {
            Unload();
        }

        public PackedScene LoadFromFile(string filePath)
        {
            scene = null;
            ReadFile(filePath);
            return scene;
        }

        public void ParseAssetsFileInstance(AssetsManager mgr, AssetsFileInstance aFileInst)
        {
            if (aFileInst == null)
                return;
            var aFile = aFileInst.file;

            List<HolderNode> nodes = new List<HolderNode>();
            foreach (var obj in aFile.GetAssetsOfType(AssetClassID.GameObject))
            {
                var goBase = mgr.GetBaseField(aFileInst, obj);
                var name = goBase["m_Name"].AsString;
                var components = goBase["m_Component.Array"];
                HolderNode node = new HolderNode();
                node.assetField = goBase;
                node.Visible = goBase["m_IsActive"].AsBool;
                node.ProcessMode = node.Visible ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
                node.Name = name;
                foreach (var data in components)
                {
                    var componentPtr = data["component"];
                    var componentExtInfo = mgr.GetExtAsset(aFileInst, componentPtr);
                    ConvertComponent(zippath, node, mgr, componentExtInfo.file, componentPtr, componentExtInfo);
                }
                nodes.Add(node);
            }
            foreach (var obj in aFile.GetAssetsOfType(AssetClassID.RenderSettings))
            {
                var baseField = mgr.GetBaseField(aFileInst, obj);
                var skyboxMatPtr = baseField["m_SkyboxMaterial"];
                var skyboxMatInfo = mgr.GetExtAsset(aFileInst, skyboxMatPtr);
                var env = new Godot.Environment();
                env.BackgroundMode = Godot.Environment.BGMode.Sky;
                env.Sky = new Sky();
                if (skyboxMatInfo.info != null)
                    env.Sky.SkyMaterial = GetSkyMaterial(zippath, mgr, skyboxMatInfo.file, skyboxMatInfo.baseField);
                var worldEnv = new WorldEnvironment();
                worldEnv.Environment = env;
                root.AddChild(worldEnv);
            }
            allNodes = new List<HolderNode>(nodes);
            int prevNodeCount = 0;
            while (prevNodeCount != nodes.Count)
            {
                prevNodeCount = nodes.Count;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j].parentFileId == null || nodes[j].parentFileId == 0)
                    {
                        root.AddChild(nodes[j], true);
                        nodes.RemoveAt(j);
                        j--;
                    }
                    else
                    {
                        var parent = allNodes.FirstOrDefault(x => x.fileId == nodes[j].parentFileId.GetValueOrDefault());
                        if (parent != null)
                        {
                            parent.AddChild(nodes[j], true);
                            nodes.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            foreach (var node in root.FindChildren("*", owned: false))
            {
                node.Owner = root;
            }
            foreach (var node in allNodes)
            {
                var goBase = node.assetField;
                var components = goBase["m_Component.Array"];
                foreach (var data in components)
                {
                    var componentPtr = data["component"];
                    var componentExtInfo = mgr.GetExtAsset(aFileInst, componentPtr);
                    ConvertMonoComponent(zippath, node, mgr, componentExtInfo.file, componentPtr, componentExtInfo);
                }
            }
        }

        public void ReadFile(string path)
        {
            zippath = path;
            Unload();
            loadedResources.Add(zippath, new List<Resource>());
            root = new Node3D();
            mgr = new AssetsManager();
            try
            {
                bundleFile = mgr.LoadBundleFile(path, true);
            }
            catch (Exception e)
            {
                GD.PrintErr(e);
                mgr.LoadClassPackage("uncompressed.tpk");
                var aFile = mgr.LoadAssetsFile(path);
                mgr.LoadClassDatabaseFromPackage(aFile.file.Metadata.UnityVersion);
                ParseAssetsFileInstance(mgr, aFile);
                scene = new PackedScene();
                scene.Pack(root);
                mgr.UnloadAll(true);
                return;
            }
            try
            {
                List<string> names = bundleFile.file.GetAllFileNames();
                for (int i = 0; i < names.Count; i++)
                {
                    var aFileInst = mgr.LoadAssetsFileFromBundle(bundleFile, bundleFile.file.GetFileIndex(names[i]), true);
                    ParseAssetsFileInstance(mgr, aFileInst);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e);
            }
            scene = new PackedScene();
            scene.Pack(root);
            mgr.UnloadAll(true);
        }

        public HolderNode GetNodeById(long id)
        {
            return allNodes.FirstOrDefault(x => x.Owner == root && x.gameObjectFileId == id);
        }

        public void ConvertMonoComponent(string zippath, HolderNode node, AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField componentPtr, AssetExternal componentExtInfo)
        {
            var compBase = componentExtInfo.baseField;
            var componentType = (AssetClassID)componentExtInfo.info.TypeId;
            if (componentType == AssetClassID.MonoBehaviour)
            {
                var monoScript = manager.GetExtAsset(fileInst, compBase["m_Script"]);
                if (monoScript.info == null)
                    return;
                Node comp = typeMappings?.Invoke(this, monoScript.baseField["m_Namespace"].AsString + '.' + monoScript.baseField["m_ClassName"].AsString, node, manager, fileInst, compBase);
                if (GodotObject.IsInstanceValid(comp))
                {
                    node.components.Add(comp);
                    node.AddChild(comp);
                    comp.Owner = root;
                }
                return;
            }
        }

        public void ConvertComponent(string zippath, HolderNode node, AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField componentPtr, AssetExternal componentExtInfo)
        {
            var compBase = componentExtInfo.baseField;
            var componentType = (AssetClassID)componentExtInfo.info.TypeId;
            switch (componentType)
            {
                case AssetClassID.Transform:
                {
                    node.fileId = componentPtr["m_PathID"].AsLong;
                    node.gameObjectFileId = compBase["m_GameObject"]["m_PathID"].AsLong;
                    var ptr = compBase["m_Father"]["m_PathID"];
                    node.parentFileId = ptr.IsDummy ? null : ptr.AsLong;
                    var position = GetVector3(compBase["m_LocalPosition"]);
                    var rotation = GetQuaternion(compBase["m_LocalRotation"]);
                    var scale = GetVector3(compBase["m_LocalScale"]) * new Vector3(1f, 1f, -1f);
                    // node.Transform = new Transform3D(new Basis(rotation).Scaled(scale), position);
                    node.Position = position;
                    node.Quaternion = rotation;
                    node.Scale = scale;
                    break;
                }
                case AssetClassID.MeshFilter:
                {
                    var meshPtr = compBase["m_Mesh"];
                    var meshAsset = manager.GetExtAsset(fileInst, meshPtr);
                    // GD.PrintS(node.Name, meshAsset.info == null);
                    if (meshAsset.info == null)
                    {
                        // GD.Print(meshPtr.ToString());
                        break;
                    }
                    var pathId = new AssetInfo(meshPtr);
                    if (assets.TryGetValue(pathId, out var val))
                        node.mesh = val as Mesh;
                    else
                    {
                        ArrayMesh mesh = GetMesh(zippath, meshAsset.file, meshAsset.baseField);
                        // GD.PrintS(node.Name, mesh.ResourceName, mesh.GetSurfaceCount());
                        assets.Add(pathId, mesh);
                        node.mesh = mesh;
                    }
                    break;
                }
                case AssetClassID.MeshRenderer:
                {
                    var materials = compBase["m_Materials.Array"];
                    node.firstSubmesh = compBase["m_StaticBatchInfo.firstSubMesh"].AsUShort;
                    node.subMeshCount = compBase["m_StaticBatchInfo.subMeshCount"].AsUShort;
                    foreach (var matPtr in materials)
                    {
                        var matAsset = manager.GetExtAsset(fileInst, matPtr);
                        if (matAsset.info == null)
                        {
                            node.materials.Add(null);
                            continue;
                        }
                        var pathId = new AssetInfo(matPtr);
                        if (assets.TryGetValue(pathId, out var val))
                            node.materials.Add(val as Material);
                        else
                        {
                            Material mat = GetStandardMaterial(zippath, manager, matAsset.file, matAsset.baseField);
                            node.materials.Add(mat);
                            assets.Add(pathId, mat);
                        }
                    }
                    break;
                }
                case AssetClassID.MeshCollider:
                {
                    var meshPtr = compBase["m_Mesh"];
                    var meshAsset = manager.GetExtAsset(fileInst, meshPtr);
                    if (meshAsset.info == null)
                        break;
                    var pathId = new AssetInfo(meshPtr);
                    Mesh mesh;
                    if (assets.TryGetValue(pathId, out var val))
                        mesh = val as Mesh;
                    else
                    {
                        mesh = GetMesh(zippath, meshAsset.file, meshAsset.baseField);
                        assets.Add(pathId, mesh);
                    }
                    if (compBase["m_Convex"].AsBool)
                        node.shape = mesh.CreateConvexShape();
                    else
                        node.shape = mesh.CreateTrimeshShape();
                    node.shapeCenter = Vector3.Zero;
                    node.shapeEnabled = compBase["m_Enabled"].AsBool;
                    node.shapeTrigger = compBase["m_IsTrigger"].AsBool;
                    break;
                }
                case AssetClassID.BoxCollider:
                {
                    var box = new BoxShape3D();
                    box.Size = GetVector3(compBase["m_Size"]).Abs();
                    node.shape = box;
                    node.shapeCenter = GetVector3(compBase["m_Center"]);
                    node.shapeEnabled = compBase["m_Enabled"].AsBool;
                    node.shapeTrigger = compBase["m_IsTrigger"].AsBool;
                    break;
                }
                case AssetClassID.AudioSource:
                {
                    var clipPtr = compBase["m_audioClip"];
                    int rolloff = compBase["rolloffMode"].AsInt;
                    bool playOnAwake = compBase["m_PlayOnAwake"].AsBool;
                    bool loop = compBase["Loop"].AsBool;
                    var audioAsset = manager.GetExtAsset(fileInst, clipPtr);
                    if (audioAsset.info != null)
                    {
                        node.audioStream = GetAudio(zippath, manager, bundleFile, fileInst, audioAsset.baseField);
                    }
                    node.autoPlayAudio = playOnAwake;
                    node.loopAudio = loop;
                    node.audioVolumeDb = Mathf.LinearToDb(compBase["m_Volume"].AsFloat);
                    node.audioMaxDistance = compBase["MaxDistance"].AsFloat;
                    switch (rolloff)
                    {
                        case 0:
                            node.attenuation = AudioStreamPlayer3D.AttenuationModelEnum.Logarithmic;
                            break;
                        case 1:
                            node.attenuation = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance;
                            break;
                        case 2:
                            node.attenuation = AudioStreamPlayer3D.AttenuationModelEnum.Disabled;
                            break;
                    }
                    node.audioPan = compBase["panLevelCustomCurve.m_Curve.Array"][0]["value"].AsFloat;
                    if (Mathf.IsZeroApprox(node.audioPan))
                    {
                        node.attenuation = AudioStreamPlayer3D.AttenuationModelEnum.Disabled;
                    }
                    break;
                }
                case AssetClassID.Light:
                {
                    int type = compBase["m_Type"].AsInt;
                    float colorR = compBase["m_Color.r"].AsFloat;
                    float colorG = compBase["m_Color.g"].AsFloat;
                    float colorB = compBase["m_Color.b"].AsFloat;
                    float colorA = compBase["m_Color.a"].AsFloat;
                    float intensity = compBase["m_Intensity"].AsFloat;
                    float range = compBase["m_Range"].AsFloat;
                    int shadowType = compBase["m_Shadows.m_Type"].AsInt;
                    int shadowAmount = compBase["m_Shadows.m_Strength"].AsInt;
                    Light3D light = null;
                    switch (type)
                    {
                        case 0: // spot
                            light = new SpotLight3D()
                            {
                                LightColor = new Color(colorR, colorG, colorB, colorA),
                                LightEnergy = intensity,
                                SpotRange = range,
                                ShadowEnabled = shadowType != 0,
                                ShadowOpacity = shadowAmount,
                            };
                            break;
                        case 1: // dir
                            light = new DirectionalLight3D()
                            {
                                LightColor = new Color(colorR, colorG, colorB, colorA),
                                LightEnergy = intensity,
                                ShadowEnabled = shadowType != 0,
                                ShadowOpacity = shadowAmount,
                            };
                            break;
                        case 2: // point
                            light = new OmniLight3D()
                            {
                                LightColor = new Color(colorR, colorG, colorB, colorA),
                                LightEnergy = intensity,
                                OmniRange = range,
                                ShadowEnabled = shadowType != 0,
                                ShadowOpacity = shadowAmount,
                            };
                            break;
                    }
                    if (GodotObject.IsInstanceValid(light))
                    {
                        node.AddChild(light);
                        node.components.Add(light);
                    }
                    break;
                }
            }
        }

        public static AssetExternal GetAsset(AssetsManager manager, AssetTypeValueField field)
        {
            foreach (var file in manager.Files)
            {
                var asset = manager.GetExtAsset(file, field);
                if (asset.info != null)
                    return asset;
            }
            return default;
        }

        public static Vector2 GetVector2(AssetTypeValueField field)
        {
            return new Vector2(field["x"].AsFloat, field["y"].AsFloat);
        }

        public static Vector3 GetVector3(AssetTypeValueField field)
        {
            // return new Vector3(field["x"].AsFloat, field["y"].AsFloat, field["z"].AsFloat);
            return new Vector3(field["x"].AsFloat, field["y"].AsFloat, -field["z"].AsFloat);
        }

        public static Quaternion GetQuaternion(AssetTypeValueField field)
        {
            // return new Quaternion(field["x"].AsFloat, field["y"].AsFloat, field["z"].AsFloat, field["w"].AsFloat);
            return new Quaternion(field["x"].AsFloat, field["y"].AsFloat, -field["z"].AsFloat, -field["w"].AsFloat);
        }

        public static Mesh.ArrayType GetMeshArrayType(int input)
        {
            // https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html
            switch (input)
            {
                case 0:
                    return Mesh.ArrayType.Vertex;
                case 1:
                    return Mesh.ArrayType.Normal;
                case 2:
                    return Mesh.ArrayType.Tangent;
                case 3:
                    return Mesh.ArrayType.Color;
                case 4:
                    return Mesh.ArrayType.TexUV;
                case 5:
                    return Mesh.ArrayType.TexUV2;
                default:
                    return Mesh.ArrayType.Max;
            }
        }

        public static int GetMeshFormatSize(int input)
        {
            // https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.html
            switch (input)
            {
                case 0:
                    return sizeof(float);
                case 1:
                    return sizeof(float) / 2;
                case 2:
                case 3:
                case 6:
                case 7:
                    return sizeof(byte);
                case 4:
                case 5:
                case 8:
                case 9:
                    return sizeof(ushort);
                case 10:
                case 11:
                    return sizeof(uint);
                default:
                    return 0;
            }
        }

        public static byte[] ReadStreamedData(AssetsFileInstance fileInst, AssetTypeValueField field, bool isResource)
        {
            string resourceSource = field[isResource ? "m_Source" : "path"].AsString;
            ulong offset = field[isResource ? "m_Offset" : "offset"].AsULong;
            ulong size = field[isResource ? "m_Size" : "size"].AsULong;
            string fileName = resourceSource.Split('/').Last();
            byte[] data;
            if (fileInst.parentBundle == null)
            {
                fileName = Path.Combine(Path.GetDirectoryName(fileInst.path), resourceSource);
                if (!File.Exists(fileName))
                    return new byte[0];
                var reader = new AssetsFileReader(fileName);
                reader.Position = (long)offset;
                data = reader.ReadBytes((int)size);
            }
            else
            {
                int index = fileInst.parentBundle.file.GetFileIndex(fileName);
                fileInst.parentBundle.file.GetFileRange(index, out long fileOffset, out long length);
                var reader = fileInst.parentBundle.file.DataReader;
                reader.Position = (long)offset + fileOffset;
                data = reader.ReadBytes((int)size);
            }
            return data;
        }

        public static AudioStream GetAudio(string zippath, AssetsManager manager, BundleFileInstance bundleFile, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            string name = field["m_Name"].AsString;
            byte[] data = ReadStreamedData(fileInst, field["m_Resource"], true);
            var soundBank = FsbLoader.LoadFsbFromByteArray(data);
            if (soundBank.Samples[0].RebuildAsStandardFileFormat(out var audioFile, out var audioExt))
            {
                AudioStream stream = null;
                switch (audioExt)
                {
                    case "ogg":
                        stream = AudioStreamOggVorbis.LoadFromBuffer(audioFile);
                        break;
                    case "wav":
                        break;
                    default:
                        return null;
                }
                stream.ResourceName = name;
                return stream;
            }
            return null;
        }

        public static Material GetSkyMaterial(string zippath, AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            Godot.Collections.Array<Image> imgs = new Godot.Collections.Array<Image>();
            imgs.Resize(6);
            Color tint = Colors.White;
            foreach (var colKvp in field["m_SavedProperties.m_Colors.Array"])
            {
                var propName = colKvp["first"].AsString;
                var r = colKvp["second.r"].AsFloat;
                var g = colKvp["second.g"].AsFloat;
                var b = colKvp["second.b"].AsFloat;
                var a = colKvp["second.a"].AsFloat;
                var color = new Color(r, g, b, a);
                switch (propName)
                {
                    case "_SkyTint":
                    case "_Tint":
                        tint = color;
                        break;
                }
            }
            foreach (var texKvp in field["m_SavedProperties.m_TexEnvs.Array"])
            {
                var propName = texKvp["first"].AsString;
                var texturePtr = texKvp["second.m_Texture"];
                var texAsset = manager.GetExtAsset(fileInst, texturePtr);
                if (texAsset.info == null || texAsset.info.TypeId != (int)AssetClassID.Texture2D)
                    continue;
                var texture = GetImage(zippath, fileInst, texAsset.baseField, out var transparent);
                if (texture == null)
                    continue;
                loadedResources[zippath].Add(texture);
                var scale = GetVector2(texKvp["second.m_Scale"]);
                var offset = GetVector2(texKvp["second.m_Offset"]);
                switch (propName)
                {
                    case "_UpTex":
                        imgs[2] = texture;
                        texture.FlipY();
                        break;
                    case "_DownTex":
                        imgs[3] = texture;
                        texture.FlipY();
                        break;
                    case "_LeftTex":
                        imgs[0] = texture;
                        texture.FlipY();
                        break;
                    case "_RightTex":
                        imgs[1] = texture;
                        texture.FlipY();
                        break;
                    case "_FrontTex":
                        imgs[4] = texture;
                        texture.FlipY();
                        break;
                    case "_BackTex":
                        imgs[5] = texture;
                        texture.FlipY();
                        break;
                    default:
                        // GD.Print(propName);
                        break;
                }
            }
            ShaderMaterial mat = new ShaderMaterial();
            mat.ResourceName = field["m_Name"].AsString;
            mat.Shader = new Shader()
            {
                Code =
    @"
    shader_type sky;

    uniform samplerCube cube_tex;
    uniform vec4 tint;

    void sky() {
        float theta = SKY_COORDS.y;
        float phi = SKY_COORDS.x;
        vec3 unit = vec3(0.0);
        unit.x = sin(phi) * sin(theta) * -1.0;
        unit.y = cos(theta) * -1.0;
        unit.z = cos(phi) * sin(theta) * -1.0;
        float a = max(abs(unit.x), max(abs(unit.y), abs(unit.z)));
        COLOR = texture(cube_tex, EYEDIR).rgb * tint.rgb;
    }
    "
            };
            Cubemap map = new Cubemap();
            for (int i = 0; i < 6; i++)
            {
                if (!GodotObject.IsInstanceValid(imgs[i]))
                {
                    imgs[i] = Image.CreateEmpty(64, 64, false, Image.Format.Rgb8);
                    loadedResources[zippath].Add(imgs[i]);
                    imgs[i].Fill(Colors.Gray);
                }
            }
            map.CreateFromImages(imgs);
            mat.SetShaderParameter("cube_tex", map);
            mat.SetShaderParameter("tint", tint);
            loadedResources[zippath].Add(mat);
            return mat;
        }

        public StandardMaterial3D GetStandardMaterial(string zippath, AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            StandardMaterial3D material = new StandardMaterial3D();
            material.ResourceName = field["m_Name"].AsString;
            loadedResources[zippath].Add(material);
            material.VertexColorUseAsAlbedo = true;
            foreach (var tagMap in field["stringTagMap.Array"])
            {
                string key = tagMap["first"].AsString;
                string val = tagMap["second"].AsString;
                if (key.Equals("RenderType", StringComparison.OrdinalIgnoreCase))
                {
                    if (val.Equals("Opaque", StringComparison.OrdinalIgnoreCase))
                        material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                    else
                        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                }
            }
            foreach (var colKvp in field["m_SavedProperties.m_Colors.Array"])
            {
                var propName = colKvp["first"].AsString;
                var r = colKvp["second.r"].AsFloat;
                var g = colKvp["second.g"].AsFloat;
                var b = colKvp["second.b"].AsFloat;
                var a = colKvp["second.a"].AsFloat;
                var color = new Color(r, g, b, a);
                switch (propName)
                {
                    case "_BaseColor":
                    case "_Color":
                        material.AlbedoColor = color;
                        break;
                }
            }
            foreach (var texKvp in field["m_SavedProperties.m_TexEnvs.Array"])
            {
                var propName = texKvp["first"].AsString;
                var texturePtr = texKvp["second.m_Texture"];
                var texAsset = manager.GetExtAsset(fileInst, texturePtr);
                if (texAsset.info == null || texAsset.info.TypeId != (int)AssetClassID.Texture2D)
                    continue;
                ImageTexture texture = null;
                bool created = false;
                var info = new AssetInfo(texturePtr);
                if (assets.TryGetValue(info, out var res))
                    texture = res as ImageTexture;
                else
                {
                    var img = GetImage(zippath, fileInst, texAsset.baseField, out var transparent);
                    texture = ImageTexture.CreateFromImage(img);
                    assets.Add(info, texture);
                    loadedResources[zippath].Add(img);
                    loadedResources[zippath].Add(texture);
                    created = true;
                }
                if (texture == null)
                    continue;
                loadedResources[zippath].Add(texture);
                var scale = GetVector2(texKvp["second.m_Scale"]);
                var offset = GetVector2(texKvp["second.m_Offset"]);
                switch (propName)
                {
                    case "_BaseMap":
                    case "_MainTex":
                        material.AlbedoTexture = texture;
                        material.Uv1Scale = new Vector3(scale.X, scale.Y, 1f);
                        material.Uv1Offset = new Vector3(offset.X, offset.Y, 1f);
                        break;
                    case "_BumpMap":
                    case "_NormalMap":
                        material.NormalEnabled = true;
                        if (created)
                        {
                            var tex = SwapColors(texture.GetImage());
                            texture.Update(tex);
                        }
                        material.NormalTexture = texture;
                        break;
                    default:
                        // GD.Print(propName);
                        break;
                }
            }
            return material;
        }

        public ArrayMesh GetMesh(string zippath, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            var vertCount = field["m_VertexData.m_VertexCount"].AsUInt;
            var vertChannels = field["m_VertexData.m_Channels.Array"];
            Dictionary<Mesh.ArrayType, int> offsets = new Dictionary<Mesh.ArrayType, int>();
            Dictionary<Mesh.ArrayType, int> formats = new Dictionary<Mesh.ArrayType, int>();
            int vertexChunkSize = 0;
            for (int i = 0; i < vertChannels.AsArray.size; i++)
            {
                byte stream = vertChannels[i]["stream"].AsByte; // should always be 0, if not then RIP
                byte offset = vertChannels[i]["offset"].AsByte;
                byte format = vertChannels[i]["format"].AsByte;
                byte dimension = vertChannels[i]["dimension"].AsByte;
                vertexChunkSize += dimension * GetMeshFormatSize(format);
                Mesh.ArrayType meshType = GetMeshArrayType(i);
                if (meshType == Mesh.ArrayType.Max || dimension * GetMeshFormatSize(format) == 0)
                    continue;
                offsets.Add(meshType, offset);
                formats.Add(meshType, format);
            }
            var totalVertexCount = field["m_VertexData.m_VertexCount"].AsUInt;
            var data = field["m_VertexData.m_DataSize"].AsByteArray;
            if (data.Length == 0)
            {
                data = ReadStreamedData(fileInst, field["m_StreamData"], false);
            }
            var indexData = field["m_IndexBuffer.Array"].AsByteArray;
            var indexFormatSize = field["m_IndexFormat"].AsInt == 0 ? sizeof(ushort) : sizeof(uint);
            var subMeshes = field["m_SubMeshes.Array"];
            ArrayMesh mesh = new ArrayMesh();
            mesh.ResourceName = field["m_Name"].AsString;
            loadedResources[zippath].Add(mesh);
            // GD.PrintS(mesh.ResourceName, totalVertexCount);
            if (totalVertexCount == 0)
            {
                vertexChunkSize = 0;
                return mesh;
            }
            else
                vertexChunkSize = (int)(data.Length / totalVertexCount);
            for (int i = 0; i < subMeshes.AsArray.size; i++)
            {
                // assuming triangles for simplicity :)
                var topology = subMeshes[i]["topology"].AsInt;
                if (topology != 0)
                {
                    GD.PrintErr(topology);
                    continue;
                }
                var indexStart = subMeshes[i]["firstByte"].AsUInt;
                var indexCount = subMeshes[i]["indexCount"].AsUInt;
                var baseVertex = subMeshes[i]["baseVertex"].AsUInt;
                var firstVertex = subMeshes[i]["firstVertex"].AsUInt;
                var vertexCount = subMeshes[i]["vertexCount"].AsUInt;
                SurfaceTool surface = new SurfaceTool();
                surface.Begin(Mesh.PrimitiveType.Triangles);
                for (uint k = 0; k < indexCount; k++)
                {
                    int j = (int)(k * indexFormatSize + indexStart);
                    if (indexFormatSize == sizeof(ushort))
                    {
                        surface.AddIndex((int)(BitConverter.ToUInt16(indexData, j) + baseVertex));
                    }
                    else
                    {
                        surface.AddIndex((int)(BitConverter.ToUInt32(indexData, j) + baseVertex));
                    }
                }
                // for (uint k = 0; k < vertexCount; k++)
                for (int j = 0; j < data.Length; j+=vertexChunkSize)
                {
                    // int j = (int)((k + firstVertex) * vertexChunkSize);
                    if (j >= data.Length)
                    {
                        surface.Clear();
                        break;
                    }
                    if (offsets.TryGetValue(Mesh.ArrayType.Normal, out int nOffset))
                        surface.SetNormal(ReadVector3(data, j + nOffset, formats[Mesh.ArrayType.Normal] == 1));
                    if (offsets.TryGetValue(Mesh.ArrayType.Tangent, out int tOffset))
                        surface.SetTangent(ReadPlane(data, j + tOffset, formats[Mesh.ArrayType.Tangent] == 1));
                    if (offsets.TryGetValue(Mesh.ArrayType.Color, out int cOffset))
                        surface.SetColor(ReadColor(data, j + cOffset));
                    if (offsets.TryGetValue(Mesh.ArrayType.TexUV, out int uv1Offset))
                        surface.SetUV(ReadVector2(data, j + uv1Offset, formats[Mesh.ArrayType.TexUV] == 1));
                    if (offsets.TryGetValue(Mesh.ArrayType.TexUV2, out int uv2Offset))
                        surface.SetUV2(ReadVector2(data, j + uv2Offset, formats[Mesh.ArrayType.TexUV2] == 1));
                    surface.AddVertex(ReadVector3(data, j + offsets[Mesh.ArrayType.Vertex], formats[Mesh.ArrayType.Vertex] == 1));
                }
                mesh = surface.Commit(mesh);
            }
            return mesh;
        }

        public static Vector2 ReadVector2(byte[] data, int offset, bool half)
        {
            if (half)
            {
                float x = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 0);
                float y = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 1);
                return new Vector2(x, y);
            }
            else
            {
                float x = BitConverter.ToSingle(data, offset + sizeof(float) * 0);
                float y = BitConverter.ToSingle(data, offset + sizeof(float) * 1);
                return new Vector2(x, y);
            }
        }

        public static Vector3 ReadVector3(byte[] data, int offset, bool half)
        {
            if (half)
            {
                float x = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 0);
                float y = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 1);
                float z = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 2);
                // return new Vector3(x, y, z);
                return new Vector3(x, y, -z);
            }
            else
            {
                float x = BitConverter.ToSingle(data, offset + sizeof(float) * 0);
                float y = BitConverter.ToSingle(data, offset + sizeof(float) * 1);
                float z = BitConverter.ToSingle(data, offset + sizeof(float) * 2);
                // return new Vector3(x, y, z);
                return new Vector3(x, y, -z);
            }
        }

        public static Vector4 ReadVector4(byte[] data, int offset, bool half)
        {
            if (half)
                throw new NotImplementedException();
            float x = BitConverter.ToSingle(data, offset + sizeof(float) * 0);
            float y = BitConverter.ToSingle(data, offset + sizeof(float) * 1);
            float z = BitConverter.ToSingle(data, offset + sizeof(float) * 2);
            float w = BitConverter.ToSingle(data, offset + sizeof(float) * 3);
            // return new Vector4(x, y, z, w);
            return new Vector4(x, y, -z, w);
        }

        public static Color ReadColor(byte[] data, int offset)
        {
            return new Color()
            {
                R8 = data[offset + 0],
                G8 = data[offset + 1],
                B8 = data[offset + 2],
                A8 = data[offset + 3],
            };
        }

        public static Plane ReadPlane(byte[] data, int offset, bool half)
        {
            if (half)
            {
                float x = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 0);
                float y = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 1);
                float z = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 2);
                float w = (float)BitConverter.ToHalf(data, offset + sizeof(float) / 2 * 3);
                // return new Plane(x, y, z, w);
                return new Plane(x, y, -z, w);
            }
            else
            {
                float x = BitConverter.ToSingle(data, offset + sizeof(float) * 0);
                float y = BitConverter.ToSingle(data, offset + sizeof(float) * 1);
                float z = BitConverter.ToSingle(data, offset + sizeof(float) * 2);
                float w = BitConverter.ToSingle(data, offset + sizeof(float) * 3);
                // return new Plane(x, y, z, w);
                return new Plane(x, y, -z, w);
            }
        }

        public static Image GetImage(string zippath, AssetsFileInstance fileInst, AssetTypeValueField field, out bool transparent)
        {
            transparent = false;
            TextureFile file = TextureFile.ReadTextureFile(field);
            byte[] data = file.GetTextureData(fileInst);
            for (int i = 0; i < data.Length; i+=4)
            {
                byte b = data[i];
                byte g = data[i+1];
                byte r = data[i+2];
                byte a = data[i+3];
                data[i] = r;
                data[i+1] = g;
                data[i+2] = b;
                data[i+3] = a;
                if (a < byte.MaxValue)
                {
                    transparent = true;
                }
            }
            Image img = Image.CreateFromData(file.m_Width, file.m_Height, false, Image.Format.Rgba8, data);
            // img.Resize(512, 512);
            img.GenerateMipmaps();
            return img;
        }

        public static Image SwapColors(Image img)
        {
            // img.ClearMipmaps();
            byte[] data = img.GetData();
            for (int i = 0; i < data.Length; i+=4)
            {
                byte r = data[i];
                byte g = data[i+1];
                byte b = data[i+2];
                byte a = data[i+3];
                data[i] = a;
                data[i+1] = g;
                data[i+2] = b;
                data[i+3] = r;
            }
            Image img2 = Image.CreateFromData(img.GetWidth(), img.GetHeight(), true, Image.Format.Rgba8, data);
            // img2.GenerateMipmaps();
            return img2;
        }
    }
}