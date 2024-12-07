using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.CityEditor.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    [CreateAssetMenu(fileName = "RSGeneratorConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIGS_PATH + "RSGeneratorConfig")]
    public class RSGeneratorConfig : ScriptableObjectBase
    {
        [Serializable]
        public struct CustomRoadData
        {
            [Tooltip("What keyword to search for to assign a custom speed limit to a road?")]
            public string RoadKeyName;

            [Tooltip("Ignore road with set word")]
            public string ExcludeWord;

            [Tooltip("Custom speed limit for straight road. 0 - default lane speed")]
            [Range(0, 130)] public float SpeedLimit;

            [Tooltip("Dividing line width")]
            [Range(0, 10f)] public float DividerWidth;

            public bool CustomLaneCount;

            [ShowIf(nameof(CustomLaneCount))]
            [Range(1, 5)] public int LaneCount;

            [Tooltip("One-way traffic for the lane is forced")]
            public bool ForceOneway;

            [Tooltip("The direction of traffic will be reversed")]
            public bool ReverseDirection;
        }

        [field: SerializeField] public RoadSegmentCreator RoadSegmentCreatorPrefab { get; set; }

        [Tooltip("Remove unnecessary path nodes that are on the same line as the previous node")]
        [SerializeField] private bool stripOutNodes = true;

        [Tooltip("Min angle between path nodes")]
        [SerializeField][Range(0, 10f)] private float minStripAngle = 1f;

        [Tooltip("Min distance between path nodes to be stripped")]
        [SerializeField][Range(0, 100f)] private float minStripDistance = 20f;

        [Tooltip("Create Traffic nodes at path nodes so that traffic can spawn at these nodes")]
        [SerializeField] private bool generateInnerPathSpawnNodes = true;

        [Tooltip("Min distance between generated path Traffic nodes")]
        [SerializeField][Range(1, 300f)] private float minNodeOffsetDistance = 20f;

        [Tooltip("Add pedestrian to roads")]
        [SerializeField] private bool addPedestrianNode = true;

        [Tooltip("Pedestrian node offset to straight road")]
        [SerializeField][Range(-10f, 10f)] private float lineNodeOffset;

        [Tooltip("Node spacing along straight road")]
        [SerializeField][Range(0, 40f)] private float nodeSpacing = 10f;

        [Tooltip("No pedestrian nodes will be created for these roads")]
        [SerializeField] private List<string> ignoreNodeRoads = new List<string>();

        [Tooltip("Custom speed limit for straight paths of crossing")]
        [SerializeField][Range(0, 120f)] private float straightSpeedLimit = 60f;

        [Tooltip("Custom speed limit for turn paths of crossing")]
        [SerializeField][Range(0, 120f)] private float turnSpeedLimit = 30f;

        [Tooltip("Custom data for custom straight roads")]
        [SerializeField] private List<CustomRoadData> customDatas = new List<CustomRoadData>();

        public bool StripOutNodes { get => stripOutNodes; set => stripOutNodes = value; }
        public float MinStripAngle { get => minStripAngle; set => minStripAngle = value; }
        public float MinStripDistance { get => minStripDistance; set => minStripDistance = value; }
        public bool GenerateSpawnNodes { get => generateInnerPathSpawnNodes; set => generateInnerPathSpawnNodes = value; }
        public float MinNodeOffsetDistance { get => minNodeOffsetDistance; set => minNodeOffsetDistance = value; }
        public bool AddPedestrianNode { get => addPedestrianNode; set => addPedestrianNode = value; }
        public float LineNodeOffset { get => lineNodeOffset; set => lineNodeOffset = value; }
        public float NodeSpacing { get => nodeSpacing; set => nodeSpacing = value; }
        public float StraightSpeedLimit { get => straightSpeedLimit; set => straightSpeedLimit = value; }
        public float TurnSpeedLimit { get => turnSpeedLimit; set => turnSpeedLimit = value; }
        public List<string> IgnoreNodeRoads => ignoreNodeRoads;

        public bool GetData(string roadName, out CustomRoadData customRoadData)
        {
            customRoadData = default;

            for (int i = 0; i < customDatas.Count; i++)
            {
                if (!string.IsNullOrEmpty(customDatas[i].RoadKeyName) && roadName.Contains(customDatas[i].RoadKeyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(customDatas[i].ExcludeWord))
                    {
                        if (!roadName.Contains(customDatas[i].ExcludeWord, StringComparison.InvariantCultureIgnoreCase))
                        {
                            customRoadData = customDatas[i];
                            return true;
                        }
                    }
                    else
                    {
                        customRoadData = customDatas[i];
                        return true;
                    }
                }
            }

            return false;
        }

        public bool GetSpeedLimit(string roadName, out float speedLimit)
        {
            speedLimit = 0;

            if (GetData(roadName, out var data))
            {
                speedLimit = data.SpeedLimit;
                return true;
            }

            return false;
        }

        public bool ShouldAddPedestrianNodes(string roadName)
        {
            if (!addPedestrianNode)
                return false;

            for (int i = 0; i < ignoreNodeRoads.Count; i++)
            {
                if (!string.IsNullOrEmpty(ignoreNodeRoads[i]) && roadName.Contains(ignoreNodeRoads[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
