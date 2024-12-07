using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    public class ConfigRoot : MonoBehaviour
    {
        [Button]
        public void SyncConfigs()
        {
            var configs = GetComponentsInChildren<ISyncableConfig>();

            for (int i = 0; i < configs?.Length; i++)
            {
                configs[i].SyncConfig();
            }
        }
    }
}
