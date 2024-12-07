using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficLightObject : MonoBehaviour
    {
        #region Helper types

        [Serializable]
        public class TrafficLightFrameData
        {
            public List<TrafficLightFrameBase> TrafficLightFrames = new List<TrafficLightFrameBase>();
        }

        [Serializable]
        public class TrafficLightFrameDataDictionary : AbstractSerializableDictionary<int, TrafficLightFrameData> { }

        #endregion

        #region Serialized Variables

        [SerializeField] private TrafficLightCrossroad trafficLightCrossroad;
        [SerializeField] private int connectedId;
        [SerializeField] private TrafficLightFrameDataDictionary trafficLightFrameData;

        #endregion

        #region Properties     

        public TrafficLightCrossroad TrafficLightCrossroad => trafficLightCrossroad;

        public int ConnectedId { get => connectedId; set => connectedId = value; }

        public TrafficLightFrameDataDictionary TrafficLightFrames { get => trafficLightFrameData; }

        #endregion

        #region Methods     

        public void SetIndexOffset(int indexOffset)
        {
            int maxIndex = 0;

            if (trafficLightFrameData.Keys?.Count > 0)
            {
                maxIndex = trafficLightFrameData.Keys.ToList().Max() + 1;
            }

            maxIndex = Mathf.Max(2, maxIndex);

            var frames = GetComponentsInChildren<TrafficLightFrameBase>().ToList();

            var tempTrafficLightFrames = new TrafficLightFrameDataDictionary();

            foreach (var frame in frames)
            {
                var currentIndex = GetCurrentIndexFrame(frame);
                var newIndex = (currentIndex + indexOffset) % maxIndex;

                if (!tempTrafficLightFrames.ContainsKey(newIndex))
                {
                    tempTrafficLightFrames.Add(newIndex, new TrafficLightFrameData() { TrafficLightFrames = new List<TrafficLightFrameBase>() });
                }

                tempTrafficLightFrames[newIndex].TrafficLightFrames.TryToAdd(frame);
            }

            trafficLightFrameData = tempTrafficLightFrames;

            EditorSaver.SetObjectDirty(this);
        }

        public bool ChangeFrameIndex(TrafficLightFrameBase sourceFrame, int oldIndex, int newIndex)
        {
            if (trafficLightFrameData.ContainsKey(oldIndex) && trafficLightFrameData[oldIndex].TrafficLightFrames.Contains(sourceFrame))
            {
                trafficLightFrameData[oldIndex].TrafficLightFrames.TryToRemove(sourceFrame);

                if (trafficLightFrameData[oldIndex].TrafficLightFrames.Count == 0)
                {
                    trafficLightFrameData.Remove(oldIndex);
                }

                if (!trafficLightFrameData.ContainsKey(newIndex))
                {
                    trafficLightFrameData.Add(newIndex, new TrafficLightFrameData() { TrafficLightFrames = new List<TrafficLightFrameBase>() });
                }

                trafficLightFrameData[newIndex].TrafficLightFrames.TryToAdd(sourceFrame);

                EditorSaver.SetObjectDirty(this);

                return true;
            }

            return false;
        }

        private int GetCurrentIndexFrame(TrafficLightFrameBase trafficLightFrame)
        {
            foreach (var frameData in trafficLightFrameData)
            {
                if (frameData.Value.TrafficLightFrames.Contains(trafficLightFrame))
                {
                    return frameData.Key;
                }
            }

            return -1;
        }

        public bool HasLightIndex(int index)
        {
            return trafficLightFrameData.ContainsKey(index);
        }

        public bool FrameHasIndex(TrafficLightFrameBase trafficLightFrame, int index)
        {
            var currentIndex = GetCurrentIndexFrame(trafficLightFrame);
            return currentIndex == index;
        }

        public ICollection<int> GetLightIndexes()
        {
            return trafficLightFrameData.Keys;
        }

        public List<TrafficLightFrameBase> GetLightFrames(int index)
        {
            if (trafficLightFrameData.ContainsKey(index))
            {
                return trafficLightFrameData[index].TrafficLightFrames;
            }

            return null;
        }

        public void SetupInitialIndexes()
        {
            foreach (var frameData in TrafficLightFrames)
            {
                var frames = frameData.Value.TrafficLightFrames;

                foreach (var frame in frames)
                {
                    frame.InitialLightIndex = frameData.Key;
                    EditorSaver.SetObjectDirty(frame);
                }
            }
        }

        public void AssignCrossRoad(TrafficLightCrossroad trafficLightCrossroad, bool reparent = false)
        {
            bool changed = false;

            if (this.trafficLightCrossroad != trafficLightCrossroad)
            {
                this.trafficLightCrossroad = trafficLightCrossroad;
                changed = true;
            }

            if (trafficLightCrossroad)
            {
                RebindCrossroad();

                if (reparent && trafficLightCrossroad.TrafficLightParent)
                {
                    transform.SetParent(trafficLightCrossroad.TrafficLightParent);
                }
            }
            else
            {
                if (connectedId != 0)
                {
                    connectedId = 0;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public bool RebindCrossroad()
        {
            if (!trafficLightCrossroad)
                return false;

            var newConnectedId = trafficLightCrossroad.UniqueId;

            if (connectedId != newConnectedId)
            {
                connectedId = newConnectedId;
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public void AssignInitialChilds()
        {
            SetIndexOffset(0);
        }
    }

    #endregion
}