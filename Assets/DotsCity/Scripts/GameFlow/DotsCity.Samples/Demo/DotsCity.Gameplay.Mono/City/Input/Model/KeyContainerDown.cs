using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class KeyContainerDown : KeyContainerBase
    {
        public override bool Fire(KeyCode key) => Input.GetKeyDown(key);
    }
}