using System.Collections.Generic;
using UnityEngine;

// 描述一次扫描得到的源图片条目。
public sealed class BitmapFontScanItem
{
    public string AbsolutePath;
    public string FileName;
    public string Character;
    public string Error;
    public bool IsValid;
}

// 描述单个字符裁边后的几何信息和贴图数据。
public sealed class BitmapGlyphMetrics
{
    public string Character;
    public string SourcePath;
    public int SourceWidth;
    public int SourceHeight;
    public int TrimmedX;
    public int TrimmedY;
    public int TrimmedWidth;
    public int TrimmedHeight;
    public int Advance;
    public int BearingX;
    public int BearingY;
    public Texture2D TrimmedTexture;
}

// 描述字符被放入 Atlas 后的像素区域。
public sealed class BitmapAtlasEntry
{
    public string Character;
    public RectInt PixelRect;
    public BitmapGlyphMetrics Metrics;
}

// 汇总一次构建过程中的信息、警告和错误。
public sealed class BitmapFontBuildReport
{
    public readonly List<string> Infos = new List<string>();
    public readonly List<string> Warnings = new List<string>();
    public readonly List<string> Errors = new List<string>();

    public bool HasBlockingError
    {
        get { return Errors.Count > 0; }
    }
}
