using Spirit604.Extensions;
using Spirit604.Gameplay.Factory;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public class CarSlots : MonoBehaviour, ICarSlots
    {
        [SerializeField] private CarSlot driverSlot;
        [SerializeField] private CarSlot[] carSlots;

        private INpcInteractCarService npcInteractCarService;

        public bool HasDriver => driverSlot.NpcInCar != null;

        public int SlotCount => carSlots.Length;

        public CarSlot DriverSlot => driverSlot;

        public bool HasSlots => TakenSeatsCount < carSlots.Length;

        public int TakenSeatsCount { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                carSlots[i].CarParent = transform;
                carSlots[i].Index = i;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < carSlots.Length; i++)
            {
                if (carSlots[i].NpcInCar != null)
                {
                    carSlots[i].NpcInCar.Dispose();
                    carSlots[i].NpcInCar = null;
                }
            }

            TakenSeatsCount = 0;
        }

        public void Initialize(INpcInteractCarService npcIteractCarService)
        {
            this.npcInteractCarService = npcIteractCarService;
        }

        public CarSlot EnterCar(string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onNpcEntered = null)
        {
            CarSlot emptyCarSlot = null;

            if (!driver)
            {
                if (HasSlots)
                {
                    emptyCarSlot = GetEmptySlot();
                }
            }
            else
            {
                if (!HasDriver)
                {
                    emptyCarSlot = driverSlot;
                }
            }

            if (emptyCarSlot != null)
            {
                if (npcInteractCarService.Enter(emptyCarSlot, npcId, enteredNpc, driver) != null)
                {
                    TakenSeatsCount++;

                    // If the car has async entering (e.g. animation)
                    onNpcEntered?.Invoke(emptyCarSlot);
                    return emptyCarSlot;
                }
            }

            return null;
        }

        public void ExitCarAll(bool includeDrive = false, Action<GameObject, bool> onExit = null)
        {
            if (npcInteractCarService == null)
            {
                Debug.Log($"CarSlots {name} NpcInteractCarService not found");
            }

            for (int i = 0; i < carSlots.Length; i++)
            {
                if (carSlots[i].NpcInCar == null)
                    continue;

                bool isDriver = carSlots[i] == driverSlot;

                if (!includeDrive && isDriver)
                    continue;

                var sourceNpc = carSlots[i].NpcInCar;

                if (npcInteractCarService != null)
                {
                    Vector3 spawnPosition = carSlots[i].GetSpawnPosition();
                    Quaternion spawnRotation = carSlots[i].GetSpawnRotation();

                    var exitingNpcEntity = npcInteractCarService.Exit(carSlots[i], sourceNpc.ID, spawnPosition, spawnRotation, isDriver);

                    carSlots[i].NpcInCar.Dispose();
                    carSlots[i].NpcInCar = null;
                    carSlots[i].EnteredSourceNpc = null;
                    onExit?.Invoke(exitingNpcEntity, isDriver);
                }
            }

            TakenSeatsCount = HasDriver ? 1 : 0;
        }

        public bool GetSlotSpawnTransform(int slotIndex, out Vector3 spawnPosition, out Quaternion spawnRotation)
        {
            spawnPosition = default;
            spawnRotation = default;

            var slot = GetSlot(slotIndex);

            if (slot != null)
            {
                spawnPosition = slot.GetSpawnPosition();
                spawnRotation = slot.GetSpawnRotation();
                return true;
            }

            return false;
        }

        public GameObject ExitDriver()
        {
            if (driverSlot.NpcInCar != null)
            {
                var sourceNpc = driverSlot.EnteredSourceNpc;
                driverSlot.NpcInCar.Dispose();
                driverSlot.NpcInCar = null;
                driverSlot.EnteredSourceNpc = null;
                TakenSeatsCount--;

                return sourceNpc;
            }

            return null;
        }

        public CarSlot GetSlot(int index)
        {
            if (index >= 0 && index < carSlots.Length)
            {
                return carSlots[index];
            }

            return null;
        }

        public void GenerateSlot(bool driver)
        {
#if UNITY_EDITOR
            if (driver)
            {
                if (driverSlot == null)
                {
                    driverSlot = CreateSlot();
                    EditorSaver.SetObjectDirty(this);
                }
            }
            else
            {

            }
#endif
        }

#if UNITY_EDITOR

        private CarSlot CreateSlot()
        {
            var slot = new GameObject("CarSlot").AddComponent<CarSlot>();

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(slot.gameObject, transform.gameObject.scene);
            slot.transform.SetParent(transform.parent);
            slot.transform.localPosition = default;
            slot.transform.localRotation = Quaternion.identity;

            return slot;
        }

#endif

        private CarSlot GetEmptySlot()
        {
            CarSlot carSlot = null;

            for (int i = 0; i < carSlots.Length; i++)
            {
                if (carSlots[i].NpcInCar == null && carSlots[i] != driverSlot)
                {
                    carSlot = carSlots[i];
                    break;
                }
            }

            return carSlot;
        }
    }
}