using System.IO;
using Editor.Models;
using UnityEditor;
using UnityEngine;

internal class ProjectSettings : ScriptableObject
{
    private static readonly string FilePath = Path.Combine("Assets", "Settings", "SaberProjectSettings.asset");
    private static readonly string DirPath = Path.Combine(Application.dataPath, "Settings");

    private static ProjectSettings instance;

    [SerializeField] public string beatSaberPath;
    [SerializeField] public string author;
    [SerializeField] public string exportFilename = "{ModelName}";
    [SerializeField] public bool showOverlay = true;

    [SerializeField] public bool showSaberGuides = true;
    [SerializeField] public bool showTrailGuides = true;
    [SerializeField] public bool showTrailPreview = true;
    [SerializeField] public float trailPreviewLength = 0.3f;
    [SerializeField] public MockColorScheme colorScheme = new();
    
    internal static SerializedObject GetSerializedSettings() => 
        new(GetOrCreateSettings());

    internal static void OpenSettingsScreen()
    {
        SettingsService.OpenProjectSettings("Project/BS Model Toolkit");
    }
    
    internal static ProjectSettings GetOrCreateSettings()
    {
        if (instance) return instance;
        var settings = AssetDatabase.LoadAssetAtPath<ProjectSettings>(FilePath);
        if (settings == null) settings = CreateSettings();
        return instance = settings;
    }

    private static ProjectSettings CreateSettings()
    {
        var dirInfo = new DirectoryInfo(DirPath);
        if (!dirInfo.Exists) dirInfo.Create();
        var settings = CreateInstance<ProjectSettings>();
        AssetDatabase.CreateAsset(settings, FilePath);
        AssetDatabase.SaveAssets();
        return settings;
    }
}