using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LocalizationCatalog",
    menuName = "Localization Demo/Localization Catalog")]
public sealed class LocalizationCatalog : ScriptableObject
{
    public DemoLanguage defaultLanguage = DemoLanguage.CN;
    public List<LocalizedStringEntry> strings = new List<LocalizedStringEntry>();
    public List<LocalizedFontEntry> fonts = new List<LocalizedFontEntry>();

    public bool TryGetText(string key, DemoLanguage language, out string value)
    {
        foreach (LocalizedStringEntry entry in strings)
        {
            if (entry != null && entry.key == key)
            {
                value = entry.GetValue(language);
                return !string.IsNullOrEmpty(value);
            }
        }

        value = null;
        return false;
    }

    public bool TryGetFonts(
        DemoLanguage language,
        out Font uiFont,
        out Font nguiFont)
    {
        foreach (LocalizedFontEntry entry in fonts)
        {
            if (entry != null && entry.language == language)
            {
                uiFont = entry.uiFont;
                nguiFont = entry.nguiFont;
                return uiFont != null || nguiFont != null;
            }
        }

        uiFont = null;
        nguiFont = null;
        return false;
    }
}

[Serializable]
public sealed class LocalizedStringEntry
{
    public string key;
    [TextArea] public string cn;
    [TextArea] public string tc;
    [TextArea] public string vn;
    [TextArea] public string en;
    [TextArea] public string kr;
    [TextArea] public string th;
    [TextArea] public string ru;

    public string GetValue(DemoLanguage language)
    {
        switch (language)
        {
            case DemoLanguage.CN: return cn;
            case DemoLanguage.TC: return tc;
            case DemoLanguage.VN: return vn;
            case DemoLanguage.EN: return en;
            case DemoLanguage.KR: return kr;
            case DemoLanguage.TH: return th;
            case DemoLanguage.RU: return ru;
            default: return null;
        }
    }
}

[Serializable]
public sealed class LocalizedFontEntry
{
    public DemoLanguage language;
    public Font uiFont;
    public Font nguiFont;
}
