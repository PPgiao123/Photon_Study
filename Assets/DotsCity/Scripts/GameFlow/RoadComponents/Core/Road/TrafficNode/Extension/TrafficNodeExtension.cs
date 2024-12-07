using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public static class TrafficNodeExtension
    {
        public static Vector3 GetLanePosition(this TrafficNode trafficNode, int laneIndex, bool leftLane = false) =>
            !leftLane ? GetRightSidePoint(trafficNode, laneIndex) : GetLeftSidePoint(trafficNode, laneIndex);

        public static Vector3 GetRightSidePoint(this TrafficNode trafficNode, int laneIndex)
        {
            Vector3 lanePoint = Vector3.zero;
            var transform = trafficNode.transform;

            if (!trafficNode.IsOneWay)
            {
                lanePoint = transform.position + GetLaneOffset(transform.right, trafficNode.LaneWidth, laneIndex, true, trafficNode.DividerWidth);
            }
            else
            {
                lanePoint = GetOneWaypoint(trafficNode, laneIndex);
            }

            return lanePoint;
        }

        public static Vector3 GetLeftSidePoint(this TrafficNode trafficNode, int laneIndex)
        {
            Vector3 lanePoint = default;
            var transform = trafficNode.transform;

            if (!trafficNode.IsOneWay)
            {
                lanePoint = transform.position + GetLaneOffset(transform.right, trafficNode.LaneWidth, laneIndex, false, trafficNode.DividerWidth);
            }
            else
            {
                lanePoint = GetOneWaypoint(trafficNode, laneIndex);
            }

            return lanePoint;
        }

        public static Vector3 GetLaneOffset(Vector3 right, float laneWidth, int laneIndex, bool rightLane, float dividerWidth = 0)
        {
            int side = rightLane ? -1 : 1;
            return side * ProjectConstants.LaneHandDirection * right * (laneWidth / 2 + laneWidth * laneIndex + dividerWidth / 2);
        }

        public static Vector3 GetOneWaypoint(this TrafficNode trafficNode, int laneIndex)
        {
            return GetOneWaypoint(trafficNode, laneIndex, trafficNode.IsEndOfOneWay);
        }

        public static Vector3 GetOneWaypoint(this TrafficNode trafficNode, int laneIndex, bool flipDirection)
        {
            var transform = trafficNode.transform;
            return GetOneWaypoint(transform.position, transform.right, trafficNode.LaneCount, trafficNode.LaneWidth, laneIndex, flipDirection);
        }

        public static Vector3 GetOneWaypoint(Vector3 position, Vector3 right, int laneCount, float laneWidth, int laneIndex, bool isEndOfOneWay)
        {
            int side = !isEndOfOneWay ? -1 : 1;
            var offset = GetOneWaypointOffset(laneCount, laneWidth, laneIndex);
            return position + side * right * offset;
        }

        public static float GetOneWaypointOffset(int laneCount, float laneWidth, int laneIndex)
        {
            float offset = default;

            if (laneCount % 2 == 0)
            {
                offset = (-((laneCount / 2 - 1) * laneWidth + laneWidth / 2) + laneWidth * laneIndex) * ProjectConstants.LaneHandDirection;
            }
            else
            {
                int sideOffset = Mathf.FloorToInt(((float)laneCount) / 2);

                offset = (laneWidth * (-sideOffset + laneIndex)) * ProjectConstants.LaneHandDirection;
            }

            return offset;
        }

        public static Quaternion GetNodeRotation(this TrafficNode trafficNode, int side)
        {
            return Quaternion.LookRotation(GetNodeForward(trafficNode, side));
        }

        public static Vector3 GetNodeForward(this TrafficNode trafficNode, bool rightSide)
        {
            int side;

            if (!trafficNode.IsOneWay)
            {
                side = rightSide ? 1 : -1;
            }
            else
            {
                side = !trafficNode.IsEndOfOneWay ? 1 : -1;
            }

            return GetNodeForward(trafficNode, side);
        }

        public static Vector3 GetNodeForward(this TrafficNode trafficNode, int side)
        {
            return -trafficNode.transform.forward * side;
        }

        public static Vector3 GetLaneDirection(this TrafficNode trafficNode, int side)
        {
            return -trafficNode.transform.forward * side * ProjectConstants.LaneHandDirection;
        }

        public static bool IsCorrectDirConnection(TrafficNode trafficNode, TrafficNode dstConnection, bool sourceRight, bool targetRight)
        {
            var forward1 = trafficNode.GetNodeForward(sourceRight);
            var forward2 = dstConnection.GetNodeForward(!targetRight);

            var connectionDir = (dstConnection.transform.position - trafficNode.transform.position).normalized;
            var dot = Vector3.Dot(forward1, connectionDir);
            var dot2 = Vector3.Dot(forward2, connectionDir);

            return dot > 0 && dot2 > 0;
        }

        public static Path GetPathByAbsoluteIndex(this TrafficNode trafficNode, int absoluteIndex)
        {
            int index = 0;

            var lanes = trafficNode.Lanes;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                for (int i = 0; i < lanes[laneIndex].paths?.Count; i++)
                {
                    if (index == absoluteIndex)
                    {
                        return lanes[laneIndex].paths[i];
                    }

                    index++;
                }
            }

            return null;
        }

        public static Path GetPathByLocalIndex(this TrafficNode trafficNode, int laneIndex, int localIndex, bool externalLanes = false)
        {
            var lanes = !externalLanes ? trafficNode.Lanes : trafficNode.ExternalLanes;

            if (lanes.Count > laneIndex)
            {
                if (lanes[laneIndex].paths.Count > localIndex)
                {
                    return lanes[laneIndex].paths[localIndex];
                }
            }

            return null;
        }

        public static void IterateAllLanes(this TrafficNode trafficNode, Action<int, bool> callback, bool includeExternal = false, bool checkForExist = true)
        {
            if (trafficNode.HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(); laneIndex++)
                {
                    if (trafficNode.LaneExist(laneIndex, false) || !checkForExist)
                        callback?.Invoke(laneIndex, false);
                }
            }

            if (includeExternal)
            {
                IterateExternalLanes(trafficNode, callback, checkForExist);
            }
        }

        public static void IterateExternalLanes(this TrafficNode trafficNode, Action<int, bool> callback, bool checkForExist = true)
        {
            if (!trafficNode.HasLeftLanes)
                return;

            for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(true); laneIndex++)
            {
                if (trafficNode.LaneExist(laneIndex, true) || !checkForExist)
                    callback?.Invoke(laneIndex, true);
            }
        }

        public static void IterateAllLanes(this TrafficNode trafficNode, Action<LaneArray, int, bool> callback, bool includeExternal = false, bool includeNull = false)
        {
            if (trafficNode.HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(); laneIndex++)
                {
                    var data = trafficNode.TryToGetLaneData(laneIndex);

                    if (data != null || includeNull)
                        callback?.Invoke(data, laneIndex, false);
                }
            }

            if (includeExternal)
            {
                IterateExternalLanes(trafficNode, callback, includeNull: includeNull);
            }
        }

        public static void IterateExternalLanes(this TrafficNode trafficNode, Action<LaneArray, int, bool> callback, bool includeNull = false)
        {
            if (!trafficNode.HasLeftLanes)
                return;

            for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(true); laneIndex++)
            {
                var data = trafficNode.TryToGetLaneData(laneIndex, true);

                if (data != null || includeNull)
                    callback?.Invoke(data, laneIndex, true);
            }
        }

        public static void IterateAllPaths(this TrafficNode trafficNode, Action<Path> callback, bool includeExternal = false)
        {
            var lanes = trafficNode.Lanes;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                var paths = lanes[laneIndex].paths;

                for (int i = 0; i < paths?.Count; i++)
                {
                    var path = lanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path);
                }
            }

            if (includeExternal)
            {
                IterateExternalPaths(trafficNode, callback);
            }
        }

        public static void IterateAllPaths(this TrafficNode trafficNode, Action<Path, int> callback, bool includeExternal = false)
        {
            var lanes = trafficNode.Lanes;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                for (int i = 0; i < lanes[laneIndex].paths?.Count; i++)
                {
                    var path = lanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path, laneIndex);
                }
            }

            if (includeExternal)
            {
                IterateExternalPaths(trafficNode, callback);
            }
        }

        public static void IterateExternalPaths(this TrafficNode trafficNode, Action<Path> callback)
        {
            var externalLanes = trafficNode.ExternalLanes;

            for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
            {
                for (int i = 0; i < externalLanes[laneIndex].paths?.Count; i++)
                {
                    var path = externalLanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path);
                }
            }
        }

        public static void IterateExternalPaths(this TrafficNode trafficNode, Action<Path, int> callback)
        {
            var externalLanes = trafficNode.ExternalLanes;

            for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
            {
                for (int i = 0; i < externalLanes[laneIndex].paths?.Count; i++)
                {
                    var path = externalLanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path, laneIndex);
                }
            }
        }

        public static void IterateAllOuterPaths(this TrafficNode trafficNode, Action<Path> callback)
        {
#if UNITY_EDITOR
            if (trafficNode.AllConnectedOuterPaths == null)
            {
                trafficNode.OnInspectorEnabled();
            }

            for (int i = 0; i < trafficNode.AllConnectedOuterPaths?.Count; i++)
            {
                Path path = trafficNode.AllConnectedOuterPaths[i];

                if (!path) continue;

                callback(path);
            }
#endif
        }

        public static List<Path> GetAllConnectedOuterPaths(this TrafficNode trafficNode, bool allowCrossroadPath = false)
        {
            if (!trafficNode.IsOneWay)
            {
                var paths = new List<Path>();

                trafficNode.IterateExternalPaths((path) =>
                {
                    if (path.ConnectedTrafficNode != null)
                    {
                        var connectedTrafficNode = path.ConnectedTrafficNode;

                        connectedTrafficNode.IterateExternalPaths((path2) =>
                        {
                            if (path2.ConnectedTrafficNode == trafficNode)
                            {
                                paths.TryToAdd(path2);
                            }
                        });
                    }
                });

                if (allowCrossroadPath && trafficNode.TrafficLightCrossroad)
                {
                    foreach (var currentTrafficNode in trafficNode.TrafficLightCrossroad.TrafficNodes)
                    {
                        if (currentTrafficNode == trafficNode || currentTrafficNode == null)
                            continue;

                        currentTrafficNode.IterateAllPaths((path) =>
                        {
                            if (path.ConnectedTrafficNode == trafficNode)
                            {
                                paths.TryToAdd(path);
                            }
                        });
                    }
                }

                return paths;
            }
            else
            {
                List<Path> paths = null;

                if (!allowCrossroadPath && trafficNode.TrafficLightCrossroad)
                {
                    paths = ObjectUtils.FindObjectsOfType<Path>().Where(a => a.ConnectedTrafficNode == trafficNode && a.SourceTrafficNode != null && !trafficNode.TrafficLightCrossroad.TrafficNodes.Contains(a.SourceTrafficNode)).ToList();
                }
                else
                {
                    paths = ObjectUtils.FindObjectsOfType<Path>().Where(a => a.ConnectedTrafficNode == trafficNode && a.SourceTrafficNode != null).ToList();
                }

                return paths;
            }
        }

        public static void CloneSettings(this TrafficNode trafficNode, TrafficNode sourceNode)
        {
            trafficNode.IsEndOfOneWay = sourceNode.IsEndOfOneWay;
            trafficNode.IsOneWay = sourceNode.IsOneWay;
            trafficNode.LaneCount = sourceNode.LaneCount;
            trafficNode.LaneWidth = sourceNode.LaneWidth;
            trafficNode.HasCrosswalk = sourceNode.HasCrosswalk;
            trafficNode.LightType = sourceNode.LightType;
            trafficNode.TrafficNodeType = sourceNode.TrafficNodeType;
            trafficNode.Weight = sourceNode.Weight;
            trafficNode.ChanceToSpawn = sourceNode.ChanceToSpawn;
            trafficNode.CustomAchieveDistance = sourceNode.CustomAchieveDistance;
            trafficNode.DividerWidth = sourceNode.DividerWidth;

            EditorSaver.SetObjectDirty(trafficNode);
        }
    }
}