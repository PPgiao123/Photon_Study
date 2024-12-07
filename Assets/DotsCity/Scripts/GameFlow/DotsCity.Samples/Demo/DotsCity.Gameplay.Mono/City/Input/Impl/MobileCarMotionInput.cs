using Spirit604.Extensions;
using Spirit604.Gameplay.UI;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class MobileCarMotionInput : ICarMotionInput
    {
        private readonly InputManager inputManager;

        public MobileCarMotionInput(InputManager inputManager)
        {
            this.inputManager = inputManager;
        }

        public Vector2 GetMovementInput(Vector3 forward)
        {
#if UNITY_EDITOR
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            if (h != 0 || v != 0)
                return new Vector3(h, v, 0);
#endif

            return inputManager.GetMove(0).ToVector2_2DSpace();
        }

        public Vector3 FireInput => inputManager.GetMove(1);

        public bool Brake => inputManager.Brake;
    }
}