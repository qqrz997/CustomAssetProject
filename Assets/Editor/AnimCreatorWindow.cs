using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimCreatorWindow : EditorWindow
{
    private InitData currentData;

    private string folder = "Animations";
    private float animationSpeed = 1;
    private Vector3 axis = new(0, 1, 0);
    private bool startFromCurrentRotation;
    private float offset;

    private static readonly Color SectionColor = new(0, 0, 0, 0.5f);

    public static AnimCreatorWindow Open()
    {
        var instance = GetWindow<AnimCreatorWindow>(true, "Anim Creator");
        PopupTools.Center(instance, 300, 250);
        return instance;
    }

    public void Setup(InitData initData)
    {
        currentData = initData;
    }

    private void OnGUI()
    {
        if (!currentData.GameObject)
        {
            return;
        }

        GUILayout.BeginVertical(UITools.SimplePadding);

        UITools.BeginSection(SectionColor);
        folder = EditorGUILayout.TextField("Folder", folder);
        EditorGUILayout.ObjectField("Gameobject", currentData.GameObject, typeof(GameObject), true);
        UITools.EndSection();
        
        UITools.BeginSection(SectionColor);
        animationSpeed = EditorGUILayout.FloatField("Speed", animationSpeed);
        axis = EditorGUILayout.Vector3Field("Axis", axis);
        offset = EditorGUILayout.FloatField("Offset", offset);
        offset = Mathf.Max(offset, 0);
        if (offset == 0)
        {
            startFromCurrentRotation = EditorGUILayout.Toggle("Start from current rotation", startFromCurrentRotation);
        }
        else
        {
            startFromCurrentRotation = false;
        }
        UITools.EndSection();

        GUILayout.Space(10);
        if (UITools.Button("Create Animation", 23))
        {
            CreateAnim();
        }

        GUILayout.EndVertical();
    }

    private void CreateAnim()
    {
        var folder = Path.Combine(Application.dataPath, this.folder);
        Directory.CreateDirectory(folder);

        var filename = IncrementalFilename(folder, currentData.GameObject.name+".anim");

        var go = currentData.GameObject.transform;
        if (offset != 0)
        {
            var newGo = new GameObject("AnimationOffset").transform;
            newGo.parent = go.parent;
            newGo.SetPositionAndRotation(go.position, go.rotation);
            go.parent = newGo;
            // quick and dirty
            go.localPosition = new(offset * (1 - axis.x), offset * (1 - axis.y), offset*(1-axis.z));
            go = newGo;
        }

        var clip = new AnimationClip();

        var start = offset > 0?Vector3.zero: startFromCurrentRotation ? go.localEulerAngles:Vector3.zero;
        var end = startFromCurrentRotation ? new(start.x+359*Mathf.Clamp(axis.x, -1, 1), start.y + 359 * Mathf.Clamp(axis.y, -1, 1), start.z + 359 * Mathf.Clamp(axis.z, -1, 1)) : new Vector3(359 * Mathf.Clamp(axis.x, -1, 1), 359 * Mathf.Clamp(axis.y, -1, 1), 359 * Mathf.Clamp(axis.z, -1, 1));

        clip.SetCurve("", typeof(Transform), "localEulerAngles.x", AnimationCurve.Linear(0, start.x, 1/animationSpeed, end.x));
        clip.SetCurve("", typeof(Transform), "localEulerAngles.y", AnimationCurve.Linear(0, start.y, 1/animationSpeed, end.y));
        clip.SetCurve("", typeof(Transform), "localEulerAngles.z", AnimationCurve.Linear(0, start.z, 1/animationSpeed, end.z));

        var relFolder = "Assets/" + this.folder;
        if (!relFolder.EndsWith("/"))
        {
            relFolder += "/";
        }

        AssetDatabase.CreateAsset(clip, relFolder+filename);

        var controller =
            AnimatorController.CreateAnimatorControllerAtPathWithClip(relFolder + filename + ".controller", clip);

        go.gameObject.AddComponent<Animator>().runtimeAnimatorController = controller;

        Close();
    }

    private string IncrementalFilename(string folder, string filename)
    {
        if (!File.Exists(Path.Combine(folder, filename)))
        {
            return filename;
        }

        var extension = Path.GetExtension(filename);
        var filewoext = Path.GetFileNameWithoutExtension(filename);

        var currentIdx = 0;
        var newFilename = filewoext + currentIdx + extension;

        while (File.Exists(Path.Combine(folder, newFilename)))
        {
            currentIdx++;
            newFilename = filewoext + currentIdx + extension;
        }

        return newFilename;
    }

    public struct InitData
    {
        public GameObject GameObject;
    }
}