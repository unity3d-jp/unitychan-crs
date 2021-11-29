/*
 * Copyright 2019,2020,2021 Sony Corporation
 */


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using SRD.Utils;


namespace SRD.Core
{
    /// <summary>
    /// A core component for Spatial Reality Display that manages the session with SRD Runtime.
    /// </summary>
    //[ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class SRDManager : MonoBehaviour
    {
        /// <summary>
        /// A flag for SR Rendering
        /// </summary>
        /// <remarks>
        /// If this is disable, SR Rendering is turned off.
        /// </remarks>
        [Tooltip("If this is disable, SR Rendering is turned off.")]
        public bool IsSRRenderingActive = true;

        /// <summary>
        /// A flag for Spatial Clipping
        /// </summary>
        /// <remarks>
        /// If this is disable, the spatial clipping is turned off.
        /// </remarks>
        [Tooltip("If this is disable, the spatial clipping is turned off.")]
        public bool IsSpatialClippingActive = true;

        /// <summary>
        /// A flag for Crosstalk Correction
        /// </summary>
        /// <remarks>
        /// If this is disable, the crosstalk correction is turned off.
        /// </remarks>
        [Tooltip("If this is disable, the crosstalk correction is turned off.")]
        public bool IsCrosstalkCorrectionActive = true;

        /// <summary>
        /// Crosstalk Correction Type
        /// </summary>
        /// <remarks>
        /// This is valid only if the crosstalk correction is active.
        /// </remarks>
        [Tooltip("This is valid only if the crosstalk correction is active.")]
        public SrdXrCrosstalkCorrectionType CrosstalkCorrectionType;

        private SRDCrosstalkCorrection _srdCrosstalkCorrection;

        private SRDSystemDescription _description;

        private const float _minScaleInInspector = 0.1f;
        private const float _maxScaleInInspector = 1000.0f; // float.MaxValue;
        [SerializeField]
        [Range(_minScaleInInspector, _maxScaleInInspector), Tooltip("The scale of SRDisplay View Space")]
        private float _SRDViewSpaceScale = 1.0f;

        /// <summary>
        /// The scale of SRDisplay View Space
        /// </summary>
        public float SRDViewSpaceScale
        {
            get
            {
                if(_SRDViewSpaceScale <= 0)
                {
                    Debug.LogWarning(String.Format("Wrong SRDViewSpaceScale: {0} \n SRDViewSpaceScale must be 0+. Now SRDViewSpaceScale is forced to 1.0.", _SRDViewSpaceScale));
                    _SRDViewSpaceScale = 1.0f;
                }
                return _SRDViewSpaceScale;
            }
            set
            {
                _SRDViewSpaceScale = value;
            }
        }

        #region Events
        /// <summary>
        /// A UnityEvent callback containing SRDisplayViewSpaceScale.
        /// </summary>
        [System.Serializable]
        public class SRDViewSpaceScaleChangedEvent : UnityEvent<float> { };
        /// <summary>
        /// An API of <see cref="SRDManager.SRDViewSpaceScaleChangedEvent"/>. Callbacks that are registered to this are called when SRDViewSpaceScale is changed.
        /// </summary>
        public SRDViewSpaceScaleChangedEvent OnSRDViewSpaceScaleChangedEvent;

        /// <summary>
        /// A UnityEvent callback containing a flag that describe FaceTrack is success or not in this frame.
        /// </summary>
        [System.Serializable]
        public class SRDFaceTrackStateEvent : UnityEvent<bool> { };
        /// <summary>
        /// An API of <see cref="SRDManager.SRDFaceTrackStateEvent"/>. Callbacks that are registered to this are called in every frame.
        /// </summary>
        public SRDFaceTrackStateEvent OnFaceTrackStateEvent;
        #endregion

        private Utils.DisplayEdges _displayEdges;
        /// <summary>
        /// Contains the positions of Spatial Reality Display edges and center.
        /// </summary>
        public Utils.DisplayEdges DisplayEdges {  get { return _displayEdges; } }

        private Coroutine _srRenderingCoroutine = null;

        private SRDCoreRenderer _srdCoreRenderer;
        internal SRDCoreRenderer SRDCoreRenderer { get { return _srdCoreRenderer; } }

        #region APIs
        /// <summary>
        /// An api to show/remove the CameraWindow
        /// </summary>
        /// <param name="isOn">The flag to show the CameraWindow. If this is true, the CameraWindow will open. If this is false, the CameraWindow will close.</param>
        /// <returns>Success or not.</returns>
        public bool ShowCameraWindow(bool isOn)
        {
            var res = SRDCorePlugin.ShowCameraWindow(SRDSessionHandler.SessionHandle, isOn);
            return (SrdXrResult.SUCCESS == res);
        }

        #endregion

        #region MainFlow

        void Awake()
        {
            UpdateSettings();
            foreach(var cond in GetForceQuitConditions())
            {
                if(cond.IsForceQuit)
                {
                    ForceQuitWithAssertion(cond.ForceQuitMessage);
                }
            }

            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                _description = new SRDSystemDescription(FaceTrackerSystem.Mouse,
                                                        EyeViewRendererSystem.UnityRenderCam,
                                                        StereoCompositerSystem.PassThrough);
            }
            else
            {
                _description = new SRDSystemDescription(FaceTrackerSystem.SRD,
                                                        EyeViewRendererSystem.UnityRenderCam,
                                                        StereoCompositerSystem.SRD);
            }

            _srdCoreRenderer = new SRDCoreRenderer(_description);
            _srdCoreRenderer.OnSRDFaceTrackStateEvent += (bool result) =>
            {
#if DEVELOPMENT_BUILD
                //Debug.LogWarning("No data from FaceRecognition: See the DebugWindow with F10");
#endif
                if(OnFaceTrackStateEvent != null)
                {
                    OnFaceTrackStateEvent.Invoke(result);
                }
            };

            SRDSessionHandler.Instance.CreateSession();

            _srdCrosstalkCorrection = new SRDCrosstalkCorrection();
        }

        void OnEnable()
        {
            SRDSessionHandler.Instance.RegisterSubsystem(_srdCoreRenderer);
            SRDSessionHandler.Instance.Start();
            CreateDisplayEdges();
            _srdCrosstalkCorrection.Init(ref IsCrosstalkCorrectionActive, ref CrosstalkCorrectionType);
            StartSRRenderingCoroutine();
        }

        void OnDisable()
        {
            SRDSessionHandler.Instance.Stop();
            StopSRRenderingCoroutine();
        }

        void Update()
        {
            SRDSessionHandler.Instance.PollEvent();
            UpdateScaleIfNeeded();
            _srdCrosstalkCorrection.HookUnityInspector(ref IsCrosstalkCorrectionActive, ref CrosstalkCorrectionType);
        }

        void OnValidate()
        {
            UpdateScaleIfNeeded();
        }

        void LateUpdate()
        {
            _srdCoreRenderer.Update(this.transform, IsSpatialClippingActive);
        }

        void OnDestroy()
        {
            _srdCoreRenderer.Dispose();
        }

        #endregion

        #region RenderingCoroutine

        private void StartSRRenderingCoroutine()
        {
            if(_srRenderingCoroutine == null)
            {
                _srRenderingCoroutine = StartCoroutine(SRRenderingCoroutine());
            }
        }

        private void StopSRRenderingCoroutine()
        {
            if(_srRenderingCoroutine != null)
            {
                StopCoroutine(_srRenderingCoroutine);
                _srRenderingCoroutine = null;
            }
        }

        private IEnumerator SRRenderingCoroutine()
        {
            var yieldEndOfFrame = new WaitForEndOfFrame();
            while(true)
            {
                yield return yieldEndOfFrame;
                _srdCoreRenderer.Composite(IsSRRenderingActive);
            }
        }
        #endregion


        #region Utils

        private void UpdateSettings()
        {
            QualitySettings.maxQueuedFrames = 0;
        }

        private void CreateDisplayEdges()
        {
            if(_displayEdges != null)
            {
                return;
            }
            SRDSettings.LoadBodyBounds();
            var dispEdges = new List<GameObject>();
            foreach(var edge in Utils.SRDSettings.DeviceInfo.BodyBounds.EdgePositions)
            {
                var go = new GameObject();
                go.transform.SetParent(this.transform);
                go.transform.localPosition = edge;
                go.transform.localRotation = Quaternion.identity;
                go.hideFlags = HideFlags.HideAndDontSave;
                dispEdges.Add(go);
            }
            _displayEdges = new Utils.DisplayEdges(dispEdges[0].transform, dispEdges[1].transform,
                                                   dispEdges[2].transform, dispEdges[3].transform);
        }

        private void UpdateScaleIfNeeded()
        {
            if(this.transform.localScale != Vector3.one * SRDViewSpaceScale)
            {
                this.transform.localScale = Vector3.one * SRDViewSpaceScale;
                if(OnSRDViewSpaceScaleChangedEvent != null)
                {
                    OnSRDViewSpaceScaleChangedEvent.Invoke(SRDViewSpaceScale);
                }
            }
        }

        struct ForceQuitCondition
        {
            public bool IsForceQuit;
            public string ForceQuitMessage;
            public ForceQuitCondition(bool isForceQuit, string forceQuitMessage)
            {
                IsForceQuit = isForceQuit;
                ForceQuitMessage = forceQuitMessage;
            }
        }

        private List<ForceQuitCondition> GetForceQuitConditions()
        {
            var ret = new List<ForceQuitCondition>();

            var isGraphicsAPINotSupported = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore &&
                                            SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;
            ret.Add(new ForceQuitCondition(isGraphicsAPINotSupported,
                                           "Select unsupported GraphicsAPI: GraphicsAPI must be DirectX11 or OpenGLCore."));

            var isSRPNotSupportedVersion = false;
#if !UNITY_2019_1_OR_NEWER
            isSRPNotSupportedVersion = (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null);
#endif
            ret.Add(new ForceQuitCondition(isSRPNotSupportedVersion,
                                           "SRP in Spatial Reality Display is supported in over 2019.1 only"));
            return ret;
        }

        private void ForceQuitWithAssertion(string assertionMessage)
        {
            Debug.LogAssertion(assertionMessage);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region GIZMO

        private void OnDrawGizmos()
        {
            if(!this.enabled)
            {
                return;
            }

            // Draw SRDisplay View Space
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(Utils.SRDSettings.DeviceInfo.BodyBounds.Center, Utils.SRDSettings.DeviceInfo.BodyBounds.BoxSize);
            Gizmos.color = Color.cyan;
            for(var i = 0; i < 4; i++)
            {
                var from = i % 4;
                var to = (i + 1) % 4;
                Gizmos.DrawLine(Utils.SRDSettings.DeviceInfo.BodyBounds.EdgePositions[from],
                                Utils.SRDSettings.DeviceInfo.BodyBounds.EdgePositions[to]);
            }
        }
        #endregion

    }
}










