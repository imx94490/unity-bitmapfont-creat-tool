using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// 负责把源目录中的图片文件转换为后续构建可用的扫描条目。
public static class BitmapFontSourceScanner
{
    public static List<BitmapFontScanItem> Scan(BitmapFontBuildProfile profile, BitmapFontBuildReport report)
    {
        List<BitmapFontScanItem> results = new List<BitmapFontScanItem>();
        if (profile == null)
        {
            if (report != null)
            {
                report.Errors.Add("构建配置不能为空。");
            }

            return results;
        }

        string sourceDirectory = ResolveSourceDirectory(profile.sourceFolder);
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
        {
            if (report != null)
            {
                report.Errors.Add($"源目录不存在: {profile.sourceFolder}");
            }

            return results;
        }

        string searchPattern = string.IsNullOrWhiteSpace(profile.filePattern) ? "*.png" : profile.filePattern;
        string[] files = Directory.GetFiles(sourceDirectory, searchPattern, SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        if (files.Length == 0)
        {
            if (report != null)
            {
                report.Errors.Add($"目录中没有匹配文件: {sourceDirectory}");
            }

            return results;
        }

        foreach (string filePath in files)
        {
            results.Add(BuildScanItem(filePath, profile, report));
        }

        return results;
    }

    public static string ResolveSourceDirectory(string profileSourceFolder)
    {
        if (string.IsNullOrWhiteSpace(profileSourceFolder))
        {
            return string.Empty;
        }

        if (profileSourceFolder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
            string relativePath = profileSourceFolder.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        }

        return Path.GetFullPath(profileSourceFolder);
    }

    public static void ValidateDuplicates(List<BitmapFontScanItem> items, BitmapFontBuildReport report)
    {
        if (items == null || report == null)
        {
            return;
        }

        IEnumerable<IGrouping<string, BitmapFontScanItem>> duplicateGroups = items
            .Where(item => item != null && item.IsValid && !string.IsNullOrEmpty(item.Character))
            .GroupBy(item => item.Character, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (IGrouping<string, BitmapFontScanItem> group in duplicateGroups)
        {
            string joinedNames = string.Join(", ", group.Select(item => item.FileName));
            report.Errors.Add($"重复字符映射: '{group.Key}' <- {joinedNames}");
        }
    }

    private static BitmapFontScanItem BuildScanItem(string absolutePath, BitmapFontBuildProfile profile, BitmapFontBuildReport report)
    {
        string fileName = Path.GetFileNameWithoutExtension(absolutePath);
        string mappedCharacter = ResolveCharacter(fileName, profile);

        BitmapFontScanItem item = new BitmapFontScanItem
        {
            AbsolutePath = absolutePath,
            FileName = fileName,
            Character = mappedCharacter,
            IsValid = !string.IsNullOrEmpty(mappedCharacter)
        };

        if (!item.IsValid)
        {
            item.Error = $"文件名无法映射字符: {fileName}";
            if (report != null)
            {
                report.Errors.Add(item.Error);
            }
        }

        return item;
    }

    private static string ResolveCharacter(string fileNameWithoutExtension, BitmapFontBuildProfile profile)
    {
        if (profile != null && profile.manualOverrides != null)
        {
            foreach (BitmapFontCharOverride item in profile.manualOverrides)
            {
                if (item == null)
                {
                    continue;
                }

                if (string.Equals(item.fileNameWithoutExtension, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return item.mappedCharacter;
                }
            }
        }

        return fileNameWithoutExtension != null && fileNameWithoutExtension.Length == 1
            ? fileNameWithoutExtension
            : string.Empty;
    }
}
