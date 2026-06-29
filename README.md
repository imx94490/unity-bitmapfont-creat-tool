# Unity Bitmap Font Creator Tool

一个 Unity 编辑器扩展工具，用于将分散的单字符 PNG 图片快速打包生成 Unity 可用的位图字体（Bitmap Font）资源。

## 功能特性

- **一键生成**：从 PNG 图片序列到可用字体资源，三步完成
- **自动裁边**：智能裁剪透明边，最大化图集利用率
- **图集打包**：自动选择最小可用图集尺寸（256~4096）
- **边缘挤出**：Padding 区域像素挤出，避免采样渗色
- **预览先行**：生成前可预览 Atlas 效果和示例文本
- **灵活配置**：支持等宽/比例字体、自动行高/基线计算、字符映射覆盖

## 系统要求

- Unity 2020.3 或更高版本
- Windows / macOS / Linux

## 安装

1. 将本仓库所有 `.cs` 文件放入 Unity 项目的 `Assets/Editor/BitmapFont` 目录（或任意 Editor 目录下）
2. Unity 编译完成后，通过菜单 `Tools → Bitmap Font → Generator` 打开工具窗口

## 使用方法

### 1. 创建构建配置

在 Project 窗口右键 → `Create → Tools → Bitmap Font → Build Profile`，创建一个配置资产。

### 2. 配置参数

| 参数分类 | 参数 | 说明 |
|---------|------|------|
| 基础信息 | fontName | 生成字体的名称 |
| | sourceFolder | 源 PNG 图片所在目录（绝对路径或 Assets/ 相对路径） |
| | outputFolder | 输出目录，必须位于 Assets 下 |
| | filePattern | 文件名匹配模式，默认 `*.png` |
| 裁边与间距 | trimTransparent | 是否裁剪透明边 |
| | alphaThreshold | 透明判定阈值（0~255） |
| | padding | 字符在 Atlas 中的间距（像素） |
| | spacingX | 字符额外水平间距 |
| 排版 | lineHeight | 行高（设为 0 且开启自动时自动计算） |
| | baseLine | 基线（设为 0 且开启自动时自动计算） |
| | useAutoLineHeight | 自动计算行高 |
| | useAutoBaseLine | 自动计算基线 |
| | forceMonospace | 强制等宽 |
| | fixedAdvance | 等宽时的固定宽度 |
| 图集与显示 | atlasMaxSize | 图集最大尺寸 |
| | usePointFilter | 使用点过滤（像素风字体推荐开启） |
| | previewText | 预览用示例文本 |
| | materialShader | 材质使用的 Shader |
| 特殊映射 | manualOverrides | 手动字符映射（文件名→字符） |

### 3. 字符命名规则

- 默认规则：PNG 文件名（不含扩展名）长度为 1 时，文件名本身即为字符
  - 例如：`A.png` → 字符 `A`，`0.png` → 字符 `0`
- 特殊字符：通过 `manualOverrides` 手动映射
  - 例如：`plus.png` → `+`，`minus.png` → `-`

### 4. 生成字体

1. 打开 `Tools → Bitmap Font → Generator`
2. 拖入 Build Profile 配置
3. 点击 **扫描** — 查看识别到的字符列表
4. 点击 **预览** — 生成 Atlas 预览图，确认效果
5. 点击 **生成** — 输出字体资源到指定目录

生成的资源包括：

```
{outputFolder}/
├── Atlas.png                # 字体图集纹理
├── Atlas.mat                # 字体材质
├── {fontName}.fontsettings  # 字体资源
└── {fontName}_BuildReport.txt  # 构建报告
```

## 架构设计

```
源目录 PNG 图片
     │
     ▼
BitmapFontSourceScanner  (扫描器)
     │  输出：List<BitmapFontScanItem>
     │
     ▼
BitmapGlyphAnalyzer      (分析器)
     │  输出：List<BitmapGlyphMetrics>
     │  - 裁透明边、计算排版度量
     │
     ▼
BitmapAtlasPacker        (打包器)
     │  输出：Texture2D + List<BitmapAtlasEntry>
     │  - 图集打包、Padding 挤出
     │
     ▼
BitmapFontAssetWriter    (写入器)
     │
     ├─► Atlas.png
     ├─► Atlas.mat
     ├─► {FontName}.fontsettings
     └─► {FontName}_BuildReport.txt
```

## 文件说明

| 文件 | 职责 |
|------|------|
| `BitmapFontBuildProfile.cs` | ScriptableObject 配置文件 |
| `BitmapFontBuildModels.cs` | 数据模型定义 |
| `BitmapFontSourceScanner.cs` | 源图片扫描与字符映射 |
| `BitmapGlyphAnalyzer.cs` | 图片分析、裁边、度量计算 |
| `BitmapAtlasPacker.cs` | 图集打包与边缘挤出 |
| `BitmapFontAssetWriter.cs` | Unity 资源写入（贴图/材质/字体） |
| `BitmapFontPreviewBuilder.cs` | 预览文本生成 |
| `BitmapFontGeneratorWindow.cs` | 编辑器窗口 UI |

## 许可证

MIT License
