using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianTalkSpawnerConfigAuthoring : RuntimeEntityConfigBase
    {
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        private BlobAssetReference<TalkSpawnSettings> blobRef;

        [InjectWrapper]
        public void Construct(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            this.pedestrianSpawnerConfigHolder = pedestrianSpawnerConfigHolder;
        }

        protected override bool AutoSync => false;

        public override void Dispose()
        {
            base.Dispose();

            if (blobRef.IsCreated)
            {
                blobRef.Dispose();
            }
        }

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            var config = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TalkSpawnSettings>();

                root.TalkingPedestrianSpawnChance = config.TalkingPedestrianSpawnChance;
                root.MinTalkTime = config.MinMaxTalkTime.x;
                root.MaxTalkTime = config.MinMaxTalkTime.y;

                blobRef = builder.CreateBlobAssetReference<TalkSpawnSettings>(Unity.Collections.Allocator.Persistent);

                dstManager.AddComponentData(entity, new TalkSpawnSettingsReference() { Config = blobRef });
            }
        }

        protected override void OnConfigUpdatedInternal()
        {
            base.OnConfigUpdatedInternal();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().UpdateTalkSpawnConfig();
        }
    }
}
