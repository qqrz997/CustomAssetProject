using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SaberProjectOverlay
{
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();
    
    static SaberProjectOverlay()
    {
        SceneView.duringSceneGui -= DrawGUI;
        SceneView.duringSceneGui += DrawGUI;
    }
    
    private const int WindowWidth = 150;
    private const int HeaderHeight = 25;
    
    private static SceneView currentSceneView;
    private static Rect windowRect = new(0, 0, WindowWidth, HeaderHeight);

    private static void DrawGUI(SceneView sceneView)
    {
        currentSceneView = sceneView;
        if (Settings.showOverlay)
            windowRect = GUILayout.Window(6767, windowRect, DraggableWindowContent, "Menu");
    }

    private static void DraggableWindowContent(int id)
    {
        GUILayout.BeginVertical();
        HandleWindowElements();
        GUILayout.EndVertical();
        GUI.DragWindow(new(0, 0, WindowWidth, HeaderHeight));
    }

    private static void HandleWindowElements()
    {
        if (GUILayout.Button("Settings"))
            ProjectSettings.OpenSettingsScreen();
        if (GUILayout.Button("Exporter"))
            ModelExporterWindow.ShowWindow();
        if (GUILayout.Button("Saber Tools"))
            SaberTools.OpenSaberTools();
        
        GUILayout.Space(10);
        if (GUILayout.Button("Start BeatSaber") 
            && !BeatSaberLauncher.TryStartBeatSaber(out var message))
            ShowNotification(message, 3);
    }

    private static void ShowNotification(string text, float duration = 1f)
    {
        currentSceneView.ShowNotification(new(text), duration);
        SceneView.RepaintAll();
    }
}