/*
 * Copyright 2019,2020 Sony Corporation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using SRD.Core;

namespace SRD.Utils
{
    [RequireComponent(typeof(Canvas))]
    public class SRDGraphicRaycaster : GraphicRaycaster
    {
        /// <summary>
        /// A UnityEvent callback for an interaction with the graphic raycast is and canvas.
        /// The arguments are a origin position of the ray, a position of the ray on Spatial Reality Display screen, and a position of the ray on the hitting canvas
        /// </summary>
        [System.Serializable]
        public class SRDCanvasInteractionEvent : UnityEvent<Vector3, Vector3, Vector3> { };

        /// <summary>
        /// A callback that are called when the ray is hitting some canvas.
        /// </summary>
        public SRDCanvasInteractionEvent OnCanvasHitEvent;

        /// <summary>
        /// A callback that are called when the ray starts hitting some canvas.
        /// </summary>
        public SRDCanvasInteractionEvent OnCanvasEnterEvent;

        /// <summary>
        /// A callback that are called when the ray has finished hitting some canvas.
        /// </summary>
        public SRDCanvasInteractionEvent OnCanvasExitEvent;

        private Canvas _canvas;
        private RectTransform _canvasRectTransform;

        private bool _isInCanvas = false;
        private Vector3 _eventPosInCanvasCache;

        private SRDManager _srdManager;
        private SRDCameras _srdCameras;

        protected override void Start()
        {
            _srdManager = SRD.Utils.SRDSceneEnvironment.GetSRDManager();
            _srdCameras = new SRDCameras(_srdManager);

            if(_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }
            if(_canvas.worldCamera == null)
            {
                _canvas.worldCamera = _srdCameras.WatcherCamera;
            }
            _canvasRectTransform = _canvas.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        private List<RaycastResult> _sortedResults = new List<RaycastResult>();
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if(!ScreenContainsPosition(eventData.position))
            {
                return;
            }
            if(_canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogError("SRD supports WorldSpace UI only");
                return;
            }

            var canvasGraphics = GraphicRegistry.GetGraphicsForCanvas(_canvas).ToList();
            if(canvasGraphics == null || canvasGraphics.Count == 0)
            {
                return;
            }

            var currentEventCamera = _canvas.worldCamera == null ? _srdCameras.WatcherCamera : _canvas.worldCamera;

            var eventPosInSRDScreen = _srdCameras.ScreenToWorldPoint(_srdCameras.SRDScreenToScreen(eventData.position));
            var cameraPosition = currentEventCamera.transform.position;
            var ray = new Ray(cameraPosition, (eventPosInSRDScreen - cameraPosition).normalized);
            var hitDistance = CalcHitDistance(ray);

            CheckOnCanvasEvent(ray, eventPosInSRDScreen, currentEventCamera);

            Vector3 eventPosInCanvas;
            _sortedResults.Clear();
            foreach(var graphic in canvasGraphics)
            {
                if(!CheckUIHit(ray, graphic.rectTransform, currentEventCamera, out eventPosInCanvas))
                {
                    continue;
                }

                if(ignoreReversedGraphics)
                {
                    var cameraFoward = ray.direction;
                    var dir = graphic.gameObject.transform.rotation * Vector3.forward;
                    if(Vector3.Dot(cameraFoward, dir) <= 0)
                    {
                        continue;
                    }
                }

                float distance = Vector3.Distance(ray.origin, eventPosInCanvas);
                if(distance >= hitDistance)
                {
                    continue;
                }

                _sortedResults.Add(new RaycastResult
                {
                    gameObject = graphic.gameObject,
                    module = this,
                    distance = distance,
                    depth = graphic.depth,
                    worldPosition = eventPosInCanvas,
                });
            }
            _sortedResults.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));

            for(var i = 0; i < _sortedResults.Count; i++)
            {
                var rr = _sortedResults[i];
                rr.index = i;
                resultAppendList.Add(rr);
            }
        }

        private bool CheckUIHit(Ray ray, RectTransform targetRectTransform, Camera eventCamera, out Vector3 eventPosInCanvas)
        {
            eventPosInCanvas = GetPositionInRectTransformPlane(targetRectTransform, ray);
            var screenPoint = eventCamera.WorldToScreenPoint(eventPosInCanvas);
            return RectTransformUtility.RectangleContainsScreenPoint(targetRectTransform, screenPoint, eventCamera);
        }

        private static Vector3 GetPositionInRectTransformPlane(RectTransform rectTransform, Ray ray)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var plane = new Plane(corners[0], corners[1], corners[2]);
            return GetPositionInPlane(plane, ray);
        }

        private static Vector3 GetPositionInPlane(Plane plane, Ray ray)
        {
            float enter = 0.0f;
            plane.Raycast(ray, out enter);
            return ray.GetPoint(enter);
        }

        private static bool ScreenContainsPosition(Vector2 position)
        {
            if(position.x < 0 || position.y < 0)
            {
                return false;
            }
            if(position.x > Screen.width || position.y > Screen.height)
            {
                return false;
            }
            return true;
        }

        private void CheckOnCanvasEvent(Ray ray, Vector3 eventPosInSRDScreen, Camera eventCamera)
        {
            Vector3 eventPosInCanvas;
            if(CheckUIHit(ray, _canvasRectTransform, eventCamera, out eventPosInCanvas))
            {
                if(_isInCanvas)
                {
                    if(OnCanvasHitEvent != null)
                    {
                        OnCanvasHitEvent.Invoke(ray.origin, eventPosInSRDScreen, eventPosInCanvas);
                    }
                }
                else
                {
                    if(OnCanvasEnterEvent != null)
                    {
                        OnCanvasEnterEvent.Invoke(ray.origin, eventPosInSRDScreen, eventPosInCanvas);
                    }
                }
                _isInCanvas = true;
                _eventPosInCanvasCache = eventPosInCanvas;
            }
            else
            {
                if(_isInCanvas)
                {
                    if(OnCanvasExitEvent != null)
                    {
                        OnCanvasExitEvent.Invoke(ray.origin, eventPosInSRDScreen, _eventPosInCanvasCache);
                    }
                }
                _isInCanvas = false;
            }
        }

        private float CalcHitDistance(Ray ray)
        {
            float hitDistance = float.MaxValue;

            if(blockingObjects != BlockingObjects.None)
            {
                float dist = eventCamera.farClipPlane;

                if(blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    var hits = Physics.RaycastAll(ray, dist, m_BlockingMask);
                    if(hits.Length > 0 && hits[0].distance < hitDistance)
                    {
                        hitDistance = hits[0].distance;
                    }
                }

                if(blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    var hits = Physics2D.GetRayIntersectionAll(ray, dist, m_BlockingMask);
                    if(hits.Length > 0 && hits[0].fraction * dist < hitDistance)
                    {
                        hitDistance = hits[0].fraction * dist;
                    }
                }
            }

            return hitDistance;
        }

    }
}

