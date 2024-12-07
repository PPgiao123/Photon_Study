using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public abstract class PcCarMotionInputBase : ICarMotionInput
    {
        private const string HorizontalAxisName = "Horizontal";
        private const string VerticalAxisName = "Vertical";

        public virtual Vector2 GetMovementInput(Vector3 forward)
        {
            float vertical = Input.GetAxis(VerticalAxisName);
            float horizontal = Input.GetAxis(HorizontalAxisName);

            return new Vector2(horizontal, vertical);
        }

        public abstract Vector3 FireInput { get; }

        public bool Brake => Input.GetKey(KeyCode.Space);
    }
}