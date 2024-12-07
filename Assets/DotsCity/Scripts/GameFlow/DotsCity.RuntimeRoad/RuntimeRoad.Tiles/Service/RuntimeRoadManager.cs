using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

#if RUNTIME_ROAD
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using System.Collections;
using System.Linq;
using Unity.Collections;
using Unity.Transforms;
#endif

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DefaultExecutionOrder(-10000)]
    public class RuntimeRoadManager : SingletonMonoBehaviour<RuntimeRoadManager>
    {
        public class BindingData
        {
            public TrafficNode Node1;
            public TrafficNode Node2;
        }

        public class PedestrianBindingData
        {
            public PedestrianNode Node1;
            public PedestrianNode Node2;
        }

#pragma warning disable 0414

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#runtimetileroad-demo-structure")]
        [SerializeField] private string link;
        [SerializeField] private float nodeHashCellSize = 0.2f;
        [SerializeField] private bool addCullingComponents = true;

        [Tooltip("If this option is enabled, entities can be retrieved with a road object reference")]
        [SerializeField] private bool bindEntities;

#pragma warning restore 0414

        private EntityQuery graphQuery;
        private EntityQuery nodeResolverQuery;
        private EntityQuery roadStatQuery;

        private PathGraphSystem.Singleton graph;

        private Queue<RuntimeSegment> initQueue = new Queue<RuntimeSegment>();
        private Coroutine coroutine;
        private bool init;
        private Entity graphEntity;
        private TrafficNodeResolverSystem.RuntimePathDataRef nodeResolver;
        private int crossroadIndex;
        private int crosswalkIndex;

        private Dictionary<int, Entity> entityNodeBinding = new Dictionary<int, Entity>();
        private Dictionary<int, BindingData> nodeBinding = new Dictionary<int, BindingData>();

        private Dictionary<int, Entity> entityPedNodeBinding = new Dictionary<int, Entity>();
        private Dictionary<int, PedestrianBindingData> pedNodeBinding = new Dictionary<int, PedestrianBindingData>();

        private Dictionary<int, List<int>> connectedPathHashes = new Dictionary<int, List<int>>();
        private Dictionary<int, List<Entity>> connectedPedHashes = new Dictionary<int, List<Entity>>();
        private List<PedestrianNode> recalcNodes = new List<PedestrianNode>();
        private Dictionary<TrafficNode, int> crosswalkBinding = new Dictionary<TrafficNode, int>();
        private Dictionary<Entity, List<Entity>> lightBinding = new Dictionary<Entity, List<Entity>>();

        private Dictionary<TrafficNode, List<Entity>> sceneTrafficNodeBindingRight;
        private Dictionary<TrafficNode, List<Entity>> sceneTrafficNodeBindingLeft;
        private Dictionary<PedestrianNode, Entity> scenePedestrianNodeBinding;
        private Dictionary<TrafficLightHandler, Entity> lightEntities = new Dictionary<TrafficLightHandler, Entity>();

        private Coroutine removeRoutine;
        private Queue<RuntimeSegment> segmentToAdds = new Queue<RuntimeSegment>();
        private bool simulationDisabled;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public event Action<RuntimeSegment> OnSegmentAdded = delegate { };
        public event Action<RuntimeSegment> OnSegmentRemoved = delegate { };

#if RUNTIME_ROAD

        private TrafficObstacleSystem.Singleton CarHashMap
        {
            get
            {
                var trafficObstacleSystem = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<TrafficObstacleSystem>();

                if (EntityManager.HasComponent<TrafficObstacleSystem.Singleton>(trafficObstacleSystem))
                {
                    return EntityManager.GetComponentData<TrafficObstacleSystem.Singleton>(trafficObstacleSystem);
                }

                return default;
            }
        }

        private PedestrianPathHashMapSystem.Singleton PedestrianHashMap
        {
            get
            {
                var pedestrianPathHashMapSystem = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<PedestrianPathHashMapSystem>();

                if (EntityManager.HasComponent<PedestrianPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem))
                {
                    return EntityManager.GetComponentData<PedestrianPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem);
                }

                return default;
            }
        }

        private TrafficNextPathHashMapSystem.Singleton TrafficNextMap
        {
            get
            {
                var pedestrianPathHashMapSystem = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<TrafficNextPathHashMapSystem>();

                if (EntityManager.HasComponent<TrafficNextPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem))
                {
                    return EntityManager.GetComponentData<TrafficNextPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem);
                }

                return default;
            }
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            graphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            nodeResolverQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeResolverSystem.RuntimePathDataRef>());
            roadStatQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RoadStatConfig>());

#if !RUNTIME_ROAD
            Debug.Log($"RuntimeRoadManager. Make sure you have added the scripting define 'RUNTIME_ROAD' to the project's player settings for the current platform.\r\n" +
                "For more info read the <a href=\"https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#installation\">https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#installation</a>\r\n\r\n\r\n\r\n\r\n");
#endif
        }

#if RUNTIME_ROAD

        public void AddSegment(RuntimeSegment runtimeSegment)
        {
            if (!init)
            {
                initQueue.Enqueue(runtimeSegment);

                if (coroutine == null)
                {
                    coroutine = StartCoroutine(WaitForInit());
                }

                return;
            }

            if (removeRoutine != null)
            {
                segmentToAdds.Enqueue(runtimeSegment);
                return;
            }

            EntityCommandBuffer commandBuffer = default;

            var crossroadData = runtimeSegment.crossroadData;

            var recorded = false;

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;

                trafficLightCrossroad.CrossroadIndex = crossroadIndex++;

                if (trafficLightCrossroad == null || !trafficLightCrossroad.HasLights) continue;

                recorded = true;
                CreateLight(trafficLightCrossroad, data.Value.Lights, ref commandBuffer);
            }

            if (recorded)
                commandBuffer.Playback(EntityManager);

            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;
                if (trafficLightCrossroad == null) continue;

                var nodes = trafficLightCrossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    CreateTrafficNodeEntities(trafficLightCrossroad, nodes[i], ref commandBuffer);
                }
            }

            runtimeSegment.TakenPaths = graph.GetEmptyPaths(runtimeSegment.paths.Length, Allocator.Persistent);

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;
                if (trafficLightCrossroad == null) continue;

                var nodes = trafficLightCrossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    GeneratePaths(runtimeSegment, trafficLightCrossroad, nodes[i]);
                }
            }

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            CreateAllPedNodes(runtimeSegment, ref commandBuffer);

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            ConnectPedNodes(runtimeSegment);

            EntityManager.SetComponentData(graphEntity, graph);
            OnSegmentAdded(runtimeSegment);
        }

        public void RemoveSegment(RuntimeSegment runtimeSegment)
        {
            var destroyEntities = new NativeHashSet<Entity>(100, Allocator.Temp);
            var removeHashes = new NativeList<int>(20, Allocator.Temp);

            var crossroadData = runtimeSegment.crossroadData;

            foreach (var data in crossroadData)
            {
                var crossroad = data.Key;
                if (crossroad == null) continue;

                DestroyLights(crossroad, ref destroyEntities);

                var nodes = crossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var trafficNode = nodes[i];

                    if (crosswalkBinding.ContainsKey(trafficNode))
                    {
                        crosswalkBinding.Remove(trafficNode);
                    }

                    if (bindEntities)
                    {
                        if (sceneTrafficNodeBindingRight != null && sceneTrafficNodeBindingRight.ContainsKey(trafficNode))
                        {
                            sceneTrafficNodeBindingRight.Remove(trafficNode);
                        }

                        if (sceneTrafficNodeBindingLeft != null && sceneTrafficNodeBindingLeft.ContainsKey(trafficNode))
                        {
                            sceneTrafficNodeBindingLeft.Remove(trafficNode);
                        }
                    }

                    trafficNode.IterateAllLanes((laneIndex, external) =>
                    {
                        var pos = trafficNode.GetLanePosition(laneIndex, external);
                        var hash = GetNodeHash(pos);

                        var bindingData = nodeBinding[hash];

                        if (bindingData.Node2 == trafficNode)
                        {
                            bindingData.Node2 = null;
                        }
                        else
                        {
                            if (bindingData.Node1 == trafficNode)
                            {
                                if (bindingData.Node2 != null)
                                {
                                    bindingData.Node1 = bindingData.Node2;
                                    bindingData.Node2 = null;
                                }
                                else
                                {
                                    var buffer = EntityManager.GetBuffer<PathConnectionElement>(entityNodeBinding[hash]);
                                    var trafficNodeComponent = EntityManager.GetComponentData<TrafficNodeComponent>(entityNodeBinding[hash]);

                                    for (int i = 0; i < buffer.Length; i++)
                                    {
                                        var currentPathIndex = buffer[i].GlobalPathIndex;
                                        RemovePath(currentPathIndex);
                                    }

                                    destroyEntities.Add(entityNodeBinding[hash]);

                                    entityNodeBinding.Remove(hash);
                                    nodeBinding.Remove(hash);
                                }
                            }
                        }

                        if (!external)
                        {
                            if (connectedPathHashes.TryGetValue(hash, out var list))
                            {
                                for (var k = 0; k < list.Count; k++)
                                {
                                    var pathIndex = list[k];

                                    nodeResolver.TryGetValue(pathIndex, out var sourceNode, out var connectedNode);

                                    var buffer = EntityManager.GetBuffer<PathConnectionElement>(connectedNode);

                                    int localBufferIndex = 0;

                                    while (localBufferIndex < buffer.Length)
                                    {
                                        var currentPathIndex = buffer[localBufferIndex].GlobalPathIndex;

                                        if (runtimeSegment.TakenPaths.Contains(currentPathIndex))
                                        {
                                            buffer.RemoveAt(localBufferIndex);
                                            RemovePath(currentPathIndex);
                                        }
                                        else
                                        {
                                            localBufferIndex++;
                                        }
                                    }
                                }
                            }
                        }
                    }, true, false);
                }
            }

            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var pos = pedestrianNode.transform.position;
                var hash = GetNodeHash(pos);
                var entity = entityPedNodeBinding[hash];

                if (bindEntities)
                {
                    if (scenePedestrianNodeBinding != null && scenePedestrianNodeBinding.ContainsKey(pedestrianNode))
                        scenePedestrianNodeBinding.Remove(pedestrianNode);
                }

                if (pedNodeBinding.TryGetValue(hash, out var bindingData))
                {
                    if (bindingData.Node2 == pedestrianNode)
                    {
                        bindingData.Node2 = null;
                        recalcNodes.Add(bindingData.Node1);
                    }
                    else if (bindingData.Node1 == pedestrianNode)
                    {
                        if (bindingData.Node2 == null)
                        {
                            destroyEntities.Add(entity);
                            removeHashes.Add(hash);
                            pedNodeBinding.Remove(hash);
                        }
                        else
                        {
                            recalcNodes.Add(bindingData.Node2);
                            bindingData.Node1 = bindingData.Node2;
                            bindingData.Node2 = null;
                        }
                    }
                }
            }

            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var pos = pedestrianNode.transform.position;
                var hash = GetNodeHash(pos);
                var entity = entityPedNodeBinding[hash];

                if (destroyEntities.Contains(entity))
                {
                    var connectedBuffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);

                    for (int j = 0; j < connectedBuffer.Length; j++)
                    {
                        var connectedPos = EntityManager.GetComponentData<LocalTransform>(connectedBuffer[j].ConnectedEntity).Position;
                        var connectedHash = GetNodeHash(connectedPos);

                        if (connectedPedHashes.TryGetValue(connectedHash, out var list))
                        {
                            list.TryToRemove(entity);
                        }

                        DestroyPedestrian(entity, connectedBuffer[j].ConnectedEntity);
                    }

                    var connectedList = connectedPedHashes[hash];

                    for (int j = 0; j < connectedList.Count; j++)
                    {
                        var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(connectedList[j]);
                        var settings = EntityManager.GetComponentData<NodeSettingsComponent>(connectedList[j]);

                        float removedWeight = 0;
                        float previousWeight = 0;

                        for (int n = 0; n < buffer.Length; n++)
                        {
                            if (buffer[n].ConnectedEntity == entity)
                            {
                                removedWeight = buffer[n].SumWeight - previousWeight;

                                for (int k = n + 1; k < buffer.Length; k++)
                                {
                                    var entry = buffer[k];
                                    entry.SumWeight -= removedWeight;
                                    buffer[k] = entry;
                                }

                                buffer.RemoveAt(n);
                                break;
                            }
                            else
                            {
                                previousWeight = buffer[n].SumWeight;
                            }
                        }

                        settings.SumWeight -= removedWeight;
                        EntityManager.SetComponentData(connectedList[j], settings);
                    }

                    connectedPedHashes.Remove(hash);
                    entityNodeBinding.Remove(hash);
                }
            }

            if (destroyEntities.Count > 0)
            {
                var arr = destroyEntities.ToNativeArray(Allocator.Temp);
                EntityManager.DestroyEntity(arr);
                arr.Dispose();
            }

            destroyEntities.Dispose();

            var tempConnectedEntities = new NativeHashSet<Entity>(10, Allocator.Temp);

            for (int i = 0; i < recalcNodes.Count; i++)
            {
                var pedestrianNode = recalcNodes[i];
                var pos = pedestrianNode.transform.position;
                var hash = GetNodeHash(pos);
                var entity = entityPedNodeBinding[hash];
                var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);

                tempConnectedEntities.Clear();

                for (int j = 0; j < pedestrianNode.DefaultConnectedPedestrianNodes.Count; j++)
                {
                    var connectedNode = pedestrianNode.DefaultConnectedPedestrianNodes[j];
                    var connectedPos = connectedNode.transform.position;
                    var connectdHash = GetNodeHash(connectedPos);
                    var connectedEntity = entityPedNodeBinding[connectdHash];
                    tempConnectedEntities.Add(connectedEntity);
                }

                int n = 0;
                float removeWeight = 0;
                float previousWeight = 0;

                var settings = EntityManager.GetComponentData<NodeSettingsComponent>(entity);

                if (pedestrianNode.RelatedTrafficLightHandler != null)
                {
                    var nodeLightSettingsComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);

                    var lightEntity = TryToGetEntity(pedestrianNode.RelatedTrafficLightHandler);

                    if (lightEntity != Entity.Null)
                    {
                        nodeLightSettingsComponent.LightEntity = lightEntity;
                        EntityManager.SetComponentData(entity, nodeLightSettingsComponent);
                    }
                }

                while (n < buffer.Length)
                {
                    var found = tempConnectedEntities.Contains(buffer[n].ConnectedEntity);

                    if (found)
                    {
                        previousWeight = buffer[n].SumWeight;
                        n++;
                    }
                    else
                    {
                        var currentRemoveWeight = buffer[n].SumWeight - previousWeight;
                        removeWeight += currentRemoveWeight;

                        for (int k = n + 1; k < buffer.Length; k++)
                        {
                            var entry = buffer[k];
                            entry.SumWeight -= currentRemoveWeight;
                            buffer[k] = entry;
                        }

                        if (connectedPedHashes.TryGetValue(hash, out var connectedList))
                        {
                            connectedList.TryToRemove(buffer[n].ConnectedEntity);
                        }

                        DestroyPedestrian(entity, buffer[n].ConnectedEntity);

                        buffer.RemoveAt(n);
                    }
                }

                if (removeWeight > 0)
                {
                    settings.SumWeight -= removeWeight;
                    EntityManager.SetComponentData(entity, settings);
                }
            }

            recalcNodes.Clear();

            for (int i = 0; i < removeHashes.Length; i++)
            {
                entityPedNodeBinding.Remove(removeHashes[i]);
            }

            removeHashes.Dispose();
            tempConnectedEntities.Dispose();

            var carHashMap = this.CarHashMap.CarHashMap;
            var trafficNextMap = this.TrafficNextMap.HashMap;

            if (carHashMap.IsCreated)
            {
                var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

                for (int i = 0; i < runtimeSegment.TakenPaths.Length; i++)
                {
                    var pathIndex = runtimeSegment.TakenPaths[i];

                    if (carHashMap.TryGetFirstValue(pathIndex, out var carHashData, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            if (EntityManager.HasComponent<PooledEventTag>(carHashData.Entity))
                                commandBuffer.SetComponentEnabled<PooledEventTag>(carHashData.Entity, true);

                        } while (carHashMap.TryGetNextValue(out carHashData, ref nativeMultiHashMapIterator));
                    }

                    if (trafficNextMap.TryGetFirstValue(pathIndex, out var carHashData2, out var nativeMultiHashMapIterator2))
                    {
                        do
                        {
                            var dest = EntityManager.GetComponentData<TrafficDestinationComponent>(carHashData2.Entity);

                            dest.NextDestinationNode = Entity.Null;
                            dest.NextGlobalPathIndex = -1;

                            commandBuffer.SetComponent(carHashData2.Entity, dest);

                        } while (trafficNextMap.TryGetNextValue(out carHashData2, ref nativeMultiHashMapIterator2));
                    }
                }

                commandBuffer.Playback(EntityManager);
                commandBuffer.Dispose();
            }

            removeRoutine = StartCoroutine(Remove(runtimeSegment));
        }

        [HideIf(nameof(simulationDisabled))]
        [Button]
        public void DisableSimulation()
        {
            if (simulationDisabled || !Application.isPlaying) return;

            simulationDisabled = true;

            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficCleanerSystem>().Clear();
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PedestrianCleanerSystem>().Clear();
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PedestrianEntitySpawnerSystem>().Enabled = false;
        }

        [ShowIf(nameof(simulationDisabled))]
        [Button]
        public void ResumeSimulation()
        {
            if (!simulationDisabled || !Application.isPlaying) return;

            simulationDisabled = false;
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>().Enabled = true;
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PedestrianEntitySpawnerSystem>().Enabled = true;
        }

        public Entity TryToGetEntity(TrafficNode node, bool rightDirection, int laneIndex = 0)
        {
            if (!bindEntities)
                throw new ArgumentException("BindEntities option is disabled.");

            if (rightDirection)
            {
                if (sceneTrafficNodeBindingRight != null && sceneTrafficNodeBindingRight.TryGetValue(node, out List<Entity> list) && list.Count > laneIndex)
                {
                    return list[laneIndex];
                }
            }
            else
            {
                if (sceneTrafficNodeBindingLeft != null && sceneTrafficNodeBindingLeft.TryGetValue(node, out List<Entity> list) && list.Count > laneIndex)
                {
                    return list[laneIndex];
                }
            }

            return Entity.Null;
        }

        public Entity TryToGetEntity(PedestrianNode node)
        {
            if (!bindEntities)
                throw new ArgumentException("BindEntities option is disabled.");

            if (scenePedestrianNodeBinding != null && scenePedestrianNodeBinding.TryGetValue(node, out var entity))
            {
                return entity;
            }

            return Entity.Null;
        }

        public Entity TryToGetEntity(TrafficLightHandler value)
        {
            if (!value)
                return Entity.Null;

            if (lightEntities.TryGetValue(value, out var entity))
            {
                return entity;
            }

            return Entity.Null;
        }

        private IEnumerator Remove(RuntimeSegment runtimeSegment)
        {
            yield return new WaitForEndOfFrame();
            graph.RemovePaths(runtimeSegment.TakenPaths);
            if (runtimeSegment.TakenPaths.IsCreated) runtimeSegment.TakenPaths.Dispose();
            removeRoutine = null;

            while (segmentToAdds.Count > 0)
            {
                var segment = segmentToAdds.Dequeue();
                AddSegment(segment);
            }

            OnSegmentRemoved(runtimeSegment);
        }

        private void DestroyPedestrian(Entity entity, Entity connectedEntity)
        {
            var hashMap = this.PedestrianHashMap.HashMap;

            if (hashMap.IsCreated)
            {
                var pair = new EntityPair(entity, connectedEntity);

                if (hashMap.TryGetFirstValue(pair, out var hashData, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        if (EntityManager.HasComponent<PooledEventTag>(hashData.Entity) && !EntityManager.IsComponentEnabled<PooledEventTag>(hashData.Entity))
                            EntityManager.SetComponentEnabled<PooledEventTag>(hashData.Entity, true);

                    } while (hashMap.TryGetNextValue(out hashData, ref nativeMultiHashMapIterator));
                }
            }
        }

        private IEnumerator WaitForInit()
        {
            while (true)
            {
                if (nodeResolverQuery.CalculateEntityCount() != 0 && graphQuery.CalculateEntityCount() != 0) break;

                yield return null;
            }

            init = true;

            graphEntity = graphQuery.GetSingletonEntity();
            graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();
            nodeResolver = nodeResolverQuery.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>();

            var count = initQueue.Count;

            for (int i = 0; i < count; i++)
            {
                var segment = initQueue.Dequeue();
                AddSegment(segment);
            }
        }

        private void DestroyLights(TrafficLightCrossroad trafficLightCrossroad, ref NativeHashSet<Entity> destroyEntities)
        {
            if (!trafficLightCrossroad.HasLights) return;

            var trafficLightHandlers = trafficLightCrossroad.TrafficLightHandlers;

            foreach (var item in trafficLightHandlers)
            {
                var lightEntity = lightEntities[item.Value];
                lightEntities.Remove(item.Value);
                destroyEntities.Add(lightEntity);

                if (!lightBinding.TryGetValue(lightEntity, out var list))
                    continue;

                while (list.Count > 0)
                {
                    Entity entity = list[0];

                    if (EntityManager.HasComponent<TrafficNodeComponent>(entity))
                    {
                        var nodeComponent = EntityManager.GetComponentData<TrafficNodeComponent>(entity);
                        nodeComponent.LightEntity = Entity.Null;
                        nodeComponent.CrossRoadIndex = -1;
                        EntityManager.SetComponentData(entity, nodeComponent);
                    }

                    if (EntityManager.HasComponent<NodeLightSettingsComponent>(entity))
                    {
                        var nodeComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);
                        nodeComponent.LightEntity = Entity.Null;
                        EntityManager.SetComponentData(entity, nodeComponent);
                    }

                    list.RemoveAt(0);
                }

                lightBinding.Remove(lightEntity);
            }
        }

        private void CreateLight(TrafficLightCrossroad trafficLightCrossroad, List<TrafficLightObjectAuthoring> lights, ref EntityCommandBuffer commandBuffer)
        {
            var handlers = trafficLightCrossroad.TrafficLightHandlers;

            trafficLightCrossroad.TryToGenerateID(true);

            var crossroadID = trafficLightCrossroad.UniqueId;

            foreach (var handler in handlers)
            {
                var lightEntity = EntityManager.CreateEntity(
                    typeof(LightHandlerInitTag),
                    typeof(LightHandlerStateUpdateTag),
                    typeof(LightHandlerComponent),
                    typeof(LightHandlerID),
                    typeof(LightHandlerStateElement));

                var buffer = commandBuffer.AddBuffer<LightHandlerStateElement>(lightEntity);

                var lightStates = handler.Value.LightStates;

                buffer.EnsureCapacity(lightStates.Count);

                float totalDuration = 0;

                for (int i = 0; i < lightStates.Count; i++)
                {
                    var lightState = lightStates[i];

                    buffer.Add(new LightHandlerStateElement()
                    {
                        LightState = lightState.LightState,
                        Duration = lightState.Duration,
                    });

                    totalDuration += lightState.Duration;
                }

                commandBuffer.SetComponent(lightEntity, new LightHandlerComponent()
                {
                    CycleDuration = totalDuration,
                    CrossRoadIndex = trafficLightCrossroad.CrossroadIndex
                });

                var handlerId = crossroadID + handler.Key;

                commandBuffer.SetComponent(lightEntity, new LightHandlerID()
                {
                    Value = handlerId
                });

                AddLightEntity(handler.Value, lightEntity);
            }

            for (int i = 0; i < lights?.Count; i++)
            {
                lights[i].TrafficLightObject.ConnectedId = crossroadID;
                lights[i].RegisterFrames();
            }
        }

        private void CreateTrafficNodeEntities(TrafficLightCrossroad trafficLightCrossroad, TrafficNode node, ref EntityCommandBuffer entityCommandBuffer)
        {
            var lightEntity = TryToGetEntity(node.TrafficLightHandler);

            IterateAllLanes(node, ref entityCommandBuffer, (commandBuffer, laneIndex, external) =>
            {
                var pos = node.GetLanePosition(laneIndex, external);

                var hash = GetNodeHash(pos);

                var side = !external ? 1 : -1;

                if (!entityNodeBinding.ContainsKey(hash))
                {
                    var rot = node.GetNodeRotation(side);

                    var entity = EntityManager.CreateEntity(
                        typeof(TrafficNodeComponent),
                        typeof(TrafficNodeAvailableComponent),
                        typeof(TrafficNodeSettingsComponent),
                        typeof(TrafficNodeCapacityComponent),
                        typeof(TrafficNodeAvailableTag),
                        typeof(LocalToWorld),
                        typeof(LocalTransform),
                        typeof(PathConnectionElement));

                    if (bindEntities)
                    {
                        if (side == 1)
                        {
                            if (sceneTrafficNodeBindingRight == null)
                            {
                                sceneTrafficNodeBindingRight = new Dictionary<TrafficNode, List<Entity>>();
                            }

                            if (!sceneTrafficNodeBindingRight.ContainsKey(node))
                            {
                                sceneTrafficNodeBindingRight.Add(node, new List<Entity>());
                            }

                            sceneTrafficNodeBindingRight[node].Add(entity);
                        }
                        else
                        {
                            if (sceneTrafficNodeBindingLeft == null)
                            {
                                sceneTrafficNodeBindingLeft = new Dictionary<TrafficNode, List<Entity>>();
                            }

                            if (!sceneTrafficNodeBindingLeft.ContainsKey(node))
                            {
                                sceneTrafficNodeBindingLeft.Add(node, new List<Entity>());
                            }

                            sceneTrafficNodeBindingLeft[node].Add(entity);
                        }
                    }

                    if (addCullingComponents)
                        commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());

                    entityNodeBinding.Add(hash, entity);

                    nodeBinding.Add(hash, new BindingData()
                    {
                        Node1 = node,
                    });

                    var currentLightEntity = Entity.Null;

                    if (node.HasLight(side))
                        currentLightEntity = lightEntity;

                    commandBuffer.SetComponent(entity, new TrafficNodeComponent()
                    {
                        CrossRoadIndex = trafficLightCrossroad.CrossroadIndex,
                        LightEntity = currentLightEntity
                    });

                    commandBuffer.SetComponent(entity, new TrafficNodeSettingsComponent()
                    {
                        TrafficNodeType = node.TrafficNodeType,
                        LaneDirectionSide = side,
                        LaneIndex = laneIndex,
                        ChanceToSpawn = node.ChanceToSpawn,
                        Weight = node.Weight,
                        CustomAchieveDistance = node.CustomAchieveDistance,
                        HasCrosswalk = node.HasCrosswalk,
                        AllowedRouteRandomizeSpawning = true,
                        IsAvailableForSpawn = true,
                        IsAvailableForSpawnTarget = true,
                    });

                    commandBuffer.SetComponent(entity, new TrafficNodeCapacityComponent()
                    {
                        Capacity = -1
                    });

                    commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(pos, rot));

                    commandBuffer.AddBuffer<PathConnectionElement>(entity);

                    if (!crosswalkBinding.ContainsKey(node))
                        crosswalkBinding.Add(node, crosswalkIndex++);
                }
                else
                {
                    var crossWalkIndex = crosswalkBinding[nodeBinding[hash].Node1];

                    nodeBinding[hash].Node2 = node;

                    if (lightEntity != Entity.Null && node.HasLight(side))
                    {
                        var entity = entityNodeBinding[hash];
                        AddLightBinding(lightEntity, entity);

                        commandBuffer.SetComponent(entity, new TrafficNodeComponent()
                        {
                            CrossRoadIndex = trafficLightCrossroad.CrossroadIndex,
                            LightEntity = lightEntity
                        });
                    }

                    if (!crosswalkBinding.ContainsKey(node))
                        crosswalkBinding.Add(node, crossWalkIndex);
                }
            });
        }

        private void GeneratePaths(RuntimeSegment runtimeSegment, TrafficLightCrossroad crossroad, TrafficNode node)
        {
            node.IterateAllPaths((path) =>
            {
                if (!path.HasConnection) return;

                var sourceHash = GetNodeHash(path.StartPosition);
                var connectedHash = GetNodeHash(path.EndPosition);

                var sourceEntity = entityNodeBinding[sourceHash];
                var connectedEntity = entityNodeBinding[connectedHash];

                var pathBuffer = EntityManager.GetBuffer<PathConnectionElement>(sourceEntity);
                var connectedPathBuffer = EntityManager.GetBuffer<PathConnectionElement>(connectedEntity);

                var options = TrafficNodeBaker.GetOptions(path, true);

                var sourcePathIndex = GetPathIndex(runtimeSegment, path);

                AddConnectedHash(connectedHash, sourcePathIndex);

                var pathData = new PathGraphSystem.PathData()
                {
                    PathIndex = sourcePathIndex,

                    ConnectedPathIndex = -1,
                    SourceLaneIndex = path.SourceLaneIndex,
                    PathLength = path.PathLength,
                    Priority = path.Priority,
                    Options = options,
                    PathCurveType = path.PathCurveType,
                    PathRoadType = path.PathRoadType,
                    PathConnectionType = path.PathConnectionType,
                    TrafficGroup = path.TrafficGroup,
                };

                graph.AddPath(pathData);

                for (int i = 0; i < connectedPathBuffer.Length; i++)
                {
                    var connectedPathIndex = connectedPathBuffer[i].GlobalPathIndex;

                    graph.AddConnectedPath(sourcePathIndex, connectedPathIndex);
                    graph.AddConnectedByPath(connectedPathIndex, sourcePathIndex);
                }

                if (connectedPathHashes.TryGetValue(sourceHash, out var connectedList))
                {
                    for (int i = 0; i < connectedList.Count; i++)
                    {
                        var connectedPathIndex = connectedList[i];
                        graph.AddConnectedByPath(sourcePathIndex, connectedPathIndex);
                    }

                    for (int i = 0; i < connectedList.Count; i++)
                    {
                        var connectedPathIndex = connectedList[i];
                        graph.AddConnectedPath(connectedPathIndex, sourcePathIndex);
                    }
                }

                if (path.Intersects.Count > 0)
                {
                    graph.InitIntersection(sourcePathIndex, path.Intersects.Count);

                    for (int i = 0; i < path.Intersects.Count; i++)
                    {
                        var intersect = path.Intersects[i];

                        graph.AddIntersection(sourcePathIndex, new IntersectPathInfo()
                        {
                            IntersectedPathIndex = GetPathIndex(runtimeSegment, intersect.IntersectedPath),
                            IntersectPosition = crossroad.transform.TransformPoint(intersect.IntersectPoint),
                            LocalNodeIndex = (byte)intersect.LocalNodeIndex
                        });
                    }
                }

                if (path.WayPoints.Count > 0)
                {
                    graph.InitRouteNodes(sourcePathIndex, path.WayPoints.Count);

                    for (int i = 0; i < path.WayPoints.Count; i++)
                    {
                        PathNode waypoint = path.WayPoints[i];

                        graph.AddRouteNode(sourcePathIndex, new RouteNodeData()
                        {
                            SpeedLimit = waypoint.SpeedLimitMs,
                            ForwardNodeDirection = !waypoint.BackwardDirection,
                            Position = waypoint.transform.position,
                            TrafficGroup = waypoint.CustomGroupType
                        });
                    }
                }

                var neighbourPaths = runtimeSegment.GetNeighbourPaths(path);

                if (neighbourPaths?.Count > 0)
                {
                    graph.InitNeighbourPaths(sourcePathIndex, neighbourPaths.Count);

                    for (int i = 0; i < neighbourPaths.Count; i++)
                    {
                        Path neighbourPath = neighbourPaths[i];

                        var neighbourPathIndex = GetPathIndex(runtimeSegment, neighbourPath);

                        graph.AddNeighbourPath(sourcePathIndex, neighbourPathIndex);
                    }
                }

                var parallelPaths = runtimeSegment.GetParallelPaths(path);

                if (parallelPaths?.Count > 0)
                {
                    graph.InitParallelPaths(sourcePathIndex, parallelPaths.Count);

                    for (int i = 0; i < parallelPaths.Count; i++)
                    {
                        Path parallelPath = parallelPaths[i];

                        var parallelPathPathIndex = GetPathIndex(runtimeSegment, parallelPath);

                        graph.AddParallelPath(sourcePathIndex, parallelPathPathIndex);
                    }
                }

                pathBuffer.Add(new PathConnectionElement()
                {
                    ConnectedHash = -1,
                    ConnectedSubHash = -1,
                    ConnectedSubNodeEntity = connectedEntity,
                    ConnectedNodeEntity = connectedEntity,
                    StartLocalNodeIndex = 0,
                    GlobalPathIndex = sourcePathIndex
                });

                nodeResolver.AddPath(sourcePathIndex, sourceEntity, connectedEntity);

            }, true);
        }

        private void RemovePath(int currentPathIndex)
        {
            var endPosition = graph.GetEndPosition(currentPathIndex);
            var connectedHash = GetNodeHash(endPosition);

            if (connectedPathHashes.TryGetValue(connectedHash, out var list))
            {
                list.TryToRemove(currentPathIndex);

                if (list.Count == 0)
                {
                    connectedPathHashes.Remove(connectedHash);
                }
            }

            nodeResolver.RemovePath(currentPathIndex);
        }

        private int GetPathIndex(RuntimeSegment runtimeSegment, Path path)
        {
            if (!path) return -1;

            return runtimeSegment.TakenPaths[Array.IndexOf(runtimeSegment.paths, path)];
        }

        private void AddConnectedHash(int sourceHash, int pathIndex)
        {
            if (!connectedPathHashes.ContainsKey(sourceHash))
            {
                connectedPathHashes.Add(sourceHash, new List<int>());
            }

            connectedPathHashes[sourceHash].Add(pathIndex);
        }

        private int GetNodeHash(Vector3 pos)
        {
            pos = new Vector3(MathF.Round(pos.x, 2), MathF.Round(pos.y, 2), MathF.Round(pos.z, 2));
            return HashMapHelper.GetHashMapRoundPosition(pos, nodeHashCellSize);
        }

        private void CreateAllPedNodes(RuntimeSegment runtimeSegment, ref EntityCommandBuffer commandBuffer)
        {
            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var node = runtimeSegment.pedestrianNodes[i];
                CreatePedNode(runtimeSegment, node, ref commandBuffer);
            }
        }

        private void ConnectPedNodes(RuntimeSegment runtimeSegment)
        {
            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var pos = pedestrianNode.transform.position;
                var hash = GetNodeHash(pos);
                var entity = entityPedNodeBinding[hash];

                var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);
                buffer.EnsureCapacity(pedestrianNode.DefaultConnectedPedestrianNodes.Count);

                bool emptyBuffer = buffer.Length == 0;

                var settings = EntityManager.GetComponentData<NodeSettingsComponent>(entity);

                for (int j = 0; j < pedestrianNode.DefaultConnectedPedestrianNodes.Count; j++)
                {
                    var connectedNode = pedestrianNode.DefaultConnectedPedestrianNodes[j];

                    if (connectedNode == null || !connectedNode.gameObject.activeInHierarchy)
                        continue;

                    if (!connectedNode.CheckConnection(pedestrianNode))
                        continue;

                    if (!connectedNode.HasConnection(pedestrianNode))
                        continue;

                    var connectedPos = connectedNode.transform.position;
                    var connectedHash = GetNodeHash(connectedPos);
                    var connectedEntity = entityPedNodeBinding[connectedHash];

                    bool exist = false;

                    if (!emptyBuffer)
                    {
                        for (int k = 0; k < buffer.Length; k++)
                        {
                            if (buffer[k].ConnectedEntity == connectedEntity)
                            {
                                exist = true;
                                break;
                            }
                        }
                    }

                    if (!exist)
                    {
                        if (!connectedPedHashes.ContainsKey(connectedHash))
                        {
                            connectedPedHashes.Add(connectedHash, new List<Entity>());
                        }

                        connectedPedHashes[connectedHash].Add(entity);

                        settings.SumWeight += connectedNode.PriorityWeight;

                        buffer.Add(new NodeConnectionDataElement()
                        {
                            SumWeight = settings.SumWeight,
                            ConnectedEntity = connectedEntity
                        });
                    }
                }

                EntityManager.SetComponentData(entity, settings);
            }
        }

        private Entity CreatePedNode(RuntimeSegment runtimeSegment, PedestrianNode pedestrianNode, ref EntityCommandBuffer commandBuffer)
        {
            var pos = pedestrianNode.transform.position;
            var hash = GetNodeHash(pos);

            if (!entityPedNodeBinding.ContainsKey(hash))
            {
                var lightEntity = TryToGetEntity(pedestrianNode.RelatedTrafficLightHandler);

                var entity = EntityManager.CreateEntity(
                    typeof(NodeCapacityComponent),
                    typeof(NodeLightSettingsComponent),
                    typeof(NodeSettingsComponent),
                    typeof(NodeConnectionDataElement),
                    typeof(LocalTransform),
                    typeof(LocalToWorld));

                if (bindEntities)
                {
                    if (scenePedestrianNodeBinding == null)
                    {
                        scenePedestrianNodeBinding = new Dictionary<PedestrianNode, Entity>();
                    }

                    scenePedestrianNodeBinding.Add(pedestrianNode, entity);
                }

                if (addCullingComponents)
                    commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());

                entityPedNodeBinding.Add(hash, entity);

                pedNodeBinding.Add(hash, new PedestrianBindingData()
                {
                    Node1 = pedestrianNode
                });

                //ProcessAdditionalSettings(pedestrianNode, entity);

                if (pedestrianNode.CanSpawnInView)
                {
                    commandBuffer.AddComponent(entity, typeof(NodeCanSpawnInVisionTag));
                }

                int capacity = pedestrianNode.Capacity;

                int maxCapacity = pedestrianNode.PedestrianNodeType == PedestrianNodeType.CarParking ? 1 : capacity;
                commandBuffer.SetComponent(entity, new NodeCapacityComponent { MaxAvailaibleCount = maxCapacity, CurrentCount = capacity });

                if (capacity >= 0)
                {
                    commandBuffer.AddComponent(entity, typeof(NodeHasCapacityOptionTag));
                }

                var connectionBuffer = commandBuffer.AddBuffer<NodeConnectionDataElement>(entity);

                int connectionCapacity = pedestrianNode.AutoConnectedPedestrianNodes.Count + pedestrianNode.DefaultConnectedPedestrianNodes.Count;
                connectionBuffer.EnsureCapacity(connectionCapacity);

                var nodeSettingsComponent = new NodeSettingsComponent()
                {
                    NodeType = pedestrianNode.PedestrianNodeType,
                    NodeShapeType = pedestrianNode.PedestrianNodeShapeType,
                    Weight = pedestrianNode.PriorityWeight,
                    CustomAchieveDistance = pedestrianNode.CustomAchieveDistance,
                    CanSpawnInVision = pedestrianNode.CanSpawnInView ? 1 : 0,
                    ChanceToSpawn = pedestrianNode.ChanceToSpawn,
                    MaxPathWidth = pedestrianNode.MaxPathWidth,
                    Height = pedestrianNode.Height,
                    HasMovementRandomOffset = pedestrianNode.HasMovementRandomOffset ? 1 : 0,
                    SumWeight = 0
                };

                EntityManager.SetComponentData(entity, nodeSettingsComponent);
                commandBuffer.SetComponent(entity, LocalTransform.FromPosition(pos));

                var crosswalkIndex = -1;

                if (pedestrianNode.ConnectedTrafficNode != null)
                {
                    crosswalkBinding.TryGetValue(pedestrianNode.ConnectedTrafficNode, out crosswalkIndex);
                }

                commandBuffer.SetComponent(entity, new NodeLightSettingsComponent()
                {
                    HasCrosswalk = true,
                    LightEntity = lightEntity,
                    CrosswalkIndex = crosswalkIndex,
                });
            }
            else
            {
                var entity = entityPedNodeBinding[hash];
                var settings = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);

                if (bindEntities)
                {
                    scenePedestrianNodeBinding.Add(pedestrianNode, entity);
                }

                var lightEntity = TryToGetEntity(pedestrianNode.RelatedTrafficLightHandler);

                if (lightEntity == Entity.Null)
                {
                    lightEntity = TryToGetEntity(pedNodeBinding[hash].Node1.RelatedTrafficLightHandler);
                }

                if (settings.LightEntity == Entity.Null && lightEntity != Entity.Null)
                {
                    settings.LightEntity = lightEntity;

                    if (settings.CrosswalkIndex == -1)
                    {
                        if (pedestrianNode.ConnectedTrafficNode != null)
                        {
                            crosswalkBinding.TryGetValue(pedestrianNode.ConnectedTrafficNode, out crosswalkIndex);
                            settings.CrosswalkIndex = crosswalkIndex;
                        }
                    }

                    commandBuffer.SetComponent(entity, settings);
                }

                if (pedNodeBinding[hash].Node1 != pedestrianNode)
                {
                    pedNodeBinding[hash].Node2 = pedestrianNode;
                }

                AddLightBinding(lightEntity, entity);
            }

            return entityPedNodeBinding[hash];
        }

        private void AddLightBinding(Entity lightEntity, Entity entity)
        {
            if (lightEntity == Entity.Null)
                return;

            if (!lightBinding.ContainsKey(lightEntity))
            {
                lightBinding.Add(lightEntity, new List<Entity>());
            }

            lightBinding[lightEntity].Add(entity);
        }

        private void AddLightEntity(TrafficLightHandler value, Entity lightEntity)
        {
            lightEntities.Add(value, lightEntity);
        }

        private static void IterateAllLanes(TrafficNode trafficNode, ref EntityCommandBuffer commandBuffer, Action<EntityCommandBuffer, int, bool> callback, bool includeExternal = true)
        {
            if (trafficNode.HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(); laneIndex++)
                {
                    if (trafficNode.LaneExist(laneIndex, false))
                        callback?.Invoke(commandBuffer, laneIndex, false);
                }
            }

            if (includeExternal)
            {
                IterateExternalLanes(trafficNode, ref commandBuffer, callback);
            }
        }

        private static void IterateExternalLanes(TrafficNode trafficNode, ref EntityCommandBuffer entityCommand, Action<EntityCommandBuffer, int, bool> callback)
        {
            if (!trafficNode.HasLeftLanes)
                return;

            for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(true); laneIndex++)
            {
                callback?.Invoke(entityCommand, laneIndex, true);
            }
        }

#endif
    }
}
