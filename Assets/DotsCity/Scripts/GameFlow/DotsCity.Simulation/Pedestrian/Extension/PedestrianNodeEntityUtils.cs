using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianNodeEntityUtils
    {
        private const int AttemptCount = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetRandomDestinationEntity(
            in DynamicBuffer<NodeConnectionDataElement> pedestrianNodeConnector,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            uint sourceSeed,
            float sumWeight = 0)
        {
            return GetRandomDestinationEntity(in pedestrianNodeConnector, in nodeSettingsLookup, in nodeCapacityLookup, sourceSeed, Entity.Null, sumWeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetRandomDestinationEntity(
            in DynamicBuffer<NodeConnectionDataElement> pedestrianNodeConnector,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            uint sourceSeed,
            Entity ignoreEntity,
            float sumWeight = 0)
        {
            if (!pedestrianNodeConnector.IsCreated || pedestrianNodeConnector.Length == 0)
            {
                return Entity.Null;
            }

            if (pedestrianNodeConnector.Length == 1)
            {
                return pedestrianNodeConnector[0].ConnectedEntity;
            }

            while (true)
            {
                int attemptCount = 0;
                var seed = sourceSeed;

                while (attemptCount < AttemptCount)
                {
                    var randomGen = new Random(seed);

                    Entity connectedEntity = Entity.Null;

                    if (sumWeight > 0)
                    {
                        float randomWeight = randomGen.NextFloat(0, sumWeight);

                        for (int i = 0; i < pedestrianNodeConnector.Length; i++)
                        {
                            var currentSumWeight = pedestrianNodeConnector[i].SumWeight;

                            if (randomWeight <= currentSumWeight)
                            {
                                connectedEntity = pedestrianNodeConnector[i].ConnectedEntity;
                                break;
                            }
                        }
                    }
                    else
                    {
                        int randomIndex = randomGen.NextInt(0, pedestrianNodeConnector.Length);
                        connectedEntity = pedestrianNodeConnector[randomIndex].ConnectedEntity;
                    }

                    if (connectedEntity != ignoreEntity && nodeSettingsLookup.HasComponent(connectedEntity) && nodeCapacityLookup[connectedEntity].IsAvailable())
                    {
                        return connectedEntity;
                    }

                    seed = MathUtilMethods.ModifySeed(sourceSeed, attemptCount);
                    attemptCount++;
                }

                if (attemptCount >= AttemptCount || ignoreEntity == Entity.Null)
                {
                    var index = math.clamp(math.abs(((int)sourceSeed)) % pedestrianNodeConnector.Length, 0, pedestrianNodeConnector.Length - 1);
                    return pedestrianNodeConnector[index].ConnectedEntity;
                }
            }
        }
    }
}