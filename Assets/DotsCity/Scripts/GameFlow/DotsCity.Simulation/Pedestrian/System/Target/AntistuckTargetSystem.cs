using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AntistuckTargetSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                  .WithAll<AntistuckActivateTag, AntistuckDestinationComponent>()
                  .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var antistuckTargetJob = new AntistuckTargetJob()
            {
                AntistuckConfigReference = SystemAPI.GetSingleton<AntistuckConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            antistuckTargetJob.Schedule();
        }

        [WithDisabled(typeof(AntistuckDeactivateTag))]
        [WithAll(typeof(AntistuckActivateTag))]
        [BurstCompile]
        public partial struct AntistuckTargetJob : IJobEntity
        {
            [ReadOnly]
            public AntistuckConfigReference AntistuckConfigReference;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref AntistuckDestinationComponent antistuckDestinationComponent,
                ref NextStateComponent nextStateComponent,
                EnabledRefRW<AntistuckDeactivateTag> antistuckDeactivateTagRW,
                EnabledRefRW<AntistuckActivateTag> antistuckActivateTagRW,
                in StateComponent stateComponent,
                in CollisionComponent collisionComponent,
                in LocalTransform localTransform)
            {
                if (nextStateComponent.HasNextState || stateComponent.IsActionState(ActionState.Idle) || stateComponent.IsDefaltActionState() || !nextStateComponent.TryToSetNextState(ActionState.Idle, true))
                {
#if UNITY_EDITOR
                    if (stateComponent.IsDefaltActionState())
                    {
                        UnityEngine.Debug.Log($"AntistuckTargetSystem. Pedestrian {entity.Index} stucked with default action state");
                    }
#endif

                    antistuckDeactivateTagRW.ValueRW = true;
                    return;
                }

                ref var antistuckConfig = ref AntistuckConfigReference.Config;
                float3 dstDirection = math.normalizesafe(localTransform.Position.Flat() - collisionComponent.CollidablePosition.Flat());

                var dstRotation = quaternion.LookRotationSafe(dstDirection, math.up());

                float3 destination = localTransform.Position.Flat() + dstDirection * antistuckConfig.Value.TargetPointOffset;

                antistuckDestinationComponent = new AntistuckDestinationComponent()
                {
                    ActivateTimestamp = Timestamp,
                    Destination = destination,
                    DstDirection = dstDirection,
                    DstRotation = dstRotation,
                    PreviousActionState = stateComponent.ActionState,
                    PreviousFlags = stateComponent.AdditiveStateFlags,
                };

                antistuckActivateTagRW.ValueRW = false;
            }
        }
    }
}