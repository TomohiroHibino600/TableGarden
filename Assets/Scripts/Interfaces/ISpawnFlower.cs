using UnityEngine;

namespace TableGarden
{
    public interface ISpawnFlower
    {
        void SpawnAt(Vector3 point, Vector3 normal);
    }
}
