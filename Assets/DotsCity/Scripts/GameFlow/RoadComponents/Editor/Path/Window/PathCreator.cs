#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathCreator : EditorWindowBase
    {
        #region Constans

        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/pathCreator.html";

        private const float DottedLineSize = 5f;
        private const float ArrowAngle = 30f;
        private const float ArrowSideLength = 2f;
        private const bool AllowClosestPoint = true;
        private const float MinCurveAngle = 10f;
        private const float MinCurveDistance = 4f;

        #endregion

        #region Helper types

        private enum ConnectSideType { DefaultSide, ExternalSide }
        private enum ConnectionType { SingleConnect, OneDirectionConnect, TwoDirectionConnect }
        private enum OverrideType { NotAllowed, Allowed }

        #endregion

        #region Inspector Variables

        [Tooltip("" +
            "<b>Straight line</b> is default line with nodes connected in series\r\n\r\n" +
            "<b>Bezier</b> generated line based on curved nodes")]
        [SerializeField] private PathCurveType pathCurveType;

        [Tooltip("<b>Straight road</b> : is used to automatically calculate lane changing by traffic")]
        [SerializeField] private PathRoadType pathRoadType;

        [SerializeField] private TrafficGroupMask trafficGroupMask = new TrafficGroupMask();

        [Tooltip("Number of waypoints in the curve segment")]
        [SerializeField][Range(2, 20)] private int straightRoadWayPointCount = 2;

        [Tooltip("Number of waypoints in the curve segment")]
        [SerializeField][Range(2, 20)] private int curvedRoadWayPointCount = 10;

        [Tooltip("Speed limit of the path. If value == 0, the speed limit is the default value")]
        [SerializeField][Range(0f, 200f)] private float speedLimitStraightLine;

        [Tooltip("Speed limit of the path. If value == 0, the speed limit is the default value")]
        [SerializeField][Range(0f, 200f)] private float speedLimitCurvedLine;

        [Tooltip("Order of crossing intersected paths (vehicle with the higher priority gets through first)")]
        [SerializeField][Range(-5, 5)] private int priority;

        [SerializeField] private bool showPreviewDottedLine = true;

        [SerializeField] private bool showPathDirection = true;

        [SerializeField][Range(0.1f, 20f)] private float arrowSpacing = 2f;

        [SerializeField] private bool pingAfterCreation = true;

        [SerializeField] private Color fontColor = Color.white;

        [Tooltip("If the path is correct connection & available")]
        [SerializeField] private Color previewConnectionColor = Color.green;

        [Tooltip("If the path already exists & can't be overridden")]
        [SerializeField] private Color forbiddenConnectionColor = Color.red;

        [Tooltip("If connection path has wrong in or out direction")]
        [SerializeField] private Color wrongConnectionColor = Color.yellow;

        [Tooltip("If the path already exists & can be overwritten")]
        [SerializeField] private Color overridenConnectionColor = Color.blue;

        [SerializeField] private bool visualSceneSettingsFoldout = true;

        [Tooltip("" +
            "<b>Single connect</b> : only 1 path is created\r\n\r\n" +
            "<b>One direction connect</b> : paths of all lanes are created for one side\r\n\r\n" +
            "<b>Two direction connect</b> : paths of all lanes are created for two sides")]
        [SerializeField] private ConnectionType connectionType = ConnectionType.SingleConnect;

        [SerializeField] private ConnectionType previousConnectionType;

        [Tooltip("" +
            "<b>Not Allowed</b> : path will be created only if the path has not been created before\r\n\r\n" +
            "<b>Allowed</b> : path will be overwritten if created earlier")]
        [SerializeField] private OverrideType overrideType = OverrideType.NotAllowed;

        [Tooltip("When selecting nodes, the selected sides will be automatically detected")]
        [SerializeField] private bool autoDetectSide = true;

        [Tooltip("Automatically switch connection type after selecting nodes depending on connection sides")]
        [SerializeField] private bool autoSwitchType = true;

        [Tooltip("Automatically switches curve type to Bezier if angle between traffic nodes is greater than 10 degrees, otherwise activates straight line")]
        [SerializeField] private bool autoSwitchCurveType = true;

        [Tooltip("Spline curve with is automatically created for Bezier curve paths")]
        [SerializeField] private bool autoSpline = true;

        [Tooltip("Target side will be the same as source side")]
        [SerializeField] private bool connectSameSide = true;

        [Tooltip("" +
            "<b>Default side</b> : selected right side point in the source traffic node.\r\n\r\n" +
            "<b>External side</b> : selected left side point in the source traffic node.")]
        [SerializeField] private ConnectSideType sourceNodeSide = ConnectSideType.DefaultSide;

        [Tooltip("" +
            "<b>Default side</b> : selected right side point in the target traffic node.\r\n\r\n" +
            "<b>External side</b> : selected left side point in the target traffic node.")]
        [SerializeField] private ConnectSideType targetNodeSide = ConnectSideType.DefaultSide;

        [Tooltip("Target index will be the same as source index")]
        [SerializeField] private bool connectSameIndex = true;

        [Tooltip("Path will be selected after creation")]
        [SerializeField] private bool selectAfterCreate = true;

        #endregion

        #region Variables

        public Path pathPrefab;
        private TrafficNode sourceTrafficNode;
        private TrafficNode targetTrafficNode;
        private int sourceLaneIndex;
        private int targetLaneIndex;
        private RoadSegmentCreator roadSegmentCreator;
        private Vector2 scrollPosition;
        private float startDistance = 0;

        private List<TrafficNode> nodes = new List<TrafficNode>();
        private bool duplicatePathErrorMessage;
        private List<Path> createdPaths = new List<Path>();

        #endregion

        #region Properties

        private int WaypointCount => pathCurveType == PathCurveType.StraightLine ? straightRoadWayPointCount : curvedRoadWayPointCount;

        private float SpeedLimit => pathCurveType == PathCurveType.StraightLine ? speedLimitStraightLine : speedLimitCurvedLine;

        public int TargetLaneIndex => connectSameIndex ? sourceLaneIndex : targetLaneIndex;

        private ConnectSideType TargetConnectionType => connectSameSide ? sourceNodeSide : targetNodeSide;

        private bool AutoSpline => autoSpline && AutoSplineAvailable;

        private bool AutoSplineAvailable => pathCurveType != PathCurveType.StraightLine;

        #endregion

        #region Unity methods & overrides

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Path Creator", secondaryPriority = 10)]
        public static PathCreator ShowWindow()
        {
            var pathCreator = (PathCreator)GetWindow(typeof(PathCreator));

#if UNITY_EDITOR
            pathCreator.Initialize((AssetDatabase.LoadAssetAtPath(CityEditorBookmarks.PATH_PREFAB_PATH, typeof(GameObject)) as GameObject).GetComponent<Path>());
#endif
            pathCreator.titleContent = new GUIContent("Path Creator");

            return pathCreator;
        }

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(380, 400);
        }

        private void OnFocus()
        {
            SceneView.duringSceneGui -= this.SceneView_duringSceneGui;
            SceneView.duringSceneGui += this.SceneView_duringSceneGui;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitNodes();
            LoadData();
            RoadEditorEvents.OnTrafficNodeAdd += RoadEditorEvents_OnTrafficNodeAdd;
            RoadEditorEvents.OnTrafficNodeRemove += RoadEditorEvents_OnTrafficNodeRemove;
            PrefabStage.prefabStageOpened += PrefabStage_prefabStageOpened;
            PrefabStage.prefabStageClosing += PrefabStage_prefabStageClosing;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            nodes.Clear();
            nodes = null;
            createdPaths.Clear();
            SaveData();
            RoadEditorEvents.OnTrafficNodeAdd -= RoadEditorEvents_OnTrafficNodeAdd;
            RoadEditorEvents.OnTrafficNodeRemove -= RoadEditorEvents_OnTrafficNodeRemove;
            PrefabStage.prefabStageOpened -= PrefabStage_prefabStageOpened;
            PrefabStage.prefabStageClosing -= PrefabStage_prefabStageClosing;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= this.SceneView_duringSceneGui;
        }

        private void OnGUI()
        {
            if (pathPrefab == null)
            {
                pathPrefab = EditorGUILayout.ObjectField("Path prefab", pathPrefab, typeof(Path), true) as Path;
            }

            var so = new SerializedObject(this);
            so.Update();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            ShowNodeSettings();

            ShowPathSettings(so);

            ShowVisualSettings(so);

            ShowConnectionSettings(so);

            so.ApplyModifiedProperties();

            if (GUILayout.Button("Swap Traffic Nodes"))
            {
                SwapNodes();
            }

            if (GUILayout.Button("Create"))
            {
                CreatePath();
            }

            if (duplicatePathErrorMessage)
            {
                EditorGUILayout.HelpBox("Path already exist.", MessageType.Error, true);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ShowNodeSettings()
        {
            System.Action nodeSettingsCallback = () =>
            {
                DocumentationLinkerUtils.ShowButtonFirst(DocLink, 34);

                EditorGUILayout.BeginHorizontal();

                var newSourceTrafficNode = EditorGUILayout.ObjectField("Source Traffic Node", sourceTrafficNode, typeof(TrafficNode), true) as TrafficNode;

                if (sourceTrafficNode != newSourceTrafficNode)
                {
                    SetSourceTrafficNode(newSourceTrafficNode);
                }

                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    sourceTrafficNode = null;
                    ValidateConnectedLaneIndexes();
                }

                EditorGUILayout.EndHorizontal();

                if (InspectorExtension.DrawVerticalArrowButton(arrowOffset: 120))
                {
                    SwapNodes();
                }

                EditorGUILayout.BeginHorizontal();

                targetTrafficNode = EditorGUILayout.ObjectField("Target Traffic Node", targetTrafficNode, typeof(TrafficNode), true) as TrafficNode;

                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    targetTrafficNode = null;
                    ValidateConnectedLaneIndexes();
                }

                EditorGUILayout.EndHorizontal();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Node settings", nodeSettingsCallback);
        }

        private void ShowVisualSettings(SerializedObject so)
        {
            System.Action visualSettingsCallback = () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(showPreviewDottedLine)));

                if (showPreviewDottedLine)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(showPathDirection)));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(arrowSpacing)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(pingAfterCreation)));

                var sourceLabelWidth = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 170f;

                EditorGUILayout.PropertyField(so.FindProperty(nameof(fontColor)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(previewConnectionColor)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(forbiddenConnectionColor)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(wrongConnectionColor)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(overridenConnectionColor)));

                EditorGUIUtility.labelWidth = sourceLabelWidth;

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Visual Settings", visualSettingsCallback, so.FindProperty(nameof(visualSceneSettingsFoldout)));
        }

        private void ShowConnectionSettings(SerializedObject so)
        {
            System.Action connectionSettingsCallback = () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(connectionType)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(overrideType)));

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    previousConnectionType = connectionType;
                    ValidateConnectedLaneIndexes();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(autoDetectSide)));

                if (autoDetectSide)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(autoSwitchType)));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(autoSwitchCurveType)));
                }

                if (!AutoSplineAvailable)
                {
                    GUI.enabled = false;
                    EditorGUILayout.Toggle("Auto Spline", false);
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(autoSpline)));
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(connectSameSide)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(sourceNodeSide)));

                if (!connectSameSide)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(targetNodeSide)));
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(sourceNodeSide)), new GUIContent("Target Connection Type"));
                    GUI.enabled = true;
                }

                EditorGUILayout.Separator();

                if (connectionType == ConnectionType.SingleConnect)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(connectSameIndex)));

                    int maxLaneCount = 10;

                    if (sourceTrafficNode)
                    {
                        maxLaneCount = sourceTrafficNode.LaneCount - 1;
                    }

                    EditorGUI.BeginChangeCheck();

                    sourceLaneIndex = EditorGUILayout.IntSlider("Source Lane Index", sourceLaneIndex, 0, maxLaneCount);

                    if (EditorGUI.EndChangeCheck())
                    {
                        ValidateConnectedLaneIndexes();
                        SceneView.RepaintAll();
                    }

                    if (!connectSameIndex)
                    {
                        maxLaneCount = 10;

                        if (targetTrafficNode)
                        {
                            maxLaneCount = targetTrafficNode.LaneCount - 1;
                        }

                        EditorGUI.BeginChangeCheck();

                        targetLaneIndex = EditorGUILayout.IntSlider("Target Lane Index", targetLaneIndex, 0, maxLaneCount);

                        if (EditorGUI.EndChangeCheck())
                        {
                            ValidateConnectedLaneIndexes();
                            SceneView.RepaintAll();
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        EditorGUILayout.IntSlider("Target Lane Index", sourceLaneIndex, 0, maxLaneCount);
                        GUI.enabled = true;
                    }
                }

            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Connection Settings", connectionSettingsCallback);
        }

        private void ShowPathSettings(SerializedObject so)
        {
            System.Action pathSettingsCallback = () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(pathCurveType)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(pathRoadType)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficGroupMask)));

                if (pathCurveType == PathCurveType.StraightLine)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(straightRoadWayPointCount)), new GUIContent("Waypoint Count"));
                }
                else
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(curvedRoadWayPointCount)), new GUIContent("Waypoint Count"));
                }

                if (pathCurveType == PathCurveType.StraightLine)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(speedLimitStraightLine)), new GUIContent("Speed Limit"));
                }
                else
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(speedLimitCurvedLine)), new GUIContent("Speed Limit"));
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(priority)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(selectAfterCreate)));
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Path Settings", pathSettingsCallback);
        }

        #endregion

        #region Methods

        public void Initialize(Path pathPrefab, RoadSegmentCreator roadSegmentCreator = null)
        {
            this.roadSegmentCreator = roadSegmentCreator;
            this.pathPrefab = pathPrefab;
        }

        public void TryToAddOrRemoveNode(TrafficNode trafficNode)
        {
            if (sourceTrafficNode == trafficNode)
            {
                sourceTrafficNode = null;
            }
            else if (targetTrafficNode == trafficNode)
            {
                targetTrafficNode = null;
            }
            else if (sourceTrafficNode == null)
            {
                SetSourceTrafficNode(trafficNode);
            }
            else
            {
                targetTrafficNode = trafficNode;
            }

            TryToCalculateSideConnection();
            ValidateConnectedLaneIndexes();

            Repaint();
        }

        private void SetSourceTrafficNode(TrafficNode trafficNode)
        {
            sourceTrafficNode = trafficNode;
            roadSegmentCreator = sourceTrafficNode.GetComponent<RoadSegmentCreator>();
        }

        private void TryToCalculateSideConnection()
        {
            if (!sourceTrafficNode || !targetTrafficNode || !autoDetectSide)
                return;

            var sameCrossRoad = sourceTrafficNode.TrafficLightCrossroad == targetTrafficNode.TrafficLightCrossroad;

            var forward1 = sourceTrafficNode.GetNodeForward(true);
            var forward2 = targetTrafficNode.GetNodeForward(false);

            var connectionDir = (targetTrafficNode.transform.position - sourceTrafficNode.transform.position).normalized;
            var dot = Vector3.Dot(forward1, connectionDir);
            var dot2 = Vector3.Dot(forward2, connectionDir);

            sourceNodeSide = dot > 0 ? ConnectSideType.DefaultSide : ConnectSideType.ExternalSide;
            targetNodeSide = dot2 > 0 ? ConnectSideType.DefaultSide : ConnectSideType.ExternalSide;

            if (!sourceTrafficNode.HasRightLanes)
            {
                sourceNodeSide = ConnectSideType.ExternalSide;
            }
            else if (!sourceTrafficNode.HasLeftLanes)
            {
                sourceNodeSide = ConnectSideType.DefaultSide;
            }

            if (!targetTrafficNode.HasLeftLanes)
            {
                targetNodeSide = ConnectSideType.ExternalSide;
            }
            else if (!targetTrafficNode.HasRightLanes)
            {
                targetNodeSide = ConnectSideType.DefaultSide;
            }

            if (sourceNodeSide != targetNodeSide)
                connectSameSide = false;

            if (sameCrossRoad)
            {
                if (autoSwitchType && connectionType == ConnectionType.TwoDirectionConnect)
                {
                    SetPreviousType();
                }
            }
            else
            {
                if (autoSwitchType)
                {
                    var externalConnection = sourceNodeSide == ConnectSideType.ExternalSide && targetNodeSide == ConnectSideType.ExternalSide;
                    var oneway = sourceTrafficNode.IsOneWay || targetTrafficNode.IsOneWay;

                    if (externalConnection && !oneway)
                    {
                        connectionType = ConnectionType.TwoDirectionConnect;
                    }
                    else
                    {
                        SetPreviousType();
                    }
                }
            }

            if (autoSwitchCurveType)
            {
                var dir = Vector3.Normalize(targetTrafficNode.transform.position - sourceTrafficNode.transform.position);
                var angle = Vector3.Angle(dir.Flat(), targetTrafficNode.transform.forward.Flat());

                var distance = Vector3.Distance(sourceTrafficNode.transform.position, targetTrafficNode.transform.position);

                if (angle > 90) angle = 180 - angle;

                pathCurveType = angle > MinCurveAngle && distance > MinCurveDistance ? PathCurveType.BezierCube : PathCurveType.StraightLine;
            }
        }

        public void SwapNodes()
        {
            var temp = sourceTrafficNode;
            SetSourceTrafficNode(targetTrafficNode);
            targetTrafficNode = temp;
            var temp2 = sourceNodeSide;
            sourceNodeSide = TargetConnectionType;
            targetNodeSide = temp2;
            ValidateConnectedLaneIndexes();
            SceneView.RepaintAll();
        }

        public bool CreatePath()
        {
            if (sourceTrafficNode == null || targetTrafficNode == null)
                return false;

            bool isExternal = sourceNodeSide == ConnectSideType.ExternalSide;

            Path lastPath = null;

            switch (connectionType)
            {
                case ConnectionType.SingleConnect:
                    {
                        int connectionLaneIndex = GetConnectionIndex();

                        lastPath = CreatePath(sourceTrafficNode, targetTrafficNode, sourceLaneIndex, connectionLaneIndex, isExternal, true);
                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        break;
                    }
                case ConnectionType.OneDirectionConnect:
                    {
                        lastPath = CreateDirectionPaths(sourceTrafficNode, targetTrafficNode, isExternal);

                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        break;
                    }
                case ConnectionType.TwoDirectionConnect:
                    {
                        lastPath = CreateDirectionPaths(sourceTrafficNode, targetTrafficNode, isExternal);
                        lastPath = CreateDirectionPaths(targetTrafficNode, sourceTrafficNode, TargetConnectionType == ConnectSideType.ExternalSide);

                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        break;
                    }
            }

            if (lastPath != null)
            {
                if (pingAfterCreation)
                {
                    EditorGUIUtility.PingObject(lastPath);
                }

                return true;
            }

            return true;
        }

        private Path CreateDirectionPaths(TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, bool isExternal)
        {
            Path lastPath = null;
            int maxLaneIndex = sourceTrafficNode.LaneCount;
            Undo.RegisterCompleteObjectUndo(sourceTrafficNode, "Created Path");

            for (int laneIndex = 0; laneIndex < maxLaneIndex; laneIndex++)
            {
                var isAvailable = targetTrafficNode.LaneCount > laneIndex;

                if (isAvailable)
                {
                    lastPath = CreatePath(sourceTrafficNode, targetTrafficNode, laneIndex, laneIndex, isExternal, false);
                }
            }

            return lastPath;
        }

        private Path CreatePath(TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, int sourceLaneIndex, int connectionLaneIndex, bool isExternalLane, bool recordSourceNodeUndo = true)
        {
            if (!AllowedConnection(sourceTrafficNode, targetTrafficNode))
                return null;

            if (sourceTrafficNode.AlreadyHasPath(sourceLaneIndex, connectionLaneIndex, targetTrafficNode, isExternalLane))
            {
                var oldPath = sourceTrafficNode.TryToGetPath(sourceLaneIndex, connectionLaneIndex, targetTrafficNode, isExternalLane);

                if (overrideType != OverrideType.Allowed)
                {
                    if (oldPath != null)
                    {
                        if (createdPaths.Contains(oldPath))
                        {
                            createdPaths.Remove(oldPath);
                        }

                        EditorGUIUtility.PingObject(oldPath);
                        UnityEngine.Debug.Log($"{GetPathName(oldPath)} already exist. Remove this path or enable the Override Path option.");
                    }

                    return null;
                }
                else
                {
                    if (createdPaths.Contains(oldPath))
                    {
                        createdPaths.Remove(oldPath);
                    }

                    sourceTrafficNode.DestroyPath(sourceLaneIndex, connectionLaneIndex, targetTrafficNode, isExternalLane, recordNodeUndo: false, recordPathUndo: true);
                }
            }

            var path = Instantiate(pathPrefab, sourceTrafficNode.PathParent);

            createdPaths.Add(path);
            Undo.RegisterCreatedObjectUndo(path.gameObject, "Created path");

            string indexNodeStr = string.Empty;

            if (roadSegmentCreator != null)
            {
                var sourceNodeIndex = roadSegmentCreator.GetNodeIndex(sourceTrafficNode);
                var targetNodeIndex = roadSegmentCreator.GetNodeIndex(targetTrafficNode);

                if (sourceNodeIndex != -1 && targetNodeIndex != -1)
                {
                    indexNodeStr = $"_{sourceNodeIndex + 1}-{targetNodeIndex + 1}";
                }
            }

            if (sourceNodeSide == ConnectSideType.DefaultSide)
            {
                path.name = $"Path{indexNodeStr}_{connectionLaneIndex}";
            }
            else
            {
                path.name = $"PathExt{indexNodeStr}_{connectionLaneIndex}";
            }

            path.SourceTrafficNode = sourceTrafficNode;
            path.ConnectedTrafficNode = targetTrafficNode;

            connectionLaneIndex = Mathf.Clamp(connectionLaneIndex, 0, targetTrafficNode.LaneCount - 1);

            var sourceDefault = sourceNodeSide == ConnectSideType.DefaultSide;
            var sourcePosition = sourceTrafficNode.GetLanePosition(sourceLaneIndex, !sourceDefault);

            var targetExternal = TargetConnectionType == ConnectSideType.DefaultSide;
            var targetPosition = targetTrafficNode.GetLanePosition(connectionLaneIndex, targetExternal);

            path.transform.parent = sourceTrafficNode.PathParent;
            path.transform.localPosition = Vector3.zero;

            Transform[] nodes = null;

            switch (pathCurveType)
            {
                case PathCurveType.StraightLine:
                    {
                        nodes = new Transform[2];
                        nodes[0] = new GameObject("Node1").transform;
                        nodes[1] = new GameObject("Node2").transform;

                        nodes[0].transform.position = sourcePosition;
                        nodes[1].transform.position = targetPosition;
                        break;
                    }
                case PathCurveType.BezierCube:
                    {
                        nodes = new Transform[3];
                        nodes[0] = new GameObject("Node1").transform;
                        nodes[1] = new GameObject("Node2").transform;
                        nodes[2] = new GameObject("Node3").transform;

                        nodes[0].transform.position = sourcePosition;
                        nodes[2].transform.position = targetPosition;

                        if (!AutoSpline)
                        {
                            nodes[1].transform.position = (sourcePosition + targetPosition) / 2;
                        }
                        else
                        {
                            nodes[1].transform.position = PathAttachHelper.GetSplineCornerPoint(sourcePosition, targetPosition, sourceTrafficNode, targetTrafficNode, AllowClosestPoint);
                        }

                        break;
                    }
                case PathCurveType.BezierQuad:
                    {
                        nodes = new Transform[4];
                        nodes[0] = new GameObject("Node1").transform;
                        nodes[1] = new GameObject("Node2").transform;
                        nodes[2] = new GameObject("Node3").transform;
                        nodes[3] = new GameObject("Node4").transform;

                        nodes[0].transform.position = sourcePosition;
                        nodes[3].transform.position = targetPosition;

                        if (!AutoSpline)
                        {
                            var middlePos = (sourcePosition + targetPosition) / 2;

                            nodes[1].transform.position = (sourcePosition + middlePos) / 2;
                            nodes[2].transform.position = (targetPosition + middlePos) / 2;
                        }
                        else
                        {
                            var cornerPoints = PathAttachHelper.GetSplineTwoCornerPoint(sourcePosition, targetPosition, sourceTrafficNode, targetTrafficNode, AllowClosestPoint);

                            nodes[1].transform.position = cornerPoints.Item1;
                            nodes[2].transform.position = cornerPoints.Item2;
                        }

                        break;
                    }
            }

            path.Nodes = nodes.ToList();
            path.WayPointsCountPerCurve = WaypointCount;
            path.PathCurveType = pathCurveType;
            path.PathRoadType = pathRoadType;
            path.TrafficGroupMask = trafficGroupMask.GetClone();
            path.PathSpeedLimit = SpeedLimit;
            path.Priority = priority;
            path.ConnectedLaneIndex = connectionLaneIndex;
            path.CreatePath();
            path.ResetSpeedLimit();

            bool externalLane = sourceNodeSide == ConnectSideType.ExternalSide;

            if (recordSourceNodeUndo)
            {
                Undo.RegisterCompleteObjectUndo(sourceTrafficNode, "Revert Source Node");
            }

            sourceTrafficNode.AddPath(path, sourceLaneIndex, isExternalLane: externalLane, lockAutoPath: true);

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            if (sourceNodeSide != TargetConnectionType)
            {
                path.ReversedConnectionSide = true;
            }

            EditorSaver.SetObjectDirty(path);

            if (selectAfterCreate)
            {
                Selection.activeObject = path;
            }

            UnityEngine.Debug.Log($"{GetPathName(path)} successfully created.");

            return path;
        }

        private int GetConnectionIndex()
        {
            return connectSameIndex ? sourceLaneIndex : targetLaneIndex;
        }

        private void AddNode(TrafficNode node)
        {
            TryToAddOrRemoveNode(node);
            Focus();
        }

        private void RemoveNode(TrafficNode node)
        {
            if (sourceTrafficNode == node)
            {
                sourceTrafficNode = null;
            }

            if (targetTrafficNode == node)
            {
                targetTrafficNode = null;
            }

            Focus();
        }

        private void SetPreviousType()
        {
            if (connectionType == ConnectionType.TwoDirectionConnect)
            {
                if (previousConnectionType != ConnectionType.TwoDirectionConnect)
                {
                    connectionType = previousConnectionType;
                }
                else
                {
                    connectionType = ConnectionType.SingleConnect;
                    previousConnectionType = connectionType;
                }
            }
        }

        private void ValidateConnectedLaneIndexes()
        {
            duplicatePathErrorMessage = false;

            if (connectionType != ConnectionType.SingleConnect || overrideType == OverrideType.Allowed)
                return;

            if (sourceTrafficNode == null || targetTrafficNode == null)
                return;

            int connectionIndex = GetConnectionIndex();

            duplicatePathErrorMessage = sourceTrafficNode.AlreadyHasPath(sourceLaneIndex, connectionIndex, targetTrafficNode);
        }

        private void TryToDrawConnectionLine(Vector3 point1, Vector3 point2, Color lineColor, bool autoArrow = true)
        {
            if (!showPreviewDottedLine || point1 == Vector3.zero || point2 == Vector3.zero)
                return;

            var oldColor = Handles.color;
            Handles.color = lineColor;
            Handles.DrawDottedLine(point1, point2, DottedLineSize);
            Handles.color = oldColor;

            if (!showPathDirection)
                return;

            var distance = Vector3.Distance(point1, point2);
            var direction = (point2 - point1).normalized;

            if (autoArrow || true)
            {
                int arrowsCount = Mathf.FloorToInt(distance / arrowSpacing);

                for (int i = 1; i <= arrowsCount; i++)
                {
                    var pos = point1 + direction * arrowSpacing * i;
                    EditorExtension.DrawArrow(pos, direction, ArrowAngle, ArrowSideLength, lineColor);
                }
            }
            else
            {
                var prevDistance = startDistance;
                startDistance += distance;

                while (startDistance > arrowSpacing)
                {
                    startDistance -= arrowSpacing;
                    var pos = point1 + direction * (arrowSpacing - prevDistance);
                    EditorExtension.DrawArrow(pos, direction, ArrowAngle, ArrowSideLength, lineColor);
                    prevDistance = startDistance;
                }
            }
        }

        private bool CanDrawConnectionPath(TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, bool sourceIsRightSide, int laneIndex, int connectionLaneIndex, out Color color)
        {
            color = previewConnectionColor;

            if (sourceTrafficNode == null || targetTrafficNode == null)
                return false;

            var path = sourceTrafficNode.TryToGetPath(laneIndex, connectionLaneIndex, targetTrafficNode, !sourceIsRightSide);

            if (createdPaths.Contains(path))
                return false;

            var hasPath = path != null;

            if (hasPath)
            {
                if (overrideType == OverrideType.Allowed)
                {
                    color = overridenConnectionColor;
                }
                else
                {
                    color = forbiddenConnectionColor;
                }
            }
            else
            {
                if (!AllowedConnection(sourceTrafficNode, targetTrafficNode))
                {
                    color = wrongConnectionColor;
                }
            }

            return true;
        }

        private bool AllowedConnection(TrafficNode trafficNode, TrafficNode dstConnection)
        {
            if (trafficNode == dstConnection) return true;

            return TrafficNodeExtension.IsCorrectDirConnection(trafficNode, dstConnection, sourceNodeSide == ConnectSideType.DefaultSide, targetNodeSide == ConnectSideType.DefaultSide);
        }

        private string GetPathName(Path path) => $"Crossroad '{path.SourceTrafficNode.TrafficLightCrossroad.name}' Node '{path.SourceTrafficNode.name}' Path '{path.name}'";

        private void InitNodes()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            InitNodes(stage);
        }

        private void InitNodes(PrefabStage prefabStage)
        {
            if (prefabStage == null)
            {
                nodes = ObjectUtils.FindObjectsOfType<TrafficNode>().ToList();
            }
            else
            {
                var stageParent = prefabStage.scene.GetRootGameObjects()[0];
                nodes = stageParent.GetComponentsInChildren<TrafficNode>().ToList();
            }
        }

        #endregion

        #region Event Handlers

        private void SceneView_duringSceneGui(SceneView sceneView)
        {
            Handles.BeginGUI();

            TrafficNodesGUIHelper.DrawNodeButtons(nodes, sourceTrafficNode, targetTrafficNode, AddNode, RemoveNode, fontColor);

            var sourceIsRightSide = sourceNodeSide == ConnectSideType.DefaultSide;
            var connectedIsRightSide = TargetConnectionType == ConnectSideType.DefaultSide;

            switch (connectionType)
            {
                case ConnectionType.SingleConnect:
                    {
                        if (CanDrawConnectionPath(sourceTrafficNode, targetTrafficNode, sourceIsRightSide, sourceLaneIndex, TargetLaneIndex, out var connectionColor))
                        {
                            DrawLaneConnection(sourceTrafficNode, targetTrafficNode, sourceIsRightSide, connectedIsRightSide, sourceLaneIndex, TargetLaneIndex, connectionColor);
                        }

                        break;
                    }
                case ConnectionType.OneDirectionConnect:
                    {
                        DrawNodeConnection(sourceTrafficNode, targetTrafficNode, sourceIsRightSide, connectedIsRightSide);
                        break;
                    }
                case ConnectionType.TwoDirectionConnect:
                    {
                        DrawNodeConnection(sourceTrafficNode, targetTrafficNode, sourceIsRightSide, connectedIsRightSide);
                        DrawNodeConnection(targetTrafficNode, sourceTrafficNode, connectedIsRightSide, sourceIsRightSide);
                        break;
                    }
            }

            Handles.EndGUI();
        }

        private void PrefabStage_prefabStageClosing(PrefabStage stage)
        {
            InitNodes(null);
        }

        private void PrefabStage_prefabStageOpened(PrefabStage stage)
        {
            InitNodes(stage);
        }

        private void DrawNodeConnection(TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, bool sourceIsRightSide, bool connectedIsRightSide)
        {
            if (sourceTrafficNode == null)
                return;

            int maxLaneIndex = sourceTrafficNode.LaneCount;

            for (int laneIndex = 0; laneIndex < maxLaneIndex; laneIndex++)
            {
                if (CanDrawConnectionPath(sourceTrafficNode, targetTrafficNode, sourceIsRightSide, laneIndex, laneIndex, out var connectionColor))
                {
                    DrawLaneConnection(sourceTrafficNode, targetTrafficNode, sourceIsRightSide, connectedIsRightSide, laneIndex, laneIndex, connectionColor);
                }
            }
        }

        private void DrawLaneConnection(
            TrafficNode sourceTrafficNode,
            TrafficNode targetTrafficNode,
            bool sourceIsRightSide,
            bool connectedIsRightSide,
            int laneIndex,
            int targetLaneIndex,
            Color connectionColor)
        {
            var pointA = TrafficNodesGUIHelper.DrawSelectedLanePoint(sourceTrafficNode, laneIndex, sourceIsRightSide, true, connectionColor);
            var pointB = TrafficNodesGUIHelper.DrawSelectedLanePoint(targetTrafficNode, targetLaneIndex, connectedIsRightSide, false, connectionColor);

            if (AutoSpline)
            {
                startDistance = 0;

                var prevPoint = pointA;

                switch (pathCurveType)
                {
                    case PathCurveType.BezierCube:
                        {
                            var middlePoint = PathAttachHelper.GetSplineCornerPoint(pointA, pointB, sourceTrafficNode, targetTrafficNode, AllowClosestPoint);

                            for (int j = 0; j < Bezier.SEGMENT_COUNT; j++)
                            {
                                var point = Bezier.GetCurvePoint(pointA, middlePoint, pointB, j, Bezier.SEGMENT_COUNT);
                                Handles.DrawLine(prevPoint, point);

                                TryToDrawConnectionLine(prevPoint, point, connectionColor, false);

                                prevPoint = point;
                            }

                            break;
                        }
                    case PathCurveType.BezierQuad:
                        {
                            var middlePoints = PathAttachHelper.GetSplineTwoCornerPoint(pointA, pointB, sourceTrafficNode, targetTrafficNode, AllowClosestPoint);

                            for (int j = 0; j < Bezier.SEGMENT_COUNT; j++)
                            {
                                var point = Bezier.GetCurvePoint(pointA, middlePoints.Item1, middlePoints.Item2, pointB, j, Bezier.SEGMENT_COUNT);
                                Handles.DrawLine(prevPoint, point);

                                TryToDrawConnectionLine(prevPoint, point, connectionColor, false);

                                prevPoint = point;
                            }

                            break;
                        }
                }

                TryToDrawConnectionLine(prevPoint, pointB, connectionColor, false);
            }
            else
            {
                TryToDrawConnectionLine(pointA, pointB, connectionColor);
            }
        }

        private void RoadEditorEvents_OnTrafficNodeAdd(TrafficNode node)
        {
            nodes.TryToAdd(node);
        }

        private void RoadEditorEvents_OnTrafficNodeRemove(TrafficNode node)
        {
            nodes.TryToRemove(node);
        }

        #endregion
    }
}
#endif