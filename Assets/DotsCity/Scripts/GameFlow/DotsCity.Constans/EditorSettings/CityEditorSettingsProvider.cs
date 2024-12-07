#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor
{
    internal static class CityEditorSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateCitySettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/604Spirit/Dots City", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "City Settings",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings = CityEditorSettings.GetSerializedSettings();

                    GUILayout.Space(10);

                    GUILayout.Label("City Scene View", EditorStyles.boldLabel);

                    GUILayout.Space(2);

                    GUILayout.Label("Traffic Node", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settings.FindProperty("arrowColor"));
                    EditorGUILayout.PropertyField(settings.FindProperty("externalArrowColor"));
                    EditorGUILayout.PropertyField(settings.FindProperty("pathArrowColor"));
                    EditorGUILayout.PropertyField(settings.FindProperty("pedSubNodeColor"));
                    EditorGUILayout.PropertyField(settings.FindProperty("arrowLength"));
                    EditorGUILayout.PropertyField(settings.FindProperty("arrowThickness"));

                    GUILayout.Space(EditorGUIUtility.singleLineHeight + 2);

                    GUILayout.Label("Scene Config Settings", EditorStyles.boldLabel);

                    GUILayout.Space(2);

                    var syncConfigOnChangeProp = settings.FindProperty("syncConfigOnChange");
                    EditorGUILayout.PropertyField(syncConfigOnChangeProp);

                    if (syncConfigOnChangeProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        var autoOpenClosedSceneProp = settings.FindProperty("autoOpenClosedScene");

                        EditorGUILayout.PropertyField(autoOpenClosedSceneProp);

                        if (autoOpenClosedSceneProp.boolValue)
                        {
                            var autoCloseSceneProp = settings.FindProperty("autoCloseScene");
                            EditorGUILayout.PropertyField(autoCloseSceneProp);

                            if (!autoCloseSceneProp.boolValue)
                                EditorGUILayout.PropertyField(settings.FindProperty("autoSaveChanges"));
                        }

                        EditorGUI.indentLevel--;
                    }

                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "City", "Dots", "Dots City" })
            };

            return provider;
        }
    }
}
#endif