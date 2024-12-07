using Spirit604.Gameplay.InputService;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class OrbitalPcInput : PcCarMotionInputBase
    {
        private Vector3 startPos;
        private bool started;

        public override Vector3 FireInput => GetFireInput();

        public void Tick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                startPos = Input.mousePosition;
                started = true;
            }
            else
            if (Input.GetMouseButtonUp(0))
            {
                started = false;
            }
        }

        private Vector3 GetFireInput()
        {
            if (started)
            {
                var offset = (Input.mousePosition - startPos) / 1000;
                offset = new Vector3(Mathf.Clamp(offset.x, -1, 1), 0, Mathf.Clamp(offset.y, -1, 1));
                return offset;
            }

            return default;
        }
    }
}