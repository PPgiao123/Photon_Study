using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    public struct TrafficFixedRouteTag : IComponentData { }

    public struct TrafficFixedRouteComponent : IComponentData
    {
        public Entity RouteEntity;

        /// <summary> Current destination Follow route node index.</summary>
        public int RouteNodeIndex;

        public bool LoopPath;

        /// <summary> Number of route nodes.</summary>
        public int RouteLength;
    }

    public struct TrafficFixedRouteLinkComponent : ICleanupComponentData
    {
        public Entity RouteEntity;
    }

    public struct FixedRouteNodeElement : IBufferElementData
    {
        public Entity TrafficNodeEntity;
        public float3 Position;
        public quaternion Rotation;
        public int PathKey;
        public int CustomLocalTargetWaypointIndex;
        public bool IsAvailable;
        public int IsChangeLaneNode;
        public float CustomSpeedLimit;
    }
}