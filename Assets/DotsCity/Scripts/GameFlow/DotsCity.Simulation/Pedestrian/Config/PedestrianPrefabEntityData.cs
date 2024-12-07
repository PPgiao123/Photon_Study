using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianPrefabEntityData", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Pedestrian/PedestrianPrefabEntityData")]
    public class PedestrianPrefabEntityData : ScriptableObject
    {
        [System.Serializable]
        public class PedestrianEntityPrefabDataDictionary : AbstractSerializableDictionary<EntityType, PedestrianAuthoring> { }

        [SerializeField] private PedestrianEntityPrefabDataDictionary pedestrianEntityPrefabData;

        public PedestrianEntityPrefabDataDictionary PedestrianEntityPrefabData => pedestrianEntityPrefabData;
    }
}
