﻿using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Sound.Authoring
{
    public class SoundEntityAuthoring : MonoBehaviour
    {
        [Button]
        public void s()
        {
            var lol = World.DefaultGameObjectInjectionWorld.Unmanaged.GetAllSystems(Allocator.TempJob);
            //var lol = World.DefaultGameObjectInjectionWorld.Unmanaged.GetAllUnmanagedSystems(Allocator.TempJob);

            for (int i = 0; i < lol.Length; i++)
            {
                var state = lol[i];
                //ref var stateRef = ref World.DefaultGameObjectInjectionWorld.
                ref var stateRef = ref World.DefaultGameObjectInjectionWorld.Unmanaged.ResolveSystemStateRef(state);

                try
                {
                    var hdn = stateRef.SystemHandle;
                    var hdn2 = stateRef.GlobalSystemVersion;
                    //var gg = stateRef.UnmanagedMetaIndex;
                }
                catch
                {
                    UnityEngine.Debug.Log($"{stateRef.DebugName}");
                }
            }

            lol.Dispose();
        }

        class SoundEntityAuthoringBaker : Baker<SoundEntityAuthoring>
        {
            public override void Bake(SoundEntityAuthoring authoring)
            {
                CreateDefault();

                CreateOneshot();

                CreateTracking();

                CreateTrackingVehicle();

                CreateTrackingAndLoop();
            }

            private void CreateDefault()
            {
                var defaultEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                AddDefaultSoundComponents(defaultEntity, SoundType.Default);
            }

            private void CreateOneshot()
            {
                var oneshotEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                AddDefaultSoundComponents(oneshotEntity, SoundType.OneShot);

                AddComponent(oneshotEntity, typeof(OneShot));
            }

            private void CreateTracking()
            {
                var trackingEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                AddDefaultSoundComponents(trackingEntity, SoundType.Tracking);

                AddComponent(trackingEntity, typeof(TrackSoundComponent));

                AddPoolComponents(trackingEntity);
            }

            private void CreateTrackingVehicle()
            {
                var trackingVehicleEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                AddDefaultSoundComponents(trackingVehicleEntity, SoundType.TrackingVehicle);

                AddComponent(trackingVehicleEntity, typeof(TrackSoundComponent));
                AddComponent(trackingVehicleEntity, typeof(CarInitSoundEntity));
                AddComponent(trackingVehicleEntity, typeof(CarInitSoundEntityTag));

                AddPoolComponents(trackingVehicleEntity);
            }

            private void CreateTrackingAndLoop()
            {
                var trackingLoopEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                AddDefaultSoundComponents(trackingLoopEntity, SoundType.TrackingAndLoop);

                AddComponent(trackingLoopEntity, typeof(TrackSoundComponent));
                AddComponent(trackingLoopEntity, typeof(LoopSoundData));

                AddPoolComponents(trackingLoopEntity);
            }

            private void AddDefaultSoundComponents(Entity entity, SoundType soundType)
            {
                AddComponent(entity, new ComponentTypeSet(
                    typeof(SoundComponent),
                    typeof(LocalToWorld),
                    typeof(LocalTransform),
                    typeof(SoundEventComponent),
                    typeof(Prefab)));

                AddComponent(entity, typeof(SoundCacheVolume));

                AddComponent(entity, new SoundVolume()
                {
                    Volume = 1f,
                    Pitch = 1f
                });

                AddSharedComponent(entity, new SoundSharedType()
                {
                    SoundType = soundType
                });
            }

            private void AddPoolComponents(Entity trackingEntity)
            {
#if FMOD
                PoolEntityUtils.AddPoolComponents(this, trackingEntity, EntityWorldType.PureEntity);
#else
                PoolEntityUtils.AddPoolComponents(this, trackingEntity, EntityWorldType.HybridEntity);
#endif
            }
        }
    }
}
