using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 负责把裁切后的字形贴图打包到单张 Atlas 中，并记录每个字符的像素区域。
public static class BitmapAtlasPacker
{
    private static readonly int[] CandidateSizes = { 256, 512, 1024, 2048, 4096 };

    public static Texture2D Pack(
        List<BitmapGlyphMetrics> glyphs,
        BitmapFontBuildProfile profile,
        List<BitmapAtlasEntry> entries,
        BitmapFontBuildReport report)
    {
        if (glyphs == null || glyphs.Count == 0)
        {
            if (report != null)
            {
                report.Errors.Add("没有可打包的字形数据。");
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

        if (entries == null)
        {
            if (report != null)
            {
                report.Errors.Add("Atlas 输出条目列表不能为空。");
            }

            return null;
        }

        int effectiveMaxAtlasSize = Mathf.Clamp(Mathf.Max(2048, profile.atlasMaxSize), 256, 4096);
        int[] allowedSizes = CandidateSizes
            .Where(size => size <= effectiveMaxAtlasSize)
            .ToArray();

        foreach (int atlasSize in allowedSizes)
        {
            Texture2D atlas = TryPackAtSize(glyphs, profile.padding, atlasSize, profile.usePointFilter, entries);
            if (atlas != null)
            {
                if (report != null)
                {
                    report.Infos.Add($"Atlas 打包成功: {atlasSize}x{atlasSize}");
                }

                return atlas;
            }
        }

        if (report != null)
        {
            report.Errors.Add($"Atlas 尺寸不足，最大尝试尺寸: {effectiveMaxAtlasSize}");
        }

        return null;
    }

    private static Texture2D TryPackAtSize(
        List<BitmapGlyphMetrics> glyphs,
        int padding,
        int atlasSize,
        bool usePointFilter,
        List<BitmapAtlasEntry> entries)
    {
        entries.Clear();

        Texture2D atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);
        atlas.wrapMode = TextureWrapMode.Clamp;
        atlas.filterMode = usePointFilter ? FilterMode.Point : FilterMode.Bilinear;
        atlas.anisoLevel = 0;
        Texture2D[] textures = glyphs
            .Select(item => item != null ? item.TrimmedTexture : null)
            .ToArray();

        if (textures.Any(texture => texture == null))
        {
            Object.DestroyImmediate(atlas);
            return null;
        }

        Rect[] packedRects = atlas.PackTextures(textures, Mathf.Max(0, padding), atlasSize, false);
        if (packedRects.Length != glyphs.Count)
        {
            Object.DestroyImmediate(atlas);
            return null;
        }

        for (int i = 0; i < glyphs.Count; i++)
        {
            Rect uvRect = packedRects[i];
            if (uvRect.width <= 0f || uvRect.height <= 0f)
            {
                Object.DestroyImmediate(atlas);
                entries.Clear();
                return null;
            }

            RectInt pixelRect = new RectInt(
                Mathf.RoundToInt(uvRect.x * atlas.width),
                Mathf.RoundToInt(uvRect.y * atlas.height),
                Mathf.RoundToInt(uvRect.width * atlas.width),
                Mathf.RoundToInt(uvRect.height * atlas.height));

            // PackTextures 会在图集太小时自动缩放纹理，这会让最终字体边缘变糊、变锯齿。
            // 这里要求图集中字符尺寸不得小于原始裁边尺寸，否则继续尝试更大的图集。
            if (pixelRect.width < glyphs[i].TrimmedWidth || pixelRect.height < glyphs[i].TrimmedHeight)
            {
                Object.DestroyImmediate(atlas);
                entries.Clear();
                return null;
            }

            entries.Add(new BitmapAtlasEntry
            {
                Character = glyphs[i].Character,
                Metrics = glyphs[i],
                PixelRect = pixelRect
            });
        }

        ExtrudeAtlasPadding(atlas, entries, Mathf.Max(1, padding));
        atlas.Apply(false, false);
        return atlas;
    }

    // 将字符边缘复制到 padding 区，避免图集缩放预览时把相邻字符颜色采样进来。
    private static void ExtrudeAtlasPadding(Texture2D atlas, List<BitmapAtlasEntry> entries, int padding)
    {
        if (atlas == null || entries == null || entries.Count == 0 || padding <= 0)
        {
            return;
        }

        foreach (BitmapAtlasEntry entry in entries)
        {
            RectInt rect = entry.PixelRect;
            if (rect.width <= 0 || rect.height <= 0)
            {
                continue;
            }

            int left = rect.xMin;
            int right = rect.xMax - 1;
            int bottom = rect.yMin;
            int top = rect.yMax - 1;

            for (int offset = 1; offset <= padding; offset++)
            {
                for (int y = bottom; y <= top; y++)
                {
                    SetPixelSafe(atlas, left - offset, y, atlas.GetPixel(left, y));
                    SetPixelSafe(atlas, right + offset, y, atlas.GetPixel(right, y));
                }

                for (int x = left; x <= right; x++)
                {
                    SetPixelSafe(atlas, x, bottom - offset, atlas.GetPixel(x, bottom));
                    SetPixelSafe(atlas, x, top + offset, atlas.GetPixel(x, top));
                }

                SetPixelSafe(atlas, left - offset, bottom - offset, atlas.GetPixel(left, bottom));
                SetPixelSafe(atlas, left - offset, top + offset, atlas.GetPixel(left, top));
                SetPixelSafe(atlas, right + offset, bottom - offset, atlas.GetPixel(right, bottom));
                SetPixelSafe(atlas, right + offset, top + offset, atlas.GetPixel(right, top));
            }
        }
    }

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (texture == null)
        {
            return;
        }

        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}
