using Spirit604.Attributes;
using Spirit604.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianRagdollSpawner : MonoBehaviour
    {
        [SerializeField][Range(1f, 20f)] private float ragdollEnabledDuration = 5;

        private List<PedestrianRagdoll> activatedRagdolls = new List<PedestrianRagdoll>();
        private Coroutine updateCoroutine = null;

        private IPedestrianRagdollFactory pedestrianRagdollFactory;

        [InjectWrapper]
        public void Construct(IPedestrianRagdollFactory pedestrianRagdollFactory)
        {
            this.pedestrianRagdollFactory = pedestrianRagdollFactory;
        }

        public void SpawnRagdoll(int skinIndex, Vector3 position, Quaternion rotation, Vector3 forceDirection, float forceMultiplier = 1)
        {
            var ragdoll = pedestrianRagdollFactory.SpawnRagdoll(skinIndex);

            if (ragdoll == null)
            {
                Debug.LogError($"Ragdoll not found. SkinIndex {skinIndex}");
                return;
            }

            ragdoll.transform.position = position;
            ragdoll.transform.rotation = rotation;
            ragdoll.RagdollEnabledDuration = ragdollEnabledDuration;
            ragdoll.OnLifeCycleExpired += PedestrianRagdoll_OnLifeCycleExpired;
            AddRagdoll(ragdoll);

            forceMultiplier = Mathf.Clamp(forceMultiplier, 1f, 5f);
            ragdoll.SwitchActiveState(true, forceDirection, forceMultiplier);

            if (updateCoroutine == null)
            {
                updateCoroutine = StartCoroutine(InternalUpdate());
            }
        }

        private IEnumerator InternalUpdate()
        {
            while (true)
            {
                for (int i = 0; i < activatedRagdolls.Count; i++)
                {
                    activatedRagdolls[i].DoUpdate();
                }

                yield return null;
            }
        }

        private void AddRagdoll(PedestrianRagdoll ragdoll)
        {
            activatedRagdolls.TryToAdd(ragdoll);
        }

        private void RemoveRagdoll(PedestrianRagdoll ragdoll)
        {
            activatedRagdolls.TryToRemove(ragdoll);

            if (activatedRagdolls.Count == 0 && updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
        }

        #region Event handlers

        private void PedestrianRagdoll_OnLifeCycleExpired(PedestrianRagdoll ragdoll)
        {
            ragdoll.OnLifeCycleExpired -= PedestrianRagdoll_OnLifeCycleExpired;
            RemoveRagdoll(ragdoll);
            ragdoll.gameObject.ReturnToPool();
        }

        #endregion
    }
}