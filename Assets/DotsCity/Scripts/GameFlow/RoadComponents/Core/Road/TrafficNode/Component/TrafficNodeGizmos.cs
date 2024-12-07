using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road.Debug;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public partial class TrafficNode : MonoBehaviour
    {
#if UNITY_EDITOR
        private static CityEditorSettings CityEditorSettings => CityEditorSettings.GetOrCreateSettings();

        private void OnDrawGizmos()
        {
            if (!PathDebugger.ShouldDrawEditorPath)
                return;

            DrawArrows();
        }

        private void DrawArrows()
        {
            if (!isOneWay)
            {
                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    var startPosition = TrafficNodeExtension.GetLanePosition(this, laneIndex);
                    DrawArrow(startPosition, this.GetNodeForward(true), CityEditorSettings.ArrowColor);
                }
            }

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                var startPosition = TrafficNodeExtension.GetLanePosition(this, laneIndex, true);
                DrawArrow(startPosition, this.GetNodeForward(false), IsOneWay && HasRightLanes ? CityEditorSettings.ArrowColor : CityEditorSettings.ExternalArrowColor);
            }
        }

        public static void DrawArrow(Vector3 pos, Vector3 dir, Color color) => DebugLine.DrawArrow(pos, dir, color, CityEditorSettings.ArrowThickness, CityEditorSettings.ArrowLength);
#endif
    }
}