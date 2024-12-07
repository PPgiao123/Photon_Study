using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerInteractCarService : MonoBehaviour, IPlayerInteractCarService
    {
        public bool EnterCar(GameObject car, GameObject enteredNpc)
        {
            bool entered = false;
            var carslots = car.GetComponent<ICarSlots>();

            if (carslots != null)
            {
                var idProvider = enteredNpc.GetComponent<INpcIDProvider>();

                if (idProvider != null)
                {
                    var slot = carslots.EnterCar(idProvider.ID, enteredNpc, true);
                    entered = slot;
                }
                else
                {
                    UnityEngine.Debug.LogError("PlayerInteractCarService. The car can't be entered. INpcIDProvider not found. Add 'NpcCustomIDComponent' component to your character & enter id according to PlayerNpcCarFactory");
                    return false;
                }
            }

            var vehicleInput = car.GetComponent<IVehicleInput>();

            if (vehicleInput != null)
                vehicleInput.SwitchEnabledState(true);

            return entered;
        }

        public void ExitCar(GameObject car)
        {
            var carslots = car.GetComponent<ICarSlots>();

            carslots.ExitCarAll(true);

            var vehicleInput = car.GetComponent<IVehicleInput>();

            if (vehicleInput != null)
                vehicleInput.SwitchEnabledState(false);
        }
    }
}