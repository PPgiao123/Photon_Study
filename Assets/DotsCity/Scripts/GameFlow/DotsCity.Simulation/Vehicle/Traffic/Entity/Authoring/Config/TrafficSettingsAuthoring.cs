using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficSettingsAuthoring : RuntimeConfigUpdater<TrafficSettingsConfigBlobReference, TrafficSettingsConfigBlob>
    {
        [ShowIfNull]
        [SerializeField] private TrafficSettings trafficSettings;

        protected override bool UpdateAvailableByDefault => false;
        protected override bool AutoSync => false;

        public override void Initialize(bool recreateOnStart)
        {
            base.Initialize(recreateOnStart);

#if UNITY_EDITOR
            var trafficSettingsConfig = trafficSettings.TrafficSettingsConfig;
            trafficSettingsConfig.OnTrafficSettingsChanged += TrafficSettingsConfig_OnTrafficSettingsChanged;
#endif
        }

        public override TrafficSettingsConfigBlobReference CreateConfig(BlobAssetReference<TrafficSettingsConfigBlob> blobRef)
        {
            return new TrafficSettingsConfigBlobReference()
            {
                Reference = CreateBlobStatic(trafficSettings)
            };
        }

        protected override BlobAssetReference<TrafficSettingsConfigBlob> CreateConfigBlob()
        {
            return CreateBlobStatic(trafficSettings);
        }

        public static TrafficSettingsConfigBlobReference CreateConfigStatic(IBaker baker, TrafficSettings trafficSettings)
        {
            var blobRef = CreateBlobStatic(trafficSettings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new TrafficSettingsConfigBlobReference()
            {
                Reference = blobRef
            };
        }

        public static BlobAssetReference<TrafficSettingsConfigBlob> CreateBlobStatic(TrafficSettings trafficSettings)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficSettingsConfigBlob>();

                root.MaxSpeed = trafficSettings.TrafficSettingsConfig.MaxCarSpeed;
                root.Acceleration = trafficSettings.TrafficSettingsConfig.Acceleration;
                root.BackwardAcceleration = trafficSettings.TrafficSettingsConfig.BackwardAcceleration;
                root.BrakePower = trafficSettings.TrafficSettingsConfig.BrakePower;
                root.BrakingRate = trafficSettings.TrafficSettingsConfig.BrakingRate;
                root.MaxSteerAngle = trafficSettings.TrafficSettingsConfig.MaxSteerAngle;
                root.MaxSteerDirectionAngle = trafficSettings.TrafficSettingsConfig.MaxSteerDirectionAngle;
                root.UseSteeringDamping = trafficSettings.TrafficSettingsConfig.UseSteeringDamping;
                root.SteeringDamping = trafficSettings.TrafficSettingsConfig.SteeringDamping;
                root.HasRotationLerp = trafficSettings.TrafficSettingsConfig.HasRotationLerp;
                root.HealthCount = trafficSettings.TrafficSettingsConfig.HealthCount;
                root.HasNavMeshObstacle = trafficSettings.TrafficSettingsConfig.HasNavObstacle;

                return builder.CreateBlobAssetReference<TrafficSettingsConfigBlob>(Unity.Collections.Allocator.Persistent);
            }
        }

#if UNITY_EDITOR
        private void TrafficSettingsConfig_OnTrafficSettingsChanged()
        {
            ConfigUpdated = true;
        }
#endif
    }
}