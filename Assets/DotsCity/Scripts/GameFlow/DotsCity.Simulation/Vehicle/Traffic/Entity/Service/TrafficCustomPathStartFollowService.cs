using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficCustomPathStartFollowService : SingletonMonoBehaviour<TrafficCustomPathStartFollowService>
    {
        private EntityQuery graphQuery;
        private EntityQuery entityGraphQuery;

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        protected override void Awake()
        {
            base.Awake();
            graphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            entityGraphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeResolverSystem.RuntimePathDataRef>());
        }

        /// <summary>
        /// Assign a custom path to follow using the GlobalPathIndex sequence of each path on the desired route.
        /// </summary>
        public void SetFollowPath(Entity trafficEntity, List<int> pathIndexes)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            if (EntityManager.HasComponent<TrafficDefaultTag>(trafficEntity))
            {
                commandBuffer.RemoveComponent<TrafficDefaultTag>(trafficEntity);
                commandBuffer.AddComponent<TrafficFixedRouteTag>(trafficEntity);
                commandBuffer.AddComponent<TrafficFixedRouteComponent>(trafficEntity);

                commandBuffer.AddBuffer<FixedRouteNodeElement>(trafficEntity);
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            var buffer = EntityManager.GetBuffer<FixedRouteNodeElement>(trafficEntity);
            var trafficTransform = EntityManager.GetComponentData<LocalTransform>(trafficEntity);
            var destinationComponent = EntityManager.GetComponentData<TrafficDestinationComponent>(trafficEntity);
            var pathComponent = EntityManager.GetComponentData<TrafficPathComponent>(trafficEntity);
            buffer.Clear();

            var roadGraph = GetRoadGraph();
            var entityGraph = GetEntityGraph();

            var initialPathIndex = pathIndexes[0];

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            bool rebindNeighbourTarget = false;

            int startIndex = 0;

            if (pathComponent.CurrentGlobalPathIndex != initialPathIndex)
            {
                // Currently moving on neighbouring path
                if (roadGraph.HasRelatedPath(pathComponent.CurrentGlobalPathIndex, initialPathIndex, PathGraphSystem.RelationType.Neighbour))
                {
                    rebindNeighbourTarget = true;
                }

                if (!rebindNeighbourTarget)
                {
                    var startPosition = roadGraph.GetStartPosition(initialPathIndex);

                    // Currently staying at start node
                    if (math.distancesq(startPosition, pathComponent.CurrentGlobalPathIndex) < 1f)
                    {
                        rebindNeighbourTarget = true;
                        startIndex = 1;
                    }
                }
            }
            else
            {
                // Already moving on target path, so path assigned starting 1 index
                startIndex = 1;

                destinationComponent.NextDestinationNode = Entity.Null;

                if (pathIndexes.Count > initialPathIndex + 1)
                {
                    entityGraph.TryGetValue(initialPathIndex + 1, out var node2, out var target2);
                    destinationComponent.NextDestinationNode = target2;
                }

                commandBuffer.SetComponent(trafficEntity, destinationComponent);
            }

            // Rebind target if it moves on neighbour path
            if (rebindNeighbourTarget)
            {
                ref readonly var pathData = ref roadGraph.GetPathData(initialPathIndex);

                var speedComponent = EntityManager.GetComponentData<SpeedComponent>(trafficEntity);

                pathComponent.CurrentGlobalPathIndex = initialPathIndex;

                var targetPathNodeIndex = roadGraph.GetTargetWaypointIndexByPoint(initialPathIndex, trafficTransform.Position);

                // Node not found
                if (targetPathNodeIndex == -1)
                {
                    var closestPoint = roadGraph.GetClosestPoint(initialPathIndex, trafficTransform.Position);
                    targetPathNodeIndex = roadGraph.GetTargetWaypointIndexByPoint(initialPathIndex, closestPoint);
                }

                if (targetPathNodeIndex < 0)
                {
                    targetPathNodeIndex = 1;
                }

                ref readonly var pathNode = ref roadGraph.GetPathNodeData(initialPathIndex, targetPathNodeIndex);

                pathComponent.DestinationWayPoint = pathNode.Position;
                pathComponent.LocalPathNodeIndex = targetPathNodeIndex;
                pathComponent.Priority = pathData.Priority;

                ref readonly var previousPathNode = ref roadGraph.GetPathNodeData(initialPathIndex, pathComponent.SourceLocalNodeIndex);
                speedComponent.LaneLimit = previousPathNode.SpeedLimit;

                commandBuffer.SetComponent(trafficEntity, pathComponent);

                entityGraph.TryGetValue(initialPathIndex, out var node, out var target);

                destinationComponent.Destination = roadGraph.GetEndPosition(initialPathIndex);
                destinationComponent.DestinationNode = target;

                if (pathIndexes.Count > initialPathIndex + 1)
                {
                    entityGraph.TryGetValue(initialPathIndex + 1, out var node2, out var target2);
                    destinationComponent.NextDestinationNode = target2;
                }
                else
                {
                    destinationComponent.NextDestinationNode = Entity.Null;
                }

                commandBuffer.SetComponent(trafficEntity, speedComponent);
                commandBuffer.SetComponent(trafficEntity, destinationComponent);
            }

            for (int i = startIndex; i < pathIndexes.Count; i++)
            {
                var pathIndex = pathIndexes[i];

                // Assign the 1st node of each path
                entityGraph.TryGetValue(pathIndex, out var sourceNode, out var targetNode);

                buffer.Add(new FixedRouteNodeElement()
                {
                    TrafficNodeEntity = sourceNode,
                    PathKey = pathIndex,
                    Position = roadGraph.GetStartPosition(pathIndex)
                });

                if (i == pathIndexes.Count - 1)
                {
                    // & the last node
                    buffer.Add(new FixedRouteNodeElement()
                    {
                        TrafficNodeEntity = targetNode,
                        PathKey = pathIndex,
                        Position = roadGraph.GetEndPosition(pathIndex)
                    });
                }
            }

            commandBuffer.SetComponent(trafficEntity, new TrafficFixedRouteComponent()
            {
                RouteEntity = trafficEntity,
                RouteLength = buffer.Length,
            });

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        /// <summary>
        /// Activate the car again to drive through the random paths.
        /// </summary>
        public void RemoveFollowPath(Entity trafficEntity)
        {
            if (EntityManager.HasComponent<TrafficFixedRouteTag>(trafficEntity))
            {
                var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

                commandBuffer.AddComponent<TrafficDefaultTag>(trafficEntity);
                commandBuffer.RemoveComponent<TrafficFixedRouteTag>(trafficEntity);
                commandBuffer.RemoveComponent<TrafficFixedRouteComponent>(trafficEntity);
                commandBuffer.RemoveComponent<FixedRouteNodeElement>(trafficEntity);

                if (EntityManager.HasComponent<TrafficNoTargetTag>(trafficEntity))
                {
                    commandBuffer.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(trafficEntity, true);

                    var trafficStateComponent = EntityManager.GetComponentData<TrafficStateComponent>(trafficEntity);

                    if (TrafficStateExtension.RemoveIdleState<TrafficNoTargetTag>(ref commandBuffer, trafficEntity, ref trafficStateComponent, TrafficIdleState.NoTarget))
                    {
                        if (!trafficStateComponent.IsIdle)
                        {
                            commandBuffer.SetComponentEnabled<TrafficIdleTag>(trafficEntity, false);
                        }
                    }

                    commandBuffer.SetComponent(trafficEntity, trafficStateComponent);
                }

                commandBuffer.Playback(EntityManager);
                commandBuffer.Dispose();
            }
        }

        public PathGraphSystem.Singleton GetRoadGraph() => graphQuery.GetSingleton<PathGraphSystem.Singleton>();

        public TrafficNodeResolverSystem.RuntimePathDataRef GetEntityGraph() => entityGraphQuery.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>();
    }
}
