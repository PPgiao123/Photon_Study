using Spirit604.DotsCity.Simulation.Binding;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [TemporaryBakingType]
    public struct TrafficNodeScopeBakingData : IComponentData
    {
        public int TrafficNodeInstanceId;
        public float3 Position;
        public int CrossWalkIndex;
        public bool IsOneWay;
        public bool IsEndOfOneWay;

        public NativeArray<TrafficNodeTempData> RightLaneEntities;
        public NativeArray<TrafficNodeTempData> LeftLaneEntities;

        public NativeArray<PathTempSettingsData> PathTempSettingsDatas;
        public NativeArray<RouteNodeData> WayPoints;
        public NativeArray<TempIntersectPointData> IntersectPoints;
        public NativeArray<TempSubTrafficNodeData> SubNodes;

        public NativeArray<TrafficNodeTempData> GetMainLaneEntities() => !IsOneWay || !IsEndOfOneWay ? RightLaneEntities : LeftLaneEntities;

        public int LanePathsCount(int laneIndex, bool rightPath = true)
        {
            if (rightPath)
            {
                if (laneIndex < RightLaneEntities.Length)
                {
                    return RightLaneEntities[laneIndex].MaxPathSettingsIndex - RightLaneEntities[laneIndex].MinPathSettingsIndex;
                }
            }
            else
            {
                if (laneIndex < LeftLaneEntities.Length)
                {
                    return LeftLaneEntities[laneIndex].MaxPathSettingsIndex - LeftLaneEntities[laneIndex].MinPathSettingsIndex;
                }
            }

            return 0;
        }
    }

    [TemporaryBakingType]
    public struct TrafficNodeCrossroadRef : IComponentData
    {
        public Entity RelatedCrossroadEntity;
        public Entity RelatedSubCrossroadEntity;

        public Entity CurrentRelatedCrossroad => SubCrossroad ? RelatedSubCrossroadEntity : RelatedCrossroadEntity;
        public bool SubCrossroad => RelatedSubCrossroadEntity != Entity.Null;
    }

    public struct TrafficNodeTempData
    {
        public Entity Entity;
        public int MinPathSettingsIndex;
        public int MaxPathSettingsIndex;
    }

    public struct TempSubTrafficNodeData
    {
        public Entity Entity;
        public int LocalWaypointIndex;
    }

    public struct PathTempSettingsData
    {
        public int InstanceId;
        public int GlobalPathIndex;
        public int MinPathWaypointIndex;
        public int MaxPathWaypointIndex;
        public int MinIntersectPointIndex;
        public int MaxIntersectPointIndex;
        public int MinSubNodeIndex;
        public int MaxSubNodeIndex;
        public int LaneIndex;
        public int ConnectedLaneIndex;
        public int ConnectedPathInstanceId;
        public int ConnectedNodeInstanceId;
        public float PathLength;
        public bool ReversedConnection;
        public int Priority;

        public PathOptions Options;
        public PathCurveType PathCurveType;
        public PathRoadType PathRoadType;
        public PathConnectionType PathConnectionType;
        public TrafficGroupType TrafficGroup;
    }

    public struct TempIntersectPointData
    {
        public Vector3 Point;
        public byte LocalNodeIndex;
        public int IntersectedPathInstanceId;
    }

    public class TrafficNodeBaker : Baker<TrafficNode>
    {
        private NativeList<PathTempSettingsData> pathTempSettingsDatas;
        private NativeList<RouteNodeData> trafficNodeScopeWayPoints;
        private NativeList<TempIntersectPointData> tempIntersectPoints;
        private NativeList<TempSubTrafficNodeData> tempSubNodes;
        private DynamicBuffer<PathConnectionElement> currentPathConnectionBuffer;

        private int minPathSettingsIndex;
        private int maxPathSettingsIndex;

        private int minWaypointPathIndex;
        private int maxWaypointPathIndex;

        private int minIntersectIndex;
        private int maxIntersectIndex;

        private int minSubNodeIndex;
        private int maxSubNodeIndex;

        public override void Bake(TrafficNode trafficNode)
        {
            var entity = GetEntity(trafficNode.gameObject, TransformUsageFlags.Dynamic);
            TrafficNodeScopeBakingData trafficNodeScopeData = new TrafficNodeScopeBakingData();
            TrafficNodeCrossroadRef trafficNodeCrossroadRef = new TrafficNodeCrossroadRef();

            trafficNodeScopeData.CrossWalkIndex = -1;
            trafficNodeScopeData.TrafficNodeInstanceId = trafficNode.GetInstanceID();
            trafficNodeScopeData.Position = trafficNode.transform.position;

            if (trafficNode.TrafficLightCrossroad)
            {
                trafficNodeCrossroadRef.RelatedCrossroadEntity = GetEntity(trafficNode.TrafficLightCrossroad.gameObject, TransformUsageFlags.Dynamic);
            }
            else
            {
                Debug.Log($"WARNING! Traffic node '{trafficNode.name}', InstanceID {trafficNode.GetInstanceID()} TrafficLightCrossroad is null{TrafficObjectFinderMessage.GetMessage()}");
            }

            pathTempSettingsDatas = new NativeList<PathTempSettingsData>(Allocator.Temp);
            trafficNodeScopeWayPoints = new NativeList<RouteNodeData>(Allocator.Temp);
            tempIntersectPoints = new NativeList<TempIntersectPointData>(Allocator.Temp);
            tempSubNodes = new NativeList<TempSubTrafficNodeData>(Allocator.Temp);

            trafficNode.CheckForNullPaths();
            int laneCount = trafficNode.GetLaneCount(false);
            int externalLaneCount = trafficNode.GetLaneCount(true);

            trafficNodeScopeData.IsOneWay = trafficNode.IsOneWay;
            trafficNodeScopeData.IsEndOfOneWay = trafficNode.IsEndOfOneWay;
            trafficNodeScopeData.RightLaneEntities = new NativeArray<TrafficNodeTempData>(laneCount, Allocator.Temp);
            trafficNodeScopeData.LeftLaneEntities = new NativeArray<TrafficNodeTempData>(externalLaneCount, Allocator.Temp);

            if (trafficNode.HasRightLanes)
            {
                CreateLaneNodes(trafficNode, ref trafficNodeScopeData, laneCount, true);
            }

            if (trafficNode.HasLeftLanes)
            {
                CreateLaneNodes(trafficNode, ref trafficNodeScopeData, externalLaneCount, false);
            }

            trafficNodeScopeData.PathTempSettingsDatas = pathTempSettingsDatas.ToArray(Allocator.Temp);
            trafficNodeScopeData.WayPoints = trafficNodeScopeWayPoints.ToArray(Allocator.Temp);
            trafficNodeScopeData.IntersectPoints = tempIntersectPoints.ToArray(Allocator.Temp);
            trafficNodeScopeData.SubNodes = tempSubNodes.ToArray(Allocator.Temp);

            Dispose();

            Validate(trafficNode);

            AddComponent(entity, trafficNodeScopeData);
            AddComponent(entity, trafficNodeCrossroadRef);
        }

        private void CreateLaneNodes(TrafficNode trafficNode, ref TrafficNodeScopeBakingData trafficNodeScopeData, int laneCount, bool isRightDirection)
        {
            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                var laneNodeEntity = CreateTrafficNodeEntity();

                int laneDirection = isRightDirection ? 1 : -1;
                InitializeTrafficNodeEntity(trafficNode, laneNodeEntity, laneIndex, laneDirection);

                var trafficNodeTempData = new TrafficNodeTempData()
                {
                    Entity = laneNodeEntity,
                };

                if (isRightDirection)
                {
                    InitializeRightNodePaths(trafficNode, laneIndex);
                }
                else
                {
                    InitializeLeftNodePaths(trafficNode, laneIndex);
                }

                trafficNodeTempData.MinPathSettingsIndex = minPathSettingsIndex;
                trafficNodeTempData.MaxPathSettingsIndex = maxPathSettingsIndex;

                if (isRightDirection)
                {
                    trafficNodeScopeData.RightLaneEntities[laneIndex] = trafficNodeTempData;
                }
                else
                {
                    trafficNodeScopeData.LeftLaneEntities[laneIndex] = trafficNodeTempData;
                }

                minPathSettingsIndex = maxPathSettingsIndex;
            }
        }

        private void Dispose()
        {
            pathTempSettingsDatas.Dispose();
            trafficNodeScopeWayPoints.Dispose();
            tempIntersectPoints.Dispose();
            tempSubNodes.Dispose();

            minPathSettingsIndex = 0;
            maxPathSettingsIndex = 0;

            minWaypointPathIndex = 0;
            maxWaypointPathIndex = 0;

            minIntersectIndex = 0;
            maxIntersectIndex = 0;

            minSubNodeIndex = 0;
            maxSubNodeIndex = 0;
        }

        private void Validate(TrafficNode trafficNode)
        {
            switch (trafficNode.TrafficNodeType)
            {
                case TrafficNodeType.TriggerLight:
                    {
                        if (!trafficNode.TryGetComponent<TrafficNodeLightTriggerAuthoring>(out var component))
                        {
                            Debug.Log($"WARNING! {GetInfoText(trafficNode)} is of type '{trafficNode.TrafficNodeType}', but doesn't have the required 'TrafficNodeLightTriggerAuthoring' component");
                        }

                        break;
                    }
            }

            if (!trafficNode.HasRightLanes && trafficNode.Lanes?.Count > 0)
            {
                Debug.Log($"WARNING! {GetInfoText(trafficNode)} has invalid right lanes{TrafficObjectFinderMessage.GetMessage()}");
            }

            if (trafficNode.HasRightLanes && trafficNode.Lanes == null)
            {
                Debug.Log($"WARNING! {GetInfoText(trafficNode)} right lanes is null{TrafficObjectFinderMessage.GetMessage()}");
            }

            if (!trafficNode.HasLeftLanes && trafficNode.ExternalLanes?.Count > 0)
            {
                Debug.Log($"WARNING! {GetInfoText(trafficNode)} has invalid left lanes{TrafficObjectFinderMessage.GetMessage()}");
            }

            if (trafficNode.HasLeftLanes && trafficNode.ExternalLanes == null)
            {
                Debug.Log($"WARNING! {GetInfoText(trafficNode)} left lanes is null{TrafficObjectFinderMessage.GetMessage()}");
            }
        }

        private Entity CreateTrafficNodeEntity(bool subNode = false)
        {
            var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

            AddComponent(entity, typeof(TrafficNodeComponent));
            AddComponent(entity, new TrafficNodeAvailableComponent()
            {
                IsAvailable = true
            });
            AddComponent(entity, typeof(TrafficNodeSettingsComponent));
            AddComponent(entity, typeof(TrafficNodeCapacityComponent));
            AddComponent(entity, typeof(TrafficNodeAvailableTag));
            AddComponent(entity, typeof(TrafficNodeCrossroadRef));
            AddComponent(entity, typeof(LocalToWorld));
            AddComponent(entity, typeof(LocalTransform));

            if (!subNode)
            {
                currentPathConnectionBuffer = AddBuffer<PathConnectionElement>(entity);
            }
            else
            {
                AddBuffer<PathConnectionElement>(entity);
            }

            return entity;
        }

        private void InitializeTrafficNodeEntity(TrafficNode trafficNode, Entity trafficNodeEntity, int laneIndex, int laneDirection)
        {
            var isAvailableForRouteRandomizeSpawning = true;

            if (laneDirection == 1 && trafficNode.TrafficLightCrossroad != null && trafficNode.TrafficLightCrossroad.HasLights)
            {
                isAvailableForRouteRandomizeSpawning = false;
            }

            SetComponent(trafficNodeEntity, new TrafficNodeSettingsComponent
            {
                TrafficNodeType = trafficNode.TrafficNodeType,
                LaneDirectionSide = laneDirection,
                LaneIndex = laneIndex,
                ChanceToSpawn = trafficNode.ChanceToSpawn,
                Weight = trafficNode.Weight,
                CustomAchieveDistance = trafficNode.CustomAchieveDistance,
                HasCrosswalk = trafficNode.HasCrosswalk,
                AllowedRouteRandomizeSpawning = isAvailableForRouteRandomizeSpawning,
                IsAvailableForSpawn = true,
                IsAvailableForSpawnTarget = true
            });

            var laneData = trafficNode.TryToGetLaneData(laneIndex, laneDirection == -1);

            if (laneData != null)
            {
                AddComponent(trafficNodeEntity, new EntityIDBakingData()
                {
                    Value = laneData.UniqueID
                });
            }

            SetComponent(trafficNodeEntity, new TrafficNodeCapacityComponent { Capacity = -1 });

            var lightEntity = Entity.Null;

            if (trafficNode.HasLight(laneDirection))
            {
                lightEntity = GetEntity(trafficNode.TrafficLightHandler.gameObject, TransformUsageFlags.Dynamic);
            }

            SetComponent(trafficNodeEntity, new TrafficNodeComponent
            {
                CrossRoadIndex = -1,
                LightEntity = lightEntity
            });

            var nodeRotation = trafficNode.GetNodeRotation(laneDirection);

            var rightSideLane = laneDirection == 1;
            Vector3 nodePosition = trafficNode.GetLanePosition(laneIndex, !rightSideLane);

            SetComponent(trafficNodeEntity, LocalTransform.FromPositionRotation(nodePosition, nodeRotation));
        }

        private void InitializeRightNodePaths(TrafficNode sourceTrafficNode, int laneIndex)
        {
            if (sourceTrafficNode.IsOneWay && (sourceTrafficNode.Lanes?.Count == 0 || sourceTrafficNode.IsEndOfOneWay))
            {
                Path path = null;

                if (sourceTrafficNode.ExternalLanes?.Count > laneIndex)
                {
                    path = sourceTrafficNode.ExternalLanes[laneIndex].paths[0];
                }

                if (path != null)
                {
                    var reversedConnection = true;

                    Entity connectedEntity = GetEntity(path.ConnectedTrafficNode.gameObject, TransformUsageFlags.Dynamic);
                    Entity lightEntity = TryToGetCustomLight(path);

                    SetPathSettings(path, laneIndex, 1, connectedEntity, lightEntity, reversedConnection);
                }
                else if (sourceTrafficNode.ExternalLanes != null && sourceTrafficNode.ExternalLanes.Count > 0)
                {
                    Debug.Log($"{GetInfoText(sourceTrafficNode)} LaneIndex {laneIndex} OneWay External path is null{TrafficObjectFinderMessage.GetMessage()}");
                }
            }
            else
            {
                if (sourceTrafficNode.Lanes.Count == 0)
                {
                    //if (!sourceTrafficNode.IgnoreEmptyLanes)
                    //{
                    //    Debug.Log($"{GetInfoText(sourceTrafficNode)} doesn't have lanes{TrafficObjectFinderMessage.GetMessage()}");
                    //}

                    return;
                }
                else if (sourceTrafficNode.Lanes.Count <= laneIndex)
                {
                    Debug.Log($"Crossroad: '{sourceTrafficNode.TrafficLightCrossroad.name}' TrafficNode: {sourceTrafficNode.name} InstanceID {sourceTrafficNode.GetInstanceID()} doesn't have {laneIndex} lane index{TrafficObjectFinderMessage.GetMessage()}");
                    return;
                }
                else if (sourceTrafficNode.Lanes[laneIndex].paths.Count == 0)
                {
                    Debug.Log($"Crossroad: '{sourceTrafficNode.TrafficLightCrossroad.name}' TrafficNode: {sourceTrafficNode.name} InstanceID {sourceTrafficNode.GetInstanceID()} lane {laneIndex} doesn't have paths{TrafficObjectFinderMessage.GetMessage()}");
                    return;
                }

                foreach (Path path in sourceTrafficNode.Lanes[laneIndex].paths)
                {
                    var connectedEntity = Entity.Null;
                    bool reversedConnection = false;

                    switch (path.PathConnectionType)
                    {
                        case PathConnectionType.TrafficNode:
                            {
                                connectedEntity = GetEntity(path.ConnectedTrafficNode.gameObject, TransformUsageFlags.Dynamic);

                                if (path.ConnectedTrafficNode.IsOneWay || path.ReversedConnectionSide)
                                {
                                    reversedConnection = true;
                                }

                                break;
                            }
                        case PathConnectionType.PathPoint:
                            {
                                reversedConnection = true;

                                if (path.ConnectedPath != null)
                                {
                                    connectedEntity = GetEntity(path.ConnectedPath.SourceTrafficNode.gameObject, TransformUsageFlags.Dynamic);
                                }
                                else
                                {
                                    Debug.Log($"{GetInfoText(sourceTrafficNode)} Path {path.name} Connected path not found{TrafficObjectFinderMessage.GetMessage()}");
                                }

                                break;
                            }
                    }

                    Entity customLightEntity = TryToGetCustomLight(path);

                    SetPathSettings(path, laneIndex, 1, connectedEntity, customLightEntity, reversedConnection);
                }
            }
        }

        private void InitializeLeftNodePaths(TrafficNode trafficNode, int laneIndex)
        {
            if (trafficNode.ExternalLanes == null || trafficNode.ExternalLanes.Count == 0)
                return;

            if (trafficNode.ExternalLanes.Count <= laneIndex)
            {
                Debug.Log($"WARNING! {GetInfoText(trafficNode)}, External lanes: out of lane capacity {trafficNode.ExternalLanes.Count} LaneIndex {laneIndex}{TrafficObjectFinderMessage.GetMessage()}");
                return;
            }

            foreach (Path path in trafficNode.ExternalLanes[laneIndex].paths)
            {
                if (path == null)
                {
                    Debug.Log($"WARNING! {GetInfoText(trafficNode)} External PathIndex {laneIndex} is null{TrafficObjectFinderMessage.GetMessage()}");
                    continue;
                }

                if (path.ConnectedTrafficNode == null)
                {
                    var sourceNodeName = path.SourceTrafficNode?.name ?? "null";
                    var sourceId = path.SourceTrafficNode?.GetInstanceID() ?? -1;
                    Debug.Log($"WARNING! {GetInfoText(trafficNode)}, SourceNode '{sourceNodeName}' InstanceID {sourceId} Path '{path.name}' InstanceID {path.GetInstanceID()}. External PathIndex {laneIndex} connected node is null{TrafficObjectFinderMessage.GetMessage()}");
                    continue;
                }

                if (!path.ConnectedTrafficNode.IsOneWay)
                {
                    bool reversedConnection = path.ReversedConnectionSide;

                    if (!reversedConnection)
                    {
                        int rightLanes = path.ConnectedTrafficNode.GetLaneCount(false);

                        if (rightLanes <= laneIndex && !path.ConnectedTrafficNode.IgnoreEmptyLanes)
                        {
                            Debug.Log($"WARNING! {GetInfoText(trafficNode)}has missing connected lane index {path.ConnectedTrafficNode.name}. Connected Node Lane Count {rightLanes} Requested External LaneIndex {laneIndex}{TrafficObjectFinderMessage.GetMessage()}");
                            continue;
                        }
                    }
                    else
                    {
                        int leftLanes = path.ConnectedTrafficNode.GetLaneCount(true);

                        if (leftLanes <= laneIndex && !path.ConnectedTrafficNode.IgnoreEmptyLanes)
                        {
                            Debug.Log($"WARNING! {GetInfoText(trafficNode)} missed connected lane index {path.ConnectedTrafficNode.name}. Connected Node Lane Count {leftLanes} Requested External LaneIndex {laneIndex}{TrafficObjectFinderMessage.GetMessage()}");
                            continue;
                        }
                    }
                }
                else
                {
                    if (!path.ConnectedTrafficNode.IsEndOfOneWay)
                    {
                        int rightLanes = path.ConnectedTrafficNode.GetLaneCount(false);

                        if (rightLanes <= path.ConnectedLaneIndex && !path.ConnectedTrafficNode.IgnoreEmptyLanes)
                        {
                            Debug.Log($"WARNING! {GetInfoText(trafficNode)} has missing connected lane index {path.ConnectedTrafficNode.name}. Connected Node Lane Count {rightLanes} Requested External LaneIndex {laneIndex}{TrafficObjectFinderMessage.GetMessage()}");
                            continue;
                        }
                    }
                    else
                    {
                        int leftLanes = path.ConnectedTrafficNode.GetLaneCount(true);

                        if (leftLanes <= path.ConnectedLaneIndex && !path.ConnectedTrafficNode.IgnoreEmptyLanes)
                        {
                            Debug.Log($"WARNING! {GetInfoText(trafficNode)} missed connected lane index {path.ConnectedTrafficNode.name}. Connected Node Lane Count {leftLanes} Requested External LaneIndex {laneIndex}{TrafficObjectFinderMessage.GetMessage()}");
                            continue;
                        }
                    }
                }

                var connectedEntity = GetEntity(path.ConnectedTrafficNode.gameObject, TransformUsageFlags.Dynamic);

                var lightEntity = TryToGetCustomLight(path);

                SetPathSettings(path, laneIndex, -1, connectedEntity, lightEntity, path.ReversedConnectionSide);
            }
        }

        private Entity TryToGetCustomLight(Path path)
        {
            Entity customLightEntity = Entity.Null;

            if (path.CustomLightHandler != null && path.CustomLightHandler.HasCustomLight(path))
            {
                customLightEntity = GetEntity(path.CustomLightHandler.gameObject, TransformUsageFlags.Dynamic);
            }

            return customLightEntity;
        }

        private void SetPathSettings(
            Path path,
            int laneIndex,
            int side,
            Entity connectedTrafficEntity,
            Entity customLightEntity,
            bool reversedConnection = false)
        {
            var wayPoints = path.WayPoints;
            bool hasCustomNode = false;

            for (int i = 0; i < wayPoints.Count; i++)
            {
                maxWaypointPathIndex++;

                var wayPoint = wayPoints[i];

                if (wayPoint == null)
                {
                    string nodeInfo = string.Empty;

                    if (path.SourceTrafficNode != null)
                    {
                        if (path.SourceTrafficNode.TrafficLightCrossroad != null)
                        {
                            nodeInfo = $"{path.SourceTrafficNode.TrafficLightCrossroad.name} {path.SourceTrafficNode.name}";
                        }
                        else
                        {
                            nodeInfo = $"{path.SourceTrafficNode.name}";
                        }
                    }

                    Debug.Log($"{nodeInfo} {path.name} InstanceID {path.GetInstanceID()} Waypoint index '{i}' is null{TrafficObjectFinderMessage.GetMessage()}");
                    continue;
                }

                if (wayPoint.SpawnNode)
                {
                    var subNodeEntity = CreateTrafficNodeEntity(true);

                    SetComponent(subNodeEntity, new TrafficNodeSettingsComponent
                    {
                        TrafficNodeType = TrafficNodeType.Default,
                        LaneDirectionSide = side,
                        LaneIndex = laneIndex,
                        ChanceToSpawn = path.SourceTrafficNode.ChanceToSpawn,
                        Weight = 1,
                        HasCrosswalk = false,
                        AllowedRouteRandomizeSpawning = true,
                        IsAvailableForSpawn = true,
                        IsAvailableForSpawnTarget = true
                    });

                    AddComponent(subNodeEntity, new EntityIDBakingData()
                    {
                        Value = wayPoint.UniqueID
                    });

                    SetComponent(subNodeEntity, new TrafficNodeCapacityComponent { Capacity = -1 });
                    SetComponent(subNodeEntity, new TrafficNodeComponent { CrossRoadIndex = -1, LightEntity = Entity.Null });
                    SetComponent(subNodeEntity, LocalTransform.FromPositionRotation(wayPoint.transform.position, wayPoint.transform.rotation));

                    tempSubNodes.Add(new TempSubTrafficNodeData()
                    {
                        Entity = subNodeEntity,
                        LocalWaypointIndex = i
                    });

                    maxSubNodeIndex++;
                }

                float speedLimitMs = wayPoint.SpeedLimitMs;

                trafficNodeScopeWayPoints.Add(new RouteNodeData()
                {
                    Position = wayPoint.transform.position,
                    SpeedLimit = speedLimitMs,
                    ForwardNodeDirection = !wayPoint.BackwardDirection,
                    TrafficGroup = !wayPoint.CustomGroup ? path.TrafficGroup : wayPoint.CustomGroupType
                });

                if (wayPoint.CustomGroup)
                {
                    hasCustomNode = true;
                }

                bool lastRouteNode = i == wayPoints.Count - 1;

                if (lastRouteNode)
                {
                    currentPathConnectionBuffer.Add(
                        new PathConnectionElement()
                        {
                            GlobalPathIndex = -1,
                            CustomLightEntity = customLightEntity,
                            ConnectedNodeEntity = connectedTrafficEntity,
                            ConnectedHash = -1,
                            ConnectedSubHash = -1
                        });

                    var intersectPoints = path.Intersects;

                    for (int j = 0; j < intersectPoints?.Count; j++)
                    {
                        Path.IntersectPointInfo intersectPointInfo = intersectPoints[j];

                        if (intersectPointInfo.IntersectedPath != null)
                        {
                            var intersectPoint = intersectPointInfo.GetIntersectPoint(path);

                            tempIntersectPoints.Add(new TempIntersectPointData()
                            {
                                Point = intersectPoint,
                                LocalNodeIndex = intersectPointInfo.LocalNodeIndex >= 0 ? (byte)intersectPointInfo.LocalNodeIndex : byte.MaxValue,
                                IntersectedPathInstanceId = intersectPointInfo.IntersectedPath.GetInstanceID()
                            });

                            maxIntersectIndex++;
                        }
                        else
                        {
                            Debug.Log($"{path.SourceTrafficNode?.TrafficLightCrossroad?.name} {path.SourceTrafficNode?.name} InstanceID {path.SourceTrafficNode?.GetInstanceID() ?? -1} {path.name} InstanceID {path.GetInstanceID()} intersected path is null{TrafficObjectFinderMessage.GetMessage()}");
                        }
                    }

                    var connectedPathInstanceId = -1;
                    var connectedNodeInstanceId = -1;

                    if (path.PathConnectionType == PathConnectionType.PathPoint && path.ConnectedPath != null)
                    {
                        connectedPathInstanceId = path.ConnectedPath.GetInstanceID();
                    }

                    if (path.PathConnectionType == PathConnectionType.TrafficNode && path.ConnectedTrafficNode != null)
                    {
                        connectedNodeInstanceId = path.ConnectedTrafficNode.GetInstanceID();
                    }

                    PathOptions options = GetOptions(path);

                    if (hasCustomNode)
                    {
                        options = options.AddFlag(PathOptions.HasCustomNode);
                    }

                    var pathTempSettings = new PathTempSettingsData()
                    {
                        InstanceId = path.GetInstanceID(),
                        MinPathWaypointIndex = minWaypointPathIndex,
                        MaxPathWaypointIndex = maxWaypointPathIndex,
                        MinIntersectPointIndex = minIntersectIndex,
                        MaxIntersectPointIndex = maxIntersectIndex,
                        MinSubNodeIndex = minSubNodeIndex,
                        MaxSubNodeIndex = maxSubNodeIndex,
                        LaneIndex = laneIndex,
                        ConnectedLaneIndex = path.ConnectedLaneIndex,
                        PathLength = path.PathLength,
                        ConnectedPathInstanceId = connectedPathInstanceId,
                        ConnectedNodeInstanceId = connectedNodeInstanceId,
                        Priority = path.Priority,
                        Options = options,
                        ReversedConnection = reversedConnection,
                        PathCurveType = path.PathCurveType,
                        PathRoadType = path.PathRoadType,
                        PathConnectionType = path.PathConnectionType,
                        TrafficGroup = path.TrafficGroup,
                    };

                    pathTempSettingsDatas.Add(pathTempSettings);
                }
            }

            minIntersectIndex = maxIntersectIndex;
            minWaypointPathIndex = maxWaypointPathIndex;
            minSubNodeIndex = maxSubNodeIndex;
            maxPathSettingsIndex++;
        }

        private string GetInfoText(TrafficNode sourceTrafficNode)
        {
            var crossRoadText = string.Empty;

            if (sourceTrafficNode.TrafficLightCrossroad != null)
            {
                crossRoadText = sourceTrafficNode.TrafficLightCrossroad.name;
            }
            else
            {
                crossRoadText = "NaN";
            }

            return $"Crossroad: '{crossRoadText}' TrafficNode: {sourceTrafficNode.name} InstanceID {sourceTrafficNode.GetInstanceID()}";
        }

        public static PathOptions GetOptions(Path path, bool checkCustomNode = false)
        {
            PathOptions options = PathOptions.None;

            if (path.Rail)
            {
                options = options.AddFlag(PathOptions.Rail);
            }

            if (path.EnterOfCrossroad)
            {
                options = options.AddFlag(PathOptions.EnterOfCrossroad);
            }

            if (checkCustomNode)
            {
                bool isCustomNode = false;

                path.IterateWaypoints(
                    pathNode =>
                    {
                        if (pathNode.CustomGroup) isCustomNode = true;
                    });

                if (isCustomNode)
                    options = options.AddFlag(PathOptions.HasCustomNode);
            }

            return options;
        }
    }
}