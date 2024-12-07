using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Core
{
    public struct DamageHitData
    {
        public Entity DamagedEntity;
        public int Damage;
        public float3 HitDirection;
        public float3 HitPosition;
        public float ForceMultiplier;
    }

    public struct HealthComponent : IComponentData
    {
        public int MaxValue;
        public int Value;
        public float3 HitDirection;
        public float3 HitPosition;
        public float ForceMultiplier;

        public bool IsAlive => Value > 0;

        public HealthComponent(int value)
        {
            MaxValue = value;
            Value = value;
            Value = math.clamp(Value, 0, int.MaxValue);
            ForceMultiplier = 0;
            HitDirection = default;
            HitPosition = default;
        }

        public HealthComponent Hit(int count, float3 hitDirection, float3 hitPosition)
        {
            HitDirection = hitDirection;
            HitPosition = hitPosition;
            Value -= count;
            Value = math.clamp(Value, 0, int.MaxValue);
            return this;
        }
    }
}
