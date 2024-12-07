using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public interface IPlayerInteractCarService
    {
        bool EnterCar(GameObject car, GameObject sourceNpc);
        void ExitCar(GameObject car);
    }
}