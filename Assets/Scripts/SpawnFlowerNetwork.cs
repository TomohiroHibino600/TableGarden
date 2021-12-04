using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.HLAPI.Object.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TableGarden
{
    public class SpawnFlowerNetwork : MonoBehaviour, ISpawnFlower
    {
        [SerializeField] NetworkedUnityObject[] flowers;
        [SerializeField] NetworkSessionManager networkingSessionManager;

        public void SpawnAt(Vector3 point, Vector3 normal)
        {
            Debug.Log("SpawnAt");

            flowers[Random.Range(0, flowers.Length)]
                .NetworkSpawn(networkingSessionManager.Networking ,point, Quaternion.Euler(normal));
        }
    }
}