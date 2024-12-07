using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficGeneralSettingsData
    {
        public EntityBakingType EntityBakingType;
        public bool HasTraffic;
        public bool ChangeLaneSupport;
        public bool AntiStuckSupport;
        public bool AvoidanceSupport;
        public bool RailSupport;
        public bool CarVisualDamageSystemSupport;
        public bool WheelSystemSupport;
    }

    public struct TrafficGeneralSettingsReference : IComponentData
    {
        public BlobAssetReference<TrafficGeneralSettingsData> Config;
    }
}
