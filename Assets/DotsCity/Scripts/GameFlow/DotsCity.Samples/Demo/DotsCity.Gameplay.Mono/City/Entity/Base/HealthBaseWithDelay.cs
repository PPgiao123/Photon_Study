using System;
using System.Collections;
using UnityEngine;

namespace Spirit604.Gameplay
{
    public abstract class HealthBaseWithDelay : HealthBase
    {
        [SerializeField][Range(0f, 100f)] private float hideTime = 3f;

        public float HideTime { get => hideTime; protected set => hideTime = value; }

        protected override void ActivateDeathVFX()
        {
            StartCoroutine(DeathDelay());
        }

        protected override void PostDeathAction()
        {
            Death();
            ActivateDeathVFX();
        }

        private IEnumerator DeathDelay()
        {
            yield return new WaitForSeconds(hideTime);
            DeathVfxFinished();
        }
    }
}