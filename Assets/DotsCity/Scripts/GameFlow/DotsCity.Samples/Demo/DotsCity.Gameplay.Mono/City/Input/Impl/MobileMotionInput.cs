using Spirit604.Gameplay.UI;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class MobileMotionInput : IMotionInput
    {
        private readonly InputManager inputManager;

        public MobileMotionInput(InputManager inputManager)
        {
            this.inputManager = inputManager;
        }

        public Vector3 MovementInput
        {
            get
            {
#if UNITY_EDITOR
                var h = Input.GetAxis("Horizontal");
                var v = Input.GetAxis("Vertical");

                if (h != 0 || v != 0)
                    return new Vector3(h, 0, v);
#endif

                return inputManager.GetMove(0);
            }
        }

        public Vector3 FireInput => inputManager.GetMove(1);
    }
}