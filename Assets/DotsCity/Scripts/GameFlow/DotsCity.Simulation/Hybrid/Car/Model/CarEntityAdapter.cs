using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [RequireComponent(typeof(PhysicsSwitcher))]
    public class CarEntityAdapter : PhysicsHybridEntityAdapter
    {
        private IVehicleInput input;

        private void Awake()
        {
            input = GetComponent<IVehicleInput>();
        }

        public void UpdateInput(in VehicleInputReader vehicleInputReader)
        {
            if (!Enabled)
                return;

            input.Throttle = vehicleInputReader.Throttle;
            input.Steering = vehicleInputReader.SteeringInput;
            input.Handbrake = vehicleInputReader.Handbrake;
        }
    }
}
