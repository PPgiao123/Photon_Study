using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class RuntimeSegment : MonoBehaviour
    {
        [Serializable]
        public class BakedData
        {
            public List<Path> NeighboardsPaths = new List<Path>();
            public List<Path> ParallelPaths = new List<Path>();
            public bool CustomNode;
        }

        [Serializable]
        public class CrossroadData
        {
            public List<TrafficLightObjectAuthoring> Lights = new List<TrafficLightObjectAuthoring>();
        }

        [Serializable]
        public class PathBakedDictionary : AbstractSerializableDictionary<Path, BakedData> { }

        [Serializable]
        public class CrossroadDataDictionary : AbstractSerializableDictionary<TrafficLightCrossroad, CrossroadData> { }

        [Tooltip("Automatically adds a segment to the simulation if it is initially on the scene and this parameter is enabled.")]
        public bool addOnAwake;

        public PathBakedDictionary bakedData = new PathBakedDictionary();
        public CrossroadDataDictionary crossroadData = new CrossroadDataDictionary();
        public PedestrianNode[] pedestrianNodes;
        public Path[] paths;

        private bool placed;

        public bool AddOnAwake
        {
            get => addOnAwake;
            set
            {
                if (addOnAwake != value)
                {
                    addOnAwake = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public NativeList<int> TakenPaths { get; set; }

        public bool Placed => placed;

        private void OnEnable()
        {
            if (addOnAwake)
                PlaceSegment();
        }

        public void Bake()
        {
            bakedData.Clear();
            crossroadData.Clear();

            var crossroads = GetComponentsInChildren<TrafficLightCrossroad>();
            var segments = GetComponentsInChildren<RoadSegment>();

            foreach (var crossroad in crossroads)
                crossroadData.Add(crossroad, new CrossroadData());

            foreach (var segment in segments)
                segment.BakeData();

            var nodes = GetComponentsInChildren<TrafficNode>();
            var lights = GetComponentsInChildren<TrafficLightObjectAuthoring>();

            this.pedestrianNodes = GetComponentsInChildren<PedestrianNode>();

            for (int i = 0; i < lights?.Length; i++)
            {
                TrafficLightObjectAuthoring light = lights[i];
                light.AutoRegister = false;

                var connectedCrossroad = light.TrafficLightObject.TrafficLightCrossroad;

                if (connectedCrossroad != null && crossroadData.ContainsKey(connectedCrossroad))
                {
                    crossroadData[connectedCrossroad].Lights.Add(light);
                }

                EditorSaver.SetObjectDirty(light);
            }

            var paths = new List<Path>();
            var tempPaths = new List<Path>();

            foreach (var node in nodes)
            {
                node.IterateAllPaths(path =>
                {
                    paths.Add(path);
                }, true);
            }

            this.paths = paths.ToArray();

            foreach (var node in nodes)
            {
                node.IterateAllPaths((path) =>
                {
                    var data = new BakedData();

                    bool isExternal = path.IsExternal;

                    var laneData = node.TryToGetLaneData(path.SourceLaneIndex, isExternal);

                    tempPaths.Clear();

                    foreach (var neighbourPath in laneData.paths)
                    {
                        if (neighbourPath == path) continue;

                        tempPaths.Add(neighbourPath);
                    }

                    data.NeighboardsPaths = new List<Path>(tempPaths);

                    tempPaths.Clear();

                    var parallelData1 = node.TryToGetLaneData(path.SourceLaneIndex - 1, isExternal);

                    if (parallelData1 != null)
                    {
                        foreach (var parallelPath in parallelData1.paths)
                        {
                            if (parallelPath != path && parallelPath.ConnectedTrafficNode == path.ConnectedTrafficNode)
                            {
                                tempPaths.Add(parallelPath);
                            }
                        }
                    }

                    var parallelData2 = node.TryToGetLaneData(path.SourceLaneIndex + 1, isExternal);

                    if (parallelData2 != null)
                    {
                        foreach (var parallelPath in parallelData2.paths)
                        {
                            if (parallelPath != path && parallelPath.ConnectedTrafficNode == path.ConnectedTrafficNode)
                            {
                                tempPaths.Add(parallelPath);
                            }
                        }
                    }

                    data.ParallelPaths = new List<Path>(tempPaths);

                    bool customNode = false;

                    path.IterateWaypoints((waypoint) =>
                    {
                        if (waypoint.CustomGroup)
                        {
                            customNode = true;
                        }
                    });

                    data.CustomNode = customNode;

                    bakedData.Add(path, data);
                }, true);
            }

            EditorSaver.SetObjectDirty(this);
        }

        public void PlaceSegment()
        {
            if (!placed)
            {
                placed = true;
#if RUNTIME_ROAD
                RuntimeRoadManager.Instance.AddSegment(this);
#endif
            }
        }

        public void RemoveSegment()
        {
            if (placed)
            {
                placed = false;
#if RUNTIME_ROAD
                RuntimeRoadManager.Instance.RemoveSegment(this);
#endif
            }
        }

        public List<Path> GetNeighbourPaths(Path path)
        {
            if (bakedData.TryGetValue(path, out var data))
            {
                return data.NeighboardsPaths;
            }

            return null;
        }

        public List<Path> GetParallelPaths(Path path)
        {
            if (bakedData.TryGetValue(path, out var data))
            {
                return data.ParallelPaths;
            }

            return null;
        }

        public void Validate()
        {
#if UNITY_EDITOR
            string message = string.Empty;

            for (int i = 0; i < pedestrianNodes.Length; i++)
            {
                var node = pedestrianNodes[i];

                if (node == null)
                {
                    TryToAddHeader(ref message);
                    message += $"- <b>PedestrianNode</b> Index {i} is null.\r\n";
                }
            }

            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];

                if (path == null)
                {
                    TryToAddHeader(ref message);
                    message += $"- <b>Path</b> Index {i} is null.\r\n";
                }
                else
                {
                    if (path.PathLength == 0)
                    {
                        TryToAddHeader(ref message);
                        message += $"- <b>Path</b> {path.name} Index {i} is not baked.\r\n";
                    }
                }
            }

            var childCrossroads = this.GetComponentsInChildren<TrafficLightCrossroad>();

            if (childCrossroads.Length != crossroadData.Keys.Count)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>TrafficLightCrossroad</b> not added or not baked.\r\n";
            }

            var childPaths = this.GetComponentsInChildren<Path>();

            if (childPaths.Length != paths.Length)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>paths</b> not added or not baked.\r\n";
            }

            var childNodes = this.GetComponentsInChildren<PedestrianNode>();

            if (childNodes.Length != pedestrianNodes.Length)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>PedestrianNode</b> not added or not baked.\r\n";
            }

            int lightCount = 0;

            foreach (var crossroad in crossroadData)
            {
                if (crossroad.Key == null)
                {
                    TryToAddHeader(ref message);
                    message += $"- Some of the crossroads is null.\r\n";
                    continue;
                }

                lightCount += crossroad.Value.Lights.Count;

                var lights = crossroad.Value.Lights;

                for (int i = 0; i < lights.Count; i++)
                {
                    TrafficLightObjectAuthoring light = lights[i];

                    if (light == null)
                    {
                        TryToAddHeader(ref message);
                        message += $"- {crossroad.Key.name}. TrafficLightObjectAuthoring Index {i} is null.\r\n";
                    }
                }

                var nodes = crossroad.Key.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];

                    if (node == null)
                    {
                        TryToAddHeader(ref message);
                        message += $"- <b>TrafficNode</b> Index {i} is null.\r\n";
                    }
                }
            }

            var childLights = this.GetComponentsInChildren<TrafficLightObjectAuthoring>();

            if (childLights.Length != lightCount)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>TrafficLightObjectAuthoring</b> not added or not baked.\r\n";
            }

            if (!string.IsNullOrEmpty(message))
            {
                message += $"\r\nOpen <b>{name}</b> <b>Prefab</b> & press <b>Bake</b> button in <b>RuntimeSegment</b> component.\r\n\r\n\r\n\r\n\r\n";
                Debug.Log(message);
            }
#endif
        }

#if UNITY_EDITOR
        private void TryToAddHeader(ref string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = $"RuntimeSegment. Prefab <b>{name}</b> has some errors:\r\n";
            }
        }

        private void Reset()
        {
            Bake();
            EditorSaver.SetObjectDirty(this);
        }
#endif
    }
}
