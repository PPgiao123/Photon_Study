using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadParent))]
public class RoadParentEditor : SharedSettingsEditorBase<RoadParentEditor.RoadParentSettings>
{
    private RoadParent roadParent;

    protected override string SaveKey => "RoadParentEditor";

    [Serializable]
    public class RoadParentSettings
    {
        public bool CachedFlag = false;
        public bool SettingsFlag = true;
        public bool UtilsFlag = false;
        public bool ButtonsFlag = true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        roadParent = target as RoadParent;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InspectorExtension.DrawGroupBox("Crossroads", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficLightCrossroads"));

        }, ref SharedSettings.CachedFlag);

        InspectorExtension.DrawGroupBox("Settings", () =>
        {
            var connectionWaypointOffsetProp = serializedObject.FindProperty("connectionWaypointOffset");
            EditorGUILayout.PropertyField(connectionWaypointOffsetProp);

            if (connectionWaypointOffsetProp.floatValue > 0)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("externalSubNodes"));
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.Toggle("External Sub Nodes", false);
                GUI.enabled = true;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("castDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("multiAngleRaycast"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pedestrianSubNodeDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("connectCrosswalks"));

        }, ref SharedSettings.SettingsFlag);

        InspectorExtension.DrawGroupBox("Utils", () =>
        {
            if (GUILayout.Button("Add Scene Crossroads"))
            {
                roadParent.AddCrossroads();
            }

            if (GUILayout.Button("Connect Pedestrian Nodes"))
            {
                roadParent.ConnectPedestrianNodes();
            }

            if (GUILayout.Button("Clear Unattached Paths"))
            {
                roadParent.ClearUnattachedPaths();
            }

        }, ref SharedSettings.UtilsFlag);

        InspectorExtension.DrawGroupBox("Buttons", () =>
        {
            if (GUILayout.Button("Connect Segments"))
            {
                roadParent.ConnectSegments();
            }

            if (GUILayout.Button("Force Connect Segments"))
            {
                roadParent.ForceConnectSegments();
            }

            if (GUILayout.Button("Bake Path Data"))
            {
                roadParent.BakePathData();
            }

        }, ref SharedSettings.ButtonsFlag);

        serializedObject.ApplyModifiedProperties();
    }

    protected override RoadParentSettings GetDefaultSettings() => new RoadParentSettings();
}
