using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        public void InitializePathHeaders()
        {
#if UNITY_EDITOR
            if (selectedTrafficNodeIndex != -1)
            {
                selectedPathIndex = -1;
                selectedPath = null;

                if (selectedTrafficNodeIndex >= createdTrafficNodes.Count)
                {
                    selectedTrafficNodeIndex = -1;
                    pathHeaders = null;
                    pathIndexBinding = null;
                    return;
                }

                var targetTrafficNode = createdTrafficNodes[selectedTrafficNodeIndex];
                var tempHeaders = new List<string>();

                pathIndexBinding = new Dictionary<int, Path>();

                tempHeaders.Add("All");

                int index = 0;

                if (PathDirection.HasFlag(TrafficNodeDirectionType.Right))
                {
                    targetTrafficNode.IterateAllPaths((path =>
                    {
                        pathIndexBinding.Add(index, path);
                        tempHeaders.Add(path.name);
                        index++;
                    }));
                }

                if (PathDirection.HasFlag(TrafficNodeDirectionType.Left))
                {
                    targetTrafficNode.IterateExternalPaths((path =>
                    {
                        pathIndexBinding.Add(index, path);
                        tempHeaders.Add(path.name);
                        index++;
                    }));
                }

                pathHeaders = tempHeaders.ToArray();
            }
            else
            {
                pathHeaders = null;
                pathIndexBinding = null;
            }
#endif
        }

        public Path CreatePath(PathDirectionType pathDirectionType, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode)
        {
            return CreatePath(pathDirectionType, sourceTrafficNode, targetTrafficNode, turnCurveType);
        }

        public Path CreatePath(PathDirectionType pathDirectionType, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, TurnCurveType turnCurveType)
        {
            Path path = Instantiate(roadSegmentCreatorConfig.PathPrefab);

            path.PathRoadType = PathRoadType.TurnRoad;

            int nodeCount = 0;
            switch (pathDirectionType)
            {
                case PathDirectionType.Left:
                    {
                        path.transform.name = "PathLeftTurn";
                        path.WayPointsCountPerCurve = wayPointTurnCurveCount;
                        path.Priority = turnRoadPriority;

                        if (turnCurveType == TurnCurveType.BezierCube)
                        {
                            path.PathCurveType = PathCurveType.BezierCube;
                            nodeCount = 3;
                        }
                        else if (turnCurveType == TurnCurveType.BezierQuad)
                        {
                            path.PathCurveType = PathCurveType.BezierQuad;
                            nodeCount = 4;
                        }
                        break;
                    }
                case PathDirectionType.Forward:
                    {
                        path.transform.name = "PathStraight";
                        path.PathCurveType = PathCurveType.StraightLine;
                        path.PathRoadType = PathRoadType.StraightRoad;
                        path.WayPointsCountPerCurve = wayPointStraightRoadCount;
                        path.Priority = straightRoadPriority;
                        nodeCount = 2;
                        break;
                    }
                case PathDirectionType.Right:
                    {
                        path.transform.name = "PathRightTurn";
                        path.WayPointsCountPerCurve = wayPointTurnCurveCount;
                        path.Priority = turnRoadPriority;

                        if (turnCurveType == TurnCurveType.BezierCube)
                        {
                            path.PathCurveType = PathCurveType.BezierCube;
                            nodeCount = 3;
                        }
                        else if (turnCurveType == TurnCurveType.BezierQuad)
                        {
                            path.PathCurveType = PathCurveType.BezierQuad;
                            nodeCount = 4;
                        }
                        break;
                    }
            }

            path.transform.parent = sourceTrafficNode.PathParent;
            path.transform.localPosition = Vector3.zero;
            path.transform.localRotation = Quaternion.identity;

            path.SourceTrafficNode = sourceTrafficNode;
            path.ConnectedTrafficNode = targetTrafficNode;

            List<Transform> nodes = new List<Transform>();

            for (int i = 0; i < nodeCount; i++)
            {
                nodes.Add(new GameObject("Node" + (i + 1).ToString()).transform);
            }

            path.Nodes = nodes;

            return path;
        }

        private List<Path> TryToCreateLeftTurnPath(int currentNodeIndex, int laneIndex, int customNextIndex = -1)
        {
            int nextNodeIndex = customNextIndex == -1 ? (currentNodeIndex + 1) % CreateTrafficNodeCount : customNextIndex;

            var currentNode = createdTrafficNodes[currentNodeIndex];
            var nextNode = createdTrafficNodes[nextNodeIndex];

            if (IsStraightRoad())
                return null;

            if (!CanConnectTurnLaneIndex(currentNodeIndex, laneIndex, currentNode.LaneCount, true) && CreateTrafficNodeCount != 2)
                return null;

            if (!ConnectionIsAvailable(nextNode))
                return null;

            float signedAngle = GetSignedAngleRelativeNextTrafficNode(currentNodeIndex, nextNodeIndex);

            var hasLeftTurn = (signedAngle < -20 && signedAngle > -120);

            if (hasLeftTurn && nextNode.IsOneWay)
            {
                hasLeftTurn = nextNode.IsEndOfOneWay;
            }

            if (roadSegmentType == RoadSegmentType.TurnRoad)
            {
                hasLeftTurn = true;
            }

            if (!hasLeftTurn)
                return null;

            var leftPaths = new List<Path>();

            var turnData = GetTurnData(currentNodeIndex);

            var laneLeftTurnConnectionCount = turnData.LaneLeftTurnConnectionCount;
            int maxConnection = Mathf.Clamp(laneLeftTurnConnectionCount, 0, nextNode.LaneCount);

            int laneDiff = ProjectConstants.LaneHandDirection == 1 ? 0 : nextNode.LaneCount - currentNode.LaneCount;

            for (int connectionIndex = 0; connectionIndex < maxConnection; connectionIndex++)
            {
                int nextLaneIndex = 0;

                if (!nextNode.IsOneWay)
                {
                    nextLaneIndex = laneIndex + connectionIndex + laneDiff;
                }
                else
                {
                    nextLaneIndex = laneIndex + connectionIndex * ProjectConstants.LaneHandDirection + laneDiff;
                }

                if (nextLaneIndex >= nextNode.LaneCount || nextLaneIndex < 0)
                    continue;

                if (!AvailableConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex))
                    continue;

                var leftPath = CreatePath(PathDirectionType.Left, currentNode, nextNode);
                leftPath.name = $"PathLeftTurn_{laneIndex}";
                leftPath.PathSpeedLimit = turnRoadPathSpeedLimit;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, leftPath, true);
                leftPaths.Add(leftPath);
            }

            return leftPaths;
        }

        private List<Path> TryToCreateStraightPath(int currentNodeIndex, int laneIndex, int customNextIndex = -1)
        {
            if (CreateTrafficNodeCount < 3 && !IsStraightRoad() && !IsCustom())
                return null;

            int nextNodeIndex = (currentNodeIndex + 2) % CreateTrafficNodeCount;

            if (CreateTrafficNodeCount == 3 && currentNodeIndex == CreateTrafficNodeCount - 1)
            {
                nextNodeIndex = 0;
            }

            bool hasStraight = true;
            bool bezier = false;

            if (!IsStraightRoad() && CreateTrafficNodeCount != 2)
            {
                nextNodeIndex = customNextIndex != -1 ? customNextIndex : nextNodeIndex;

                var signedAngle = GetSignedAngleRelativeNextTrafficNode(currentNodeIndex, nextNodeIndex);

                hasStraight = (Mathf.Abs(signedAngle) < 10);

                if (!hasStraight)
                {
                    hasStraight = (Mathf.Abs(signedAngle) >= 10 && Mathf.Abs(signedAngle) < 30);
                    bezier = true;
                }
            }
            else
            {
                nextNodeIndex = (currentNodeIndex + 1) % 2;
            }

            var currentNode = createdTrafficNodes[currentNodeIndex];
            var nextNode = createdTrafficNodes[nextNodeIndex];

            if (!ConnectionIsAvailable(nextNode))
                return null;

            if (!hasStraight)
                return null;

            var straightPaths = new List<Path>();

            int maxConnectionCount = roadSegmentType != RoadSegmentType.MergeStraightRoad ? 1 : nextNode.LaneCount;

            for (int connectionIndex = 0; connectionIndex < maxConnectionCount; connectionIndex++)
            {
                int nextLaneIndex = laneIndex + connectionIndex;

                if (!AvailableConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex))
                    continue;

                nextLaneIndex = Mathf.Clamp(nextLaneIndex, 0, nextNode.LaneCount - 1);

                var type = !bezier ? PathDirectionType.Forward : PathDirectionType.Right;

                var straightPath = CreatePath(type, currentNode, nextNode);

                straightPath.name = $"PathStraight_{laneIndex}";
                straightPath.PathSpeedLimit = straightRoadPathSpeedLimit;
                straightPath.PathRoadType = PathRoadType.StraightRoad;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, straightPath, bezier);

                straightPaths.Add(straightPath);
            }

            return straightPaths;
        }

        private List<Path> TryToCreateRightPath(int currentNodeIndex, int laneCount, int laneIndex, int customNextIndex = -1)
        {
            if (IsStraightRoad())
                return null;

            int nextNodeIndex = (currentNodeIndex + CreateTrafficNodeCount - 1) % CreateTrafficNodeCount;

            nextNodeIndex = customNextIndex != -1 ? customNextIndex : nextNodeIndex;

            var currentNode = createdTrafficNodes[currentNodeIndex];
            var nextNode = createdTrafficNodes[nextNodeIndex];

            if (!ConnectionIsAvailable(nextNode))
                return null;

            var nextLaneIndex = laneIndex;
            var currentLaneCount = currentNode.LaneCount;

            if (!CanConnectTurnLaneIndex(currentNodeIndex, nextLaneIndex, currentLaneCount, false) && CreateTrafficNodeCount != 2)
                return null;

            var signedAngle = GetSignedAngleRelativeNextTrafficNode(currentNodeIndex, nextNodeIndex);

            var hasRightTurn = (signedAngle > 20 && signedAngle < 120);

            if (hasRightTurn && nextNode.IsOneWay)
            {
                hasRightTurn = nextNode.IsEndOfOneWay;
            }

            if (roadSegmentType == RoadSegmentType.TurnRoad)
            {
                hasRightTurn = false;
            }

            if (!hasRightTurn)
                return null;

            var turnData = GetTurnData(currentNodeIndex);

            var rightPaths = new List<Path>();

            var laneRightTurnConnectionCount = turnData.LaneRightTurnConnectionCount;
            int maxConnection = Mathf.Clamp(laneRightTurnConnectionCount, 0, nextNode.LaneCount);

            for (int connectionIndex = 0; connectionIndex < maxConnection; connectionIndex++)
            {
#pragma warning disable CS0162
                if (ProjectConstants.LaneHandDirection == 1)
                {
                    if (IsSubLane(nextNodeIndex))
                    {
                        nextLaneIndex = Mathf.Clamp(nextLaneIndex, 0, nextNode.LaneCount - 1);
                    }
                    else if (IsSubLane(currentNodeIndex))
                    {
                        nextLaneIndex = nextNode.LaneCount - connectionIndex - 1;
                    }
                    else
                    {
                        nextLaneIndex -= connectionIndex;
                    }
                }
                else
                {
                    nextLaneIndex += connectionIndex;
                }

#pragma warning restore CS0162

                if (nextLaneIndex < 0 || nextLaneIndex >= nextNode.LaneCount)
                    continue;

                if (!AvailableConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex))
                    continue;

                var rightPath = CreatePath(PathDirectionType.Right, currentNode, nextNode);
                rightPath.name = $"PathRightTurn_{laneIndex}";
                rightPath.PathSpeedLimit = turnRoadPathSpeedLimit;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, rightPath, true);
                rightPaths.Add(rightPath);
            }

            return rightPaths;
        }

        private bool CanConnectTurnLaneIndex(int currentNodeIndex, int laneIndex, int currentLaneCount, bool isLeft)
        {
            var turnData = GetTurnData(currentNodeIndex);
            var turnCount = isLeft ? turnData.LeftTurnCount : turnData.RightTurnCount;

            isLeft = ProjectConstants.LaneHandDirection == 1 ? isLeft : !isLeft;

            if (isLeft)
            {
                return laneIndex < turnCount;
            }
            else
            {
                return laneIndex >= currentLaneCount - turnCount;
            }
        }

        private bool ConnectionIsAvailable(TrafficNode trafficNode) => trafficNode.AvailableForInnerConnection();

        private Vector3 GetStartOneWayPoint(int currentTrafficNodeIndex, int laneIndex)
        {
            return GetOneWayLanePosition(createdTrafficNodes[currentTrafficNodeIndex], laneIndex);
        }

        private Vector3 GetTargetOneWayPoint(int currentTrafficNodeIndex, int connectedLaneIndex, int nextTrafficNodeIndex)
        {
            return GetOneWayLanePosition(createdTrafficNodes[nextTrafficNodeIndex], connectedLaneIndex, -1);
        }

        private Vector3 GetLanePosition(TrafficNode trafficNode, int laneIndex, int side = 1)
        {
            return trafficNode.GetLanePosition(laneIndex, side == -1);
        }

        private Vector3 GetOneWayLanePosition(TrafficNode trafficNode, int laneIndex, int side = 1)
        {
            return trafficNode.GetOneWaypoint(laneIndex, side == -1);
        }

        private float GetSignedAngleRelativeNextTrafficNode(int i, int nextIndex)
        {
            Vector3 dir1 = (transform.position.Flat() - createdTrafficNodes[i].transform.FlatPosition()).normalized;
            Vector3 dir2 = (createdTrafficNodes[nextIndex].transform.FlatPosition() - transform.position.Flat()).normalized;

            var angle = Vector3.SignedAngle(dir1, dir2, Vector3.up);
            return angle;
        }

        public void ClearOffsetPaths()
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                var createdNode = createdTrafficNodes[i];

                if (createdNode.Lanes?.Count > 0)
                {
                    var lanes = createdNode.Lanes;

                    for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
                    {
                        var paths = lanes[laneIndex].paths;

                        for (int n = 0; n < paths?.Count; n++)
                        {
                            var path = paths[n];

                            if (path)
                                DestroyImmediate(path.gameObject);
                        }
                    }
                }

                createdTrafficNodes[i].Lanes = null;
            }
        }

        private void CreatePathNodes(int currentIndex, int nextIndex)
        {
            LaneArray[] pathArray = new LaneArray[laneCount];

            var tempCustomPath = GetTempPath(0);

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                var path = Instantiate(roadSegmentCreatorConfig.PathPrefab, createdTrafficNodes[currentIndex].transform.parent);

                path.PathCurveType = tempCustomPath.PathCurveType;

                path.WayPointsCountPerCurve = path.PathCurveType == PathCurveType.StraightLine ? wayPointStraightRoadCount : wayPointTurnCurveCount;
                path.PathSpeedLimit = path.PathCurveType == PathCurveType.StraightLine ? straightRoadPathSpeedLimit : turnRoadPathSpeedLimit;

                List<Transform> nodes = new List<Transform>();

                for (int i = 0; i < tempCustomPath.Nodes.Count; i++)
                {
                    var node = new GameObject("Node" + (i + 1).ToString()).transform;

                    if (currentIndex == 0)
                    {
                        nodes.Add(node);
                    }
                    else
                    {
                        nodes.Insert(0, node);
                    }
                }

                path.Nodes = nodes;

                Path[] paths = new Path[1];
                paths[0] = path;

                pathArray[laneIndex].paths = paths.ToList();
            }

            createdTrafficNodes[currentIndex].Lanes = pathArray.ToList();
        }

        private bool AvailableConnection(int sourceNodeIndex, int targetNodeIndex, int source, int target)
        {
            var nodeIndexes = new Vector2Int(sourceNodeIndex, targetNodeIndex);

#if UNITY_EDITOR
            if (ignoreConnections.Contains(nodeIndexes))
            {
                return false;
            }
#endif

            var connectionIndexes = new Vector2Int(source, target);

            if (!addedConnections.ContainsKey(nodeIndexes))
            {
                addedConnections.Add(nodeIndexes, new HashSet<Vector2Int>());
            }

            if (!addedConnections[nodeIndexes].Contains(connectionIndexes))
            {
                addedConnections[nodeIndexes].Add(connectionIndexes);
                return true;
            }

            return false;
        }

        private void SetPathConnection(int currentNodeIndex, int nextNodeIndex, int laneIndex, int nextLaneIndex, Path path, bool bezier = true)
        {
            Vector3 point1, point2;
            GetLaneConnectionPoints(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, out point1, out point2);

            path.Nodes[0].transform.position = point1;
            path.Nodes[path.Nodes.Count - 1].transform.position = point2;
            path.ConnectedLaneIndex = nextLaneIndex;

            if (bezier)
            {
                int relativeTrafficNodeIndex = CreateTrafficNodeCount == 2 ? 0 : currentNodeIndex;

                if (turnCurveType == TurnCurveType.BezierCube)
                {
                    var cornerPoint = GetSplineCornerPoint(point1, point2, currentNodeIndex, nextNodeIndex, true);

                    path.Nodes[1].transform.position = cornerPoint + createdTrafficNodes[relativeTrafficNodeIndex].transform.rotation * PathCorner1Offset;
                }
                else
                {
                    var cornerPoints = GetSplineTwoCornerPoint(point1, point2, currentNodeIndex, nextNodeIndex, true);

                    path.Nodes[1].transform.position = cornerPoints.Item1 + createdTrafficNodes[relativeTrafficNodeIndex].transform.rotation * PathCorner1Offset;
                    path.Nodes[2].transform.position = cornerPoints.Item2 + createdTrafficNodes[relativeTrafficNodeIndex].transform.rotation * PathCorner2Offset;
                }
            }

            path.ConnectedTrafficNode = createdTrafficNodes[nextNodeIndex];
            path.ConnectedLaneIndex = nextLaneIndex;
            path.CreatePath(true);

            EditorSaver.SetObjectDirty(path);
        }

        private void GetLaneConnectionPoints(int currentNodeIndex, int nextNodeIndex, int sourceLaneIndex, int nextLaneIndex, out Vector3 point1, out Vector3 point2)
        {
            point1 = Vector3.zero;

            if (!createdTrafficNodes[currentNodeIndex].IsOneWay)
            {
                point1 = GetLanePosition(createdTrafficNodes[currentNodeIndex], sourceLaneIndex);
            }
            else
            {
                point1 = GetStartOneWayPoint(currentNodeIndex, sourceLaneIndex);
            }

            point2 = Vector3.zero;

            if (!createdTrafficNodes[nextNodeIndex].IsOneWay)
            {
                point2 = GetLanePosition(createdTrafficNodes[nextNodeIndex], nextLaneIndex, -1);
            }
            else
            {
                point2 = GetTargetOneWayPoint(nextNodeIndex, nextLaneIndex, nextNodeIndex);
            }
        }

        private Vector3 GetSplineCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, int sourceNodeIndex, int targetNodeIndex, bool allowClosestPoint = false)
        {
#if UNITY_EDITOR
            return PathAttachHelper.GetSplineCornerPoint(sourcePoint, targetPoint, createdTrafficNodes[sourceNodeIndex], createdTrafficNodes[targetNodeIndex], allowClosestPoint);
#else
            return default;
#endif
        }

        private (Vector3, Vector3) GetSplineTwoCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, int sourceNodeIndex, int targetNodeIndex, bool allowClosestPoint = false)
        {
#if UNITY_EDITOR
            return PathAttachHelper.GetSplineTwoCornerPoint(sourcePoint, targetPoint, createdTrafficNodes[sourceNodeIndex], createdTrafficNodes[targetNodeIndex], allowClosestPoint);
#else
            return default;
#endif
        }

        private Vector3 GetCornerPointRelativeCenter(Vector3 spawnPoint1, Vector3 spawnPoint2)
        {
            Vector3 point1 = new Vector3(spawnPoint1.x, spawnPoint1.y, spawnPoint2.z);
            Vector3 point2 = new Vector3(spawnPoint2.x, spawnPoint2.y, spawnPoint1.z);

            float distance1 = Vector3.Distance(transform.position, point1);
            float distance2 = Vector3.Distance(transform.position, point2);

            Vector3 spawnPosition = distance1 < distance2 ? point1 : point2;
            return spawnPosition;
        }

        private void SetSpawnNodes(Path path, float minSpawnNodeOffset)
        {
            PathNode lastNode = null;

            path.IterateWaypoints(node =>
            {
                lastNode = node;
                node.SpawnNode = true;
            },
            minSpawnNodeOffset);

            if (lastNode)
            {
                if (Vector3.Distance(lastNode.transform.position, path.EndPosition) < minSpawnNodeOffset / 2)
                {
                    lastNode.SpawnNode = false;
                }
            }
        }

        private Path GetPathByIndex()
        {
#if UNITY_EDITOR
            CheckBinding();

            if (selectedPathIndex == -1)
            {
                return null;
            }

            if (!pathIndexBinding.ContainsKey(selectedPathIndex))
            {
                InitializePathHeaders();
            }

            return pathIndexBinding[selectedPathIndex];
#else
            return null;
#endif
        }

        private void CheckBinding()
        {
#if UNITY_EDITOR
            var headerCount = pathHeaders.Length - 1;

            if (pathIndexBinding == null || pathIndexBinding.Count != headerCount)
            {
                InitializePathHeaders();
            }
#endif
        }

        private string GetPathName(string name, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode)
        {
            return $"{name}_{GetNodeIndex(sourceTrafficNode) + 1}_{GetNodeIndex(targetTrafficNode) + 1}";
        }
    }
}
