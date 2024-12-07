using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Bootstrap;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.DotsCity.Gameplay.Weapon.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Weapons;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory
{
    public class BulletEntityFactory : MonoBehaviour, IBulletFactory
    {
        private EntityManager entityManager;
        private Dictionary<BulletType, Entity> bulletPrefabs = new Dictionary<BulletType, Entity>();

        private void Awake()
        {
            SceneBootstrap.OnEntityLoaded += SceneBootstrap_OnEntityLoaded;
        }

        private void OnDestroy()
        {
            SceneBootstrap.OnEntityLoaded -= SceneBootstrap_OnEntityLoaded;
        }

        public void SpawnBullet(Vector3 heading, Vector3 spawnPosition, BulletType bulletType, FactionType factionType)
        {
            var prefab = bulletPrefabs[bulletType];

            var instance = entityManager.Instantiate(prefab);
            InitializeBullet(instance, heading, spawnPosition, factionType);
        }

        public void Initialize()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var prefabQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<BulletEntityPrefab>());

            var prefabs = prefabQuery.ToComponentDataArray<BulletEntityPrefab>(Unity.Collections.Allocator.TempJob);

            for (int i = 0; i < prefabs.Length; i++)
            {
                var bulletType = prefabs[i].BulletType;
                var prefabEntity = prefabs[i].PrefabEntity;

                bulletPrefabs.Add(bulletType, prefabEntity);
            }

            prefabs.Dispose();
        }

        private void InitializeBullet(Entity entity, float3 heading, float3 spawnPosition, FactionType factionType)
        {
            heading = heading.Flat();
            entityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(spawnPosition, Quaternion.LookRotation(heading)));
            entityManager.SetComponentData(entity, new BulletComponent() { FactionType = factionType });
        }

        private void SceneBootstrap_OnEntityLoaded()
        {
            Initialize();
        }
    }
}