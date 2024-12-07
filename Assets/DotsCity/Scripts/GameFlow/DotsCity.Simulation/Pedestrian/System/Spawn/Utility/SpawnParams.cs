using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct SpawnParams
    {
        public DestinationComponent DestinationComponent;
        public RigidTransform RigidTransform;
        public PedestrianEntitySpawnerSystem.CaptureNodeInfo CapturedNodeInfo;
        public uint Seed;

        public SpawnParams(uint seed, DestinationComponent destinationComponent, RigidTransform rigidTransform, PedestrianEntitySpawnerSystem.CaptureNodeInfo capturedNodeInfo)
        {
            DestinationComponent = destinationComponent;
            RigidTransform = rigidTransform;
            CapturedNodeInfo = capturedNodeInfo;
            Seed = seed;
        }
    }
}
