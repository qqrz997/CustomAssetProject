using System;
using System.Collections.Generic;
using System.Linq;
using SaberComponents.Components;
using UnityEngine;

public class EventTester : MonoBehaviour
{
    private List<EventManager> managers;
    private List<ComboReachedEvent> comboNbEvents;
    private List<EveryNthComboFilter> comboNthEvents;

    private string comboInput = "0";

    private void Start () 
    {
        managers = FindObjectsByType<EventManager>(FindObjectsSortMode.None).ToList();
        comboNbEvents = FindObjectsByType<ComboReachedEvent>(FindObjectsSortMode.None).ToList();
        comboNthEvents = FindObjectsByType<EveryNthComboFilter>(FindObjectsSortMode.None).ToList();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Event Tester");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        
        Button("On Slice", OnSlice);
        Button("Combo Break", ComboBreak);
        Button("Multiplier Up", MultiplierUp);
        Button("Start Colliding", StartColliding);
        Button("Stop Colliding", StopColliding);
        Button("Level Start", LevelStart);
        Button("Level Fail", LevelFail);
        Button("Level Ended", LevelEnded);
        GUILayout.BeginHorizontal();
        Button("Test Combo", TestCombo);
        comboInput = GUILayout.TextField(comboInput, GUILayout.Width(36));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private static void Button(string text, Action action)
    {
        if (GUILayout.Button(text)) action();
    }

    private void OnSlice() { foreach(var manager in managers) manager.noteCut.Invoke(); }

    private void ComboBreak() { foreach (var manager in managers) manager.comboBroken.Invoke(); }
    private void MultiplierUp() { foreach (var manager in managers) manager.multiplierUp.Invoke(); }

    private void StartColliding() { foreach (var manager in managers) manager.saberStartColliding.Invoke(); }
    private void StopColliding() { foreach (var manager in managers) manager.saberStopColliding.Invoke(); }

    private void LevelStart() { foreach (var manager in managers) manager.levelStarted.Invoke(); }
    private void LevelFail() { foreach (var manager in managers) manager.levelFailed.Invoke(); }
    private void LevelEnded() { foreach (var manager in managers) manager.onLevelEnded.Invoke(); }

    private void TestCombo()
    {
        if (!int.TryParse(comboInput, out var combo))
        {
            comboInput = "0";
            return;
        }
        
        foreach (var manager in managers) 
            manager.comboChanged.Invoke(combo);

        foreach (var ev in comboNbEvents) 
            if (ev.comboTarget == combo) 
                ev.nthComboReached.Invoke();

        foreach (var ev in comboNthEvents) 
            if (ev.comboStep == combo) 
                ev.nthComboReached.Invoke();
    }
}
