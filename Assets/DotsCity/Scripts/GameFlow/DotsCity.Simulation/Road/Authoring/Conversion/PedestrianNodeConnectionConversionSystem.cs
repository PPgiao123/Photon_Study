using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficSectionConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class PedestrianNodeConnectionConversionSystem : SimpleSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PedestrianNodeBakingData>();
            RequireForUpdate<RoadSceneData>();
            RequireForUpdate<RoadStreamingConfigReference>();
        }

        protected override void OnUpdate()
        {
            var roadSceneData = SystemAPI.GetSingleton<RoadSceneData>();
            var roadStreamingConfig = SystemAPI.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            if (!roadStreamingConfig.StreamingIsEnabled)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((
                Entity nodeEntity,
                in PedestrianNodeBakingData pedestrianNodeBakingData,
                in DynamicBuffer<NodeConnectionDataElement> connectionBuffer,
                in SceneSection currentSection) =>
            {
                bool sectionConnection = false;

                for (int i = 0; i < connectionBuffer.Length; i++)
                {
                    var connectedBakerEntity = connectionBuffer[i].ConnectedEntity;
                    var connectedEntity = BakerExtension.GetEntity(EntityManager, connectedBakerEntity);
                    var connectedSection = EntityManager.GetSharedComponent<SceneSection>(connectedEntity);

                    if (currentSection.Section != connectedSection.Section)
                    {
                        sectionConnection = true;
                        break;
                    }
                }

                if (!sectionConnection)
                    return;

                var buffer = EntityManager.GetBuffer<SegmentPedestrianNodeData>(pedestrianNodeBakingData.CrossroadEntity);

                buffer.Add(new SegmentPedestrianNodeData()
                {
                    Entity = nodeEntity
                });

                var position = pedestrianNodeBakingData.Position;
                var hash = HashMapHelper.GetHashMapPosition(position, roadStreamingConfig.NodeCellSize);

                commandBuffer.AddComponent(nodeEntity, new PedestrianSectionData()
                {
                    NodeHash = hash
                });

                var sectionBuffer = commandBuffer.AddBuffer<NodeSectionConnectionDataElement>(nodeEntity);
                sectionBuffer.EnsureCapacity(connectionBuffer.Length);

                for (int i = 0; i < connectionBuffer.Length; i++)
                {
                    var connectedBakerEntity = connectionBuffer[i].ConnectedEntity;
                    var connectedEntity = BakerExtension.GetEntity(EntityManager, connectedBakerEntity);
                    var connectedSection = EntityManager.GetSharedComponent<SceneSection>(connectedEntity);

                    var connectedHash = -1;

                    if (currentSection.Section != connectedSection.Section)
                    {
                        var connectedTransform = EntityManager.GetComponentData<LocalTransform>(connectedEntity);
                        connectedHash = HashMapHelper.GetHashMapPosition(connectedTransform.Position, roadStreamingConfig.NodeCellSize);
                    }

                    sectionBuffer.Add(new NodeSectionConnectionDataElement()
                    {
                        ConnectedHash = connectedHash
                    });
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}