/*
 * Copyright 2019,2020,2021 Sony Corporation
 */


using UnityEngine;

using SRD.Utils;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.Generic;

namespace SRD.Core
{
    /// <summary>
    /// A class for managing Cameras(WatcherCamera, LeftEyeCamera, and RightEyeCamera).
    /// </summary>
    public class SRDCameras
    {
        private SRDManager _srdManager;

        private GameObject _watcherAnchorObj;

        /// <summary>
        /// An anchor GameObject to show the user's head position at runtime.
        /// </summary>
        public GameObject WatcherAnchorObject { get { return _watcherAnchorObj; } }

        /// <summary>
        /// A transform of WatcherAnchor
        /// </summary>
        public Transform AnchorTransform { get { return _watcherAnchorObj.transform; } }

        private GameObject _watcherCameraObj;

        /// <summary>
        /// Has a camera component that is in the user's head position and will be disable at runtime.
        /// </summary>
        public GameObject WatcherCameraObject { get { return _watcherCameraObj; } }

        private Camera _watcherCamera;

        /// <summary>
        /// A camera component for raycasting from the user, UICamera, or something other than rendering.
        /// This will be disable at runtime.
        /// </summary>
        public Camera WatcherCamera { get { return _watcherCamera; } }

        private Dictionary<EyeType, Camera> _eyeCameras;

        /// <summary>
        /// A camera component to render a scene for the user's left eye.
        /// </summary>
        public Camera LeftEyeCamera { get { return GetEyeCamera(EyeType.Left); } }

        /// <summary>
        /// A camera component to render a scene for the user's right eye.
        /// </summary>
        public Camera RightEyeCamera { get { return GetEyeCamera(EyeType.Right); } }

        /// <summary>
        /// A constructor of SRDCameras.
        /// </summary>
        /// <param name="manager">Must set a SRDManager object in the current scene</param>
        public SRDCameras(SRDManager manager)
        {
            _srdManager = manager;

            var watcherAnchorName = SRDHelper.SRDConstants.WatcherGameObjDefaultName;
            _watcherAnchorObj = SRDSceneEnvironment.GetOrCreateChild(_srdManager.transform, watcherAnchorName);

            var watcherCameraName = SRDHelper.SRDConstants.WatcherCameraGameObjDefaultName;
            _watcherCameraObj = SRDSceneEnvironment.GetOrCreateChild(_watcherAnchorObj.transform, watcherCameraName);

            _watcherCamera = SRDSceneEnvironment.GetOrAddComponent<Camera>(_watcherCameraObj);
            _eyeCameras = new Dictionary<EyeType, Camera>();
        }

        internal GameObject GetOrCreateEyeCameraObj(EyeType type)
        {
            ToggleWatcherCamera(true);

            var eyeAnchorName = GetEyeAnchorName(type);
            var eyeAnchorObj = SRDSceneEnvironment.GetOrCreateChild(_watcherAnchorObj.transform, eyeAnchorName);

            var eyeCameraName = GetEyeCameraName(type);
            var eyeCameraObjTf = eyeAnchorObj.transform.Find(eyeCameraName);
            GameObject eyeCameraObj;
            if(eyeCameraObjTf == null)
            {
                eyeCameraObj = GameObject.Instantiate(_watcherCameraObj);
                eyeCameraObj.name = eyeCameraName;
                eyeCameraObj.transform.SetParent(eyeAnchorObj.transform);
            }
            else
            {
                eyeCameraObj = eyeCameraObjTf.gameObject;
                _eyeCameras[type] = eyeCameraObj.GetComponent<Camera>();
                if(_eyeCameras[type] == null)
                {
                    _eyeCameras[type] = eyeCameraObj.AddComponent<Camera>();
                    _eyeCameras[type].CopyFrom(_watcherCameraObj.GetComponent<Camera>());
                }
            }

            SRDSceneEnvironment.InitializePose(eyeCameraObj.transform);
            return eyeCameraObj;
        }

        internal void RemoveSourceCamera()
        {
            //UnityEngine.Object.DestroyImmediate(_watcherCameraObj);
            ToggleWatcherCamera(false);
        }

        private void ToggleWatcherCamera(bool isActive)
        {
            //_watcherCameraObj.SetActive(isActive);
            _watcherCamera.enabled = isActive;
        }

        /// <summary>
        /// Returns a ray going from the user through a screen point.
        /// Similar to <a href="https://docs.unity3d.com/ScriptReference/Camera.ScreenPointToRay.html">Camera.ScreenPointToRay</a>
        /// </summary>
        /// <param name="positionInScreen">Position in screen space to get from Unity API. For example, <a href="https://docs.unity3d.com/ScriptReference/Input-mousePosition.html">Input.mousePosition</a> </param>
        /// <returns>A ray going from the user through a screen point.</returns>
        public Ray ScreenPointToRay(Vector2 positionInScreen)
        {
            var posInWorld = ScreenToWorldPoint(positionInScreen);
            var cameraPosition = _watcherCamera.transform.position;
            return new Ray(cameraPosition, (posInWorld - cameraPosition).normalized);
        }

        /// <summary>
        /// Transform a point from screen space into world space.
        /// A return position must be on the Gizmo cyan wireframe plane from <see cref="SRDManager"/>
        /// Similar to <a href="https://docs.unity3d.com/ScriptReference/Camera.ScreenToWorldPoint.html">Camera.ScreenToWorldPoint</a>
        /// </summary>
        /// <param name="positionInScreen">Position in screen space to get from Unity API. For example, <a href="https://docs.unity3d.com/ScriptReference/Input-mousePosition.html">Input.mousePosition</a> </param>
        /// <returns>The world space point converted from the screen space point</returns>
        public Vector3 ScreenToWorldPoint(Vector2 positionInScreen)
        {
            var screenRate = new Vector2(positionInScreen.x / Screen.width, positionInScreen.y / Screen.height);
            var edges = _srdManager.DisplayEdges.Positions;
            var xDirection = edges[2] - edges[1];
            var yDirection = edges[0] - edges[1];
            return edges[1] + xDirection * screenRate.x + yDirection * screenRate.y;
        }

        /// <summary>
        /// Transform a point from world space into actual Spatial Reality Display screen space.
        /// </summary>
        /// <param name="positionInWorld">Position in world space</param>
        /// <returns>A position in Spatial Reality Display screen space</returns>
        public Vector2 WorldToSRDScreenPoint(Vector3 positionInWorld)
        {
            return ScreenToSRDScreen(_watcherCamera.WorldToScreenPoint(positionInWorld));
        }

        /// <summary>
        /// Transform a point from screen space that Unity provides into actual Spatial Reality Display screen space.
        /// The inverse transform of <see cref="SRDCameras.SRDScreenToScreen">
        /// </summary>
        /// <param name="positionInScreen">Position in screen space to get from Unity API. For example, <a href="https://docs.unity3d.com/ScriptReference/Input-mousePosition.html">Input.mousePosition</a></param>
        /// <returns>A position in Spatial Reality Display screen space</returns>
        public Vector2 ScreenToSRDScreen(Vector2 positionInScreen)
        {
            var h = SRDHelper.CalcHomographyMatrix(_srdManager.DisplayEdges.LeftUp.position,
                                                   _srdManager.DisplayEdges.LeftBottom.position,
                                                   _srdManager.DisplayEdges.RightBottom.position,
                                                   _srdManager.DisplayEdges.RightUp.position,
                                                   _watcherCamera);
            var x = positionInScreen.x / Screen.width;
            var y = positionInScreen.y / Screen.height;
            var w = h[6] * x + h[7] * y + h[8];
            return new Vector2(Screen.width * (h[0] * x + h[1] * y + h[2]) / w,
                               Screen.height * (h[3] * x + h[4] * y + h[5]) / w);

        }

        /// <summary>
        /// Transform a point from actual Spatial Reality Display screen space into rendering camera based screen space.
        /// The inverse transform of <see cref="SRDCameras.ScreenToSRDScreen">
        /// </summary>
        /// <param name="positionInSRDScreen">Position in Spatial Reality Display screen space </param>
        /// <returns>Position in rendering camera based screen space</returns>
        public Vector2 SRDScreenToScreen(Vector2 positionInSRDScreen)
        {
            var h = SRDHelper.CalcHomographyMatrix(_srdManager.DisplayEdges.LeftUp.position,
                                                   _srdManager.DisplayEdges.LeftBottom.position,
                                                   _srdManager.DisplayEdges.RightBottom.position,
                                                   _srdManager.DisplayEdges.RightUp.position,
                                                   _watcherCamera);
            h = SRDHelper.CalcInverseMatrix3x3(h);
            var x = positionInSRDScreen.x / Screen.width;
            var y = positionInSRDScreen.y / Screen.height;
            var w = h[6] * x + h[7] * y + h[8];
            return new Vector2(Screen.width * (h[0] * x + h[1] * y + h[2]) / w,
                               Screen.height * (h[3] * x + h[4] * y + h[5]) / w);

        }

        private Camera GetEyeCamera(EyeType type)
        {
            if(_eyeCameras.ContainsKey(type))
            {
                return _eyeCameras[type];
            }

            var eyeAnchorName = GetEyeAnchorName(type);
            var eyeAnchorObj = _watcherAnchorObj.transform.Find(eyeAnchorName);
            if(eyeAnchorObj == null)
            {
                return null;
            }

            var eyeCameraName = GetEyeCameraName(type);
            var eyeCameraObjTf = eyeAnchorObj.transform.Find(eyeCameraName);
            if(eyeCameraObjTf == null)
            {
                return null;
            }
            _eyeCameras[type] = eyeCameraObjTf.gameObject.GetComponent<Camera>();
            return _eyeCameras[type];
        }

        private string GetEyeAnchorName(EyeType type)
        {
            return SRDHelper.EyeSideName[type] + SRDHelper.SRDConstants.EyeAnchorGameObjDefaultName;
        }

        private string GetEyeCameraName(EyeType type)
        {
            return SRDHelper.EyeSideName[type] + SRDHelper.SRDConstants.EyeCamGameObjDefaultName;
        }

    }
}
