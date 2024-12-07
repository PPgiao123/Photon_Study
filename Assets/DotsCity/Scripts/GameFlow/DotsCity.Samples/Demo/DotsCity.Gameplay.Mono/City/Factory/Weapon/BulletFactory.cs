using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Weapons;
using UnityEngine;

namespace Spirit604.Gameplay.Factory
{
    public class BulletFactory : SimpleEnumKeyFactoryBase<BulletType, BulletBehaviour>, IBulletFactory
    {
        public void SpawnBullet(Vector3 heading, Vector3 spawnPosition, BulletType bulletType, FactionType factionType)
        {
            var bullet = Get(bulletType);
            bullet.Initialize(heading, spawnPosition, factionType);
        }
    }
}
