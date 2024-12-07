﻿using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class KeyContainerPressing : KeyContainerBase
    {
        public override bool Fire(KeyCode key) => Input.GetKey(key);
    }
}