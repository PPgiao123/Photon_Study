﻿using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class EmptyInteractCarService : INpcInteractCarService
    {
        public INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject sourceNpc = null, bool driver = false)
        {
            throw new System.NotImplementedException();
        }

        public GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver)
        {
            throw new System.NotImplementedException();
        }
    }
}