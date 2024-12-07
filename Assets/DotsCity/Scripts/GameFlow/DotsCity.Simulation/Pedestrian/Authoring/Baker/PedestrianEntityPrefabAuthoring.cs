using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public struct PedestrianEntityPrefabComponent : IComponentData
    {
        public Entity PrefabEntity;
    }

    public class PedestrianEntityPrefabAuthoring : MonoBehaviourBase, ISyncableConfig
    {
        [SerializeField] private PedestrianSettingsConfig pedestrianSettingsConfig;

        [SerializeField] private CitySettingsInitializerBase citySettingsInitializerBase;

        [Expandable]
        [SerializeField] private PedestrianPrefabEntityData pedestrianPrefabEntityData;

        public PedestrianSettingsConfig PedestrianSettingsConfig
        {
            get => pedestrianSettingsConfig;
            set
            {
                if (pedestrianSettingsConfig != value)
                {
                    pedestrianSettingsConfig = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public GeneralSettingDataSimulation GeneralSettings
        {
            get => citySettingsInitializerBase.GetSettings<GeneralSettingDataSimulation>();
        }

        public PedestrianPrefabEntityData PedestrianPrefabEntityData
        {
            get => pedestrianPrefabEntityData;
            set
            {
                if (pedestrianPrefabEntityData != value)
                {
                    pedestrianPrefabEntityData = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        [Button]
        public void SyncConfig()
        {
            var pedestrianSpawnerConfigHolder = ObjectUtils.FindObjectOfType<PedestrianSpawnerConfigHolder>();

            if (pedestrianSpawnerConfigHolder)
            {
                PedestrianSettingsConfig = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;
                PedestrianPrefabEntityData = pedestrianSpawnerConfigHolder.PedestrianPrefabEntityData;
            }

            if (!citySettingsInitializerBase)
            {
                citySettingsInitializerBase = ObjectUtils.FindObjectOfType<CitySettingsInitializerBase>();
            }
        }

        class PedestrianEntityPrefabAuthoringBaker : Baker<PedestrianEntityPrefabAuthoring>
        {
            public override void Bake(PedestrianEntityPrefabAuthoring authoring)
            {
                DependsOn(authoring.GeneralSettings);
                DependsOn(authoring.pedestrianSettingsConfig);
                DependsOn(authoring.pedestrianPrefabEntityData);

                var prefab = authoring.GetPrefab();

                var prefabEntity = Entity.Null;

                if (prefab != null)
                {
                    prefabEntity = GetEntity(prefab, TransformUsageFlags.Dynamic);
                }

                var spawnerData = new PedestrianEntityPrefabComponent
                {
                    PrefabEntity = prefabEntity,
                };

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, spawnerData);
            }
        }

        private GameObject GetPrefab()
        {
            var pedestrianEntityType = PedestrianSettingsConfig.PedestrianEntityType;

            if (GeneralSettings && PedestrianSettingsConfig.PedestrianEntityType == EntityType.Physics && this.GeneralSettings.SimulationType == Unity.Physics.SimulationType.NoPhysics)
            {
                pedestrianEntityType = EntityType.NoPhysics;
            }

            GameObject sourcePrefab = null;

            if (pedestrianPrefabEntityData.PedestrianEntityPrefabData.TryGetValue(pedestrianEntityType, out var sourceData) && sourceData != null)
            {
                sourcePrefab = sourceData.gameObject;
            }
            else
            {
                UnityEngine.Debug.LogError($"PedestrianEntityPrefabAuthoring. PedestrianEntityType {pedestrianEntityType} not found!");
            }

            return sourcePrefab;
        }
    }
}