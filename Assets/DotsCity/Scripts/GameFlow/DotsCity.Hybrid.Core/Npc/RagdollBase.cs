using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public class RagdollBase : MonoBehaviour, IRagdoll
    {
        private const float DEFAULT_RAGDOLL_FORCE = 10f;

        private Rigidbody[] rigidbodies;
        private Collider parentCollider;
        private Rigidbody parentRigidbody;
        private Animator animator;
        private Vector3 forceDirection;

        private Collider[] colliders;
        private Vector3[] localPositions;
        private Quaternion[] localRotations;
        private float forceMultiplier = 1f;

        protected virtual void Awake()
        {
            parentCollider = GetComponent<Collider>();
            parentRigidbody = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            colliders = GetComponentsInChildren<Collider>().Where(collider => collider.transform != transform).ToArray();

            localPositions = new Vector3[colliders.Length];
            localRotations = new Quaternion[colliders.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                localPositions[i] = colliders[i].transform.localPosition;
                localRotations[i] = colliders[i].transform.localRotation;
            }

            rigidbodies = GetComponentsInChildren<Rigidbody>().Where(rb => rb != parentRigidbody).ToArray();
        }

        private void OnEnable()
        {
            SwitchActiveState(false);
        }

        public void SwitchActiveState(bool isActive, Vector3 forceDirection, float forceMultiplier = 1)
        {
            this.forceDirection = forceDirection;
            this.forceMultiplier = forceMultiplier;
            SwitchActiveState(isActive);
        }

        protected virtual void SwitchActiveState(bool isActive)
        {
            if (parentCollider)
            {
                parentCollider.enabled = !isActive;
            }

            if (parentRigidbody)
            {
                parentRigidbody.isKinematic = isActive;
                parentRigidbody.detectCollisions = !isActive;
            }

            if (animator)
            {
                animator.enabled = !isActive;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = isActive;

                if (!isActive)
                {
                    colliders[i].transform.localPosition = localPositions[i];
                    colliders[i].transform.localRotation = localRotations[i];
                }

                rigidbodies[i].isKinematic = !isActive;
                rigidbodies[i].detectCollisions = isActive;

                if (isActive && forceDirection != Vector3.zero)
                {
                    rigidbodies[i].AddForce(forceDirection * DEFAULT_RAGDOLL_FORCE * forceMultiplier, ForceMode.Impulse);
                }
            }
        }
    }
}