using System.IO;
using UnityEngine;

// 负责读取图片、裁掉透明边并生成后续打包所需的字形度量。
public static class BitmapGlyphAnalyzer
{
    public static BitmapGlyphMetrics Analyze(BitmapFontScanItem scanItem, BitmapFontBuildProfile profile, BitmapFontBuildReport report)
    {
        if (scanItem == null)
        {
            if (report != null)
            {
                report.Errors.Add("扫描条目不能为空。");
            }

            return null;
        }

        if (profile == null)
        {
            if (report != null)
            {
                report.Errors.Add("构建配置不能为空。");
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(scanItem.AbsolutePath) || !File.Exists(scanItem.AbsolutePath))
        {
            if (report != null)
            {
                report.Errors.Add($"源图片不存在: {scanItem.AbsolutePath}");
            }

            return null;
        }

        byte[] bytes = File.ReadAllBytes(scanItem.AbsolutePath);
        Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!sourceTexture.LoadImage(bytes))
        {
            Object.DestroyImmediate(sourceTexture);
            if (report != null)
            {
                report.Errors.Add($"图片读取失败: {scanItem.AbsolutePath}");
            }

            return null;
        }

        RectInt trimRect = profile.trimTransparent
            ? CalculateTrimRect(sourceTexture, profile.alphaThreshold)
            : new RectInt(0, 0, sourceTexture.width, sourceTexture.height);

        if (trimRect.width <= 0 || trimRect.height <= 0)
        {
            Object.DestroyImmediate(sourceTexture);
            if (report != null)
            {
                report.Errors.Add($"图片为空白透明图: {scanItem.AbsolutePath}");
            }

            return null;
        }

        BitmapGlyphMetrics metrics = BuildMetrics(scanItem, sourceTexture, trimRect, profile);
        Object.DestroyImmediate(sourceTexture);
        return metrics;
    }

    private static RectInt CalculateTrimRect(Texture2D texture, int alphaThreshold)
    {
        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color32 pixel = pixels[(y * texture.width) + x];
                if (pixel.a <= alphaThreshold)
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new RectInt(0, 0, 0, 0);
        }

        return new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
    }

    private static Texture2D BuildTrimmedTexture(Texture2D sourceTexture, RectInt trimRect, bool usePointFilter)
    {
        Texture2D trimmedTexture = new Texture2D(trimRect.width, trimRect.height, TextureFormat.RGBA32, false);
        Color[] pixels = sourceTexture.GetPixels(trimRect.x, trimRect.y, trimRect.width, trimRect.height);
        trimmedTexture.SetPixels(pixels);
        trimmedTexture.wrapMode = TextureWrapMode.Clamp;
        trimmedTexture.filterMode = usePointFilter ? FilterMode.Point : FilterMode.Bilinear;
        trimmedTexture.anisoLevel = 0;
        trimmedTexture.Apply(false, false);
        return trimmedTexture;
    }

    private static BitmapGlyphMetrics BuildMetrics(
        BitmapFontScanItem scanItem,
        Texture2D sourceTexture,
        RectInt trimRect,
        BitmapFontBuildProfile profile)
    {
        Texture2D trimmedTexture = BuildTrimmedTexture(sourceTexture, trimRect, profile.usePointFilter);
        int advance = profile.forceMonospace
            ? Mathf.Max(1, profile.fixedAdvance)
            : trimRect.width + profile.spacingX;

        return new BitmapGlyphMetrics
        {
            Character = scanItem.Character,
            SourcePath = scanItem.AbsolutePath,
            SourceWidth = sourceTexture.width,
            SourceHeight = sourceTexture.height,
            TrimmedX = trimRect.x,
            TrimmedY = trimRect.y,
            TrimmedWidth = trimRect.width,
            TrimmedHeight = trimRect.height,
            Advance = advance,
            BearingX = trimRect.x,
            BearingY = sourceTexture.height - (trimRect.y + trimRect.height),
            TrimmedTexture = trimmedTexture
        };
    }
}
