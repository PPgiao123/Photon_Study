using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct SwitchTalkingKeySystem : ISystem
    {
        private const double MIN_SWITCH_RANDOM_TIME = 3f;
        private const double MAX_SWITCH_RANDOM_TIME = 5f;

        private EntityQuery updateGroup;
        private NativeArray<AnimationState> talkingAnimationStates;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<UpdateCustomAnimationTag>()
                .WithAll<TalkComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);

            talkingAnimationStates = new NativeArray<AnimationState>(3, Allocator.Persistent);
            talkingAnimationStates[0] = AnimationState.Talking1;
            talkingAnimationStates[1] = AnimationState.Talking2;
            talkingAnimationStates[2] = AnimationState.Talking3;
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            talkingAnimationStates.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchTalkKeyJob = new SwitchTalkKeyJob()
            {
                TalkAnimationStates = talkingAnimationStates,
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            switchTalkKeyJob.Run();
        }

        [WithDisabled(typeof(UpdateCustomAnimationTag))]
        [WithAll(typeof(TalkComponent))]
        [BurstCompile]
        private partial struct SwitchTalkKeyJob : IJobEntity
        {
            [ReadOnly]
            public NativeArray<AnimationState> TalkAnimationStates;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref TalkComponent talkComponent,
                ref AnimationStateComponent animationStateComponent,
                EnabledRefRW<UpdateCustomAnimationTag> switchTalkingEventComponentRW)
            {
                if (talkComponent.SwitchTalkingTime > Timestamp)
                    return;

                var rndGenLocal = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index);

                var randomTalkParameter = rndGenLocal.NextInt(0, TalkAnimationStates.Length);

                talkComponent.SwitchTalkingTime = Timestamp + rndGenLocal.NextDouble(MIN_SWITCH_RANDOM_TIME, MAX_SWITCH_RANDOM_TIME);
                animationStateComponent.NewAnimationState = TalkAnimationStates[randomTalkParameter];

                switchTalkingEventComponentRW.ValueRW = true;
            }
        }
    }
}