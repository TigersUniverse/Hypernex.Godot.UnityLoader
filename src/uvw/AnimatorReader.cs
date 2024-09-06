using System;
using System.Collections.Generic;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Godot;
using HashNum = System.Int64;

namespace Hypernex.GodotVersion.UnityLoader
{
    public partial class BundleReader
    {
        public AnimationPlayer GetAnimPlayer(AssetsFileInstance fileInst, AssetTypeValueField animatorfield, HolderNode node)
        {
            var ctrlPtr = animatorfield["m_Controller"];
            var ctrlAsset = mgr.GetExtAsset(fileInst, ctrlPtr);
            if (ctrlAsset.info == null)
                return null;
            var avatarPtr = animatorfield["m_Avatar"];
            var avatarAsset = mgr.GetExtAsset(fileInst, avatarPtr);
            if (avatarAsset.info == null)
                return null;

            Dictionary<HashNum, long> hashToXform = new Dictionary<HashNum, long>();
            BuildTOS(hashToXform, node.assetTransformField);
            Dictionary<HashNum, HolderNode> hashToNode = new Dictionary<HashNum, HolderNode>();
            foreach (var kvp in hashToXform)
            {
                var target = GetNodeByTransformComponentId(kvp.Value);
                node.hashToNodePath.TryAdd(kvp.Key, node.GetPathTo(target));
                hashToNode.TryAdd(kvp.Key, target);
            }
            foreach (var item in avatarAsset.baseField["m_TOS.Array"])
            {
                break;
                var first = item["first"].AsUInt;
                var second = item["second"].AsString;
                var path = string.Join("/", second.Split("/").Select(x => x.ValidateNodeName()));
                hashToNode.TryAdd(first, node.GetNode<HolderNode>(path));
                node.hashToNodePath.TryAdd(first, path);
            }
            AnimationPlayer player = new AnimationPlayer();
            AnimationLibrary library = GetAnimationLibrary(ctrlAsset, hashToNode, node);
            Error err = player.AddAnimationLibrary(string.Empty, library);
            if (err != Error.Ok)
                GD.PrintErr(err);
            ParseHumanAvatarData(avatarAsset, hashToNode, node);
            return player;
        }

        public AnimationLibrary GetAnimationLibraryUseExistingTOS(AssetExternal ctrlAsset, HolderNode node)
        {
            return GetAnimationLibrary(ctrlAsset, node.hashToNode, node);
        }

        public AnimationLibrary GetAnimationLibrary(AssetExternal ctrlAsset, Dictionary<HashNum, HolderNode> hashToNode, HolderNode node)
        {
            AnimationLibrary library = new AnimationLibrary();
            foreach (var clipPtr in ctrlAsset.baseField["m_AnimationClips.Array"])
            {
                var clipAsset = mgr.GetExtAsset(ctrlAsset.file, clipPtr);
                if (clipAsset.info == null)
                    continue;
                Animation anim = GetAnimation(hashToNode, clipAsset, node);
                library.AddAnimation(anim.ResourceName, anim);
            }
            return library;
        }

        public void ParseHumanAvatarData(AssetExternal avatarAsset, Dictionary<HashNum, HolderNode> hashToNode, HolderNode node)
        {
            var human = avatarAsset.baseField["m_Avatar.m_Human.data"];
            node.twists = new float[4];
            node.twists[0] = human["m_ArmTwist"].AsFloat;
            node.twists[1] = human["m_ForeArmTwist"].AsFloat;
            node.twists[2] = human["m_UpperLegTwist"].AsFloat;
            node.twists[3] = human["m_LegTwist"].AsFloat;
            node.avatarScale = human["m_Scale"].AsFloat;
            // node.avatarScale = avatarAsset.baseField["m_HumanDescription.m_GlobalScale"].AsFloat;

            var humanNodes = human["m_Skeleton.data.m_Node.Array"];
            var humanIds = human["m_Skeleton.data.m_ID.Array"];
            var humanAxes = human["m_Skeleton.data.m_AxesArray.Array"];
            var humanHumanIds = human["m_HumanBoneIndex.Array"];
            var humanSkeletonPose = human["m_SkeletonPose.data.m_X.Array"];
            var humanDesc = avatarAsset.baseField["m_HumanDescription.m_Human.Array"];
            var humanSkeleton = avatarAsset.baseField["m_HumanDescription.m_Skeleton.Array"];
            // this is not what unity uses. :(
            /*
            var boneMap = new Dictionary<string, HolderNode>();
            var humanToBone = new Dictionary<string, string>();
            foreach (var item in humanDesc)
            {
                var boneName = item["m_BoneName"].AsString;
                var humanName = item["m_HumanName"].AsString;
                // var limits = item["m_Limit"];
                humanToBone.TryAdd(humanName, boneName);
            }
            foreach (var item in humanSkeleton)
            {
                var objName = item["m_Name"].AsString;
                var parentObjName = item["m_ParentName"].AsString;
                boneMap.TryAdd(objName, node.FindChildTransform(objName, parentObjName));
            }
            node.boneToNode.Clear();
            for (int i = 0; i < HumanTrait.BoneName.Length; i++)
            {
                var hName = HumanTrait.BoneName[i];
                if (humanToBone.ContainsKey(hName) && boneMap.ContainsKey(humanToBone[hName]))
                {
                    node.boneToNode.TryAdd(hName, node.GetPathTo(boneMap[humanToBone[hName]]));
                }
            }
            */

            node.humanBoneAxes.Clear();
            node.boneToNode.Clear();
            for (int i = 0; i < humanIds.AsArray.size; i++)
            {
                var data = humanIds[i].AsUInt;
                var axesId = humanNodes[i]["m_AxesId"].AsInt;
                if (axesId != -1)
                {
                    var item = humanAxes[axesId];
                    var axes = new Axes();
                    axes.preQ = GetQuaternionNoFlip(item["m_PreQ"]);
                    axes.postQ = GetQuaternionNoFlip(item["m_PostQ"]);
                    axes.sgn = GetVector3NoFlip(item["m_Sgn"]);
                    axes.length = item["m_Length"].AsFloat;
                    axes.maxRad = GetVector3NoFlip(item["m_Limit.m_Max"]);
                    axes.minRad = GetVector3NoFlip(item["m_Limit.m_Min"]);
                    axes.position = GetVector3(humanSkeletonPose[i]["t"]);
                    axes.rotation = GetQuaternion(humanSkeletonPose[i]["q"]);
                    axes.scale = GetVector3NoFlip(humanSkeletonPose[i]["s"]);
                    hashToNode[data].Transform = axes.xform;
                    node.humanBoneAxes.TryAdd(node.GetPathTo(hashToNode[data]), axes);
                    // var hName = HumanTrait.BoneName[i];
                    string hName = null;
                    for (int j = 0; j < humanHumanIds.AsArray.size; j++)
                    {
                        if (i == humanHumanIds[j].AsInt)
                        {
                            hName = HumanTrait.BoneIndexToName[j];
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(hName))
                    {
                        // GD.PrintErr($"hName not found! i: {i} axesId: {axesId}");
                        continue;
                    }
                    axes.humanBoneName = hName;
                    node.boneToNode.TryAdd(hName/*humanSkeleton[i]["m_Name"].AsString*/, node.GetPathTo(hashToNode[data]));
                }
            }

            // parse the second skeleton
            {
                var human2 = avatarAsset.baseField["m_Avatar"];
                var human2Nodes = human2["m_AvatarSkeleton.data.m_Node.Array"];
                var human2Ids = human2["m_AvatarSkeleton.data.m_ID.Array"];
                var human2SkeletonPose = human2["m_AvatarSkeletonPose.data.m_X.Array"];

                for (int i = 0; i < human2Ids.AsArray.size; i++)
                {
                    var data = human2Ids[i].AsUInt;
                    if (!hashToNode.ContainsKey(data))
                        continue;
                    NodePath path = node.GetPathTo(hashToNode[data]);
                    if (!node.humanBoneAxes.ContainsKey(path))
                    {
                        node.humanBoneAxes.TryAdd(path, new Axes());
                    }
                    var axes = node.humanBoneAxes[path];
                    axes.position = GetVector3(human2SkeletonPose[i]["t"]);
                    axes.rotation = GetQuaternion(human2SkeletonPose[i]["q"]);
                    axes.scale = GetVector3NoFlip(human2SkeletonPose[i]["s"]);
                    hashToNode[data].Transform = axes.xform;
                    node.boneToNode.TryAdd(hashToNode[data].Name, path);
                }
            }
        }

        public Animation GetAnimation(Dictionary<HashNum, HolderNode> hashToNode, AssetExternal asset, HolderNode node)
        {
            if (asset.info == null)
                return null;

            var sampleRate = asset.baseField["m_SampleRate"].AsFloat;
            var startTime = asset.baseField["m_MuscleClip.m_StartTime"].AsFloat;
            var stopTime = asset.baseField["m_MuscleClip.m_StopTime"].AsFloat;
            var genericBinds = asset.baseField["m_ClipBindingConstant.genericBindings.Array"];
            var pptrCurveMaps = asset.baseField["m_ClipBindingConstant.pptrCurveMapping.Array"];

            var muscleClipData = asset.baseField["m_MuscleClip.m_Clip.data"];
            var streamedClip = muscleClipData["m_StreamedClip"];
            var streamedClipData = muscleClipData["m_StreamedClip.data.Array"];
            var streamedCount = streamedClip["curveCount"].AsUInt;
            var denseClip = muscleClipData["m_DenseClip"];
            var denseClipData = denseClip["m_SampleArray.Array"];
            var denseCount = denseClip["m_CurveCount"].AsUInt;
            var constClip = muscleClipData["m_ConstantClip.data.Array"];
            var constCount = constClip.AsArray.size;

            var anim = new Animation();
            anim.ResourceName = asset.baseField["m_Name"].AsString;

            var binders = new Dictionary<HashNum, HolderNode>();
            var bindings = new Dictionary<HashNum, PropertyBinding>();
            var tracks = new Dictionary<HashNum, Dictionary<NodePath, int>>();

            foreach (var bind in genericBinds)
            {
                var key = GetBindKey(bind["path"].AsUInt, bind["typeID"].AsInt, bind["customType"].AsByte);
                if (!hashToNode.ContainsKey(bind["path"].AsUInt))
                    continue;
                var target = hashToNode[bind["path"].AsUInt];
                binders.TryAdd(key, target);
            }

            float[] values = new float[streamedCount + denseCount + constCount];
            uint offset = 0;
            foreach (var bind in genericBinds)
            {
                var customType = bind["customType"].AsByte;
                var path = bind["path"].AsUInt;
                var typeId = bind["typeID"].AsInt;
                var key = GetBindKey(path, typeId, customType);
                var attribute = bind["attribute"].AsUInt;
                AssetClassID typeName = (AssetClassID)typeId;
                if (!binders.ContainsKey(key))
                {
                    GD.PrintErr($"Key not found: {key} ({path} {typeId} {customType})");
                    offset += typeId == 4 ? (uint)(attribute == 2 ? 4 : 3) : 1;
                    continue;
                }
                HolderNode target = binders[key];
                if (typeId == 4)
                {
                    bindings.TryAdd(key, new TransformBinding(target));
                }
                else
                {
                    switch (customType)
                    {
                        case 8: // BindMuscle
                            bindings.TryAdd(key, new MuscleBinding(target));
                            break;
                    }
                }
                if (bindings.ContainsKey(key))
                {
                    NodePath trackPath = node.GetPathTo(target) + ":" + bindings[key].Set(attribute, values, offset, false);
                    if (!tracks.ContainsKey(key))
                        tracks.Add(key, new Dictionary<NodePath, int>());
                    if (!tracks[key].ContainsKey(trackPath))
                    {
                        int track = anim.AddTrack(Animation.TrackType.Value);
                        tracks[key].Add(trackPath, track);
                    }
                    anim.TrackSetPath(tracks[key][trackPath], trackPath);
                    anim.Length = stopTime - startTime;
                }
                offset += typeId == 4 ? (uint)(attribute == 2 ? 4 : 3) : 1;
            }
            for (float time = startTime; time <= stopTime; time+=1f/sampleRate)
            {
                values = AnimEvalTimestamp(asset, time);
                offset = 0;
                foreach (var bind in genericBinds)
                {
                    var typeId = bind["typeID"].AsInt;
                    var key = GetBindKey(bind["path"].AsUInt, typeId, bind["customType"].AsByte);
                    var attribute = bind["attribute"].AsUInt;
                    if (!bindings.ContainsKey(key))
                    {
                        offset += typeId == 4 ? (uint)(attribute == 2 ? 4 : 3) : 1;
                        continue;
                    }
                    HolderNode target = binders[key];
                    string path = bindings[key].Set(attribute, values, offset, true);
                    NodePath trackPath = node.GetPathTo(target) + ":" + path;
                    if (tracks.ContainsKey(key) && tracks[key].ContainsKey(trackPath))
                    {
                        // int j = anim.TrackFindKey(tracks[key][trackPath], time, Animation.FindMode.Approx);
                        // if (j != -1)
                        //     anim.TrackInsertKey(tracks[key][trackPath], time, target.Get(path));
                    }
                    offset += typeId == 4 ? (uint)(attribute == 2 ? 4 : 3) : 1;
                }

                foreach (var track in tracks)
                {
                    foreach (var kvp in track.Value)
                    {
                        var val = node.GetNode(kvp.Key.GetConcatenatedNames()).Get(kvp.Key.GetConcatenatedSubNames());
                        // var val = node.GetNodeAndResource(kvp.Key)[1];
                        // GD.PrintS(kvp.Key, val);
                        anim.TrackInsertKey(kvp.Value, time, val);
                    }
                }
            }
            // GD.PrintS("Bindings");
            // GD.Print(string.Join(", ", binders.Select(x => $"{x.Key} {x.Value.Name}")));

            return anim;
        }

        public float[] AnimEvalTimestamp(AssetExternal asset, float time)
        {
            var sampleRate = asset.baseField["m_SampleRate"].AsFloat;
            var startTime = asset.baseField["m_MuscleClip.m_StartTime"].AsFloat;
            var stopTime = asset.baseField["m_MuscleClip.m_StopTime"].AsFloat;

            var muscleClipData = asset.baseField["m_MuscleClip.m_Clip.data"];
            var streamedClip = muscleClipData["m_StreamedClip"];
            var streamedClipData = muscleClipData["m_StreamedClip.data.Array"];
            var streamedCount = streamedClip["curveCount"].AsUInt;
            var denseClip = muscleClipData["m_DenseClip"];
            var denseCount = denseClip["m_CurveCount"].AsUInt;
            var constClip = muscleClipData["m_ConstantClip.data.Array"];
            var constCount = constClip.AsArray.size;

            float[] values = new float[streamedCount + denseCount + constCount];

            // streamed
            {
                float cursorTime = float.NegativeInfinity;
                int cursorIndex = 0;
                float[] frameTimes = new float[streamedCount];
                uint[] dataOffsets = new uint[streamedCount];
                float[] data = new float[streamedClipData.AsArray.size];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = BitConverter.UInt32BitsToSingle(streamedClipData[i].AsUInt);
                }
                for (int i = cursorIndex; i < data.Length; cursorIndex=i)
                {
                    var frameTime = data[i];
                    var length = streamedClipData[i+1].AsUInt;
                    if (frameTime > time)
                        break;
                    i += 2;
                    for (int j = 0; j < length; j++, i+=5)
                    {
                        var curveIndex = streamedClipData[i].AsUInt;
                        frameTimes[curveIndex] = frameTime;
                        dataOffsets[curveIndex] = (uint)(i + 1);
                    }
                    cursorTime = frameTime;
                }
                for (int i = 0; i < streamedCount; i++)
                {
                    var j = dataOffsets[i];
                    var x = time - frameTimes[i];
                    values[i] = ((data[j+0]*x + data[j+1])*x + data[j+2])*x + data[j+3];
                }
            }

            // dense
            {
                var denseClipData = denseClip["m_SampleArray.Array"];
                var denseBeginTime = denseClip["m_BeginTime"].AsFloat;
                var denseSampleRate = denseClip["m_SampleRate"].AsFloat;
                var denseFrameCount = denseClip["m_FrameCount"].AsInt;
                var frameIndex = Mathf.Max(0, Mathf.Min(denseFrameCount-1, (time-denseBeginTime)*denseSampleRate));
                var frameIndexInt = Mathf.FloorToInt(frameIndex);
                var w1 = frameIndex - frameIndexInt;
                var w0 = 1 - w1;
                var i0 = frameIndexInt*(int)denseCount;
                var i1 = i0+(Mathf.IsZeroApprox(w1) ? 0 : (int)denseCount);
                for (int i = 0; i < denseCount; i++)
                {
                    values[streamedCount + i] = denseClipData[i0+i].AsFloat*w0 + denseClipData[i1+i].AsFloat*w1;
                }
            }

            // const
            {
                for (int i = 0; i < constCount; i++)
                {
                    values[streamedCount + denseCount + i] = constClip[i].AsFloat;
                }
            }

            return values;
        }

        public void BuildTOS(Dictionary<HashNum, long> dict, AssetExternal xform, HashNum hash = 0)
        {
            var goAsset2 = mgr.GetExtAsset(xform.file, xform.baseField["m_GameObject"]);
            // GD.PrintS(goAsset2.baseField["m_Name"].AsString, hash);
            if (!dict.ContainsKey(hash))
                dict.Add(hash, xform.info.PathId);
            if (xform.info == null)
                return;
            foreach (var ptr in xform.baseField["m_Children.Array"])
            {
                var xformAsset = mgr.GetExtAsset(xform.file, ptr);
                if (xformAsset.info == null)
                    continue;
                var goAsset = mgr.GetExtAsset(xformAsset.file, xformAsset.baseField["m_GameObject"]);
                if (goAsset.info == null)
                    continue;
                BuildTOS(dict, xformAsset, (HashNum)AnimCrc32.AppendPathHash((uint)hash, goAsset.baseField["m_Name"].AsString));
            }
        }

        public static HashNum GetBindKey(uint path, int typeId, byte customType)
        {
            return (HashNum)(((long)path << 64) | ((long)typeId << 32) | (long)customType);
        }
    }
}
