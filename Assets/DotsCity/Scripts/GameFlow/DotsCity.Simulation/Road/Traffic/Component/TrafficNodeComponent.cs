using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficNodeComponent : IComponentData
    {
        public int CrossRoadIndex;
        public Entity LightEntity;
    }
}