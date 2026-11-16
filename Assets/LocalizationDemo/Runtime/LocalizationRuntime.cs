using System;
using UnityEngine;

public static class LocalizationRuntime
{
    private const string LanguagePreferenceKey = "LocalizationDemo.Language";
    private const string CatalogResourcePath = "Localization/LocalizationCatalog";

    private static LocalizationCatalog catalog;
    private static bool catalogLoadAttempted;
    private static bool languageInitialized;
    private static DemoLanguage currentLanguage;
    private static DemoLanguage? previewLanguage;

    public static event Action<DemoLanguage> LanguageChanged;

    public static DemoLanguage CurrentLanguage
    {
        get
        {
            InitializeLanguage();
            return previewLanguage ?? currentLanguage;
        }
    }

    public static LocalizationCatalog Catalog
    {
        get
        {
            if (!catalogLoadAttempted)
            {
                catalogLoadAttempted = true;
                catalog = Resources.Load<LocalizationCatalog>(CatalogResourcePath);
                if (catalog == null)
                {
                    Debug.LogError(
                        "Localization catalog not found at Resources/" +
                        CatalogResourcePath + ".");
                }
            }

            return catalog;
        }
    }

    public static bool SetLanguage(DemoLanguage language)
    {
        if (!IsSupportedLanguage(language))
        {
            Debug.LogError("Unsupported localization language: " + language);
            return false;
        }

        InitializeLanguage();
        DemoLanguage previousLanguage = CurrentLanguage;
        previewLanguage = null;
        currentLanguage = language;
        languageInitialized = true;
        PlayerPrefs.SetString(LanguagePreferenceKey, language.ToString());
        PlayerPrefs.Save();
        if (previousLanguage != language)
        {
            NotifyLanguageChanged(language);
        }

        return true;
    }

    public static void SetEditorPreviewLanguage(DemoLanguage language)
    {
        if (!IsSupportedLanguage(language))
        {
            Debug.LogError("Unsupported localization preview language: " + language);
            return;
        }

        InitializeLanguage();
        DemoLanguage previousLanguage = CurrentLanguage;
        previewLanguage = language;
        if (previousLanguage != language)
        {
            NotifyLanguageChanged(language);
        }
    }

    public static void ClearEditorPreviewLanguage()
    {
        DemoLanguage previousLanguage = CurrentLanguage;
        previewLanguage = null;
        if (languageInitialized && previousLanguage != currentLanguage)
        {
            NotifyLanguageChanged(currentLanguage);
        }
    }

    private static void InitializeLanguage()
    {
        if (languageInitialized)
        {
            return;
        }

        LocalizationCatalog languageCatalog = Catalog;
        DemoLanguage fallback = languageCatalog != null
            ? languageCatalog.defaultLanguage
            : DemoLanguage.CN;
        string savedLanguage = PlayerPrefs.GetString(
            LanguagePreferenceKey,
            fallback.ToString());

        DemoLanguage parsedLanguage;
        currentLanguage = Enum.TryParse(savedLanguage, false, out parsedLanguage) &&
                          IsSupportedLanguage(parsedLanguage)
            ? parsedLanguage
            : fallback;
        languageInitialized = true;
    }

    private static void NotifyLanguageChanged(DemoLanguage language)
    {
        Action<DemoLanguage> handler = LanguageChanged;
        if (handler != null)
        {
            handler(language);
        }
    }

    private static bool IsSupportedLanguage(DemoLanguage language)
    {
        return Enum.IsDefined(typeof(DemoLanguage), language);
    }
}
