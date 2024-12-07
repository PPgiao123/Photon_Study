using Spirit604.AnimationBaker.Entities;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(DummyGroup))]
    public partial struct AnimatorDataProviderSystem : ISystem
    {
        public struct Singleton : IComponentData
        {
            internal NativeHashMap<int, LegacyAnimationDataComponent> legacyAnimationData;
            internal NativeHashMap<int, LegacyAnimationDataComponent> legacyExitAnimationData;
            internal NativeHashMap<int, GPUAnimationDataComponent> gpuAnimationData;
            internal NativeHashMap<int, AnimationState> gpuToLegacyBinding;
            internal NativeHashMap<int, AnimationState> movementAnimationBinding;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool PlayAnimation(Animator animator, AnimationState animationState, float timestamp, float clipLength, float startTime = 0)
            {
                var key = (int)animationState;

                if (legacyAnimationData.ContainsKey(key))
                {
                    var animData = legacyAnimationData[key];

                    PlayAnimation(animator, animData);

                    var normalizedClipTime = ((timestamp + startTime) % clipLength) / clipLength;

                    animator.Play(animData.StateNameHash, animData.StateLayer, normalizedClipTime);
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool PlayAnimationByGPUHash(Animator animator, int hash, float timestamp, float clipLength, float startTime = 0)
            {
                var animationState = GetAnimationState(hash);

                if (animationState != AnimationState.Default)
                {
                    return PlayAnimation(animator, animationState, timestamp, clipLength, startTime);
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool PlayAnimation(Animator animator, AnimationState animationState)
            {
                var key = (int)animationState;

                if (legacyAnimationData.ContainsKey(key))
                {
                    var animData = legacyAnimationData[key];

                    PlayAnimation(animator, animData);
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ExitAnimation(Animator animator, AnimationState animationState)
            {
                var key = (int)animationState;

                if (legacyExitAnimationData.ContainsKey(key))
                {
                    var animData = legacyExitAnimationData[key];
                    SetParam(animator, animData.ExitParamType, animData.ExitParamNameHash, animData.ExitParamValue);
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool PlayGPUAnimation(ref SkinUpdateComponent skinUpdateComponent, AnimationState animationState, float startTime = 0)
            {
                var key = (int)animationState;

                if (gpuAnimationData.ContainsKey(key))
                {
                    var animData = gpuAnimationData[key];

                    skinUpdateComponent.StartTime = startTime;
                    skinUpdateComponent.NewAnimationHash = animData.AnimationHash;
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool PlayGPUAnimation(ref SkinUpdateComponent skinUpdateComponent, ref EnabledRefRW<UpdateSkinTag> updateSkinTagRW, AnimationState animationState, float startTime = 0)
            {
                if (PlayGPUAnimation(ref skinUpdateComponent, animationState, startTime))
                {
                    updateSkinTagRW.ValueRW = true;
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AnimationState GetAnimationState(MovementState movementState)
            {
                return movementAnimationBinding[(int)movementState];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AnimationState GetAnimationState(int gpuHash)
            {
                if (gpuToLegacyBinding.ContainsKey(gpuHash))
                {
                    var animationState = gpuToLegacyBinding[gpuHash];
                    return animationState;
                }

                return AnimationState.Default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GPUAnimationDataComponent GetGPUAnimationData(MovementState movementState)
            {
                var state = movementAnimationBinding[(int)movementState];
                return GetGPUAnimationData(state);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GPUAnimationDataComponent GetGPUAnimationData(AnimationState animationState)
            {
                return gpuAnimationData[(int)animationState];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void PlayAnimation(Animator animator, LegacyAnimationDataComponent animData)
            {
                SetParam(animator, animData.ParamType1, animData.ParamNameHash1, animData.Value1);
                SetParam(animator, animData.ParamType2, animData.ParamNameHash2, animData.Value2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SetParam(Animator animator, AnimParamType animParamType, int paramNameHash, float value)
            {
                switch (animParamType)
                {
                    case AnimParamType.Bool:
                        animator.SetBool(paramNameHash, value == 1);
                        break;
                    case AnimParamType.Float:
                        animator.SetFloat(paramNameHash, value);
                        break;
                    case AnimParamType.Int:
                        animator.SetInteger(paramNameHash, (int)value);
                        break;
                }
            }
        }

        private NativeHashMap<int, LegacyAnimationDataComponent> legacyAnimationData;
        private NativeHashMap<int, LegacyAnimationDataComponent> legacyExitAnimationData;
        private NativeHashMap<int, GPUAnimationDataComponent> gpuAnimationData;
        private NativeHashMap<int, AnimationState> gpuToLegacyBinding;
        private NativeHashMap<int, AnimationState> movementAnimationBinding;

        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationDataBlobReference>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (legacyAnimationData.IsCreated) legacyAnimationData.Dispose();
            if (legacyExitAnimationData.IsCreated) legacyExitAnimationData.Dispose();
            if (gpuAnimationData.IsCreated) gpuAnimationData.Dispose();
            if (gpuToLegacyBinding.IsCreated) gpuToLegacyBinding.Dispose();
            if (movementAnimationBinding.IsCreated) movementAnimationBinding.Dispose();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var blobData = SystemAPI.GetSingleton<AnimationDataBlobReference>();

            CreateLegacyData(in blobData);
            CreateGPUData(in blobData);
            CreateMovementBinding(in blobData);

            var singleton = new Singleton()
            {
                gpuAnimationData = this.gpuAnimationData,
                legacyAnimationData = this.legacyAnimationData,
                legacyExitAnimationData = this.legacyExitAnimationData,
                gpuToLegacyBinding = this.gpuToLegacyBinding,
                movementAnimationBinding = this.movementAnimationBinding,
            };

            state.EntityManager.AddComponentData(state.SystemHandle, singleton);

            state.Enabled = false;
        }

        private void CreateLegacyData(in AnimationDataBlobReference blobData)
        {
            ref var legacyKeys = ref blobData.Config.Value.LegacyKeys;
            ref var legacyData = ref blobData.Config.Value.LegacyData;

            var exitKeys = new List<AnimationState>();
            var exitData = new List<LegacyAnimationDataComponent>();
            legacyAnimationData = new NativeHashMap<int, LegacyAnimationDataComponent>(legacyKeys.Length, Allocator.Persistent);

            for (int i = 0; i < legacyKeys.Length; i++)
            {
                legacyAnimationData.Add((int)legacyKeys[i], legacyData[i]);

                if (legacyData[i].ExitParamType != AnimParamType.None)
                {
                    exitKeys.Add(legacyKeys[i]);
                    exitData.Add(legacyData[i]);
                }
            }

            legacyExitAnimationData = new NativeHashMap<int, LegacyAnimationDataComponent>(exitKeys.Count, Allocator.Persistent);

            for (int i = 0; i < exitKeys.Count; i++)
            {
                legacyExitAnimationData.Add((int)exitKeys[i], exitData[i]);
            }
        }

        private void CreateGPUData(in AnimationDataBlobReference blobData)
        {
            ref var gpuKeys = ref blobData.Config.Value.GPUKeys;
            ref var gpuData = ref blobData.Config.Value.GPUData;

            gpuAnimationData = new NativeHashMap<int, GPUAnimationDataComponent>(gpuKeys.Length, Allocator.Persistent);
            gpuToLegacyBinding = new NativeHashMap<int, AnimationState>(gpuKeys.Length, Allocator.Persistent);

            for (int i = 0; i < gpuKeys.Length; i++)
            {
                gpuAnimationData.Add((int)gpuKeys[i], gpuData[i]);
                gpuToLegacyBinding.Add(gpuData[i].AnimationHash, gpuKeys[i]);
            }
        }

        private void CreateMovementBinding(in AnimationDataBlobReference blobData)
        {
            ref var movementKeys = ref blobData.Config.Value.MovementKeys;
            ref var movementValues = ref blobData.Config.Value.MovementValues;

            movementAnimationBinding = new NativeHashMap<int, AnimationState>(movementKeys.Length, Allocator.Persistent);

            for (int i = 0; i < movementKeys.Length; i++)
            {
                movementAnimationBinding.Add((int)movementKeys[i], movementValues[i]);
            }
        }
    }
}