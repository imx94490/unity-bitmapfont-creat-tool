using System;
using System.Collections.Generic;
using UnityEngine;

// 保存一套 BitmapFont 构建参数，便于重复生成多套艺术字。
[CreateAssetMenu(
    fileName = "BitmapFontBuildProfile",
    menuName = "Tools/Bitmap Font/Build Profile")]
public sealed class BitmapFontBuildProfile : ScriptableObject
{
    [Header("基础信息")]
    public string fontName = "ArtNumberOrange";
    public string sourceFolder = @"c:\Users\YF-MSJ-007\Desktop\艺术字";
    public string outputFolder = "Assets/Art/Common/Fonts/BitmapFonts/ArtNumberOrange";
    public string filePattern = "*.png";

    [Header("裁边与间距")]
    public bool trimTransparent = true;
    public int alphaThreshold = 8;
    public int padding = 2;
    public int spacingX = 0;

    [Header("排版")]
    public int lineHeight = 0;
    public int baseLine = 0;
    public bool useAutoLineHeight = true;
    public bool useAutoBaseLine = true;
    public bool forceMonospace = false;
    public int fixedAdvance = 0;

    [Header("图集与显示")]
    public int atlasMaxSize = 1024;
    public bool usePointFilter = false;
    public string previewText = "0123456789+-";
    public Shader materialShader;

    [Header("特殊映射")]
    public List<BitmapFontCharOverride> manualOverrides = new List<BitmapFontCharOverride>();
}

// 当文件名不是单字符时，允许手动覆盖字符映射。
[Serializable]
public sealed class BitmapFontCharOverride
{
    public string fileNameWithoutExtension;
    public string mappedCharacter;
}
