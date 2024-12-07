using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileGameInput : TileGameInputBase
    {
        public override bool EscapeClicked => Input.GetKeyDown(KeyCode.Escape);

        public override bool ActionClicked => Input.GetKeyDown(KeyCode.E);

        public override bool RotateClicked => Input.GetKeyDown(KeyCode.CapsLock);

        public override Vector3 GetMousePosition()
        {
            var mousePosition = Input.mousePosition;

            var ray = Camera.main.ScreenPointToRay(mousePosition);
            return GetCenterOfSceneView(ray);
        }

        private Vector3 GetCenterOfSceneView()
        {
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
            return GetCenterOfSceneView(ray);
        }

        private Vector3 GetCenterOfSceneView(Ray ray)
        {
            Vector3 worldPosition = Vector3.zero;

            Plane hPlane = new Plane(Vector3.up, Vector3.zero);

            // Plane.Raycast stores the distance from ray.origin to the hit point in this variable:
            float distance = 0;

            // if the ray hits the plane...
            if (hPlane.Raycast(ray, out distance))
            {
                // get the hit point:
                worldPosition = ray.GetPoint(distance).Flat();
            }

            return worldPosition;
        }
    }
}
