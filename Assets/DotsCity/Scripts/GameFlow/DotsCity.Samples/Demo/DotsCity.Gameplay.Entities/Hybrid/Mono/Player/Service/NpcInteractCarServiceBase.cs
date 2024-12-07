using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public abstract class NpcInteractCarServiceBase : INpcInteractCarService
    {
        protected readonly INpcInCarFactory npcInCarFactory;
        protected readonly INpcFactory npcFactory;

        public NpcInteractCarServiceBase(INpcInCarFactory npcInCarFactory, INpcFactory npcFactory)
        {
            this.npcInCarFactory = npcInCarFactory;
            this.npcFactory = npcFactory;
        }

        public virtual INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject enteredNpc = null, bool driver = false)
        {
            if (!sourceSlot)
                return null;

            INpcInCar newNpc = GetNewNpc(sourceSlot, npcId);

            if (newNpc != null)
            {
                ProcessEnteredNpc(sourceSlot, enteredNpc);
            }

            return newNpc;
        }

        public virtual GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver)
        {
            return Exit(npcFactory, sourceSlot, npcId, spawnPosition, spawnRotation);
        }

        protected virtual GameObject Exit(INpcFactory npcFactory, CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var exitingNpc = sourceSlot.EnteredSourceNpc;

            if (exitingNpc == null)
            {
                exitingNpc = npcFactory.Get(npcId, spawnPosition, spawnRotation);
            }
            else
            {
                exitingNpc.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                exitingNpc.transform.SetParent(sourceSlot.EnteredSourceNpcParent);
                exitingNpc.gameObject.SetActive(true);
            }

            return exitingNpc;
        }

        protected virtual INpcInCar GetNewNpc(CarSlot sourceSlot, string npcId)
        {
            var newNpc = npcInCarFactory.GetNpc(npcId);

            if (newNpc != null)
            {
                newNpc.Transform.SetParent(sourceSlot.transform);
                newNpc.Transform.localPosition = Vector3.zero;
                newNpc.Transform.rotation = Quaternion.identity;
                newNpc.Initialize(sourceSlot);
                newNpc.SnapHide();
                sourceSlot.NpcInCar = newNpc;
            }

            return newNpc;
        }

        protected virtual void ProcessEnteredNpc(CarSlot sourceSlot, GameObject enteredNpc)
        {
            if (enteredNpc != null)
            {
                sourceSlot.EnteredSourceNpc = enteredNpc;
                enteredNpc.transform.SetParent(sourceSlot.transform);
                enteredNpc.gameObject.SetActive(false);
            }
        }
    }
}