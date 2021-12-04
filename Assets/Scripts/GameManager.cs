using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TableGarden
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] GameObject checkMeshObj;
        [SerializeField] GameObject spawnFlowerObj;

        ICheckMesh checkMesh;
        ISpawnFlower spawnFlower;

        void Start()
        {
            checkMesh = checkMeshObj.GetComponent<ICheckMesh>();
            spawnFlower = spawnFlowerObj.GetComponent<ISpawnFlower>();
        }

        // Update is called once per frame
        void Update()
        {
            if(checkMesh.HitMesh)
            {
                Debug.Log("GameManager");
                spawnFlower.SpawnAt(checkMesh.HitPoint, checkMesh.HitNormal);
                checkMesh.HitMesh = false;
            }
        }
    }
}