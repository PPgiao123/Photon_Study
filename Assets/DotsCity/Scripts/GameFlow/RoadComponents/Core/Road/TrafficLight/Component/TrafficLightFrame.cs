using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficLightFrame : TrafficLightFrameBase
    {
        [SerializeField] private GameObject redLight;
        [SerializeField] private GameObject yellowLight;
        [SerializeField] private GameObject greenLight;

        public GameObject RedLight => redLight;
        public GameObject YellowLight => yellowLight;
        public GameObject GreenLight => greenLight;

        protected override bool HasYellowLight => yellowLight != null;

        protected override void SwitchLight(LightColor lightColor, bool isOn)
        {
            var lightObject = GetLightObject(lightColor);

            if (lightObject != null && lightObject.activeSelf != isOn)
            {
                lightObject.SetActive(isOn);
            }
        }

        private GameObject GetLightObject(LightColor lightColor)
        {
            switch (lightColor)
            {
                case LightColor.Red:
                    return redLight;
                case LightColor.Yellow:
                    return yellowLight;
                case LightColor.Green:
                    return greenLight;
            }

            return null;
        }
    }
}