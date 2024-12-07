using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class PcMotionInput : IMotionInput
    {
        protected readonly Camera mainCamera;

        public PcMotionInput(Camera camera)
        {
            this.mainCamera = camera;
        }

        public virtual Vector3 MovementInput
        {
            get
            {
                var h = Input.GetAxis("Horizontal");
                var v = Input.GetAxis("Vertical");

                return new Vector3(h, 0, v);
            }
        }

        public virtual Vector3 FireInput
        {
            get
            {
                if (Input.GetMouseButton(0))
                {
                    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                    Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance = 0;

                    Vector3 worldPosition = Vector3.zero;

                    if (hPlane.Raycast(ray, out distance))
                    {
                        worldPosition = ray.GetPoint(distance).Flat();
                    }

                    return worldPosition;
                }

                return default;
            }
        }
    }
}