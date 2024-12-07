﻿using Spirit604.Gameplay.Factory;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public interface ICarSlots
    {
        bool HasSlots { get; }

        CarSlot EnterCar(string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onNpcEntered = null);
        void ExitCarAll(bool includeDrive = false, Action<GameObject, bool> onNpcExit = null);

        GameObject ExitDriver();

        void Initialize(INpcInteractCarService npcInteractCarService);
    }
}