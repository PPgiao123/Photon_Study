using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        public void CreateCustomSegment()
        {
            CreateTrafficNodeCount = 2;
            bool recreated = false;

            if (!customRoadSegmentCreated)
            {
                Clear();

                customRoadSegmentCreated = true;

                var trafficNode1 = CreateTrafficNode(0);
                var trafficNode2 = CreateTrafficNode(2);

                GenerateCustomTempPath();

                ClearNullNodes();
                trafficLightCrossroad.AddNode(trafficNode1);
                trafficLightCrossroad.AddNode(trafficNode2);
                trafficLightCrossroad.HasLights = false;
                recreated = true;
            }
            else
            {
                if (createdTrafficNodes.Count == 0)
                {
                    Clear();
                    Create();
                    return;
                }

                Recalculate();
            }

            if (recreated)
            {
                CreateOffsetPaths();
            }
        }

        public void GenerateCustomTempPath()
        {
            var trafficNode1 = TryToGetNode(0);
            var trafficNode2 = TryToGetNode(1);

            var tempCustomPath = AddTempPath(PathDirectionType.Forward, trafficNode1, trafficNode2);
            tempCustomPath.transform.parent = trafficNode1.transform.parent;

#if UNITY_EDITOR
            tempCustomPath.HighlightColor = Color.green;
            SceneVisibilityManager.instance.DisablePicking(tempCustomPath.gameObject, true);
#endif

            var point1 = createdTrafficNodes[0].transform.position;
            var point2 = createdTrafficNodes[1].transform.position;

            tempCustomPath.Nodes[0].transform.position = point1;
            tempCustomPath.Nodes[tempCustomPath.Nodes.Count - 1].transform.position = point2;

            tempCustomPath.PathCurveType = PathCurveType.StraightLine;
            tempCustomPath.WayPointsCountPerCurve = tempCustomPath.PathCurveType == PathCurveType.StraightLine ? wayPointStraightRoadCount : wayPointTurnCurveCount;

            Vector3 middlePoint = (point1 + point2) / 2 + (point2 - point1).normalized * 1f;

            if (tempCustomPath.Nodes.Count == 3)
            {
                tempCustomPath.Nodes[1].transform.position = middlePoint;

                Vector3 dir = (tempCustomPath.Nodes[tempCustomPath.Nodes.Count - 1].transform.position - middlePoint).normalized;

                tempCustomPath.Nodes[1].transform.rotation = Quaternion.LookRotation(dir * ProjectConstants.LaneHandDirection);
            }

            tempCustomPath.CreatePath(true);
        }

        public void RecalculateCustomPath(bool recreatePath = false, bool attachExternalPaths = false)
        {
            var tempCustomPath = GetTempPath(0);

            if (tempCustomPath == null)
            {
                Debug.Log("Temp Custom Path not found.");
                return;
            }

            if (attachExternalPaths)
            {
                AttachAllInnerOuterPaths(false);
            }

            var point1 = createdTrafficNodes[0].transform.position;
            var point2 = createdTrafficNodes[1].transform.position;

            tempCustomPath.Nodes[0].transform.position = point1;
            tempCustomPath.Nodes[0].rotation = createdTrafficNodes[0].GetNodeRotation(1);
            tempCustomPath.Nodes[tempCustomPath.Nodes.Count - 1].transform.position = point2;
            tempCustomPath.Nodes[tempCustomPath.Nodes.Count - 1].rotation = createdTrafficNodes[1].GetNodeRotation(-1);
            tempCustomPath.CreatePath(recreatePath);

            CreateOffsetPaths();
        }

        public void AttachAllInnerOuterPaths(bool recordUndo = false)
        {
#if UNITY_EDITOR
            RecalculateAllOuterConnectedPaths(recordUndo);
            IterateAllNodes(node => AttachInnerExternalPaths(node, recordUndo));
#endif
        }

        public void AlignCustomPath()
        {
#if UNITY_EDITOR
            SnapToSurfaceCustomPath();

            var tempCustomPath = GetTempPath(0);

            if (tempCustomPath == null)
            {
                return;
            }

            List<Transform> sourceNodes = new List<Transform>(tempCustomPath.Nodes);

            bool changed = false;

            float castOffset = minWaypointOffset;

            int insertIndex = 0;

            for (int i = 0; i < sourceNodes.Count - 1; i++)
            {
                var currentNodePos = sourceNodes[i].transform.position;
                var targetNodePos = sourceNodes[i + 1].transform.position;

                var points = PathAttachHelper.GetPointsAlongSurface(currentNodePos, targetNodePos, SnapLayerMask, castOffset, angleThreshold, SnapSurfaceOffset, DebugCast);

                if (points?.Count > 0 && !changed)
                {
                    changed = true;
                    tempCustomPath.SavePath(false, true);
                }

                for (int j = 0; j < points.Count; j++)
                {
                    insertIndex++;

                    var node = tempCustomPath.InsertNode(points[j], insertIndex, false);

                    if (node != null)
                    {
                        Undo.RegisterCreatedObjectUndo(node.gameObject, "Created node");
                    }
                }

                insertIndex++;
            }

            if (changed)
            {
                RecalculateCustomPath(true);
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
#endif
        }

        public void ContinuePath(Vector3 position)
        {
            var tempCustomPath = GetTempPath(0);

            if (tempCustomPath == null)
            {
                return;
            }

            var distance1 = Vector3.Distance(createdTrafficNodes[0].transform.position, position);
            var distance2 = Vector3.Distance(createdTrafficNodes[1].transform.position, position);

            bool byEnd = distance2 < distance1;

            tempCustomPath.SavePath();

            if (byEnd)
            {
                Vector3 direction = (position - tempCustomPath.Nodes.Last().transform.position).normalized.Flat();

#if UNITY_EDITOR
                Undo.RecordObject(createdTrafficNodes[1].transform, "Edited node");
#endif

                createdTrafficNodes[1].transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));

                tempCustomPath.AddNode(createdTrafficNodes[1].transform.position, false);
            }
            else
            {
                var sourcePos = createdTrafficNodes[0].transform.position;
                Vector3 direction = (position - tempCustomPath.Nodes[0].transform.position).normalized.Flat();

#if UNITY_EDITOR
                Undo.RecordObject(createdTrafficNodes[0].transform, "Edited node");
#endif
                createdTrafficNodes[0].transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));

                tempCustomPath.InsertNode(sourcePos, 0);

                EditorSaver.SetObjectDirty(createdTrafficNodes[0].transform);
            }

            tempCustomPath.CreatePath(true, true);

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif

            RecalculateCustomPath();
        }

        public void StripNodes()
        {
            var removedCount = StripNodes(roadSegmentCreatorConfig.MinStripAngle, roadSegmentCreatorConfig.MinStripDistance, true);

            Debug.Log($"Removed {removedCount} waypoints.");
        }

        public void ClearPathSpawnNodes()
        {
            generateSpawnNodes = false;

            IterateAllTrafficNodesPath(path =>
            {
                path.IterateWaypoints(node => node.SpawnNode = false);
            });
        }

        public void GeneratePathSpawnNodes()
        {
            GeneratePathSpawnNodes(roadSegmentCreatorConfig.MinSpawnNodeOffset);
        }

        public void GeneratePathSpawnNodes(float minSpawnNodeOffset)
        {
            ClearPathSpawnNodes();

            generateSpawnNodes = true;

            IterateAllTrafficNodesPath(path =>
            {
                SetSpawnNodes(path, minSpawnNodeOffset);
            });
        }

        public int StripNodes(float maxStripAngle, float minStripDistance, bool recordUndo = false)
        {
            var tempPath = GetTempPath(0);
            var points = tempPath.Nodes;

            int index = 1;

            bool changed = false;

            Vector3 lastPoint = points[0].transform.position;

            float currentDistance = 0;

            int removedCount = 0;

            while (index < points.Count - 1)
            {
                var prevPoint = points[index - 1].transform.position;
                var currentPoint = points[index].transform.position;
                var nextPoint = points[index + 1].transform.position;

                Vector3 dirPrevious = (currentPoint - prevPoint).normalized;
                Vector3 dirNext = (nextPoint - currentPoint).normalized;

                var angle = Vector3.Angle(dirPrevious, dirNext);

                bool removed = false;

                if (angle < maxStripAngle)
                {
                    var remove = true;

                    if (minStripDistance > 0)
                    {
                        currentDistance += Vector3.Distance(currentPoint, lastPoint);
                        remove = currentDistance <= minStripDistance;
                    }

                    if (remove)
                    {
                        changed = true;
                        tempPath.RemoveNodeAt(index, recordUndo, false);
                        removed = true;
                        removedCount++;
                    }
                    else
                    {
                        lastPoint = currentPoint;
                        currentDistance = 0;
                    }
                }
                else
                {
                    lastPoint = currentPoint;
                    currentDistance = 0;
                }

                if (!removed)
                {
                    index++;
                }
            }

            if (changed)
            {
                tempPath.CreatePath(true, recordUndo);
                CreateOffsetPaths();
            }

            return removedCount;
        }

        public void UpdatePaths(bool recordUndo = false, bool includeOuter = false)
        {
#if UNITY_EDITOR
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                UpdatePaths(createdTrafficNodes[i], recordUndo);
            }

            if (includeOuter)
            {
                RecalculateAllOuterConnectedPaths(recordUndo);
            }
#endif
        }

        public void UpdatePaths(TrafficNode trafficNode, bool recordUndo = false)
        {
            if (trafficNode.IsOneWay && trafficNode.IsEndOfOneWay)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(trafficNode, "Edited Node");
#endif
                }

                List<Path> paths = null;

                for (int laneIndex = 0; laneIndex < trafficNode.Lanes?.Count; laneIndex++)
                {
                    for (int j = 0; j < trafficNode.Lanes[laneIndex].paths?.Count; j++)
                    {
                        var path = trafficNode.Lanes[laneIndex].paths[j];

                        if (paths == null)
                        {
                            paths = new List<Path>();
                        }

                        paths.TryToAdd(path);
                    }
                }

                while (paths?.Count > 0)
                {
                    var path = paths[0];

                    if (path != null)
                    {
                        if (recordUndo)
                        {
#if UNITY_EDITOR
                            Undo.DestroyObjectImmediate(path.gameObject);
#endif
                        }
                        else
                        {
                            GameObject.DestroyImmediate(path.gameObject);
                        }
                    }

                    paths.RemoveAt(0);
                }

                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
                }
            }

            AttachDefaultPaths(trafficNode, recordUndo);
            AttachInnerExternalPaths(trafficNode, recordUndo);
        }

        public void AttachDefaultPaths(TrafficNode trafficNode, bool recordUndo = false)
        {
            trafficNode.IterateAllPaths((path, laneIndex) =>
            {
                if (recordUndo)
                {
                    path.SaveMovementUndo();
                }

                path.AutoAttachToTrafficNodes();
            });
        }

        public void AttachInnerExternalPaths(TrafficNode trafficNode, bool recordUndo = false)
        {
            trafficNode.IterateExternalPaths((path, laneIndex) =>
            {
                if (recordUndo)
                {
                    path.SaveMovementUndo();
                }

                path.AttachToTrafficNodes(laneIndex, true, !path.ReversedConnectionSide);
            });
        }

        public void RecalculateCustomPaths(bool shouldRecreateWaypoints)
        {
            shouldRecreateWaypoints = true;

            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                int currentIndex = i;
                int nextIndex = (currentIndex + 1) % 2;

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    createdTrafficNodes[currentIndex].Lanes[laneIndex].paths[0].Nodes[0].transform.position = createdTrafficNodes[currentIndex].transform.position + Quaternion.Euler(0, -90, 0) * createdTrafficNodes[currentIndex].transform.forward * (LaneWidth / 2 + laneIndex * LaneWidth);
                    createdTrafficNodes[currentIndex].Lanes[laneIndex].paths[0].Nodes[createdTrafficNodes[currentIndex].Lanes[laneIndex].paths[0].Nodes.Count - 1].transform.position = createdTrafficNodes[nextIndex].transform.position + Quaternion.Euler(0, 90, 0) * createdTrafficNodes[nextIndex].transform.forward * (LaneWidth / 2 + laneIndex * LaneWidth);
                }
            }

            var tempCustomPath = GetTempPath(0);

            for (int trafficNodeIndex = 0; trafficNodeIndex < createdTrafficNodes.Count; trafficNodeIndex++)
            {
                for (int i = 1; i < tempCustomPath?.Nodes.Count - 1; i++)
                {
                    Vector3 cross = Quaternion.Euler(0, 90, 0) * tempCustomPath.Nodes[i].transform.forward;

                    for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                    {
                        Vector3 offset = cross * (LaneWidth / 2 + LaneWidth * laneIndex);

                        createdTrafficNodes[0].Lanes[laneIndex].paths[0].Nodes[i].transform.position = tempCustomPath.Nodes[i].transform.position + offset;
                    }

                    for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                    {
                        Vector3 offset = -cross * (LaneWidth / 2 + LaneWidth * laneIndex);
                        createdTrafficNodes[1].Lanes[laneIndex].paths[0].Nodes[tempCustomPath.Nodes.Count - i - 1].transform.position = tempCustomPath.Nodes[i].transform.position + offset;
                    }
                }
            }

            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                if (createdTrafficNodes[i].Lanes.Count > 0)
                {
                    for (int j = 0; j < laneCount; j++)
                    {
                        var path = createdTrafficNodes[i].Lanes[j].paths[0];
                        path.CreatePath(shouldRecreateWaypoints);
                    }
                }
            }
        }

        public void SnapToSurfaceCustomPath(GameObject customSnapObject = null)
        {
            var tempCustomPath = GetTempPath(0);

            if (tempCustomPath == null)
                return;

            for (int i = 0; i < tempCustomPath.Nodes.Count; i++)
            {
                Transform node = tempCustomPath.Nodes[i];

                bool canSnap = (i != 0 && i != tempCustomPath.Nodes.Count - 1) || straightRoadSelectedNodeIndex == 0 || (i == 0 && straightRoadSelectedNodeIndex == 1) || (i == tempCustomPath.Nodes.Count - 1 && straightRoadSelectedNodeIndex == 2);

                if (canSnap)
                {
                    SnapToSurface(node, SnapSurfaceOffset, customSnapObject: customSnapObject);
                }
            }

            if (straightRoadSelectedNodeIndex == 0 || straightRoadSelectedNodeIndex == 1)
            {
                RecordAllPathUndo(createdTrafficNodes[0], true, true);
                SnapToSurface(createdTrafficNodes[0].transform, SnapSurfaceOffset, false, true, customSnapObject: customSnapObject);
            }

            if (straightRoadSelectedNodeIndex == 0 || straightRoadSelectedNodeIndex == 2)
            {
                RecordAllPathUndo(createdTrafficNodes[1], true, true);
                SnapToSurface(createdTrafficNodes[1].transform, SnapSurfaceOffset, false, true, customSnapObject: customSnapObject);
            }

            RecalculateCustomPath(attachExternalPaths: true);

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }

        public void RecordAllPathUndo(bool includeOuter = false, bool includeNodeTransform = false)
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                var trafficNode = createdTrafficNodes[i];
                RecordAllPathUndo(trafficNode, includeOuter, includeNodeTransform);
            }
        }

        public void RecordAllPathUndo(TrafficNode trafficNode, bool includeOuter = false, bool includeNodeTransform = false)
        {
            if (includeNodeTransform)
            {
#if UNITY_EDITOR
                Undo.RecordObject(trafficNode.transform, "Change Node Position");
#endif
            }

            trafficNode.IterateAllPaths(path =>
            {
                path.SaveMovementUndo(true);
            }, true);

            if (includeOuter)
            {
                RecordOuterConnectedPathUndo(trafficNode);
            }
        }

        public void RecordAllOuterPathUndo(TrafficNode trafficNode, bool includeNodeTransform = false)
        {
            if (includeNodeTransform)
            {
#if UNITY_EDITOR
                Undo.RecordObject(trafficNode.transform, "Change Node Position");
#endif
            }

            trafficNode.IterateExternalPaths(path =>
            {
                path.SaveMovementUndo(true);
            });

            RecordOuterConnectedPathUndo(trafficNode);
        }

        public void SetOffsetLanes(int localIndex)
        {
            var tempPath = GetTempPath(0);

            IterateOffsetLanes((currentNodeIndex, opposite) =>
            {
                var trafficNode = createdTrafficNodes[currentNodeIndex];

                trafficNode.IterateAllPaths((path, laneIndex) =>
                {
                    var currentLocalIndex = localIndex;

                    if (opposite && !oneWay)
                    {
                        currentLocalIndex = path.Nodes.Count - localIndex - 1;
                    }

                    var pathNode = path.Nodes[currentLocalIndex];
                    SetPathNodeOffset(currentNodeIndex, laneIndex, tempPath, pathNode, localIndex, opposite);
                    path.CreatePath(false);
                });
            });

            var pedIndex = cornerPedestrianNodesBinding.IndexOf(localIndex);

            if (pedIndex >= 0)
            {
                var node1 = cornerPedestrianNodes[pedIndex];

                if (node1 != null)
                {
                    SetPedestrianNodeOffset(node1, tempPath.SourceTrafficNode, tempPath.Nodes[localIndex], 1);
                }

                var oppositeNodeIndex = pedIndex + cornerPedestrianNodesBinding.Count;

                if (cornerPedestrianNodes.Count > oppositeNodeIndex)
                {
                    var node2 = cornerPedestrianNodes[oppositeNodeIndex];

                    if (node2 != null)
                    {
                        SetPedestrianNodeOffset(node2, tempPath.SourceTrafficNode, tempPath.Nodes[localIndex], -1);
                    }
                }
            }
        }

        private void CreateOffsetPaths()
        {
            ClearOffsetPaths();
            ClearCornerNodes();

            IterateOffsetLanes((currentNodeIndex, opposite) =>
            {
                CreateLanes(currentNodeIndex, opposite);
            });

            if (generateSpawnNodes)
            {
                GeneratePathSpawnNodes();
            }

            CreateOffsetPedestrianNodes();
        }

        private void IterateOffsetLanes(Action<int, bool> action)
        {
            if (!oneWay)
            {
                for (int currentNodeIndex = 0; currentNodeIndex < createdTrafficNodes.Count; currentNodeIndex++)
                {
                    action(currentNodeIndex, currentNodeIndex != 0);
                }
            }
            else
            {
                var currentNodeIndex = !shouldRevertDirection ? 0 : 1;
                action(currentNodeIndex, shouldRevertDirection);
            }
        }

        private void CreateOffsetPedestrianNodes()
        {
            if (!AddAlongLine)
                return;

            var tempCustomPath = GetTempPath(0);

            var prevNode = tempCustomPath.SourceTrafficNode.TrafficNodeCrosswalk.PedestrianNode2;
            var lastNode = tempCustomPath.ConnectedTrafficNode.TrafficNodeCrosswalk.PedestrianNode1;

            tempCustomPath.IterateWaypoints((node, index) =>
            {
                var pedNode = CreateOffsetNode(tempCustomPath.SourceTrafficNode, tempCustomPath.Nodes[index], index, 1);

                pedNode.AddConnection(prevNode);
                prevNode = pedNode;

            }, nodeSpacing);

            prevNode.AddConnection(lastNode);

            prevNode = tempCustomPath.SourceTrafficNode.TrafficNodeCrosswalk.PedestrianNode1;
            lastNode = tempCustomPath.ConnectedTrafficNode.TrafficNodeCrosswalk.PedestrianNode2;

            tempCustomPath.IterateWaypoints((node, index) =>
            {
                var pedNode = CreateOffsetNode(tempCustomPath.SourceTrafficNode, tempCustomPath.Nodes[index], index, -1);

                pedNode.AddConnection(prevNode);
                prevNode = pedNode;

            }, nodeSpacing);

            prevNode.AddConnection(lastNode);
        }

        private PedestrianNode CreateOffsetNode(TrafficNode trafficNode, Transform pathNode, int index, int side)
        {
            var cornerPedestrianNode = CreatePedestrianNode();

            SetPedestrianNodeOffset(cornerPedestrianNode, trafficNode, pathNode, side);

            cornerPedestrianNode.transform.SetParent(cornerNodes);

            cornerPedestrianNodes.TryToAdd(cornerPedestrianNode);

            if (side == 1)
            {
                cornerPedestrianNodesBinding.Add(index);
            }

            return cornerPedestrianNode;
        }

        private void SetPedestrianNodeOffset(PedestrianNode cornerPedestrianNode, TrafficNode trafficNode, Transform pathNode, int side)
        {
            Vector3 offset = trafficNode.TrafficNodeCrosswalk.GetOffset(trafficNode, -side);
            Vector3 nodePosition = pathNode.position + pathNode.right * (offset.x - side * lineNodeOffset);

            cornerPedestrianNode.transform.SetPositionAndRotation(nodePosition, Quaternion.identity);
        }

        private void CreateLanes(int currentNodeIndex, bool oppositeOrientation = false)
        {
            List<LaneArray> pathArray = new List<LaneArray>(laneCount);

            for (int i = 0; i < laneCount; i++)
            {
                pathArray.Add(new LaneArray());
            }

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                var path = GameObject.Instantiate(roadSegmentCreatorConfig.PathPrefab, createdTrafficNodes[currentNodeIndex].PathParent);

                path.name = $"Path_{laneIndex}";
                path.transform.localPosition = Vector3.zero;
                path.PathCurveType = PathCurveType.StraightLine;
                path.WayPointsCountPerCurve = wayPointStraightRoadCount;
                path.PathSpeedLimit = straightRoadPathSpeedLimit;

                List<Transform> pathNodes = new List<Transform>();

                var tempCustomPath = GetTempPath(0);

                for (int i = 0; i < tempCustomPath.WayPoints.Count; i++)
                {
                    int index = i + 1;

                    if (currentNodeIndex == 1)
                    {
                        index = tempCustomPath.WayPoints.Count - i;
                    }

                    var pathNode = new GameObject("Node" + (index).ToString()).transform;

                    if (currentNodeIndex == 0)
                    {
                        pathNodes.Add(pathNode);
                    }
                    else
                    {
                        pathNodes.Insert(0, pathNode);
                    }

                    SetPathNodeOffset(currentNodeIndex, laneIndex, tempCustomPath, pathNode, i, oppositeOrientation);
                }

                path.Nodes = pathNodes;

                Path[] paths = new Path[1];
                paths[0] = path;

                path.CreatePath(true);
                path.SourceTrafficNode = createdTrafficNodes[currentNodeIndex];

                int nextIndex = (currentNodeIndex + 1) % 2;

                path.ConnectedTrafficNode = createdTrafficNodes[nextIndex];
                path.ConnectedLaneIndex = laneIndex;

                pathArray[laneIndex].paths = paths.ToList();
            }

            createdTrafficNodes[currentNodeIndex].Lanes = pathArray;

            EditorSaver.SetObjectDirty(createdTrafficNodes[currentNodeIndex]);
        }

        private void SetPathNodeOffset(int currentNodeIndex, int laneIndex, Path tempCustomPath, Transform pathNode, int localIndex, bool oppositeOrientation)
        {
            int side = currentNodeIndex == 0 ? -1 : 1;

            var sourceNode = tempCustomPath.Nodes[localIndex];

            bool isEdgeNode = localIndex + 1 < tempCustomPath.WayPoints.Count && localIndex != 0;

            Vector3 direction;

            if (isEdgeNode)
            {
                direction = sourceNode.transform.forward * ProjectConstants.LaneHandDirection;
            }
            else
            {
                if (localIndex == 0)
                {
                    direction = createdTrafficNodes[0].GetLaneDirection(1);
                }
                else
                {
                    direction = createdTrafficNodes[1].GetLaneDirection(-1);
                }
            }

            var cross = Vector3.Cross(direction, Vector3.up);

            Vector3 offset = GetLaneOffset(laneIndex, side, cross);
            Vector3 nodePosition = tempCustomPath.WayPoints[localIndex].transform.position + offset;

            pathNode.transform.position = nodePosition;
            pathNode.transform.rotation = sourceNode.transform.rotation;

            if (oppositeOrientation)
            {
                pathNode.transform.rotation *= Quaternion.Euler(0, 180, 0);
            }
        }

        private Vector3 GetLaneOffset(int laneIndex, int side, Vector3 cross)
        {
            if (!oneWay)
            {
                return cross * (LaneWidth / 2 + LaneWidth * laneIndex + DividerWidth / 2) * side;
            }
            else
            {
                return cross * TrafficNodeExtension.GetOneWaypointOffset(laneCount, LaneWidth, laneIndex) * side;
            }
        }

        private void RecordOuterConnectedPathUndo(TrafficNode sourceNode, bool includeMovement = true, bool includeSettings = false)
        {
#if UNITY_EDITOR
            if (allConnectedOuterPaths == null || sourceNode == null || !allConnectedOuterPaths.ContainsKey(sourceNode))
            {
                return;
            }

            foreach (var path in allConnectedOuterPaths[sourceNode])
            {
                if (!path) continue;

                if (includeMovement)
                {
                    path.SaveMovementUndo();
                }

                if (includeSettings)
                {
                    path.SavePath();
                }
            }
#endif
        }

        private void RecordAllOuterConnectedPathUndo(bool includeMovement = true, bool includeSettings = false)
        {
#if UNITY_EDITOR
            foreach (var createdTrafficNode in createdTrafficNodes)
            {
                if (!createdTrafficNode) continue;

                RecordOuterConnectedPathUndo(createdTrafficNode, includeMovement, includeSettings);
            }
#endif
        }
    }
}
