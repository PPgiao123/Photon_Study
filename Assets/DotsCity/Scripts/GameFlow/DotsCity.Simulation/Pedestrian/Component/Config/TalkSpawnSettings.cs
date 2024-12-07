using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct TalkSpawnSettings
    {
        public float TalkingPedestrianSpawnChance;
        public float MaxTalkTime;
        public float MinTalkTime;
    }

    public struct TalkSpawnSettingsReference : IComponentData
    {
        public BlobAssetReference<TalkSpawnSettings> Config;
    }
}
