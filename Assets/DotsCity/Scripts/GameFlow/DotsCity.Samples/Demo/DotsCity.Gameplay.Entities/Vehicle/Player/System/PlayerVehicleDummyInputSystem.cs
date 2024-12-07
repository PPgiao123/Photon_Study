#if UNITY_EDITOR
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitGroup))]
    public partial class PlayerVehicleDummyInputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithName("PlayerVehicleDummyInputJob")
            .WithoutBurst()
            .WithAll<PlayerTag, CarEngineStartedTag>()
            .ForEach((ref VehicleInputReader playerVehicleInput, in LocalTransform transform) =>
            {
                playerVehicleInput.Throttle = Input.GetAxis("Vertical");
                playerVehicleInput.SteeringInput = Input.GetAxis("Horizontal");
                playerVehicleInput.HandbrakeInput = Input.GetKey(KeyCode.Space) ? 1 : 0;
            }).Run();

            Entities
            .WithBurst()
            .WithNone<CarEngineStartedTag>()
            .WithAll<PlayerTag>()
            .ForEach((ref VehicleInputReader playerVehicleInput) =>
            {
                playerVehicleInput = VehicleInputReader.GetBrake();
            }).Run();
        }
    }
}
#endif