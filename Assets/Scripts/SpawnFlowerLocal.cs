using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TableGarden
{
    public class SpawnFlowerLocal : MonoBehaviour, ISpawnFlower
    {
        [SerializeField] GameObject[] flowers;

        public void SpawnAt(Vector3 point, Vector3 normal)
        {
            Debug.Log("SpawnAt");

            Instantiate(flowers[Random.Range(0, flowers.Length)])
                .transform.SetPositionAndRotation(point, Quaternion.Euler(normal));
        }
    }
}