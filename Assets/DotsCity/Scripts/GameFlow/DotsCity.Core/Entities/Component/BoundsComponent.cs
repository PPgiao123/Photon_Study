using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public struct BoundsComponent : IComponentData
    {
        public Vector3 Size;
        public Vector3 Center;

        public Bounds Bounds => new Bounds(Center, Size);
        public Vector3 Extents => Size / 2;
    }
}
