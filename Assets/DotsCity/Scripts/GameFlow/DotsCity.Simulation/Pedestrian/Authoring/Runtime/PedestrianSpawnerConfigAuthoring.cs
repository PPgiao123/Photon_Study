using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianSpawnerConfigAuthoring : RuntimeEntityConfigBase
    {
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        private BlobAssetReference<PedestrianSpawnSettings> blobRef;

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
            PedestrianSpawnerConfig config = null;

            if (pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig)
            {
                config = pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig;
            }

            if (!config)
                return;

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PedestrianSpawnSettings>();

                root.MinPedestrianCount = config.MinPedestrianCount;
                root.MaxPedestrianPerNode = config.MaxPedestrianPerNode;
                root.MinSpawnDelay = config.MinSpawnDelay;
                root.MaxSpawnDelay = config.MaxSpawnDelay;

                blobRef = builder.CreateBlobAssetReference<PedestrianSpawnSettings>(Unity.Collections.Allocator.Persistent);

                dstManager.AddComponentData(entity, new PedestrianSpawnSettingsReference() { Config = blobRef });
            }
        }

        protected override void OnConfigUpdatedInternal()
        {
            base.OnConfigUpdatedInternal();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().UpdateSpawnConfig();
        }
    }
}
