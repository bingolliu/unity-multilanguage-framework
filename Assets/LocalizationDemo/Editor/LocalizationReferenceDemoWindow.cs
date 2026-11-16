using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LocalizationReferenceDemoWindow : EditorWindow
{
    private static readonly string[] TextureRoots =
    {
        "Assets/LocalizationDemo/Resources/Localization/",
        "Assets/LocalizationDemo/Resources/Models/Textures/",
        "Assets/LocalizationDemo/Resources/Effects/Textures/",
        "Assets/LocalizationDemo/Resources/Atlases/"
    };

    private static readonly string[] MaterialRoots =
    {
        "Assets/LocalizationDemo/Resources/Models/Materials/",
        "Assets/LocalizationDemo/Resources/Effects/Materials/"
    };
    private static readonly string[] AtlasRoots =
    {
        "Assets/LocalizationDemo/Resources/Atlases/"
    };
    private static readonly string[] FontRoots =
    {
        "Assets/LocalizationDemo/Resources/Localization/Fonts/"
    };

    [InitializeOnLoadMethod]
    private static void RegisterSceneOpenedCallback()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [MenuItem("Demo/Localization Language")]
    private static void Open()
    {
        GetWindow<LocalizationReferenceDemoWindow>("Localization Language").Show();
    }

    private void OnGUI()
    {
        DemoLanguage currentLanguage;
        if (!LanguageAwareImportProcessor.TryGetCurrentLanguage(out currentLanguage))
        {
            EditorGUILayout.HelpBox(
                "当前语言配置无效，请检查 ProjectSettings/LocalizationLanguage。",
                MessageType.Error);
            return;
        }

        EditorGUILayout.HelpBox(
            "演示场景已预置 UGUI、NGUI、模型和特效资源。切换语言会刷新文本、字体以及已存在的资源配对引用，并刷新演示 Prefab、材质和图集。",
            MessageType.Info);

        DemoLanguage targetLanguage = (DemoLanguage)EditorGUILayout.EnumPopup(
            "当前语言",
            currentLanguage);
        if (targetLanguage != currentLanguage)
        {
            LanguageAwareImportProcessor.SetCurrentLanguage(targetLanguage);
            LocalizationRuntime.SetEditorPreviewLanguage(targetLanguage);
            int replacements = ApplyToLoadedScenes(targetLanguage);
            Debug.Log(string.Format(
                "多语言资源切换完成：目标 {0}，已替换 {1} 个场景组件引用。请保存已修改的场景。",
                targetLanguage,
                replacements));
        }
        else
        {
            LocalizationRuntime.SetEditorPreviewLanguage(currentLanguage);
        }
    }

    internal static int ApplyToLoadedScenes(DemoLanguage language)
    {
        int replacements = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            bool sceneChanged = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite == null)
                    {
                        continue;
                    }

                    Sprite target = LoadLocalizedAsset<Sprite>(
                        AssetDatabase.GetAssetPath(image.sprite),
                        TextureRoots,
                        language);
                    if (target != null && target != image.sprite)
                    {
                        Undo.RecordObject(image, "Switch Localization Language");
                        image.sprite = target;
                        EditorUtility.SetDirty(image);
                        replacements++;
                        sceneChanged = true;
                    }
                }

                foreach (UITexture texture in root.GetComponentsInChildren<UITexture>(true))
                {
                    if (texture.mainTexture == null)
                    {
                        continue;
                    }

                    Texture target = LoadLocalizedAsset<Texture>(
                        AssetDatabase.GetAssetPath(texture.mainTexture),
                        TextureRoots,
                        language);
                    if (target != null && target != texture.mainTexture)
                    {
                        Undo.RecordObject(texture, "Switch Localization Language");
                        texture.mainTexture = target;
                        EditorUtility.SetDirty(texture);
                        replacements++;
                        sceneChanged = true;
                    }
                }

                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                {
                    if (text.font == null)
                    {
                        continue;
                    }

                    Font target = LoadLocalizedAsset<Font>(
                        AssetDatabase.GetAssetPath(text.font),
                        FontRoots,
                        language);
                    if (target != null && target != text.font)
                    {
                        Undo.RecordObject(text, "Switch Localization Language");
                        text.font = target;
                        EditorUtility.SetDirty(text);
                        replacements++;
                        sceneChanged = true;
                    }
                }

                foreach (UILabel label in root.GetComponentsInChildren<UILabel>(true))
                {
                    if (label.trueTypeFont == null)
                    {
                        continue;
                    }

                    Font target = LoadLocalizedAsset<Font>(
                        AssetDatabase.GetAssetPath(label.trueTypeFont),
                        FontRoots,
                        language);
                    if (target != null && target != label.trueTypeFont)
                    {
                        Undo.RecordObject(label, "Switch Localization Language");
                        label.trueTypeFont = target;
                        EditorUtility.SetDirty(label);
                        replacements++;
                        sceneChanged = true;
                    }
                }

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    bool rendererChanged = false;
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        if (materials[materialIndex] == null)
                        {
                            continue;
                        }

                        Material target = LoadLocalizedAsset<Material>(
                            AssetDatabase.GetAssetPath(materials[materialIndex]),
                            MaterialRoots,
                            language);
                        if (target != null && target != materials[materialIndex])
                        {
                            materials[materialIndex] = target;
                            rendererChanged = true;
                        }
                    }

                    if (rendererChanged)
                    {
                        Undo.RecordObject(renderer, "Switch Localization Language");
                        renderer.sharedMaterials = materials;
                        EditorUtility.SetDirty(renderer);
                        replacements++;
                        sceneChanged = true;
                    }
                }

                foreach (UIAtlas atlas in root.GetComponentsInChildren<UIAtlas>(true))
                {
                    if (atlas.replacement == null)
                    {
                        continue;
                    }

                    string targetPath = GetLocalizedAssetPath(
                        AssetDatabase.GetAssetPath((UnityEngine.Object)atlas.replacement),
                        AtlasRoots,
                        language);
                    INGUIAtlas target = targetPath == null
                        ? null
                        : AssetDatabase.LoadMainAssetAtPath(targetPath) as INGUIAtlas;
                    if (targetPath != null && target == null)
                    {
                        Debug.LogError("目标语言 Atlas 类型不匹配：" + targetPath);
                    }

                    if (target != null && target != atlas.replacement)
                    {
                        Undo.RecordObject(atlas, "Switch Localization Language");
                        atlas.replacement = target;
                        EditorUtility.SetDirty(atlas);
                        replacements++;
                        sceneChanged = true;
                    }
                }
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        return replacements;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!scene.path.StartsWith(
                "Assets/LocalizationDemo/Scenes/",
                StringComparison.Ordinal))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            DemoLanguage language;
            if (LanguageAwareImportProcessor.TryGetCurrentLanguage(out language))
            {
                LocalizationRuntime.SetEditorPreviewLanguage(language);
                ApplyToLoadedScenes(language);
            }
        };
    }

    private static T LoadLocalizedAsset<T>(
        string currentPath,
        string[] roots,
        DemoLanguage language)
        where T : UnityEngine.Object
    {
        string targetPath = GetLocalizedAssetPath(currentPath, roots, language);
        if (targetPath == null)
        {
            return null;
        }

        T target = AssetDatabase.LoadAssetAtPath<T>(targetPath);
        if (target == null)
        {
            Debug.LogError("目标语言资源类型不匹配：" + targetPath);
        }

        return target;
    }

    private static string GetLocalizedAssetPath(
        string currentPath,
        string[] roots,
        DemoLanguage language)
    {
        return LanguageAssetPathResolver.GetLocalizedAssetPath(
            currentPath,
            roots,
            language);
    }
}
