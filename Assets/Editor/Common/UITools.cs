using System;
using UnityEditor;
using UnityEngine;

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
}