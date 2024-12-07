using Spirit604.Gameplay.UI;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class TopDownOrbitalMobileCarMotionInput : ICarMotionInput
    {
        private const float MAX_ROTATE_ANGLE = 90f;
        private const float MAX_JOYSTICK_FORWARD_ANGLE = 100f;

        private readonly InputManager inputManager;

        public TopDownOrbitalMobileCarMotionInput(InputManager inputManager)
        {
            this.inputManager = inputManager;
        }

        public Vector2 GetMovementInput(Vector3 forward)
        {
#if UNITY_EDITOR
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            if (h != 0 || v != 0)
                return new Vector2(h, v);
#endif

            var moveVector = inputManager.GetMove(0);

            float signedAngle = Vector3.SignedAngle(forward, moveVector, Vector3.up);

            int side = signedAngle > 0 ? 1 : -1;

            var InputDirection = Mathf.Abs(signedAngle) < MAX_JOYSTICK_FORWARD_ANGLE ? 1 : -1;

            if (InputDirection == -1)
            {
                signedAngle = Vector3.SignedAngle(-forward, moveVector, Vector3.up) * -1;
            }

            var vertical = moveVector.magnitude * InputDirection;

            signedAngle = Mathf.Clamp(signedAngle, -MAX_ROTATE_ANGLE, MAX_ROTATE_ANGLE);

            float rotateForce = 0;

            rotateForce = signedAngle / MAX_ROTATE_ANGLE;

            var horizontal = rotateForce;

            return new Vector2(horizontal, vertical);
        }

        public Vector3 FireInput => inputManager.GetMove(1);

        public bool Brake => false;
    }
}
