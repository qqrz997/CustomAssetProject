using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class UITools
{
    public static void Header(string text, Color? clr = null, float space = 2)
    {
        if (clr.HasValue) GUI.color = clr.Value;
        GUILayout.Label(text, EditorStyles.boldLabel);
        if (clr.HasValue) GUI.color = Color.white;
        GUILayout.Space(space);
    }

    public static void CenterHeader(string text, Color clr, float space = 2)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.color = clr;
        GUILayout.Label(text, EditorStyles.boldLabel);
        GUI.color = Color.white;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(space);
    }

    public static void ChangedToggle(ref bool isActive, string text, Action<bool> changedAction)
    {
        var newIsActive = EditorGUILayout.Toggle(text, isActive);
        if (newIsActive != isActive)
        {
            changedAction.Invoke(newIsActive);
        }

        isActive = newIsActive;
    }

    public static bool Button(string text, float height = 20, Color? color = null)
    {
        if (color.HasValue)
        {
            GUI.color = color.Value;
        }

        var pressed = GUILayout.Button(text, GUILayout.Height(height));

        GUI.color = Color.white;

        return pressed;
    }

    public static void BoldLabel(string msg, Color clr)
    {
        var oldColor = GUI.color;
        GUI.color = clr;
        GUILayout.Label(msg, EditorStyles.boldLabel);
        GUI.color = oldColor;
    }

    public static void SelectAllRenderers()
    {
        var go = Selection.activeGameObject;
        if (go)
        {
            var gos = new List<GameObject>();
            foreach (var meshRenderer in go.GetComponentsInChildren<MeshRenderer>())
            {
                gos.Add(meshRenderer.gameObject);
            }
            Selection.objects = gos.Cast<Object>().ToArray();
        }
    }
}