using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TableGarden
{
    public class CheckMesh : MonoBehaviour, ICheckMesh
    {
        [SerializeField] Camera ARCamera;

        private bool hitMesh;
        private Vector3 hitPoint;
        private Vector3 hitNormal;
        private float hitDistance;

        bool ICheckMesh.HitMesh { get { return hitMesh; } set { hitMesh = value; } }
        Vector3 ICheckMesh.HitPoint => hitPoint;
        Vector3 ICheckMesh.HitNormal => hitNormal;
        float ICheckMesh.HitDistance => hitDistance;

        // Update is called once per frame
        void Update()
        {
            if(Input.GetMouseButtonDown(0))
            {
                Ray clickRay = ARCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit = new RaycastHit();

                if(Physics.Raycast(clickRay, out hit))
                {
                    if(hit.collider.gameObject.CompareTag("ScannedMesh"))
                    {
                        hitMesh = true;

                        hitPoint = hit.point;
                        hitNormal = hit.normal;
                        hitDistance = hit.distance;

                        Debug.Log("touched");
                    }
                }
            }
        }
    }
}
