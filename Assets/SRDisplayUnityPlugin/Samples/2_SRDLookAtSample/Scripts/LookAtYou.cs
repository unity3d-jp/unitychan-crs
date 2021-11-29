/*
 * Copyright 2019,2020 Sony Corporation
 */
using UnityEngine;

namespace SRD.Sample.LookAt
{
    public class LookAtYou : MonoBehaviour
    {
        public GameObject LookAtTarget;

        void Start()
        {
            if(LookAtTarget == null)
            {
                Debug.LogWarning("Add \"WatcherObject\" in Inspector");
            }
        }

        void Update()
        {
            if(LookAtTarget == null)
            {
                return;
            }

            var forwardVec = LookAtTarget.transform.position - this.transform.position;
            this.transform.rotation = Quaternion.LookRotation(forwardVec, Vector3.up);
        }
    }
}
