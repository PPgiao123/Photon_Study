using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.Gameplay.Factory
{
    public interface INpcInteractCarService
    {
        public INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject sourceNpc = null, bool driver = false);
        public GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver);
    }
}