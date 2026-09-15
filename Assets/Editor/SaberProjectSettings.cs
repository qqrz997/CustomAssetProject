using System.IO;
using UnityEditor;
using UnityEngine;

internal class SaberProjectSettings : ScriptableObject
{
    private static readonly string FilePath = Path.Combine("Assets", "Settings", "SaberProjectSettings.asset");
    private static readonly string DirPath = Path.Combine(Application.dataPath, "Settings");

    private static SaberProjectSettings instance;

    [SerializeField] public string beatSaberPath;
    [SerializeField] public string author;
    [SerializeField] public string exportFilename = "{ModelName}";
    [SerializeField] public bool showOverlay = true;

    [SerializeField] public bool showSaberGuides = true;
    [SerializeField] public bool showTrailGuides = true;
    [SerializeField] public bool showTrailPreview = true;
    [SerializeField] public float trailPreviewLength = 0.3f;
    [SerializeField] public Color customColorLeft = new(0.75f, 0f, 0f);
    [SerializeField] public Color customColorRight = new(0f, 0.42f, 0.75f);
    
    internal static SerializedObject GetSerializedSettings() => 
        new(GetOrCreateSettings());

    internal static void OpenSettingsScreen()
    {
        SettingsService.OpenProjectSettings("Project/BS Model Toolkit");
    }
    
    internal static SaberProjectSettings GetOrCreateSettings()
    {
        if (instance) return instance;
        var settings = AssetDatabase.LoadAssetAtPath<SaberProjectSettings>(FilePath);
        if (settings == null) settings = CreateSettings();
        return instance = settings;
    }

    private static SaberProjectSettings CreateSettings()
    {
        var dirInfo = new DirectoryInfo(DirPath);
        if (!dirInfo.Exists) dirInfo.Create();
        var settings = CreateInstance<SaberProjectSettings>();
        AssetDatabase.CreateAsset(settings, FilePath);
        AssetDatabase.SaveAssets();
        return settings;
    }
}