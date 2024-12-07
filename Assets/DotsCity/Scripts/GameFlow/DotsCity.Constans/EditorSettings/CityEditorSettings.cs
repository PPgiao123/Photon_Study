#if UNITY_EDITOR
using Spirit604.Extensions;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor
{
    public class CityEditorSettings : ScriptableObject
    {
        private const string FolderPath = "Prefabs/CityEditor/Editor/";
        private const string FileName = "CityEditorSettings.asset";

        [Tooltip("Right (default) lane TrafficNode color")]
        [SerializeField] private Color arrowColor = Color.green;

        [Tooltip("Left (external) lane TrafficNode color")]
        [SerializeField] private Color externalArrowColor = Color.blue;

        [Tooltip("Path TrafficNode color")]
        [SerializeField] private Color pathArrowColor = Color.yellow;

        [Tooltip("Pedestrian subnode color")]
        [SerializeField] private Color pedSubNodeColor = Color.yellow;

        [Tooltip("Traffic node arrow length")]
        [SerializeField][Range(0, 5)] float arrowLength = 3f;

        [Tooltip("Traffic node arrow thickness")]
        [SerializeField][Range(0, 2)] float arrowThickness = 0.7f;

        [Tooltip("Auto sync city scene config between MainScene & Subscene on change")]
        [SerializeField] private bool syncConfigOnChange;

        [Tooltip("Subscene will open & sync after config change, otherwise config will only sync when subscene is opened by user")]
        [SerializeField] private bool autoOpenClosedScene = true;

        [Tooltip("Auto close subscene if the subscene was closed before config change after config deselection")]
        [SerializeField] private bool autoCloseScene = true;

        [SerializeField] private bool autoSaveChanges = true;

        public Color ArrowColor => arrowColor;
        public Color ExternalArrowColor => externalArrowColor;
        public Color SubNodeTrafficColor => pathArrowColor;
        public Color PedSubNodeColor => pedSubNodeColor;
        public float ArrowLength => arrowLength;
        public float ArrowThickness => arrowThickness;

        public bool SyncConfigOnChange => syncConfigOnChange;
        public bool AutoOpenClosedScene => autoOpenClosedScene;
        public bool AutoCloseScene => autoCloseScene;
        public bool AutoSaveChanges => autoSaveChanges || autoCloseScene;

        public static CityEditorSettings GetOrCreateSettings()
        {
            var folderPath = CityEditorBookmarks.GetPath(FolderPath);
            var fullPath = Path.Combine(folderPath, FileName);

            var settings = AssetDatabase.LoadAssetAtPath<CityEditorSettings>(fullPath);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<CityEditorSettings>();

                AssetDatabaseExtension.CheckForFolderExist(folderPath);
                AssetDatabase.CreateAsset(settings, fullPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}
#endif