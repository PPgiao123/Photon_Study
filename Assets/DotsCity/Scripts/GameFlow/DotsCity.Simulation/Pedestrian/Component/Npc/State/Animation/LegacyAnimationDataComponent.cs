using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct LegacyAnimationDataComponent
    {
        public int StateLayer;
        public int StateNameHash;
        public AnimParamType ParamType1;
        public AnimParamType ParamType2;
        public int ParamNameHash1;
        public int ParamNameHash2;
        public float Value1;
        public float Value2;

        public AnimParamType ExitParamType;
        public int ExitParamNameHash;
        public float ExitParamValue;

        public LegacyAnimationDataComponent(LegacyAnimationData legacyAnimationData)
        {
            StateLayer = legacyAnimationData.StateLayer;
            StateNameHash = GetHash(legacyAnimationData.StateName);
            ParamType1 = legacyAnimationData.ParamType1;
            ParamType2 = legacyAnimationData.ParamType2;
            ParamNameHash1 = GetHash(legacyAnimationData.ParamName1);
            ParamNameHash2 = GetHash(legacyAnimationData.ParamName2);
            Value1 = legacyAnimationData.Value1;
            Value2 = legacyAnimationData.Value2;

            ExitParamType = legacyAnimationData.ExitParamType;
            ExitParamNameHash = GetHash(legacyAnimationData.ExitParamName);
            ExitParamValue = legacyAnimationData.ExitValue;
        }

        private static int GetHash(string sourceString)
        {
            if (!string.IsNullOrEmpty(sourceString))
            {
                return Animator.StringToHash(sourceString);
            }

            return 0;
        }
    }
}