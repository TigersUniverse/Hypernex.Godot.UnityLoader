using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Extra.Decompressors.LZ4;
using LZ4ps;

namespace Hypernex.GodotVersion.UnityLoader
{
    public static partial class ShaderReader
    {
        public const uint SHADER_V201608170 = 201608170;
        public const uint SHADER_V201806140 = 201806140;
        public const uint SHADER_V202012090 = 202012090;

        public class ShaderData
        {
            public string Name { get; set; }
            public List<uint> Platforms { get; set; } = new List<uint>();
            public List<byte[][]> PlatformData { get; set; } = new List<byte[][]>();
            public List<ShaderSubProgram[]> SubPrograms { get; set; } = new List<ShaderSubProgram[]>();
            public Dictionary<uint, ShaderSubProgram[]> PlatformToSubShaders
            {
                get
                {
                    Dictionary<uint, ShaderSubProgram[]> dict = new Dictionary<uint, ShaderSubProgram[]>();
                    for (int i = 0; i < Platforms.Count; i++)
                    {
                        dict.TryAdd(Platforms[i], SubPrograms[i]);
                    }
                    return dict;
                }
            }

            public byte[] ReadSubProgram(ShaderSubProgram prog)
            {
                using var file = new MemoryStream(PlatformData[prog.platformIdx][prog.index], false);
                file.Position = prog.offset;
                byte[] data = LoadSubProgram(file);
                return data;
            }
        }

        public struct ShaderSubProgram
        {
            public int platformIdx;
            public uint offset;
            public uint size;
            public uint index;
        }

        private static byte[] uint16_data = new byte[sizeof(ushort)];
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(Stream reader)
        {
            Array.Fill(uint16_data, default);
            reader.Read(uint16_data);
            // Array.Reverse(uint16_data);
            return BitConverter.ToUInt16(uint16_data);
        }

        private static byte[] uint32_data = new byte[sizeof(uint)];
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(Stream reader)
        {
            Array.Fill(uint32_data, default);
            reader.Read(uint32_data);
            // Array.Reverse(uint32_data);
            return BitConverter.ToUInt32(uint32_data);
        }

        private static byte[] int32_data = new byte[sizeof(int)];
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(Stream reader)
        {
            Array.Fill(int32_data, default);
            reader.Read(int32_data);
            // Array.Reverse(int32_data);
            return BitConverter.ToInt32(int32_data);
        }

        public static ShaderData ReadShader(AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            ShaderData finalData = new ShaderData();
            bool segmented = true; // TODO: read this somehow
            finalData.Name = field["m_ParsedForm.m_Name"].AsString;

            var platformsArray = field["platforms.Array"];
            for (int i = 0; i < platformsArray.AsArray.size; i++)
            {
                finalData.Platforms.Add(platformsArray[i].AsUInt);
            }
            var offsetsArray = field["offsets.Array"];
            var cSizesArray = field["compressedLengths.Array"];
            var uSizesArray = field["decompressedLengths.Array"];
            var blob = field["compressedBlob.Array"].AsByteArray;
            for (int i = 0; i < offsetsArray.AsArray.size; i++)
            {
                var offsets = segmented ? offsetsArray[i]["Array"] : offsetsArray[i];
                var csizes = segmented ? cSizesArray[i]["Array"] : cSizesArray[i];
                var usizes = segmented ? uSizesArray[i]["Array"] : uSizesArray[i];
                var segments = usizes.Select(x => new byte[x.AsUInt]).ToArray();
                for (int j = 0; j < segments.Length; j++)
                {
                    segments[j] = LZ4Codec.Decode32(blob, (int)offsets[j].AsUInt, (int)csizes[j].AsUInt, (int)usizes[j].AsUInt);
                }
                using var header = new MemoryStream(segments[0], false); // TODO: endian??
                var subPrograms = new ShaderSubProgram[ReadUInt32(header)];
                for (int j = 0; j < subPrograms.Length; j++)
                {
                    var offset = ReadUInt32(header);
                    var size = ReadUInt32(header);
                    var index = segmented ? ReadUInt32(header) : 0;
                    subPrograms[j] = new ShaderSubProgram()
                    {
                        platformIdx = i,
                        offset = offset,
                        size = size,
                        index = index,
                    };
                }
                finalData.PlatformData.Add(segments);
                finalData.SubPrograms.Add(subPrograms);
            }
            return finalData;
        }

        public static byte[][] ReadPlayerSubPrograms(AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            var arr = field["Array"];
            byte[][] data = new byte[arr.AsArray.size][];
            for (int i = 0; i < arr.AsArray.size; i++)
            {
                data[i] = ReadPlayerSubProgram(manager, fileInst, arr[i]);
            }
            return data;
        }

        public static byte[] ReadPlayerSubProgram(AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField field)
        {
            Console.WriteLine(field["m_BlobIndex"].AsUInt);
            return null;
        }

        public static string ReadCStringPadded(Stream file)
        {
            string str = string.Empty;
            int b = file.ReadByte();
            while (b != 0 && b != -1)
            {
                str += (char)b;
                b = file.ReadByte();
            }
            Align(file);
            return str;
        }

        public static string ReadStringAligned(Stream file)
        {
            int strLen = ReadInt32(file);
            if (strLen > 0 && strLen <= file.Length - file.Position)
            {
                byte[] buffer = new byte[strLen];
                file.Read(buffer);
                string str = Encoding.UTF8.GetString(buffer);
                Align(file);
                return str;
            }
            return string.Empty;
        }

        public static void Align(Stream file)
        {
            var pos = file.Position;
            var mod = pos % 4;
            if (mod != 0)
                file.Position += 4 - mod;
        }

        public static byte[] LoadSubProgram(Stream file)
        {
            var version = ReadInt32(file);
            var progType = ReadInt32(file);
            if (progType != 6 && progType != 7 && progType != 8)
            {
                // anything not opengl core not supported
                return Array.Empty<byte>();
            }
            file.Position += 12;
            if (version >= SHADER_V201608170)
            {
                file.Position += 4;
            }
            var oldPos = file.Position;
            string gl = ReadStringAligned(file);
            if (gl == "$Globals")
            {
                // not supported
                return Array.Empty<byte>();
            }
            else
                file.Position = oldPos;
            var keywordsSize = ReadInt32(file);
            var keywords = new string[keywordsSize];
            for (int i = 0; i < keywords.Length; i++)
            {
                keywords[i] = ReadStringAligned(file);
                Thread.Yield();
            }
            if (version >= SHADER_V201806140 && version < SHADER_V202012090)
            {
                var localKeywordsSize = ReadInt32(file);
                var localKeywords = new string[localKeywordsSize];
                for (int i = 0; i < localKeywords.Length; i++)
                {
                    localKeywords[i] = ReadStringAligned(file);
                }
            }
            // uint8 array
            var programCodeLen = ReadInt32(file);
            var programCode = new byte[programCodeLen];
            int amountRead = file.Read(programCode);
            Align(file);
            Array.Resize(ref programCode, amountRead);
            // unknown what comes after
            return programCode;
        }
    }
}