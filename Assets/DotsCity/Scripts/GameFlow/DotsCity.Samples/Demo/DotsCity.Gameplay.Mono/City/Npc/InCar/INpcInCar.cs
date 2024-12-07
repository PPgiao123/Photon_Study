using Spirit604.Gameplay.Car;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public interface INpcInCar : INpcIDProvider
    {
        Transform Transform { get; }

        void Initialize(CarSlot carSlot);
        void Shoot(Vector3 direction);
        void Show(bool isShowing);
        void SnapHide();
        void Dispose();
    }
}