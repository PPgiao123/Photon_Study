using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup), OrderLast = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class RagdollSystem : BeginSimulationSystemBase
    {
        public struct ActivateRagdollData
        {
            public int SkinIndex;
            public float3 Position;
            public quaternion Rotation;
            public float3 ForceDirection;
            public float ForceMultiplier;
        }

        private NpcDeathEventConsumerSystem npcDeathEventConsumerSystem;
        private PedestrianRagdollSpawner pedestrianRagdollSpawner;
        private EntityQuery npcQuery;
        private EntityQuery chunkNpcQuery;
        private NativeQueue<ActivateRagdollData> activateRagdollQueue = new NativeQueue<ActivateRagdollData>(Allocator.Persistent);
        private NativeStream emptyStream;

        public bool TriggerSupported { get; set; } = true;

        protected override void OnCreate()
        {
            base.OnCreate();

            npcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<PooledEventTag>()
                .WithAll<RagdollActivateEventTag>()
                .WithAll<StateComponent, HasSkinTag, PedestrianCommonSettings, RagdollComponent>()
                .Build(this);

            chunkNpcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<PooledEventTag>()
                .WithAll<RagdollActivateEventTag>()
                .WithAll<StateComponent, HasSkinTag, PedestrianCommonSettings, RagdollComponent>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this);

            RequireForUpdate(npcQuery);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (activateRagdollQueue.IsCreated)
            {
                activateRagdollQueue.Dispose();
            }

            if (emptyStream.IsCreated)
            {
                emptyStream.Dispose();
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!TriggerSupported)
            {
                if (!emptyStream.IsCreated)
                {
                    emptyStream = new NativeStream(0, Allocator.Persistent);
                }
            }
        }

        protected override void OnUpdate()
        {
            NativeStream.Writer writer = default;

            if (TriggerSupported)
            {
                writer = npcDeathEventConsumerSystem.CreateConsumerWriter(chunkNpcQuery.CalculateChunkCount());
            }
            else
            {
                writer = emptyStream.AsWriter();
            }

            var conversionSettings = SystemAPI.GetSingleton<MiscConversionSettingsReference>();

            Dependency = new ActivateRagdollJob()
            {
                Writer = writer,
                PooledEventTagHandle = SystemAPI.GetComponentTypeHandle<PooledEventTag>(false),
                CommonSettingsHandle = SystemAPI.GetComponentTypeHandle<PedestrianCommonSettings>(true),
                CustomRagdollTagHandle = SystemAPI.GetComponentTypeHandle<CustomRagdollTag>(true),
                RagdollComponentHandle = SystemAPI.GetComponentTypeHandle<RagdollComponent>(true),
                RagdollActivatedComponentHandle = SystemAPI.GetComponentTypeHandle<RagdollActivateEventTag>(true),
                ConversionSettings = conversionSettings,
                TriggerSupported = this.TriggerSupported,
                RagdollQueue = this.activateRagdollQueue
            }.Schedule(npcQuery, Dependency);

            if (TriggerSupported)
            {
                npcDeathEventConsumerSystem.RegisterTriggerDependency(Dependency);
            }

            if (conversionSettings.Config.Value.DefaultRagdollSystem)
            {
                Dependency.Complete();

                while (activateRagdollQueue.Count > 0)
                {
                    var ragdollData = activateRagdollQueue.Dequeue();
                    pedestrianRagdollSpawner.SpawnRagdoll(ragdollData.SkinIndex, ragdollData.Position, ragdollData.Rotation, ragdollData.ForceDirection, ragdollData.ForceMultiplier);
                }
            }
        }

        public void Initialize(PedestrianRagdollSpawner pedestrianRagdollSpawner, NpcDeathEventConsumerSystem npcDeathEventConsumerSystem)
        {
            this.pedestrianRagdollSpawner = pedestrianRagdollSpawner;
            this.npcDeathEventConsumerSystem = npcDeathEventConsumerSystem;
        }

        [BurstCompile]
        public struct ActivateRagdollJob : IJobChunk
        {
            public NativeStream.Writer Writer;

            public ComponentTypeHandle<PooledEventTag> PooledEventTagHandle;
            [ReadOnly] public ComponentTypeHandle<PedestrianCommonSettings> CommonSettingsHandle;
            [ReadOnly] public ComponentTypeHandle<CustomRagdollTag> CustomRagdollTagHandle;
            [ReadOnly] public ComponentTypeHandle<RagdollComponent> RagdollComponentHandle;
            [ReadOnly] public ComponentTypeHandle<RagdollActivateEventTag> RagdollActivatedComponentHandle;
            [ReadOnly] public bool TriggerSupported;

            [NativeDisableParallelForRestriction]
            public NativeQueue<ActivateRagdollData> RagdollQueue;

            [ReadOnly]
            public MiscConversionSettingsReference ConversionSettings;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var pedestrianCommonSettingss = chunk.GetNativeArray(ref CommonSettingsHandle);
                var ragdollComponents = chunk.GetNativeArray(ref RagdollComponentHandle);

                if (TriggerSupported)
                {
                    Writer.BeginForEachIndex(chunkIndex);
                }

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    if (useEnabledMask && !chunk.IsComponentEnabled(ref RagdollActivatedComponentHandle, entityIndex))
                    {
                        continue;
                    }

                    var pedestrianCommonSettings = pedestrianCommonSettingss[entityIndex];
                    var ragdollComponent = ragdollComponents[entityIndex];

                    if (ConversionSettings.Config.Value.DefaultRagdollSystem)
                    {
                        PoolEntityUtils.DestroyEntity(in chunk, ref PooledEventTagHandle, entityIndex);
                    }

                    if (TriggerSupported)
                    {
                        Writer.Write(new NpcDeathEventData()
                        {
                            Position = ragdollComponent.Position
                        });
                    }

                    if (ConversionSettings.Config.Value.DefaultRagdollSystem)
                    {
                        if (!math.isnan(ragdollComponent.Position.x))
                        {
                            RagdollQueue.Enqueue(new ActivateRagdollData()
                            {
                                SkinIndex = pedestrianCommonSettings.SkinIndex,
                                Position = ragdollComponent.Position,
                                Rotation = ragdollComponent.Rotation,
                                ForceDirection = ragdollComponent.ForceDirection,
                                ForceMultiplier = ragdollComponent.ForceMultiplier,
                            });
                        }
                    }
                }

                if (TriggerSupported)
                {
                    Writer.EndForEachIndex();
                }
            }
        }
    }
}