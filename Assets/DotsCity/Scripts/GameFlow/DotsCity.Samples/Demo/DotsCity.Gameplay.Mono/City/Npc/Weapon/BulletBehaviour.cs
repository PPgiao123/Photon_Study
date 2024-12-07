using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Weapons
{
    public class BulletBehaviour : MonoBehaviour
    {
        [SerializeField][Range(0.01f, 50f)] private float speed = 16;
        [SerializeField][Range(0.01f, 30f)] private float lifetime = 2f;
        [SerializeField][Range(1, 100)] private int damage = 1;
        [SerializeField] private LayerMask castMask;

        private FactionType factionType;
        private float disableTime;

        private void FixedUpdate()
        {
            var dist = speed * Time.fixedDeltaTime;
            var dt = transform.forward * dist;

            if (Physics.Raycast(transform.position, transform.forward, out var hit, dist, castMask.value, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.TryGetComponent<IHealth>(out var health))
                {
                    bool destroy = true;
                    var flag = (int)factionType;

                    if (hit.collider.gameObject.TryGetComponent<IFactionProvider>(out var factionProvider) && flag != 0)
                    {
                        destroy = !factionProvider.FactionType.HasFlag(factionType);
                    }

                    if (destroy)
                    {
                        health.TakeDamage(damage, hit.point, transform.forward);
                        Destroy();
                    }
                }
            }

            Move(dt);
            CheckLifetime();
        }

        public void Initialize(Vector3 heading, Vector3 spawnPosition, FactionType factionType)
        {
            this.factionType = factionType;
            this.disableTime = Time.time + lifetime;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(heading));
        }

        private void Move(Vector3 dt)
        {
            transform.position += dt;
        }

        private void CheckLifetime()
        {
            if (Time.time >= disableTime)
            {
                Destroy();
            }
        }

        private void Destroy()
        {
            gameObject.ReturnToPool();
        }
    }
}
