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

        SetupNoteSets(descriptor, createRightNotes);
        if (createBomb) descriptor.bomb = CreateBomb(rootGo.transform);
        
        MaterialColorerPreviewer.RefreshAll();
        Selection.activeGameObject = rootGo;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawGizmos(NoteDescriptor descriptor, GizmoType gizmoType)
    {
        if (!Settings.showGuides) return;
        
        DrawNoteGizmos(Settings.colorScheme.saberAColor, descriptor.leftNoteArrow, descriptor.leftNoteDot,
            descriptor.leftChainArrow, descriptor.leftChainDot, descriptor.leftChainSegment);
        DrawNoteGizmos(Settings.colorScheme.saberBColor, descriptor.rightNoteArrow, descriptor.rightNoteDot,
            descriptor.rightChainArrow, descriptor.rightChainDot, descriptor.rightChainSegment);
        DrawBombGizmo(descriptor.bomb);
    }

    private static void DrawNoteGizmos(Color color, GameObject noteArrow, GameObject noteDot, GameObject chainArrow,
        GameObject chainDot, GameObject chainSegment)
    {
        var oldColor = Gizmos.color;
        color.a = Settings.guidesTransparency;
        Gizmos.color = color;
        if (noteArrow) 
            Gizmos.DrawWireCube(noteArrow.transform.position, new(NoteSize, NoteSize, NoteSize));
        if (noteDot)
            Gizmos.DrawWireCube(noteDot.transform.position, new(NoteSize, NoteSize, NoteSize));
        if (chainArrow)
            Gizmos.DrawWireCube(chainArrow.transform.position, new(NoteSize, ChainHeadHeight, NoteSize));
        if (chainDot)
            Gizmos.DrawWireCube(chainDot.transform.position, new(NoteSize, ChainHeadHeight, NoteSize));
        if (chainSegment)
            Gizmos.DrawWireCube(chainSegment.transform.position, new(NoteSize, ChainSegmentHeight, NoteSize));
        Gizmos.color = oldColor;
    }

    private static void DrawBombGizmo(GameObject parent)
    {
        if (!parent) return;
        if (bombIndices == null || bombVertices == null)
            (bombVertices, bombIndices) = LoadBombMesh();

        var oldColor = Gizmos.color;
        Gizmos.color = new(0.15f, 0.15f, 0.18f, Settings.guidesTransparency);
        Gizmos.matrix = parent.transform.localToWorldMatrix;
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
    
    private static void SetupNoteSets(NoteDescriptor descriptor, bool createRightNotes)
    {
        var left = new GameObject("Left");
        left.transform.SetParent(descriptor.transform, false);
        left.transform.localPosition = new(-0.4f, 0f, 0f);
        descriptor.leftNoteArrow = CreateNote(left.transform, "NoteArrow", 2.4f);
        descriptor.leftNoteDot = CreateNote(left.transform, "NoteDot", 1.8f);
        descriptor.leftChainArrow = CreateNote(left.transform, "ChainArrow", 1.2f);
        descriptor.leftChainDot = CreateNote(left.transform, "ChainDot", 0.6f);
        descriptor.leftChainSegment = CreateNote(left.transform, "ChainSegment", 0f);
        if (createRightNotes)
        {
            var right = new GameObject("Right");
            right.transform.SetParent(descriptor.transform, false);
            right.transform.localPosition = new(0.4f, 0f, 0f);
            descriptor.rightNoteArrow = CreateNote(right.transform, "NoteArrow", 2.4f);
            descriptor.rightNoteDot = CreateNote(right.transform, "NoteDot", 1.8f);
            descriptor.rightChainArrow = CreateNote(right.transform, "ChainArrow", 1.2f);
            descriptor.rightChainDot = CreateNote(right.transform, "ChainDot", 0.6f);
            descriptor.rightChainSegment = CreateNote(right.transform, "ChainSegment", 0f);
        }
        
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
