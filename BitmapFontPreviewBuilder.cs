using System.Collections.Generic;
using System.Linq;

// 负责根据当前配置和字形列表生成窗口预览文本。
public static class BitmapFontPreviewBuilder
{
    public static string BuildPreviewText(BitmapFontBuildProfile profile, List<BitmapGlyphMetrics> glyphs)
    {
        if (profile != null && !string.IsNullOrWhiteSpace(profile.previewText))
        {
            return profile.previewText;
        }

        if (glyphs == null || glyphs.Count == 0)
        {
            return string.Empty;
        }

        return string.Concat(glyphs
            .Where(item => item != null && !string.IsNullOrEmpty(item.Character))
            .Select(item => item.Character));
    }
}
