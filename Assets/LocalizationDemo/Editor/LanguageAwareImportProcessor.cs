using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanguageAwareImportProcessor : AssetPostprocessor
{
    private const string LanguageSettingPath = "ProjectSettings/LocalizationLanguage";
    private const string ResourceRoot = "Assets/LocalizationDemo/Resources/";
    private const string AtlasResourceRoot = ResourceRoot + "Atlases/";
    private static readonly string[] LanguageResourceRoots =
    {
        ResourceRoot + "Localization/",
        ResourceRoot + "Localization/Fonts/",
        ResourceRoot + "Models/Textures/",
        ResourceRoot + "Effects/Textures/",
        AtlasResourceRoot
    };
    private static readonly string[] FontRoots =
    {
        ResourceRoot + "Localization/Fonts/"
    };
    private static readonly string[] MaterialRoots =
    {
        ResourceRoot + "Models/Materials/",
        ResourceRoot + "Effects/Materials/"
    };
    private static readonly string[] AtlasRoots = { AtlasResourceRoot };

    private static readonly HashSet<string> PendingAssets = new HashSet<string>();
    private static bool processScheduled;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (IsSupportedAsset(path))
            {
                PendingAssets.Add(path);
            }
        }

        ScheduleProcessing();
    }

    public static bool TryGetCurrentLanguage(out DemoLanguage language)
    {
        if (!File.Exists(LanguageSettingPath))
        {
            language = DemoLanguage.CN;
            return true;
        }

        string value = File.ReadAllText(LanguageSettingPath).Trim();
        if (!Enum.TryParse(value, false, out language) ||
            !Enum.IsDefined(typeof(DemoLanguage), language))
        {
            Debug.LogError(
                "Invalid localization setting in " + LanguageSettingPath + ": " + value);
            return false;
        }

        return true;
    }

    public static void SetCurrentLanguage(DemoLanguage language)
    {
        File.WriteAllText(LanguageSettingPath, language.ToString());
        QueueAllSupportedAssets();
    }

    private static void QueueAllSupportedAssets()
    {
        string[] guids = AssetDatabase.FindAssets(
            string.Empty,
            new[] { ResourceRoot.TrimEnd('/') });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsSupportedAsset(path))
            {
                PendingAssets.Add(path);
            }
        }

        ScheduleProcessing();
    }

    private static bool IsSupportedAsset(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith(ResourceRoot, StringComparison.Ordinal))
        {
            return false;
        }

        string extension = Path.GetExtension(path);
        if (extension == ".mat")
        {
            return IsUnderAnyRoot(path, MaterialRoots);
        }

        return extension == ".prefab" || extension == ".asset";
    }

    private static bool IsUnderAnyRoot(string path, string[] roots)
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

    private static void ScheduleProcessing()
    {
        if (processScheduled || PendingAssets.Count == 0)
        {
            return;
        }

        processScheduled = true;
        EditorApplication.delayCall += ProcessPendingAssets;
    }

    private static void ProcessPendingAssets()
    {
        processScheduled = false;
        string[] paths = new string[PendingAssets.Count];
        PendingAssets.CopyTo(paths);
        PendingAssets.Clear();

        bool changed = false;
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                continue;
            }

            string extension = Path.GetExtension(path);
            if (extension == ".mat")
            {
                changed |= ProcessMaterial(path);
            }
            else if (extension == ".prefab")
            {
                changed |= ProcessPrefab(path);
            }
            else if (extension == ".asset")
            {
                changed |= ProcessAtlas(path);
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }

        ScheduleProcessing();
    }

    private static bool ProcessMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null || material.shader == null)
        {
            return false;
        }

        DemoLanguage targetLanguage;
        if (!TryGetTargetLanguage(path, MaterialRoots, out targetLanguage))
        {
            return false;
        }

        bool changed = false;
        Shader shader = material.shader;
        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
            {
                continue;
            }

            string propertyName = ShaderUtil.GetPropertyName(shader, i);
            Texture currentTexture = material.GetTexture(propertyName);
            if (currentTexture == null)
            {
                continue;
            }

            string targetPath = GetTargetLanguagePath(
                AssetDatabase.GetAssetPath(currentTexture),
                LanguageResourceRoots,
                targetLanguage);
            if (targetPath == null)
            {
                continue;
            }

            Texture targetTexture = AssetDatabase.LoadAssetAtPath<Texture>(targetPath);
            if (targetTexture == null)
            {
                Debug.LogError("Localized texture not found: " + targetPath);
                continue;
            }

            if (targetTexture != currentTexture)
            {
                material.SetTexture(propertyName, targetTexture);
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(material);
        }

        return changed;
    }

    private static bool ProcessPrefab(string path)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        if (prefab == null)
        {
            Debug.LogError("Could not load imported prefab: " + path);
            return false;
        }

        bool changed = false;
        try
        {
            DemoLanguage targetLanguage;
            if (!TryGetTargetLanguage(path, new[] { ResourceRoot }, out targetLanguage))
            {
                return false;
            }

            foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null)
                {
                    continue;
                }

                string targetPath = GetTargetLanguagePath(
                    AssetDatabase.GetAssetPath(image.sprite),
                    LanguageResourceRoots,
                    targetLanguage);
                if (targetPath == null)
                {
                    continue;
                }

                Sprite targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);
                if (targetSprite == null)
                {
                    Debug.LogError("Localized Sprite not found: " + targetPath);
                    continue;
                }

                if (targetSprite != image.sprite)
                {
                    image.sprite = targetSprite;
                    changed = true;
                }
            }

            foreach (UITexture texture in prefab.GetComponentsInChildren<UITexture>(true))
            {
                if (texture.mainTexture == null)
                {
                    continue;
                }

                string targetPath = GetTargetLanguagePath(
                    AssetDatabase.GetAssetPath(texture.mainTexture),
                    LanguageResourceRoots,
                    targetLanguage);
                if (targetPath == null)
                {
                    continue;
                }

                Texture targetTexture = AssetDatabase.LoadAssetAtPath<Texture>(targetPath);
                if (targetTexture == null)
                {
                    Debug.LogError("Localized NGUI texture not found: " + targetPath);
                    continue;
                }

                if (targetTexture != texture.mainTexture)
                {
                    texture.mainTexture = targetTexture;
                    changed = true;
                }
            }

            foreach (Text text in prefab.GetComponentsInChildren<Text>(true))
            {
                if (text.font == null)
                {
                    continue;
                }

                string targetPath = GetTargetLanguagePath(
                    AssetDatabase.GetAssetPath(text.font),
                    FontRoots,
                    targetLanguage);
                Font targetFont = targetPath == null
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Font>(targetPath);
                if (targetFont != null && targetFont != text.font)
                {
                    text.font = targetFont;
                    changed = true;
                }
            }

            foreach (UILabel label in prefab.GetComponentsInChildren<UILabel>(true))
            {
                if (label.trueTypeFont == null)
                {
                    continue;
                }

                string targetPath = GetTargetLanguagePath(
                    AssetDatabase.GetAssetPath(label.trueTypeFont),
                    FontRoots,
                    targetLanguage);
                Font targetFont = targetPath == null
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Font>(targetPath);
                if (targetFont != null && targetFont != label.trueTypeFont)
                {
                    label.trueTypeFont = targetFont;
                    changed = true;
                }
            }

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                changed |= ProcessRenderer(renderer, targetLanguage);
            }

            foreach (UIAtlas atlas in prefab.GetComponentsInChildren<UIAtlas>(true))
            {
                if (atlas.replacement == null)
                {
                    continue;
                }

                string targetPath = GetTargetLanguagePath(
                    AssetDatabase.GetAssetPath((UnityEngine.Object)atlas.replacement),
                    AtlasRoots,
                    targetLanguage);
                if (targetPath == null)
                {
                    continue;
                }

                INGUIAtlas targetAtlas = (INGUIAtlas)AssetDatabase.LoadAssetAtPath(
                    targetPath,
                    typeof(INGUIAtlas));
                if (targetAtlas == null)
                {
                    Debug.LogError("Localized NGUI atlas not found: " + targetPath);
                    continue;
                }

                if (targetAtlas != atlas.replacement)
                {
                    atlas.replacement = targetAtlas;
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        return changed;
    }

    private static bool ProcessRenderer(Renderer renderer, DemoLanguage targetLanguage)
    {
        Material[] materials = renderer.sharedMaterials;
        bool changed = false;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
            {
                continue;
            }

            string targetPath = GetTargetLanguagePath(
                AssetDatabase.GetAssetPath(materials[i]),
                MaterialRoots,
                targetLanguage);
            if (targetPath == null)
            {
                continue;
            }

            Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (targetMaterial == null)
            {
                Debug.LogError("Localized material not found: " + targetPath);
                continue;
            }

            if (targetMaterial != materials[i])
            {
                materials[i] = targetMaterial;
                changed = true;
            }
        }

        if (changed)
        {
            renderer.sharedMaterials = materials;
        }

        return changed;
    }

    private static bool ProcessAtlas(string path)
    {
        NGUIAtlas atlas = AssetDatabase.LoadAssetAtPath<NGUIAtlas>(path);
        if (atlas == null || atlas.spriteMaterial == null)
        {
            return false;
        }

        DemoLanguage targetLanguage;
        if (!TryGetTargetLanguage(path, AtlasRoots, out targetLanguage))
        {
            return false;
        }

        string targetPath = GetTargetLanguagePath(
            AssetDatabase.GetAssetPath(atlas.spriteMaterial),
            MaterialRoots,
            targetLanguage);
        if (targetPath == null)
        {
            return false;
        }

        Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (targetMaterial == null)
        {
            Debug.LogError("Localized NGUI atlas material not found: " + targetPath);
            return false;
        }

        if (targetMaterial == atlas.spriteMaterial)
        {
            return false;
        }

        atlas.spriteMaterial = targetMaterial;
        EditorUtility.SetDirty(atlas);
        return true;
    }

    private static bool TryGetTargetLanguage(
        string assetPath,
        string[] languageRoots,
        out DemoLanguage targetLanguage)
    {
        if (TryGetLanguageFromPath(assetPath, languageRoots, out targetLanguage))
        {
            return true;
        }

        return TryGetCurrentLanguage(out targetLanguage);
    }

    private static bool TryGetLanguageFromPath(
        string path,
        string[] roots,
        out DemoLanguage language)
    {
        language = DemoLanguage.CN;
        if (string.IsNullOrEmpty(path) || !IsUnderAnyRoot(path, roots))
        {
            return false;
        }

        foreach (string languageName in Enum.GetNames(typeof(DemoLanguage)))
        {
            string languageSegment = "/" + languageName + "/";
            if (path.Contains(languageSegment))
            {
                language = (DemoLanguage)Enum.Parse(typeof(DemoLanguage), languageName);
                return true;
            }
        }

        return false;
    }

    private static string GetTargetLanguagePath(
        string path,
        string[] roots,
        DemoLanguage targetLanguage)
    {
        return LanguageAssetPathResolver.GetLocalizedAssetPath(
            path,
            roots,
            targetLanguage);
    }
}
