using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// 负责把 Atlas、材质、字体和构建日志落到 Unity 资源目录。
public static class BitmapFontAssetWriter
{
    public static void WriteAll(
        BitmapFontBuildProfile profile,
        Texture2D atlas,
        List<BitmapAtlasEntry> entries,
        BitmapFontBuildReport report)
    {
        if (profile == null || atlas == null || entries == null || report == null)
        {
            return;
        }

        string outputFolder = NormalizeAssetPath(profile.outputFolder);
        if (!outputFolder.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
        {
            report.Errors.Add($"输出目录必须位于 Assets 下: {profile.outputFolder}");
            return;
        }

        EnsureFolder(outputFolder);

        string atlasPath = $"{outputFolder}/Atlas.png";
        string materialPath = $"{outputFolder}/Atlas.mat";
        string fontPath = $"{outputFolder}/{profile.fontName}.fontsettings";
        string reportPath = $"{outputFolder}/{profile.fontName}_BuildReport.txt";

        File.WriteAllBytes(ToAbsolutePath(atlasPath), atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);

        Texture2D atlasTexture = ConfigureAtlasImporter(atlasPath, profile.usePointFilter);
        Material material = CreateOrUpdateMaterial(profile, materialPath, atlasTexture);
        CreateOrUpdateFont(profile, fontPath, material, atlasTexture, entries, report);
        WriteReport(reportPath, report);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Texture2D ConfigureAtlasImporter(string atlasPath, bool usePointFilter)
    {
        TextureImporter importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = usePointFilter ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
    }

    private static Material CreateOrUpdateMaterial(BitmapFontBuildProfile profile, string materialPath, Texture2D atlasTexture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = profile.materialShader != null ? profile.materialShader : Shader.Find("UI/Default");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        material.shader = profile.materialShader != null ? profile.materialShader : Shader.Find("UI/Default");
        material.mainTexture = atlasTexture;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateOrUpdateFont(
        BitmapFontBuildProfile profile,
        string fontPath,
        Material material,
        Texture2D atlasTexture,
        List<BitmapAtlasEntry> entries,
        BitmapFontBuildReport report)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            font = new Font();
            AssetDatabase.CreateAsset(font, fontPath);
            font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        }

        int resolvedLineHeight = profile.useAutoLineHeight
            ? GetAutoLineHeight(entries)
            : Mathf.Max(1, profile.lineHeight);
        int resolvedBaseLine = profile.useAutoBaseLine
            ? GetAutoBaseLine(entries, resolvedLineHeight)
            : profile.baseLine;

        CharacterInfo[] characterInfos = entries
            .Where(item => item != null && item.Metrics != null && !string.IsNullOrEmpty(item.Character))
            .Select(item => BuildCharacterInfo(item, atlasTexture.width, atlasTexture.height))
            .ToArray();

        WriteFontAssetYaml(
            fontPath,
            profile.fontName,
            materialPath: AssetDatabase.GetAssetPath(material),
            atlasPath: AssetDatabase.GetAssetPath(atlasTexture),
            lineHeight: resolvedLineHeight,
            fontSize: resolvedLineHeight,
            ascent: 0f,
            characterInfos: characterInfos);

        AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceUpdate);

        report.Infos.Add($"Font 资源生成成功: {fontPath}");
    }

    private static CharacterInfo BuildCharacterInfo(
        BitmapAtlasEntry entry,
        int atlasWidth,
        int atlasHeight)
    {
        RectInt rect = entry.PixelRect;
        BitmapGlyphMetrics metrics = entry.Metrics;
        float u = (float)rect.xMin / atlasWidth;
        float v = (float)rect.yMin / atlasHeight;
        float width = (float)rect.width / atlasWidth;
        float height = (float)rect.height / atlasHeight;

        return new CharacterInfo
        {
            index = entry.Character[0],
            uv = new Rect(u, v, width, height),
            // 保留原图顶部透明留白的纵向偏移，避免较矮符号在字体排版时整体贴上。
            vert = new Rect(0f, -metrics.BearingY, metrics.TrimmedWidth, -metrics.TrimmedHeight),
            advance = metrics.Advance
        };
    }

    private static void WriteFontAssetYaml(
        string fontPath,
        string fontName,
        string materialPath,
        string atlasPath,
        float lineHeight,
        float fontSize,
        float ascent,
        CharacterInfo[] characterInfos)
    {
        string materialGuid = AssetDatabase.AssetPathToGUID(materialPath);
        string atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
        StringBuilder builder = new StringBuilder(4096);

        builder.AppendLine("%YAML 1.1");
        builder.AppendLine("%TAG !u! tag:yousandi.cn,2023:");
        builder.AppendLine("--- !u!128 &12800000");
        builder.AppendLine("Font:");
        builder.AppendLine("  m_ObjectHideFlags: 0");
        builder.AppendLine("  m_CorrespondingSourceObject: {fileID: 0}");
        builder.AppendLine("  m_PrefabInstance: {fileID: 0}");
        builder.AppendLine("  m_PrefabAsset: {fileID: 0}");
        builder.AppendLine($"  m_Name: {fontName}");
        builder.AppendLine("  serializedVersion: 5");
        builder.AppendLine($"  m_LineSpacing: {FormatFloat(lineHeight)}");
        builder.AppendLine($"  m_DefaultMaterial: {{fileID: 2100000, guid: {materialGuid}, type: 2}}");
        builder.AppendLine($"  m_FontSize: {FormatFloat(fontSize)}");
        builder.AppendLine($"  m_Texture: {{fileID: 2800000, guid: {atlasGuid}, type: 3}}");
        builder.AppendLine("  m_AsciiStartOffset: 0");
        builder.AppendLine("  m_Tracking: 1");
        builder.AppendLine("  m_CharacterSpacing: 0");
        builder.AppendLine("  m_CharacterPadding: 1");
        builder.AppendLine("  m_ConvertCase: 0");
        builder.AppendLine("  m_CharacterRects:");

        if (characterInfos != null)
        {
            foreach (CharacterInfo info in characterInfos)
            {
                AppendCharacterInfoYaml(builder, info);
            }
        }

        builder.AppendLine("  m_KerningValues: []");
        builder.AppendLine("  m_PixelScale: 0.1");
        builder.AppendLine("  m_FontData: ");
        builder.AppendLine($"  m_Ascent: {FormatFloat(ascent)}");
        builder.AppendLine("  m_Descent: 0");
        builder.AppendLine("  m_DefaultStyle: 0");
        builder.AppendLine("  m_FontNames: []");
        builder.AppendLine("  m_FallbackFonts: []");
        builder.AppendLine("  m_FontRenderingMode: 0");
        builder.AppendLine("  m_UseLegacyBoundsCalculation: 0");
        builder.AppendLine("  m_ShouldRoundAdvanceValue: 1");

        File.WriteAllText(ToAbsolutePath(fontPath), builder.ToString(), Encoding.UTF8);
    }

    private static void AppendCharacterInfoYaml(StringBuilder builder, CharacterInfo info)
    {
        builder.AppendLine("  - serializedVersion: 2");
        builder.AppendLine($"    index: {info.index}");
        builder.AppendLine("    uv:");
        builder.AppendLine("      serializedVersion: 2");
        builder.AppendLine($"      x: {FormatFloat(info.uv.x)}");
        builder.AppendLine($"      y: {FormatFloat(info.uv.y)}");
        builder.AppendLine($"      width: {FormatFloat(info.uv.width)}");
        builder.AppendLine($"      height: {FormatFloat(info.uv.height)}");
        builder.AppendLine("    vert:");
        builder.AppendLine("      serializedVersion: 2");
        builder.AppendLine($"      x: {FormatFloat(info.vert.x)}");
        builder.AppendLine($"      y: {FormatFloat(info.vert.y)}");
        builder.AppendLine($"      width: {FormatFloat(info.vert.width)}");
        builder.AppendLine($"      height: {FormatFloat(info.vert.height)}");
        builder.AppendLine($"    advance: {FormatFloat(info.advance)}");
        builder.AppendLine($"    flipped: {(info.flipped ? 1 : 0)}");
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static int GetAutoLineHeight(List<BitmapAtlasEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return 1;
        }

        int maxHeight = 0;
        foreach (BitmapAtlasEntry item in entries)
        {
            if (item == null || item.Metrics == null)
            {
                continue;
            }

            if (item.Metrics.SourceHeight > maxHeight)
            {
                maxHeight = item.Metrics.SourceHeight;
            }
        }

        return Mathf.Max(1, maxHeight);
    }

    private static int GetAutoBaseLine(List<BitmapAtlasEntry> entries, int lineHeight)
    {
        if (entries == null || entries.Count == 0)
        {
            return Mathf.Max(1, lineHeight);
        }

        int maxBaseLine = 0;
        foreach (BitmapAtlasEntry item in entries)
        {
            if (item == null || item.Metrics == null)
            {
                continue;
            }

            int candidate = item.Metrics.TrimmedHeight + item.Metrics.BearingY;
            if (candidate > maxBaseLine)
            {
                maxBaseLine = candidate;
            }
        }

        return Mathf.Clamp(maxBaseLine, 1, Mathf.Max(1, lineHeight));
    }

    private static void WriteReport(string reportPath, BitmapFontBuildReport report)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Infos:");
        foreach (string item in report.Infos)
        {
            builder.AppendLine(item);
        }

        builder.AppendLine();
        builder.AppendLine("Warnings:");
        foreach (string item in report.Warnings)
        {
            builder.AppendLine(item);
        }

        builder.AppendLine();
        builder.AppendLine("Errors:");
        foreach (string item in report.Errors)
        {
            builder.AppendLine(item);
        }

        File.WriteAllText(ToAbsolutePath(reportPath), builder.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceUpdate);
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        string[] segments = NormalizeAssetPath(assetFolderPath).Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = $"{current}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }

            current = next;
        }
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        return (assetPath ?? string.Empty).Replace("\\", "/").TrimEnd('/');
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
