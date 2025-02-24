using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Hypernex.GodotVersion.UnityLoader;

public static class Program
{
    public static void Main()
    {
        var mgr = new AssetsManager();
        mgr.UseQuickLookup = true;
        mgr.UseTemplateFieldCache = true;
        mgr.UseMonoTemplateFieldCache = true;
        mgr.UseRefTypeManagerCache = true;
        var bundleFile = mgr.LoadBundleFile("path_here", true);
        List<string> names = bundleFile.file.GetAllFileNames();
        for (int i = 0; i < names.Count; i++)
        {
            var aFileInst = mgr.LoadAssetsFileFromBundle(bundleFile, bundleFile.file.GetFileIndex(names[i]), false);
            ParseAssetsFileInstance(mgr, aFileInst);
        }
    }

    private static void Print(StringBuilder sb, ClassDatabaseFile file, ClassDatabaseTypeNode node, int rec = 0)
    {
        for (int i = 0; i < rec; i++)
            sb.Append('\t');
        sb.Append(file.GetString(node.TypeName));
        sb.Append(':');
        sb.Append(file.GetString(node.FieldName));
        sb.AppendLine();
        foreach (var ch in node.Children)
            Print(sb, file, ch, rec + 1);
    }

    public static string ConvertUnityGLSLToGodot(string glsl)
    {
        string[] lines = glsl.Split('\n');
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("shader_type spatial;");
        sb.AppendLine("render_mode skip_vertex_transform, unshaded;");

        // defines
        sb.AppendLine("#define UNITY_LOCATION(x)");
        sb.AppendLine("#define UNITY_BINDING(x)");
        sb.AppendLine("#define hlslcc_mtx4x4unity_ObjectToWorld MODEL_MATRIX");
        sb.AppendLine("#define hlslcc_mtx4x4unity_WorldToObject inverse(MODEL_MATRIX)");
        sb.AppendLine("#define hlslcc_mtx4x4unity_MatrixVP (PROJECTION_MATRIX * VIEW_MATRIX)");
        sb.AppendLine("#define gl_Position POSITION");
        sb.AppendLine("#define in_POSITION0 vec4(VERTEX, 0.0)");
        sb.AppendLine("#define in_TEXCOORD0 vec4(UV, 0.0, 0.0)");
        sb.AppendLine("#define in_NORMAL0 vec4(NORMAL, 0.0)");
        
        int vertexIdx = Array.IndexOf(lines, "#ifdef VERTEX");
        if (vertexIdx == -1)
            return string.Empty;
        int vertexMain = Array.IndexOf(lines, "void main()", vertexIdx);
        int vertexMainEnd = Array.IndexOf(lines, "}", vertexMain);

        // varyings + uniforms
        for (int i = vertexIdx; i < vertexMain; i++)
        {
            if (lines[i].StartsWith("out"))
            {
                sb.Append("varying");
                sb.AppendLine(lines[i].Substring(3));
            }
            if (lines[i].Contains("uniform") && !lines[i].StartsWith('#') && lines[i].EndsWith(';'))
            {
                sb.AppendLine(lines[i]);
            }
        }

        // vertex main
        sb.AppendLine("void vertex()");
        sb.AppendLine("{");
        for (int i = vertexIdx; i < vertexMain; i++)
        {
            if (lines[i].Contains("u_xlat"))
                sb.AppendLine(lines[i]);
        }
        for (int i = vertexMain + 2; i < vertexMainEnd; i++)
        {
            if (!lines[i].Contains("return;"))
                sb.AppendLine(lines[i]);
        }
        sb.AppendLine("}");

        int fragIdx = Array.IndexOf(lines, "#ifdef FRAGMENT");
        int fragMain = Array.IndexOf(lines, "void main()", fragIdx);
        int fragMainEnd = Array.IndexOf(lines, "}", fragMain);

        // fragment uniforms
        for (int i = fragIdx; i < fragMain; i++)
        {
            if (lines[i].Contains("uniform") && !lines[i].StartsWith('#') && lines[i].EndsWith(';'))
            {
                sb.AppendLine(lines[i]);
            }
        }

        // fragment main
        sb.AppendLine("void fragment()");
        sb.AppendLine("{");
        for (int i = fragIdx; i < fragMain; i++)
        {
            if (lines[i].Contains("u_xlat"))
                sb.AppendLine(lines[i]);
        }
        sb.AppendLine("vec4 SV_Target0;");
        for (int i = fragMain + 2; i < fragMainEnd; i++)
        {
            if (!lines[i].Contains("return;"))
                sb.AppendLine(lines[i]);
        }
        sb.AppendLine("ALBEDO.rgb = SV_Target0.rgb;");
        sb.AppendLine("ALPHA = SV_Target0.a;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void ParseAssetsFileInstance(AssetsManager mgr, AssetsFileInstance aFileInst)
    {
        if (aFileInst == null)
            return;
        var aFile = aFileInst.file;
        var shaders = aFile.GetAssetsOfType(AssetClassID.Shader);
        foreach (var obj in shaders)
        {
            var shaderBase = mgr.GetBaseField(aFileInst, obj);
            ShaderReader.ShaderData data = ShaderReader.ReadShader(mgr, aFileInst, shaderBase);
            Console.WriteLine(data.Name);
            string file = $"{data.Name.Replace('/', '_')}.glsl";
            File.WriteAllText(file, $"// Shader {data.Name}\n");
            File.WriteAllText(file + ".gdshader", $"// Shader {data.Name}\n");
            int idx = data.Platforms.IndexOf(15); // opengl
            var subPrograms = data.SubPrograms[idx];
            for (int i = 0; i < subPrograms.Length; i++)
            {
                File.AppendAllText(file, $"\n// Shader Variant {i} ({subPrograms[i].size})\n");
                byte[] buffer = data.ReadSubProgram(subPrograms[i]);
                File.AppendAllText(file, Encoding.UTF8.GetString(buffer));
                File.AppendAllText(file + ".gdshader", $"\n// Shader Variant {i} ({subPrograms[i].size})\n");
                File.AppendAllText(file + ".gdshader", ConvertUnityGLSLToGodot(Encoding.UTF8.GetString(buffer)));
            }
        }
    }
}