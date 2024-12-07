using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.Extensions;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class PlayerNpcInteractCarService : NpcInteractCarServiceBase
    {
        private readonly PlayerActorTracker playerActorTracker;
        private readonly PlayerSession playerSession;
        private readonly IPlayerNpcFactory playerNpcFactory;
        private EntityManager entityManager;
        private EntityQuery playerGroup;

        public PlayerNpcInteractCarService(
            PlayerActorTracker playerActorTracker,
            PlayerSession playerSession,
            IPlayerNpcFactory playerNpcFactory,
            INpcInCarFactory npcInCarFactory,
            INpcFactory playerMobNpcFactory) :
            base(npcInCarFactory, playerMobNpcFactory)
        {
            this.playerActorTracker = playerActorTracker;
            this.playerSession = playerSession;
            this.playerNpcFactory = playerNpcFactory;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            playerGroup = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlayerNpcComponent>(),
                    },
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlayerTag>(),
                    }
                });
        }

        public override INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject enteredNpc = null, bool driver = false)
        {
            var npc = base.Enter(sourceSlot, npcId, enteredNpc, driver);

            if (npc != null)
            {
                if (enteredNpc != null)
                {
                    var sourceNpcBehaviour = enteredNpc.GetComponent<NpcBehaviourBase>();
                    var npcInCar = sourceSlot.NpcInCarTransform.GetComponent<NpcInCar>();
                    playerSession.UpdateLinkNpc(sourceNpcBehaviour, npcInCar);
                }
                else
                {
                    playerSession.LinkNpc(sourceSlot.NpcInCarTransform.GetComponent<NpcInCar>());
                }

                playerSession.LastCar = playerActorTracker.PlayerEntity;
                playerSession.SwitchWeaponHidedState(true);

                if (driver)
                {
                    playerActorTracker.Actor = sourceSlot.CarParent.transform;
                }
            }

            return npc;
        }

        public override GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver)
        {
            var factory = isDriver ? playerNpcFactory : npcFactory;
            var exitingNpc = base.Exit(factory, sourceSlot, npcId, spawnPosition, spawnRotation);

            var exitingNpcEntity = exitingNpc.GetComponent<NpcBehaviourBase>();

            if (exitingNpcEntity)
            {
                var npcInCar = sourceSlot.NpcInCarTransform.GetComponent<NpcBehaviourBase>();
                exitingNpcEntity.Initialize(spawnPosition, sourceSlot.Index);
                playerSession.UpdateLinkNpc(npcInCar, exitingNpcEntity);

                if (isDriver)
                {
                    var newPlayerCarEntity = playerGroup.GetSingletonEntity();
                    playerSession.LastCar = newPlayerCarEntity;
                }
            }

            if (isDriver)
                playerActorTracker.Actor = exitingNpc.transform;

            return exitingNpc;
        }

        protected override void ProcessEnteredNpc(CarSlot sourceSlot, GameObject enteredNpc)
        {
            if (!enteredNpc)
                return;

            var enteredNpcBehaviour = enteredNpc.GetComponent<NpcBehaviourBase>();

            if (enteredNpcBehaviour != null)
            {
                enteredNpcBehaviour.DestroyEntity();
                enteredNpcBehaviour.gameObject.ReturnToPool();
            }
            else
            {
                base.ProcessEnteredNpc(sourceSlot, enteredNpc);
            }
        }
    }
}