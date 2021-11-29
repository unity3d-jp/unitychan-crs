/*
 * Copyright 2019,2020 Sony Corporation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Sample.UI
{
    public class SRDUISampleController : MonoBehaviour
    {
        public GameObject TargetGameObject;
        public MeshFilter TargetMeshFilter;

        public Mesh OptionAMesh;
        public Mesh OptionBMesh;
        public Mesh OptionCMesh;

        private List<Mesh> _objMeshList;
        private Vector3 _baseScale;

        void Start()
        {
            _objMeshList = new List<Mesh>() { OptionAMesh, OptionBMesh, OptionCMesh };
            _baseScale = TargetGameObject.transform.localScale;
        }

        void Update()
        {

        }

        public void ButtonClicked()
        {
            Debug.Log("ButtonClicked");
            StartCoroutine(AnimateMove(3f));
        }

        public void DropMenuChanged(int idx)
        {
            Debug.Log("DropMenuChanged");
            TargetMeshFilter.mesh = _objMeshList[idx];
        }

        public void ToggleChanged(bool toggle)
        {
            Debug.Log("ToggleChanged");
            TargetGameObject.SetActive(toggle);
        }

        public void SliderChanged(float rate)
        {
            Debug.Log("SliderChanged");
            TargetGameObject.transform.localScale = _baseScale + Vector3.up * rate;
        }

        IEnumerator AnimateMove(float duration)
        {
            var time = 0f;
            var rotationAxis = new Vector3(UnityEngine.Random.Range(0f, 1f),
                                           UnityEngine.Random.Range(0f, 1f),
                                           UnityEngine.Random.Range(0f, 1f)).normalized;
            Func<float, float> deltaAngleDegPerSec = (float t) => { return -8f * t * t * t * t + 18f * t * t * t - 11f * t * t + 1; };

            while(time < duration)
            {
                time += Time.deltaTime;
                TargetGameObject.transform.rotation *= Quaternion.AngleAxis(500f * deltaAngleDegPerSec((time / duration + 0.25f) * 0.8f) * Time.deltaTime,
                                                                            rotationAxis);
                yield return null;
            }
        }
    }
}
