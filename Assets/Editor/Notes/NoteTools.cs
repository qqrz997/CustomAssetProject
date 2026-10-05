using System;
using System.IO;
using System.Linq;
using AssetComponents.Components.Notes;
using UnityEditor;
using UnityEngine;

public class NoteTools : EditorWindow
{
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();
    
    private string templateName = "NewNote";
    private bool createRightNotes;
    private bool createBomb;
    
    private static Vector3[] bombVertices;
    private static ushort[] bombIndices;
    
    private const float NoteSize = 0.494768f;
    private const float ChainHeadHeight = 0.287384f;
    private const float ChainSegmentHeight = 0.07999998f;
    
    [MenuItem("Window/BS Asset Project/Note Tools")]
    public static void OpenNoteTools()
    {
        GetWindow<NoteTools>(false, "Note Tools");
    }

    private void OnGUI()
    {
        UITools.Header("Visuals");
        
        UITools.ChangedToggle(ref Settings.showGuides, "Show Guides", _ => SceneView.RepaintAll());
        
        GUILayout.Space(10);
        UITools.Header("Create Template");
        
        templateName = EditorGUILayout.TextField("Name", templateName);
        createRightNotes = EditorGUILayout.Toggle("Create Right Notes", createRightNotes);
        createBomb = EditorGUILayout.Toggle("Create Bomb", createBomb);
        if (GUILayout.Button("Create Template", GUILayout.Height(20)))
        {
            CreateTemplate();
        }
    }

    private void CreateTemplate()
    {
        var rootGo = new GameObject(templateName);
        var descriptor = rootGo.AddComponent<NoteDescriptor>();
        descriptor.name = templateName;
        descriptor.authorName = Settings.author;

        descriptor.leftNotes = CreateNoteSet(rootGo.transform, "Left", -0.4f);
        if (createRightNotes) descriptor.rightNotes = CreateNoteSet(rootGo.transform, "Right", 0.4f);
        if (createBomb) descriptor.bomb = CreateBomb(rootGo.transform);
        
        MaterialColorerPreviewer.RefreshAll();
        Selection.activeGameObject = rootGo;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawGizmos(NoteDescriptor descriptor, GizmoType gizmoType)
    {
        if (!Settings.showGuides) return;
        
        if (descriptor.leftNotes)
            DrawNoteGizmos(descriptor.leftNotes, Settings.colorScheme.saberAColor);
        if (descriptor.rightNotes)
            DrawNoteGizmos(descriptor.rightNotes, Settings.colorScheme.saberBColor);
        if (descriptor.bomb)
            DrawBombGizmo(descriptor.bomb.transform);
    }

    private static void DrawNoteGizmos(NoteSet notes, Color color)
    {
        var oldColor = Gizmos.color;
        color.a = Settings.guidesTransparency;
        Gizmos.color = color;
        if (notes.noteArrow) 
            Gizmos.DrawWireCube(notes.noteArrow.transform.position, new(NoteSize, NoteSize, NoteSize));
        if (notes.noteDot)
            Gizmos.DrawWireCube(notes.noteDot.transform.position, new(NoteSize, NoteSize, NoteSize));
        if (notes.chainArrow)
            Gizmos.DrawWireCube(notes.chainArrow.transform.position, new(NoteSize, ChainHeadHeight, NoteSize));
        if (notes.chainDot)
            Gizmos.DrawWireCube(notes.chainDot.transform.position, new(NoteSize, ChainHeadHeight, NoteSize));
        if (notes.chainSegment)
            Gizmos.DrawWireCube(notes.chainSegment.transform.position, new(NoteSize, ChainSegmentHeight, NoteSize));
        Gizmos.color = oldColor;
    }

    private static void DrawBombGizmo(Transform parent)
    {
        if (bombIndices == null || bombVertices == null)
            (bombVertices, bombIndices) = LoadBombMesh();

        var oldColor = Gizmos.color;
        Gizmos.color = new(0.15f, 0.15f, 0.18f, Settings.guidesTransparency);
        Gizmos.matrix = parent.localToWorldMatrix;
        for (var i = 0; i < bombIndices.Length; i += 3)
        {
            var a = bombVertices[bombIndices[i]];
            var b = bombVertices[bombIndices[i + 1]];
            var c = bombVertices[bombIndices[i + 2]];

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, a);
        }
        Gizmos.color = oldColor;
    }

    private static (Vector3[] vertices, ushort[] indices) LoadBombMesh()
    {
        var filePath = Path.Combine(Application.dataPath, "Editor", "Notes", "bombMesh.txt");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No bomb mesh file found");
            return (Array.Empty<Vector3>(), Array.Empty<ushort>());
        }
        var lines = File.ReadAllLines(filePath);
        var vertices = lines.SkipLast(1)
            .Select(l => l.Trim().Split(' '))
            .Select(l => new Vector3(float.Parse(l[0]), float.Parse(l[1]), float.Parse(l[2]))).ToArray();
        var indices = lines.Last().Trim().Split(' ').Select(ushort.Parse).ToArray();
        return (vertices, indices);
    }
    
    private static NoteSet CreateNoteSet(Transform parent, string name, float x)
    {
        var noteSet = new GameObject(name).AddComponent<NoteSet>();
        noteSet.transform.SetParent(parent, false);
        noteSet.transform.localPosition = new(x, 0f, 0f);
        noteSet.noteArrow = CreateNote(noteSet.transform, "NoteArrow", 2.4f);
        noteSet.noteDot = CreateNote(noteSet.transform, "NoteDot", 1.8f);
        noteSet.chainArrow = CreateNote(noteSet.transform, "ChainArrow", 1.2f);
        noteSet.chainDot = CreateNote(noteSet.transform, "ChainDot", 0.6f);
        noteSet.chainSegment = CreateNote(noteSet.transform, "ChainSegment", 0f);
        return noteSet;
        static GameObject CreateNote(Transform parent, string name, float y)
        {
            var note = new GameObject(name);
            note.transform.SetParent(parent, false);
            note.transform.localPosition =  new(0f, y, 0f);
            return note;
        }
    }

    private static GameObject CreateBomb(Transform parent)
    {
        var bomb = new GameObject("Bomb");
        bomb.transform.SetParent(parent, false);
        bomb.transform.localPosition = new(-1.2f, 2.4f, 0f);
        return bomb;
    }
}
