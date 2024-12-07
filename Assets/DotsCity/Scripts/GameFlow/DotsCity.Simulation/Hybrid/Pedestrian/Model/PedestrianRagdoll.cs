using Spirit604.DotsCity.Hybrid.Core;
using System;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianRagdoll : RagdollBase
    {
        private float SwitchTime;
        private bool isActivated;

        public float RagdollEnabledDuration { get; set; } = 5f;

        public Action<PedestrianRagdoll> OnLifeCycleExpired = delegate { };

        public void DoUpdate()
        {
            if (!isActivated)
            {
                return;
            }

            if (UnityEngine.Time.time > SwitchTime)
            {
                isActivated = false;
                OnLifeCycleExpired(this);
            }
        }

        protected override void SwitchActiveState(bool isActive)
        {
            base.SwitchActiveState(isActive);

            isActivated = isActive;

            if (isActive)
            {
                SwitchTime = UnityEngine.Time.time + RagdollEnabledDuration;
            }
        }
    }
}