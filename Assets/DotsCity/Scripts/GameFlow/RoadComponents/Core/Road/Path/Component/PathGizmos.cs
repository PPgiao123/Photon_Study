using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road.Debug;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public partial class Path : MonoBehaviour
    {
        private const float WaypointRadius = 1f;
        private const float IntersectPointRadius = 0.5f;

        public const float HighlightedLineThick = 1f;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (ShowIntersectedPoints)
            {
                if (intersects?.Count > 0)
                {
                    for (int i = 0; i < intersects.Count; i++)
                    {
                        if (intersects[i].IntersectedPath != null)
                        {
                            var point = intersects[i].GetIntersectPoint(this);
                            Gizmos.DrawSphere(point, IntersectPointRadius);
                        }
                    }
                }
            }

            if (!PathDebugger.ShouldDrawEditorPath && !Selected && !Highlighted)
            {
                return;
            }

            Gizmos.color = HighlightColor;

            if (wayPoints == null || wayPoints.Count == 0 || wayPoints[0] == null)
            {
                Gizmos.color = Color.red;
            }

            if (customLightHandler)
            {
                Gizmos.color = Color.green;
            }

            if (nodes.Count > 1)
            {
                if (wayPoints?.Count > 0 && wayPoints[0] != null)
                {
                    float pathLength = GetPathLength();

                    float currentDistance = 0;
                    bool disabledHightlight = false;
                    bool forceDrawHighlight = false;

                    HightlightNormalizedLength = Mathf.Clamp(HightlightNormalizedLength, -1, 1);

                    float normalizedLength = Math.Abs(HightlightNormalizedLength);

                    for (int i = 0; i < WayPoints.Count - 1; i++)
                    {
                        Vector3 A1point = WayPoints[i].transform.position;
                        Vector3 A2point = WayPoints[i + 1].transform.position;
                        Vector3 offsetPoint = A2point;

                        float overDistance = 0;

                        if (WayPoints[i].SpawnNode)
                        {
                            TrafficNode.DrawArrow(A1point, WayPoints[i].transform.forward, CityEditorSettings.GetOrCreateSettings().SubNodeTrafficColor);
                        }

                        if (HightlightNormalizedLength != 1f && !disabledHightlight)
                        {
                            currentDistance += Vector3.Distance(A1point, A2point);

                            if (currentDistance >= normalizedLength * pathLength)
                            {
                                if (HightlightNormalizedLength >= 0)
                                {
                                    overDistance = currentDistance - (1 - normalizedLength) * pathLength;
                                }
                                else
                                {
                                    overDistance = currentDistance - normalizedLength * pathLength;
                                }

                                disabledHightlight = true;

                                offsetPoint = A1point + (A2point - A1point).normalized * overDistance;

                                if (HightlightNormalizedLength < 0)
                                {
                                    forceDrawHighlight = true;
                                }
                            }
                        }

                        if (!Highlighted)
                        {
                            Color color = HasConnection ? HighlightColor : Color.red;

                            if (WayPoints[i].HasPathCustomColor)
                            {
                                color = WayPoints[i].PathCustomColor;
                            }

                            Gizmos.color = color;

                            Gizmos.DrawLine(A1point, A2point);
                        }
                        else
                        {
                            var color = HighlightColor;

                            if (WayPoints[i].HasPathCustomColor)
                            {
                                color = WayPoints[i].PathCustomColor;
                            }

                            if (offsetPoint != A2point)
                            {
                                if (HightlightNormalizedLength >= 0)
                                {
                                    DebugLine.DrawThickLine(A1point, offsetPoint, HighlightedLineThick, HighlightColor, true);
                                    Gizmos.DrawLine(offsetPoint, A2point);
                                }
                                else
                                {
                                    DebugLine.DrawThickLine(offsetPoint, A2point, HighlightedLineThick, HighlightColor, true);
                                    Gizmos.DrawLine(A1point, offsetPoint);
                                }
                            }
                            else
                            {
                                if (HightlightNormalizedLength >= 0 || forceDrawHighlight)
                                {
                                    DebugLine.DrawThickLine(A1point, A2point, HighlightedLineThick, color, true);
                                }
                                else
                                {
                                    Gizmos.DrawLine(A1point, A2point);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (showInfoWaypoints)
            {
                for (int i = 0; i < wayPoints?.Count; i++)
                {
                    var color = Color.white;

                    if (wayPoints[i].HasSelectCustomColor)
                    {
                        color = wayPoints[i].SelectCustomColor;
                    }

                    Gizmos.color = color;
                    Gizmos.DrawWireSphere(wayPoints[i].transform.position, WaypointRadius);
                }
            }
        }
#endif
    }
}