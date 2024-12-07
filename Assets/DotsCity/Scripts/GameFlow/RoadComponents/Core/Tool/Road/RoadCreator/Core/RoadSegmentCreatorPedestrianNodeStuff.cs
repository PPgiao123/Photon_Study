using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        private void InitializeTrafficNodeCrosswalk(TrafficNode trafficNode)
        {
            TrafficNodeCrosswalk trafficNodeCrosswalk = trafficNode.TrafficNodeCrosswalk;

            trafficNodeCrosswalk.CrossWalkOffset = CrossWalkOffset;

            var trafficLightCrossroad = trafficNode.TrafficLightCrossroad;

            if (trafficLightCrossroad)
            {
                var currentTrafficLightHandler = trafficNode.TrafficLightHandler;

                TrafficLightHandler relatedTrafficLightHandler = null;

                foreach (var item in trafficLightCrossroad.TrafficLightHandlers)
                {
                    if (item.Value != currentTrafficLightHandler)
                    {
                        relatedTrafficLightHandler = item.Value;
                        break;
                    }
                }

                trafficNodeCrosswalk.PedestrianNode1.RelatedTrafficLightHandler = relatedTrafficLightHandler;
                trafficNodeCrosswalk.PedestrianNode2.RelatedTrafficLightHandler = relatedTrafficLightHandler;
            }

            trafficNodeCrosswalk.SetType(crossWalkNodeShape);

            EditorSaver.SetObjectDirty(trafficNodeCrosswalk.PedestrianNode1);
            EditorSaver.SetObjectDirty(trafficNodeCrosswalk.PedestrianNode2);
        }

        private void ClearCornerNodes()
        {
            cornerPedestrianNodes.DestroyGameObjects();
            cornerPedestrianNodesBinding.Clear();

            int optionalNode = 1;

            if (CreateTrafficNodeCount == 3)
            {
                optionalNode = 0;
            }

            for (int i = 0; i < CreateTrafficNodeCount - 1 + optionalNode; i++)
            {
                int nextIndex = (i + 1) % CreateTrafficNodeCount;

                if (createdTrafficNodes.Count > i && createdTrafficNodes.Count > nextIndex)
                {
                    if (!createdTrafficNodes[i] || !createdTrafficNodes[nextIndex])
                    {
                        continue;
                    }

                    var crossWalkNode1 = createdTrafficNodes[i].TrafficNodeCrosswalk.PedestrianNode2;
                    var crossWalkNode2 = createdTrafficNodes[nextIndex].TrafficNodeCrosswalk.PedestrianNode1;

                    crossWalkNode1.TryToRemoveNode(crossWalkNode2);
                    crossWalkNode2.TryToRemoveNode(crossWalkNode1);
                }
            }
        }

        private void CreateCornerNodes()
        {
            int optionalNode = 1;

            if (CreateTrafficNodeCount == 3)
            {
                optionalNode = 0;
            }

            for (int i = 0; i < CreateTrafficNodeCount - 1 + optionalNode; i++)
            {
                int nextIndex = (i + 1) % CreateTrafficNodeCount;
                var crossWalkNode1 = createdTrafficNodes[i].TrafficNodeCrosswalk.PedestrianNode2;
                var crossWalkNode2 = createdTrafficNodes[nextIndex].TrafficNodeCrosswalk.PedestrianNode1;

                switch (pedestrianCornerConnectionType)
                {
                    case PedestrianCornerConnectionType.Disabled:
                        break;
                    case PedestrianCornerConnectionType.Corner:
                        {
                            Vector3 spawnPosition = GetCornerPointRelativeCenter(crossWalkNode1.transform.position, crossWalkNode2.transform.position);

                            var cornerPedestrianNode = CreatePedestrianNode();
                            cornerPedestrianNode.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                            cornerPedestrianNode.transform.SetParent(cornerNodes);

                            var dir = (cornerPedestrianNode.transform.position - transform.position).normalized;

                            cornerPedestrianNode.transform.position += MathUtilMethods.LookRotationSafe(dir, Vector3.up) * cornerOffset;

                            cornerPedestrianNode.AddNode(crossWalkNode1);
                            cornerPedestrianNode.AddNode(crossWalkNode2);

                            cornerPedestrianNode.MaxPathWidth = pedestrianRouteWidth;

                            crossWalkNode1.AddNode(cornerPedestrianNode);
                            crossWalkNode2.AddNode(cornerPedestrianNode);

                            cornerPedestrianNodes.TryToAdd(cornerPedestrianNode);

                            break;
                        }
                    case PedestrianCornerConnectionType.Straight:
                        {
                            crossWalkNode1.AddNode(crossWalkNode2);
                            crossWalkNode2.AddNode(crossWalkNode1);
                            break;
                        }
                }
            }
        }

        private void TryToCreatePedestrianLoopedConnection()
        {
            if (!PedestrianLoopConnectionSupported)
            {
                return;
            }

            var crossWalkNode1 = createdTrafficNodes[0].GetComponent<TrafficNodeCrosswalk>().PedestrianNode1;
            var crossWalkNode2 = createdTrafficNodes[2].GetComponent<TrafficNodeCrosswalk>().PedestrianNode2;

            if (loopPedestrianConnection)
            {
                crossWalkNode1.AddNode(crossWalkNode2);
                crossWalkNode2.AddNode(crossWalkNode1);
            }
            else
            {
                crossWalkNode1.RemoveConnection(crossWalkNode2);
            }
        }

        private PedestrianNode CreatePedestrianNode()
        {
#if UNITY_EDITOR
            return (PrefabUtility.InstantiatePrefab(roadSegmentCreatorConfig.PedestrianNodePrefab.gameObject, transform) as GameObject).GetComponent<PedestrianNode>();
#else
            return null;
#endif
        }

        private void ConnectStraightRoadCrosswalks()
        {
            if (createdTrafficNodes.Count < 2)
                return;

            var spawnPoint11 = createdTrafficNodes[0].TrafficNodeCrosswalk.PedestrianNode1;
            var spawnPoint12 = createdTrafficNodes[0].TrafficNodeCrosswalk.PedestrianNode2;
            var spawnPoint21 = createdTrafficNodes[1].TrafficNodeCrosswalk.PedestrianNode1;
            var spawnPoint22 = createdTrafficNodes[1].TrafficNodeCrosswalk.PedestrianNode2;

            spawnPoint11.AddNode(spawnPoint22);
            spawnPoint21.AddNode(spawnPoint12);

            spawnPoint12.AddNode(spawnPoint21);
            spawnPoint22.AddNode(spawnPoint11);
        }
    }
}