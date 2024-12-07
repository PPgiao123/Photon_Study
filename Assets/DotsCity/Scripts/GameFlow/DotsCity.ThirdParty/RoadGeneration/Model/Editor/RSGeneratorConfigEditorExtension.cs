#if UNITY_EDITOR
using UnityEditor;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public static class RSGeneratorConfigEditorExtension
    {
        public static void DrawSettings(SerializedObject serializedObject)
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("GroupBox");

            var stripOutNodesProp = serializedObject.FindProperty("stripOutNodes");

            EditorGUILayout.PropertyField(stripOutNodesProp);

            if (stripOutNodesProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripDistance"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            var generateSpawnNodesProp = serializedObject.FindProperty("generateInnerPathSpawnNodes");

            EditorGUILayout.PropertyField(generateSpawnNodesProp);

            if (generateSpawnNodesProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minNodeOffsetDistance"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            var addPedestrianNodeProp = serializedObject.FindProperty("addPedestrianNode");

            EditorGUILayout.PropertyField(addPedestrianNodeProp);

            if (addPedestrianNodeProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lineNodeOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nodeSpacing"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreNodeRoads"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.LabelField("Crossing", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("straightSpeedLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("turnSpeedLimit"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.LabelField("Custom Straight Road", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("customDatas"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
