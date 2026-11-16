using System;
using UnityEditor;

internal static class LanguageAssetPathResolver
{
    public static bool IsUnderAnyRoot(string path, string[] roots)
    {
        foreach (string root in roots)
        {
            if (path.StartsWith(root, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetLocalizedAssetPath(
        string currentPath,
        string[] roots,
        DemoLanguage language)
    {
        if (string.IsNullOrEmpty(currentPath) ||
            !IsUnderAnyRoot(currentPath, roots))
        {
            return null;
        }

        foreach (string languageName in Enum.GetNames(typeof(DemoLanguage)))
        {
            string languageSegment = "/" + languageName + "/";
            if (!currentPath.Contains(languageSegment))
            {
                continue;
            }

            string targetPath = currentPath.Replace(
                languageSegment,
                "/" + language + "/");
            return AssetDatabase.LoadMainAssetAtPath(targetPath) != null
                ? targetPath
                : null;
        }

        return null;
    }
}
