/*
 * Copyright 2019,2020 Sony Corporation
 */

using SRD.Core;
using SRD.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SRD.Sample.UI
{
    public class PointerSample : MonoBehaviour
    {
        private GameObject _pointerObject;

        void Start()
        {
            _pointerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _pointerObject.name = "Pointer";
            _pointerObject.transform.localScale = Vector3.one * 0.2f;
            _pointerObject.GetComponent<Renderer>().material.color = Color.red;
            Destroy(_pointerObject.GetComponent<SphereCollider>());
        }

        void OnDisable()
        {
            Cursor.visible = true;
        }

        public void OnCanvasEnter(Vector3 RayOriginPosition, Vector3 SRDScreenPosition, Vector3 CanvasRectPosition)
        {
            if(!this.enabled)
            {
                return;
            }

            Debug.Log("OnCanvasEnter");
            Cursor.visible = false;
            _pointerObject.transform.position = CanvasRectPosition;
        }

        public void OnCanvasHitting(Vector3 RayOriginPosition, Vector3 SRDScreenPosition, Vector3 CanvasRectPosition)
        {
            if(!this.enabled)
            {
                return;
            }

            _pointerObject.transform.position = CanvasRectPosition;
#if UNITY_EDITOR
            Debug.DrawRay(RayOriginPosition, (SRDScreenPosition - RayOriginPosition), Color.red);
#endif
        }

        public void OnCanvasExit(Vector3 RayOriginPosition, Vector3 SRDScreenPosition, Vector3 CanvasRectPosition)
        {
            if(!this.enabled)
            {
                return;
            }

            Debug.Log("OnCanvasExit");
            Cursor.visible = true;
            _pointerObject.transform.position = CanvasRectPosition;
        }
    }
}
