using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public sealed class LocalizedText : MonoBehaviour
{
    public string key;
    public string fallbackText;
    public bool applyLanguageFont = true;

    [SerializeField] private LocalizationCatalog catalog;

    private Text uiText;
    private UILabel nguiLabel;

    private void OnEnable()
    {
        LocalizationRuntime.LanguageChanged += Refresh;
        Refresh(LocalizationRuntime.CurrentLanguage);
    }

    private void OnDisable()
    {
        LocalizationRuntime.LanguageChanged -= Refresh;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && isActiveAndEnabled)
        {
            Refresh(LocalizationRuntime.CurrentLanguage);
        }
    }

    public void Refresh()
    {
        Refresh(LocalizationRuntime.CurrentLanguage);
    }

    private void Refresh(DemoLanguage language)
    {
        if (catalog == null)
        {
            catalog = LocalizationRuntime.Catalog;
        }

        CacheTargets();
        string localizedValue;
        if (catalog == null || !catalog.TryGetText(key, language, out localizedValue))
        {
            if (string.IsNullOrEmpty(fallbackText))
            {
                Debug.LogWarning(
                    "No localized value for key '" + key + "' in " + language + ".",
                    this);
                return;
            }

            localizedValue = fallbackText;
        }

        if (uiText != null)
        {
            if (uiText.text != localizedValue)
            {
                uiText.text = localizedValue;
                MarkEditedObject(uiText);
            }
        }

        if (nguiLabel != null)
        {
            if (nguiLabel.text != localizedValue)
            {
                nguiLabel.text = localizedValue;
                MarkEditedObject(nguiLabel);
            }
        }

        if (applyLanguageFont && catalog != null)
        {
            ApplyLanguageFont(catalog, language);
        }
    }

    private void CacheTargets()
    {
        if (uiText == null)
        {
            uiText = GetComponent<Text>();
        }

        if (nguiLabel == null)
        {
            nguiLabel = GetComponent<UILabel>();
        }
    }

    private void ApplyLanguageFont(LocalizationCatalog languageCatalog, DemoLanguage language)
    {
        Font uiFont;
        Font nguiFont;
        if (!languageCatalog.TryGetFonts(language, out uiFont, out nguiFont))
        {
            return;
        }

        if (uiText != null && uiFont != null)
        {
            if (uiText.font != uiFont)
            {
                uiText.font = uiFont;
                MarkEditedObject(uiText);
            }
        }

        if (nguiLabel != null && nguiFont != null)
        {
            if (nguiLabel.trueTypeFont != nguiFont)
            {
                nguiLabel.trueTypeFont = nguiFont;
                MarkEditedObject(nguiLabel);
            }
        }
    }

    private void MarkEditedObject(Component component)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(component);
            if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif
    }
}
