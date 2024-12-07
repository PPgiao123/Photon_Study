using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeHasCapacityOptionTag : IComponentData { }

    public struct NodeCapacityComponent : IComponentData
    {
        public int MaxAvailaibleCount;
        public int CurrentCount;

        public bool IsAvailable() => MaxAvailaibleCount == -1 || CurrentCount > 0;

        public NodeCapacityComponent Enter()
        {
            if (MaxAvailaibleCount == -1)
            {
                return this;
            }

            return new NodeCapacityComponent() { MaxAvailaibleCount = this.MaxAvailaibleCount, CurrentCount = math.clamp(--this.CurrentCount, 0, MaxAvailaibleCount) };
        }

        public NodeCapacityComponent Leave()
        {
            if (MaxAvailaibleCount == -1)
            {
                return this;
            }

            this.CurrentCount = math.clamp(++this.CurrentCount, -1, MaxAvailaibleCount);

            return this;
        }
    }
}