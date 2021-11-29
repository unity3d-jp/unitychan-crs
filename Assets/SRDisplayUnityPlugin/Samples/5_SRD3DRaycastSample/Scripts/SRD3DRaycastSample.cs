/*
 * Copyright 2019,2020 Sony Corporation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SRD.Core;
using SRD.Utils;

namespace SRD.Sample.Raycast
{
    public class SRD3DRaycastSample : MonoBehaviour
    {
        public GameObject PointerObject;
        public Color DraggingColor = Color.cyan;

        private SRDManager _srdManager;
        private SRDCameras _srdCameras;

        private ColorToggler _colorToggler;

        private bool _isDragging = false;
        private GameObject _targetObjectCache;
        private Vector3 _mousePosInWorldCache;

        private readonly int LeftMouseButton = 0;

        void Start()
        {
            _srdManager = SRDSceneEnvironment.GetSRDManager();
            _srdCameras = new SRDCameras(_srdManager);
            _colorToggler = new ColorToggler(DraggingColor);

            if(PointerObject != null)
            {
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            Cursor.visible = true;
        }

        void Update()
        {
            var mousePosInScreen = Input.mousePosition;
            var mousePosInWorld = _srdCameras.ScreenToWorldPoint(mousePosInScreen);

            var ray = _srdCameras.ScreenPointToRay(mousePosInScreen);
            RaycastHit hit;
            if(!Physics.Raycast(ray, out hit))
            {
                if(_isDragging)
                {
                    _colorToggler.TurnOff(_targetObjectCache.GetComponent<MeshRenderer>());
                }
                if(PointerObject != null)
                {
                    PointerObject.transform.position = mousePosInWorld;
                }
                return;
            }

            if(PointerObject != null)
            {
                PointerObject.transform.position = hit.point;
            }
            MoveTarget(hit.collider.gameObject, mousePosInWorld);
        }

        private void MoveTarget(GameObject target, Vector3 mousePosInWorld)
        {
            var mouseDownThisFrame = Input.GetMouseButtonDown(LeftMouseButton);
            var mouseUpThisFrame = Input.GetMouseButtonUp(LeftMouseButton);
            var mousePressed = Input.GetMouseButton(LeftMouseButton);

            _targetObjectCache = target;

            if(mouseDownThisFrame)
            {
                _isDragging = true;
                _colorToggler.TurnOn(_targetObjectCache.GetComponent<MeshRenderer>());
                _mousePosInWorldCache = mousePosInWorld;
            }
            else if(mouseUpThisFrame)
            {
                _isDragging = false;
                _colorToggler.TurnOff(_targetObjectCache.GetComponent<MeshRenderer>());
            }
            else if(mousePressed)
            {
                var diff = mousePosInWorld - _mousePosInWorldCache;

                var watcherTransform = _srdCameras.WatcherAnchorObject.transform;
                var diffInWatcherCoord = watcherTransform.InverseTransformVector(diff);
                diffInWatcherCoord.z = 0;

                _targetObjectCache.transform.position += watcherTransform.TransformVector(diffInWatcherCoord);

                _mousePosInWorldCache = mousePosInWorld;
            }
        }

        class ColorToggler
        {
            private Color _defaultColorCache;
            private Color _turnOnColor;
            private bool _isOn = false;
            public ColorToggler(Color turnOnColor)
            {
                _turnOnColor = turnOnColor;
            }

            public void TurnOn(MeshRenderer mr)
            {
                if(_isOn)
                {
                    return;
                }
                _defaultColorCache = mr.material.color;
                mr.material.color = _turnOnColor;
                _isOn = true;
            }

            public void TurnOff(MeshRenderer mr)
            {
                if(!_isOn)
                {
                    return;
                }
                mr.material.color = _defaultColorCache;
                _isOn = false;
            }
        }
    }

}
