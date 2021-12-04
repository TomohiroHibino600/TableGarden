using UnityEngine;

namespace TableGarden
{
    public interface ICheckMesh
    {
        bool HitMesh { get; set; }
        Vector3 HitPoint { get; }
        Vector3 HitNormal { get; }
        float HitDistance { get; }
    }
}
