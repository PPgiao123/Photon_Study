using Spirit604.Gameplay.UI;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class TopDownMobileMotionInput : IMotionInput
    {
        private readonly InputManager inputManager;
        private readonly Camera mainCamera;

        public TopDownMobileMotionInput(InputManager inputManager, Camera camera)
        {
            this.inputManager = inputManager;
            this.mainCamera = camera;
        }

        public Vector3 MovementInput
        {
            get
            {
                var cameraRotation = Quaternion.Euler(0, mainCamera.transform.rotation.eulerAngles.y, 0);

#if UNITY_EDITOR
                var h = Input.GetAxis("Horizontal");
                var v = Input.GetAxis("Vertical");

                if (h != 0 || v != 0)
                    return cameraRotation * new Vector3(h, 0, v);
#endif

                return cameraRotation * inputManager.GetMove(0);
            }
        }

        public Vector3 FireInput => inputManager.GetMove(1);
    }
}