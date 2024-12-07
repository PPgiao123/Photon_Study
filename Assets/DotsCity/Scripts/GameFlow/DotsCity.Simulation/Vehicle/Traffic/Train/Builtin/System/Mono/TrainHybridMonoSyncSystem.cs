using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MonoSyncGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrainHybridMonoSyncSystem : SystemBase
    {
        private const int InterpolateSpeed = 10;

        protected override void OnUpdate()
        {
            var dt = SystemAPI.Time.DeltaTime;

            Entities
            .WithoutBurst()
            .ForEach((
                PhysicsHybridEntityAdapter adapter,
                ref LocalTransform localTransform,
                ref MonoAdapterComponent monoAdapterComponent,
                in CullStateComponent cullStateComponent) =>
            {
                if (adapter.CurrentPhysicsState == CarEntityAdapter.PhysicsState.FullEnabled)
                {
                    localTransform.Position = adapter.Transform.position;
                    localTransform.Rotation = adapter.Transform.rotation;
                }
                else if (adapter.CurrentPhysicsState == CarEntityAdapter.PhysicsState.Disabled)
                {
                    bool synced = monoAdapterComponent.Synced;

                    if (synced && monoAdapterComponent.Interpolate)
                    {
                        monoAdapterComponent.Position = math.lerp(monoAdapterComponent.Position, localTransform.Position, dt * InterpolateSpeed);
                        monoAdapterComponent.Rotation = math.slerp(monoAdapterComponent.Rotation, localTransform.Rotation, dt * InterpolateSpeed);
                    }
                    else
                    {
                        if (!synced)
                            monoAdapterComponent.Synced = true;

                        monoAdapterComponent.Position = localTransform.Position;
                        monoAdapterComponent.Rotation = localTransform.Rotation;
                    }

                    adapter.Transform.position = monoAdapterComponent.Position;
                    adapter.Transform.rotation = monoAdapterComponent.Rotation;
                }

                if (adapter.CheckCullState(cullStateComponent.State))
                {
                    monoAdapterComponent.Synced = false;
                }

            }).Run();
        }
    }
}