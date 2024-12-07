#if UNITY_EDITOR
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor : Editor
    {
        private const int maxRowSelectionGridCount = 5;
        private const string AutoCrossroadTip = "AutoCrossroad";
        private string[] autoCrossroadPopup;

        private void ShowPathSettings()
        {
            bool showCurvedRoadSettings = true;
            bool showStraightRoadSettings = true;

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                showStraightRoadSettings = false;
                showCurvedRoadSettings = false;
            }

            if (creator.IsStraightRoad())
            {
                showCurvedRoadSettings = false;
            }

            EditorGUI.BeginChangeCheck();

            if (creator.trafficNodeHeaders?.Length > 1)
            {
                var headerCount = creator.trafficNodeHeaders.Length - 1;

                if (headerCount != creator.CreatedTrafficNodes.Count)
                {
                    creator.InitializeTrafficNodeHeaders();
                }

                creator.selectedTrafficNodeIndex = GUILayout.SelectionGrid(creator.selectedTrafficNodeIndex + 1, creator.trafficNodeHeaders, maxRowSelectionGridCount) - 1;
            }

            if (EditorGUI.EndChangeCheck())
            {
                creator.InitializePathHeaders();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            if (creator.pathHeaders?.Length > 1)
            {
                creator.selectedPathIndex = GUILayout.SelectionGrid(creator.selectedPathIndex + 1, creator.pathHeaders, maxRowSelectionGridCount) - 1;
            }

            if (EditorGUI.EndChangeCheck())
            {
                creator.OnPathSelectionChanged();
                SceneView.RepaintAll();
            }

            if (showStraightRoadSettings || showCurvedRoadSettings)
            {
                InspectorExtension.DrawGroupBox("Road Settings", () =>
                {
                    if (showStraightRoadSettings)
                    {
                        if (creator.GetRoadSegmentType != RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                        {
                            GUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.LabelField("Straight Road Settings", EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.wayPointStraightRoadCount)));

                            if (EditorGUI.EndChangeCheck())
                            {
                                serializedObject.ApplyModifiedProperties();
                                creator.OnWayPointStraightCountChanged();
                            }

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPathSpeedLimit)));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPriority)));

                            if (EditorGUI.EndChangeCheck())
                            {
                                serializedObject.ApplyModifiedProperties();
                                creator.OnStraightSpeedLimitChanged();
                            }

                            GUILayout.EndVertical();
                        }
                        else
                        {
                            GUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPathSpeedLimit)), new GUIContent("Speed Limit"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                creator.OnStraightSpeedLimitChanged();
                            }

                            GUILayout.EndVertical();
                        }
                    }

                    if (showCurvedRoadSettings)
                    {
                        GUILayout.BeginVertical("GroupBox");

                        EditorGUILayout.LabelField("Turn Road Settings", EditorStyles.boldLabel);

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnCurveType)));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.wayPointTurnCurveCount)));

                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            creator.OnWayPointTurnCountChanged();
                        }

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPathSpeedLimit)));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPriority)));

                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            creator.OnTurnSpeedLimitChanged();
                        }

                        GUILayout.EndVertical();
                    }

                }, ref segmentEditorSettings.PathRoadSettingsSubFoldOut);
            }

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Scene Settings", EditorStyles.boldLabel);

            if (creator.IsCustom())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showTrafficNodeHandles)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showTrafficNodeForward)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(configSo.FindProperty("pathDirection"));

            if (EditorGUI.EndChangeCheck())
            {
                configSo.ApplyModifiedProperties();
                creator.InitializePathHeaders();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showPathHandles)));

            if (creator.showPathHandles)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showEditButtonsPathNodes)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showWaypoints)));

            if (creator.showWaypoints)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showWaypointsInfo)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            if (creator.IsCustom(false))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.additionalSettings)));

                var nonDefaultBox = creator.additionalSettings != RoadSegmentCreator.AdditionalPathSettingsType.Default;

                if (nonDefaultBox)
                {
                    GUILayout.BeginVertical("HelpBox");
                }

                if (creator.additionalSettings == RoadSegmentCreator.AdditionalPathSettingsType.ExtrudeLane)
                {
                    EditorGUILayout.PropertyField(configSo.FindProperty("selectAfterCreation"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPathSpeedLimit)), new GUIContent("New Lane Speed Limit"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPriority)), new GUIContent("New Lane Priority"));

                    Config.ExtrudeLaneCount = EditorGUILayout.IntSlider("Extrude Lane Count", Config.ExtrudeLaneCount, 1, creator.MaxExtrudeLaneCount);

                    GUI.enabled = creator.SourceExtrudeNode;

                    if (GUILayout.Button("Create"))
                    {
                        creator.CreateLaneExtrude();
                    }

                    GUI.enabled = true;
                }

                if (creator.additionalSettings == RoadSegmentCreator.AdditionalPathSettingsType.AutoCrossroad)
                {
                    EditorTipExtension.TryToShowInspectorTip(AutoCrossroadTip, "Auto crossroad is used for quick generation of custom crossroad with custom shape. Place Traffic nodes at the entrances/exits of the intersection and press Create button.");

                    if (creator.ignoreConnections.Count > 0)
                    {
                        EditorGUILayout.BeginVertical("GroupBox");

                        EditorGUILayout.LabelField($"Ignore {creator.ignoreConnections.Count} connections:", EditorStyles.boldLabel);

                        Vector2Int removeElement = default;

                        foreach (var item in creator.ignoreConnections)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"Traffic node connection [{item.x + 1}, {item.y + 1}] is ignored");

                            if (GUILayout.Button("X", GUILayout.Width(25f)))
                            {
                                removeElement = item;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (!removeElement.Equals(default))
                        {
                            creator.RemoveIgnore(removeElement);
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("Ignore Source/Target Traffic Node indexes");

                    if (autoCrossroadPopup == null || autoCrossroadPopup.Length != creator.TrafficNodeCount)
                    {
                        autoCrossroadPopup = new string[creator.TrafficNodeCount];

                        for (int i = 0; i < autoCrossroadPopup.Length; i++)
                        {
                            autoCrossroadPopup[i] = (i + 1).ToString();
                        }
                    }

                    var labelWidth = EditorGUIUtility.labelWidth;

                    EditorGUIUtility.labelWidth = 0;

                    creator.sourceIgnoreIndex = EditorGUILayout.Popup(string.Empty, creator.sourceIgnoreIndex, autoCrossroadPopup, GUILayout.MaxWidth(50));
                    creator.targetIgnoreIndex = EditorGUILayout.Popup(string.Empty, creator.targetIgnoreIndex, autoCrossroadPopup, GUILayout.MaxWidth(50));

                    EditorGUIUtility.labelWidth = labelWidth;
                    if (GUILayout.Button("Add", GUILayout.MaxWidth(40)))
                    {
                        creator.AddAutoIgnore();
                    }

                    EditorGUILayout.EndHorizontal();

                    if (GUILayout.Button("Clear"))
                    {
                        creator.ClearAutoPaths();
                    }

                    if (GUILayout.Button("Create"))
                    {
                        creator.CreateAutoCrossroad();
                    }
                }

                if (nonDefaultBox)
                {
                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndVertical();

            EditorGUI.BeginChangeCheck();

            if (creator.CustomTurnSupport())
            {
                InspectorExtension.DrawGroupBox("Turn Connection Settings", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customNodeTurnSettings)));

                    if (!creator.customNodeTurnSettings)
                    {
                        GUILayout.BeginVertical("GroupBox");

                        DrawCustomTurnSetting(creator.customTurnDatas[0]);

                        GUILayout.EndVertical();
                    }
                    else
                    {
                        for (int i = 0; i < creator.CreatedTrafficNodes?.Count; i++)
                        {
                            GUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.LabelField(creator.CreatedTrafficNodes[i].name, EditorStyles.boldLabel);
                            var customTurnData = creator.customTurnDatas[i];
                            DrawCustomTurnSetting(customTurnData);

                            GUILayout.EndVertical();
                        }
                    }
                }, ref segmentEditorSettings.TurnConnectionSettingsFoldOut);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                creator.Create();
            }

            if (creator.selectedTrafficNodeIndex >= 0)
            {
                GUI.enabled = creator.selectedPathIndex >= 0;

                if (GUILayout.Button("Open Path Settings"))
                {
                    OpenPathSettingsWindow();
                }

                GUI.enabled = true;
            }
        }
    }
}
#endif
