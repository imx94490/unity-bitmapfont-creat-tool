using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 提供 BitmapFont 的扫描、预览和生成入口窗口。
public sealed class BitmapFontGeneratorWindow : EditorWindow
{
    [SerializeField] private BitmapFontBuildProfile profile;

    private Vector2 _scrollPosition;
    private List<BitmapFontScanItem> _scanItems = new List<BitmapFontScanItem>();
    private List<BitmapGlyphMetrics> _glyphs = new List<BitmapGlyphMetrics>();
    private List<BitmapAtlasEntry> _atlasEntries = new List<BitmapAtlasEntry>();
    private BitmapFontBuildReport _report = new BitmapFontBuildReport();
    private Texture2D _previewAtlas;
    private string _previewText = string.Empty;

    [MenuItem("Tools/Bitmap Font/Generator")]
    public static void Open()
    {
        GetWindow<BitmapFontGeneratorWindow>("Bitmap Font Generator");
    }

    private void OnDisable()
    {
        ClearPreviewAtlas();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("Bitmap Font Generator", EditorStyles.boldLabel);
        profile = (BitmapFontBuildProfile)EditorGUILayout.ObjectField("Profile", profile, typeof(BitmapFontBuildProfile), false);

        using (new EditorGUI.DisabledScope(profile == null))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("扫描", GUILayout.Height(28f)))
                {
                    Scan();
                }

                if (GUILayout.Button("预览", GUILayout.Height(28f)))
                {
                    BuildPreview();
                }

                using (new EditorGUI.DisabledScope(_report == null || _report.HasBlockingError || _previewAtlas == null))
                {
                    if (GUILayout.Button("生成", GUILayout.Height(28f)))
                    {
                        Generate();
                    }
                }
            }
        }

        EditorGUILayout.Space();
        DrawScanItems();
        EditorGUILayout.Space();
        DrawReport();
        EditorGUILayout.Space();
        DrawPreview();

        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        ClearPreviewAtlas();
        _glyphs.Clear();
        _atlasEntries.Clear();
        _previewText = string.Empty;
        _report = new BitmapFontBuildReport();
        _scanItems = BitmapFontSourceScanner.Scan(profile, _report);
        BitmapFontSourceScanner.ValidateDuplicates(_scanItems, _report);
    }

    private void BuildPreview()
    {
        ClearPreviewAtlas();
        _glyphs.Clear();
        _atlasEntries.Clear();
        _previewText = string.Empty;
        _report = new BitmapFontBuildReport();

        _scanItems = BitmapFontSourceScanner.Scan(profile, _report);
        BitmapFontSourceScanner.ValidateDuplicates(_scanItems, _report);
        if (_report.HasBlockingError)
        {
            return;
        }

        foreach (BitmapFontScanItem item in _scanItems)
        {
            if (item == null || !item.IsValid)
            {
                continue;
            }

            BitmapGlyphMetrics metrics = BitmapGlyphAnalyzer.Analyze(item, profile, _report);
            if (metrics != null)
            {
                _glyphs.Add(metrics);
            }
        }

        if (_report.HasBlockingError || _glyphs.Count == 0)
        {
            return;
        }

        _previewAtlas = BitmapAtlasPacker.Pack(_glyphs, profile, _atlasEntries, _report);
        _previewText = BitmapFontPreviewBuilder.BuildPreviewText(profile, _glyphs);
    }

    private void Generate()
    {
        if (profile == null || _report == null || _report.HasBlockingError || _previewAtlas == null)
        {
            return;
        }

        BitmapFontAssetWriter.WriteAll(profile, _previewAtlas, _atlasEntries, _report);
    }

    private void DrawScanItems()
    {
        EditorGUILayout.LabelField($"扫描结果 ({_scanItems.Count})", EditorStyles.boldLabel);
        if (_scanItems.Count == 0)
        {
            EditorGUILayout.HelpBox("尚未扫描到图片。", MessageType.None);
            return;
        }

        foreach (BitmapFontScanItem item in _scanItems)
        {
            if (item == null)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("文件名", item.FileName ?? string.Empty);
                EditorGUILayout.LabelField("字符", item.Character ?? string.Empty);
                EditorGUILayout.LabelField("路径", item.AbsolutePath ?? string.Empty);
                if (!string.IsNullOrEmpty(item.Error))
                {
                    EditorGUILayout.HelpBox(item.Error, MessageType.Error);
                }
            }
        }
    }

    private void DrawReport()
    {
        if (_report == null)
        {
            return;
        }

        EditorGUILayout.LabelField("构建信息", EditorStyles.boldLabel);

        foreach (string info in _report.Infos)
        {
            EditorGUILayout.HelpBox(info, MessageType.Info);
        }

        foreach (string warning in _report.Warnings)
        {
            EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        foreach (string error in _report.Errors)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);
        EditorGUILayout.TextField("示例文本", _previewText ?? string.Empty);

        if (_previewAtlas == null)
        {
            EditorGUILayout.HelpBox("尚未生成预览图集。", MessageType.None);
            return;
        }

        Rect previewRect = GUILayoutUtility.GetRect(256f, 256f, GUILayout.ExpandWidth(false));
        EditorGUI.DrawTextureTransparent(previewRect, _previewAtlas, ScaleMode.ScaleToFit);
    }

    private void ClearPreviewAtlas()
    {
        if (_previewAtlas != null)
        {
            DestroyImmediate(_previewAtlas);
            _previewAtlas = null;
        }
    }
}
