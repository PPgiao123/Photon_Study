﻿using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class RoadParent : MonoBehaviour
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/structure.html#road-parent")]
        [SerializeField] private string link;

        [SerializeField] private List<TrafficLightCrossroad> trafficLightCrossroads = new List<TrafficLightCrossroad>();

        [Tooltip("Automatically adds a waypoint at each selected offset to the automatically created paths (if the value is greater than zero)")]
        [SerializeField][Range(0f, 100f)] private float connectionWaypointOffset = 20;

        [Tooltip("Add additional traffic sub-nodes for waypoints on external paths")]
        [SerializeField] private bool externalSubNodes = true;

        [Tooltip("Raycast connection distance between traffic nodes, if zero, then infinite value")]
        [SerializeField][Range(0f, 100f)] private float castDistance = 0;

        [Tooltip("Multi-angle raycasting along Z-axis")]
        [SerializeField] private bool multiAngleRaycast;

        [Tooltip("If the value is greater than 0, additional pedestrian nodes will be created for the specified distance, useful for balancing spawn between long pedestrian links. Generated by pressing the path bake data button")]
        [SerializeField][Range(0f, 100f)] private float pedestrianSubNodeDistance = 20;

        [Tooltip("Auto-connect pedestrian node crosswalks between road segments. Connection is made by pressing the force connect button")]
        [SerializeField] private bool connectCrosswalks;

        public List<TrafficLightCrossroad> TrafficLightCrossroads => trafficLightCrossroads;

        public void ConnectSegments()
        {
            ConnectSegments(connectionWaypointOffset, multiAngleRaycast, castDistance);
        }

        public void ConnectSegments(float connectionWaypointOffset, bool multiAngleRaycast, float maxConnectionLength = 0)
        {
            var layer = LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME);

            if (layer == -1)
            {
                UnityEngine.Debug.Log($"RoadParent. Road segments cannot be connected because the '{ProjectConstants.TRAFFIC_NODE_LAYER_NAME}' layer is not defined, make sure you have added the '{ProjectConstants.TRAFFIC_NODE_LAYER_NAME}' layer in the Spirit604/Package Initialiazation/Layer settings");
                return;
            }
            else
            {
#if UNITY_EDITOR

                var trafficNodeObj = AssetDatabase.LoadAssetAtPath(CityEditor.CityEditorBookmarks.TRAFFIC_NODE_PREFAB_PATH, typeof(GameObject)) as GameObject;

                if (trafficNodeObj && trafficNodeObj.layer != layer)
                {
                    string layerName = "NaN";

                    string tempLayerName = LayerMask.LayerToName(trafficNodeObj.layer);

                    if (!string.IsNullOrEmpty(tempLayerName))
                    {
                        layerName = tempLayerName;
                    }

                    UnityEngine.Debug.Log($"RoadParent. TrafficNode prefab has '{layerName}' layer, but should have '{ProjectConstants.TRAFFIC_NODE_LAYER_NAME}' layer");
                    return;
                }
#endif
            }

            var settings = new TrafficNode.AutoConnectionSettings(connectionWaypointOffset, multiAngleRaycast, 0, maxConnectionLength, connectCrosswalks, externalSubNodes);
            RoadSegment[] roadSegments = GetComponentsInChildren<RoadSegment>();

            foreach (var roadSegment in roadSegments)
            {
                roadSegment.ConnectNodes(settings);
            }
        }

        public void ForceConnectSegments()
        {
            ResetSegments();
            ConnectSegments();
        }

        public void ResetSegments()
        {
            RoadSegment[] roadSegments = GetComponentsInChildren<RoadSegment>();

            foreach (var roadSegment in roadSegments)
            {
                roadSegment.ResetNodes();
            }
        }

        public void ConnectPedestrianNodes()
        {
            var nodes = GetComponentsInChildren<PedestrianNode>();

            foreach (var node in nodes)
            {
                node.ConnectButton();
            }
        }

        public void AddCrossroads()
        {
            trafficLightCrossroads = GetComponentsInChildren<TrafficLightCrossroad>().ToList();
            EditorSaver.SetObjectDirty(this);
        }

        public void BakePathData()
        {
            HashSet<int> ids = new HashSet<int>();

            RoadSegment[] roadSegments = GetComponentsInChildren<RoadSegment>();

            foreach (var roadSegment in roadSegments)
            {
                roadSegment.BakeData();

                var creator = roadSegment.GetComponent<RoadSegmentCreator>();

                if (creator)
                {
                    var id = creator.trafficLightCrossroad.UniqueId;

                    if (id == 0 || ids.Contains(id))
                    {
                        if (id != 0)
                        {
                            UnityEngine.Debug.Log($"RoadSegment '{creator.name}' all ids have been regenerated");
                        }

#if UNITY_EDITOR
                        creator.RegenerateAllIds();
#endif

                        id = creator.trafficLightCrossroad.UniqueId;
                    }

                    ids.Add(id);
                }
            }

            if (transform.parent && transform.parent.parent)
            {
                var parent = transform.parent.parent;

                var pedestrianNodesParent = parent.GetChild(1);
                var bakedObjects = pedestrianNodesParent.GetComponentsInChildren<IBakeRoad>();

                foreach (var bakedObject in bakedObjects)
                {
                    bakedObject.Bake();
                }
            }

            ProcessPedestrianNodes();
            UnityEngine.Debug.Log("Baking complete.");
        }

        public void ClearUnattachedPaths(bool recordUndo = true, bool reportResult = true)
        {
            var paths = ObjectUtils.FindObjectsOfType<Path>().Where(item => !item.HasConnection).ToList();
            List<TrafficNode> savedNodes = new List<TrafficNode>();
            int count = 0;

            while (paths.Count > 0)
            {
#if UNITY_EDITOR
                var sourceTrafficNode = paths[0].SourceTrafficNode;

                if (sourceTrafficNode != null)
                {
                    if (savedNodes.TryToAdd(sourceTrafficNode))
                    {
                        if (recordUndo)
                        {
                            Undo.RegisterCompleteObjectUndo(sourceTrafficNode, "Revert node paths");
                        }
                    }
                }

                if (recordUndo)
                {
                    Undo.DestroyObjectImmediate(paths[0].gameObject);
                }
                else
                {
                    GameObject.DestroyImmediate(paths[0].gameObject);
                }
#endif

                paths.RemoveAt(0);
                count++;
            }

            if (count > 0)
            {
#if UNITY_EDITOR
                if (recordUndo)
                {
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }
#endif
            }

            if (reportResult)
            {
                UnityEngine.Debug.Log($"Deleted {count} paths");
            }
        }

        public void AddCrossRoad(TrafficLightCrossroad trafficLightCrossroad)
        {
            if (trafficLightCrossroads.TryToAdd(trafficLightCrossroad))
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void RemoveCrossRoad(TrafficLightCrossroad trafficLightCrossroad)
        {
            if (trafficLightCrossroads.TryToRemove(trafficLightCrossroad))
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void ProcessPedestrianNodes()
        {
            var nodes = ObjectUtils.FindObjectsOfType<PedestrianNode>();

            for (int i = 0; i < nodes.Length; i++)
            {
                PedestrianNode node = nodes[i];
                node.BakeConnections(pedestrianSubNodeDistance);
            }
        }

        private void OnValidate()
        {
            trafficLightCrossroads.RemoveAll(item => item == null);
        }
    }
}