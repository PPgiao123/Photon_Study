﻿#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Road;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNodeAvailableDebugger : ITrafficNodeDebugger
    {
        private EntityManager entityManager;

        public TrafficNodeAvailableDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public void Tick(Entity entity)
        {
            var nodePosition = entityManager.GetComponentData<LocalToWorld>(entity).Position;
            var isAvailable = entityManager.IsComponentEnabled<TrafficNodeAvailableTag>(entity);
            Gizmos.color = isAvailable ? Color.green : Color.red;
            Gizmos.DrawWireSphere(nodePosition, 1f);
        }
    }
}
#endif