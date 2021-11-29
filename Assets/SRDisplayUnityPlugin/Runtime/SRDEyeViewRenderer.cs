/*
 * Copyright 2019,2020,2021 Sony Corporation
 */

#if UNITY_2019_1_OR_NEWER
    #define SRP_AVAILABLE
#endif

using System;
using System.Collections.Generic;
using SRD.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if SRP_AVAILABLE
    using SRPCallbackFunc = System.Action<UnityEngine.Rendering.ScriptableRenderContext, UnityEngine.Camera>;
#endif

namespace SRD.Core
{
    internal interface ISRDEyeViewRenderer : ISRDSubsystem
    {
        SrdXrResult UpdateFacePose(ISRDFaceTracker faceTracker, bool isBoxFrontNearClipActive);
        // For control of scene render timing
        void Render();
        Texture GetLeftEyeViewTexture();
        Texture GetRightEyeViewTexture();
    }


    internal class SRDEyeViewRenderer : ISRDEyeViewRenderer
    {
        private SRDManager _srdManager;
        private ISRDFaceTracker _faceTracker;

        private Dictionary<EyeType, Transform> _eyeTransform;
        private Dictionary<EyeType, Camera> _eyeCamera;
        //private Dictionary<EyeType, RenderTexture> _eyeCamRenderTexture;
        private Dictionary<EyeType, RenderTexture> _eyeCamRenderTextureCache;
        private Dictionary<EyeType, Material> _eyeCamMaterial;
        private SRDCameras _srdCameras;

        private List<EyeType> _eyeTypes;
        private bool _isSRPUsed = false;
#if SRP_AVAILABLE
        private Dictionary<EyeType, SRPCallbackFunc> _eyeCamSRPPreCallback = new Dictionary<EyeType, SRPCallbackFunc>();
        private Dictionary<EyeType, SRPCallbackFunc> _eyeCamSRPPostCallback = new Dictionary<EyeType, SRPCallbackFunc>();
#endif
        private Dictionary<EyeType, Camera.CameraCallback> _eyeCamStateUpdateCallback = new Dictionary<EyeType, Camera.CameraCallback>();

        private FacePose _currentFacePose;
        private EyeProjectionMatrices _currentProjMatrices;
        private bool _isBoxFrontClippingCache = true;

        private readonly float ObliquedNearClipOffset = -0.025f;
        private readonly int RenderTextureDepth = 24;

        public SRDEyeViewRenderer()
        {
            _eyeTransform = new Dictionary<EyeType, Transform>();
            _eyeCamera = new Dictionary<EyeType, Camera>();
            //_eyeCamRenderTexture = new Dictionary<EyeType, RenderTexture>();
            _eyeCamRenderTextureCache = new Dictionary<EyeType, RenderTexture>();
            _eyeCamMaterial = new Dictionary<EyeType, Material>();

            _eyeTypes = new List<EyeType>() { EyeType.Left, EyeType.Right };
            _isSRPUsed = (GraphicsSettings.renderPipelineAsset != null);

            _currentFacePose = SRDFaceTracker.CreateDefaultFacePose();
            _currentProjMatrices = SRDFaceTracker.CreateDefaultProjMatrices();

            var width = SRDSettings.DeviceInfo.ScreenRect.Width;
            var height = SRDSettings.DeviceInfo.ScreenRect.Height;
            foreach(var type in _eyeTypes)
            {
                // RenderTarget
                //var outrt = new RenderTexture(width, height, RenderTextureDepth, RenderTextureFormat.ARGB32);
                //outrt.name = SRDHelper.SRDConstants.EyeCamRenderTexDefaultName + SRDHelper.EyeSideName[type] + "_Temp";
                //outrt.Create();
                //_eyeCamRenderTexture.Add(type, outrt);
                var camrt = new RenderTexture(width, height, RenderTextureDepth, RenderTextureFormat.ARGB32,
                                              (QualitySettings.desiredColorSpace == ColorSpace.Linear) ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
                camrt.name = SRDHelper.SRDConstants.EyeCamRenderTexDefaultName + SRDHelper.EyeSideName[type];
                camrt.Create();
                _eyeCamRenderTextureCache.Add(type, camrt);

                var homographyMaterial = new Material(Shader.Find("uHomography/Homography"));
                homographyMaterial.hideFlags = HideFlags.HideAndDontSave;
                _eyeCamMaterial[type] = homographyMaterial;
            }
        }

        ~SRDEyeViewRenderer()
        {
        }

        private void Initialize()
        {
            _srdManager = SRDSceneEnvironment.GetSRDManager();
            _srdCameras = new SRDCameras(_srdManager);

            foreach(var type in _eyeTypes)
            {
                var eyeCameraObj = _srdCameras.GetOrCreateEyeCameraObj(type);
                _eyeCamera[type] = SRDSceneEnvironment.GetOrAddComponent<Camera>(eyeCameraObj);
                _eyeCamera[type].targetTexture = _eyeCamRenderTextureCache[type];

                var eyeAnchorName = SRDHelper.EyeSideName[type] + SRDHelper.SRDConstants.EyeAnchorGameObjDefaultName;
                var eyeAnchor = SRDSceneEnvironment.GetOrCreateChild(_srdCameras.AnchorTransform, eyeAnchorName);
                _eyeTransform[type] = eyeAnchor.transform;
            }
            _srdCameras.RemoveSourceCamera();
        }

        private void SetupCameraUpdateCallback(EyeType type)
        {
            var eyeCamera = _eyeCamera[type];
            var eyeTransform = _eyeTransform[type];
            var homographyMaterial = _eyeCamMaterial[type];

            Action<Camera> updateState = (camera) =>
            {
                _faceTracker.GetCurrentFacePose(out _currentFacePose);
                var eyePose = _currentFacePose.GetEyePose(type);
                eyeTransform.SetPositionAndRotation(eyePose.position, eyePose.rotation);

                _faceTracker.GetCurrentProjMatrices(eyeCamera.nearClipPlane, eyeCamera.farClipPlane,
                                                    out _currentProjMatrices);
                var projMat = _currentProjMatrices.GetProjectionMatrix(type);

                if(!SRDHelper.HasNanOrInf(projMat))
                {
                    eyeCamera.ResetProjectionMatrix();
                    eyeCamera.fieldOfView = CalcVerticalFoVFromProjectionMatrix(projMat);
                    eyeCamera.aspect = CalcAspectWperHFromProjectionMatrix(projMat);
                    eyeCamera.projectionMatrix = projMat;

                    if(_isBoxFrontClippingCache)
                    {
                        var nearClipPlaneTF = _srdManager.DisplayEdges.LeftBottom;
                        eyeCamera.projectionMatrix = CalcObliquedNearClipProjectionMatrix(eyeCamera, nearClipPlaneTF.forward,
                                                                                          nearClipPlaneTF.position + nearClipPlaneTF.forward * ObliquedNearClipOffset * nearClipPlaneTF.lossyScale.x);
                    }
                }

                var homographyMat = SRDHelper.CalcHomographyMatrix(_srdManager.DisplayEdges.LeftUp.position, _srdManager.DisplayEdges.LeftBottom.position,
                                                                   _srdManager.DisplayEdges.RightBottom.position, _srdManager.DisplayEdges.RightUp.position,
                                                                   eyeCamera);
                var invHomographyMat = SRDHelper.CalcInverseMatrix3x3(homographyMat);
                homographyMaterial.SetFloatArray("_Homography", invHomographyMat);
                homographyMaterial.SetFloatArray("_InvHomography", homographyMat);

            };

            if(_isSRPUsed)
            {
#if SRP_AVAILABLE
                SRPCallbackFunc srpCallback = (context, camera) =>
                {
                    if(camera.name != eyeCamera.name)
                    {
                        return;
                    }
                    updateState(camera);
                };
                _eyeCamSRPPreCallback[type] = srpCallback;
                RenderPipelineManager.beginCameraRendering += _eyeCamSRPPreCallback[type];
#endif
            }
            else
            {
                Camera.CameraCallback cameraStateUpdate = (camera) =>
                {
                    if(camera.name != eyeCamera.name)
                    {
                        return;
                    }
                    updateState(camera);
                };
                _eyeCamStateUpdateCallback[type] = cameraStateUpdate;
                // This Should be onPreCull for correct frustum culling, however onPreCull is fired before vblank sometimes.
                // That's why onPreRender is used to make the latency shorter as possible
                Camera.onPreRender += _eyeCamStateUpdateCallback[type];
            }
        }

        private void SetupHomographyCallback(EyeType type)
        {
            var eyeCamera = _eyeCamera[type];
            var homographyMaterial = _eyeCamMaterial[type];

            if(_isSRPUsed)
            {
#if SRP_AVAILABLE
                SRPCallbackFunc srpCallback = (context, camera) =>
                {
                    if(camera.name != eyeCamera.name)
                    {
                        return;
                    }
                    var rt = RenderTexture.GetTemporary(_eyeCamera[type].targetTexture.descriptor);
                    Graphics.Blit(_eyeCamera[type].targetTexture, rt, homographyMaterial);
                    Graphics.Blit(rt, _eyeCamera[type].targetTexture);
                    rt.Release();
                    //Graphics.Blit(_eyeCamera[type].targetTexture, _eyeCamRenderTexture[type], homographyMaterial);
                };
                _eyeCamSRPPostCallback[type] = srpCallback;
                RenderPipelineManager.endCameraRendering += _eyeCamSRPPostCallback[type];
#endif
            }
            else
            {
                // CommandBuffer
                var camEvent = CameraEvent.AfterImageEffects;
                var buf = new CommandBuffer();
                buf.name = SRDHelper.SRDConstants.HomographyCommandBufferName;
                foreach(var attachedBuf in _eyeCamera[type].GetCommandBuffers(camEvent))
                {
                    if(attachedBuf.name == buf.name)
                    {
                        _eyeCamera[type].RemoveCommandBuffer(camEvent, attachedBuf);
                        break;
                    }
                }
                int temp = Shader.PropertyToID("_Temp");
                buf.GetTemporaryRT(temp, -1, -1, 0, FilterMode.Bilinear);
                buf.Blit(_eyeCamera[type].targetTexture, temp, homographyMaterial);
                buf.Blit(temp, _eyeCamera[type].targetTexture);
                buf.ReleaseTemporaryRT(temp);
                //buf.Blit(_eyeCamera[type].targetTexture, _eyeCamRenderTexture[type], homographyMaterial);

                _eyeCamera[type].AddCommandBuffer(camEvent, buf);
            }
        }


        public SrdXrResult UpdateFacePose(ISRDFaceTracker faceTracker, bool isBoxFrontNearClipActive)
        {
            _faceTracker = faceTracker;
            FacePose facePose;
            var xrResult = _faceTracker.GetCurrentFacePose(out facePose);

            _srdCameras.AnchorTransform.SetPositionAndRotation(facePose.HeadPose.position,
                                                               facePose.HeadPose.rotation);
            _isBoxFrontClippingCache = isBoxFrontNearClipActive;
            return xrResult;
        }

        public static EyeProjectionMatrices CreateDefaultProjMatrices()
        {
            var eyeProjMatrices = new EyeProjectionMatrices();
            var aspect = (float)SRD.Utils.SRDSettings.DeviceInfo.ScreenRect.Width / (float)SRD.Utils.SRDSettings.DeviceInfo.ScreenRect.Height;
            eyeProjMatrices.LeftMatrix = Matrix4x4.Perspective(35f, aspect, 0.3f, 100f);
            eyeProjMatrices.RightMatrix = Matrix4x4.Perspective(35f, aspect, 0.3f, 100f);

            return eyeProjMatrices;
        }

        public static Matrix4x4 CalcObliquedNearClipProjectionMatrix(Camera cam, Vector3 obliquedNearClipNormalVecInWorldCoord, Vector3 obliquedNearClipIncludedPointInWorldCoord)
        {
            var worldToCameraMatrix = cam.worldToCameraMatrix;
            var normalVecInCamCoord = worldToCameraMatrix.MultiplyVector(obliquedNearClipNormalVecInWorldCoord);
            var centerPosInCamCoord = worldToCameraMatrix.MultiplyPoint(obliquedNearClipIncludedPointInWorldCoord);
            var clipPlane = new Vector4(normalVecInCamCoord.x, normalVecInCamCoord.y, normalVecInCamCoord.z, -Vector3.Dot(normalVecInCamCoord, centerPosInCamCoord));
            return cam.CalculateObliqueMatrix(clipPlane);
        }

        public static float CalcVerticalFoVFromProjectionMatrix(Matrix4x4 projMat)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(1 / projMat.m11);
        }

        public static float CalcAspectWperHFromProjectionMatrix(Matrix4x4 projMat)
        {
            return projMat.m11 / projMat.m00;
        }

        public void Render()
        {
            foreach(var type in _eyeTypes)
            {
                _eyeCamera[type].Render();
            }
        }

        public Texture GetLeftEyeViewTexture()
        {
            return _eyeCamera[Utils.EyeType.Left].targetTexture;
            //return _eyeCamRenderTexture[Utils.EyeType.Left];
        }
        public Texture GetRightEyeViewTexture()
        {
            return _eyeCamera[Utils.EyeType.Right].targetTexture;
            //return _eyeCamRenderTexture[Utils.EyeType.Right];
        }

        public void Start()
        {
            Initialize();

            foreach(var type in _eyeTypes)
            {
                SetupCameraUpdateCallback(type);
                SetupHomographyCallback(type);
            }
        }

        public void Stop()
        {
            foreach(var type in _eyeTypes)
            {
                _eyeCamera[type].targetTexture.Release();

                if(_isSRPUsed)
                {
#if SRP_AVAILABLE
                    RenderPipelineManager.beginCameraRendering -= _eyeCamSRPPreCallback[type];
                    RenderPipelineManager.endCameraRendering -= _eyeCamSRPPostCallback[type];
#endif
                }
                else
                {
                    Camera.onPreRender -= _eyeCamStateUpdateCallback[type];
                }
            }
        }

        public void Dispose()
        {
            foreach(var type in _eyeTypes)
            {
                _eyeCamRenderTextureCache[type].Release();
                UnityEngine.Object.Destroy(_eyeCamRenderTextureCache[type]);
                //_eyeCamRenderTexture[type].Release();
                //UnityEngine.Object.Destroy(_eyeCamRenderTexture[type]);
            }
        }
    }

    internal class SRDTexturesBasedEyeViewRenderer : ISRDEyeViewRenderer, ISRDSubsystem
    {
        private Texture2D _leftTexture;
        private Texture2D _rightTexture;
        private SRDStereoTexture _stereoTextureIO;

        private readonly float DefaultNearClip = 0.3f;
        private readonly float DefaultFarClip = 100.0f;

        public SRDTexturesBasedEyeViewRenderer(Texture2D leftTexture, Texture2D rightTexture)
        {
            var texWidth = SRDSettings.DeviceInfo.ScreenRect.Width;
            var texHeight = SRDSettings.DeviceInfo.ScreenRect.Height;
            _leftTexture = new Texture2D(texWidth, texHeight);
            _rightTexture = new Texture2D(texWidth, texHeight);
            _stereoTextureIO = UnityEngine.Object.FindObjectOfType<SRDStereoTexture>();
            if(_stereoTextureIO)
            {
                UpdateTextures();
                return;
            }
            if(leftTexture != null && rightTexture != null)
            {
                Graphics.ConvertTexture(leftTexture, _leftTexture);
                Graphics.ConvertTexture(rightTexture, _rightTexture);
            }
        }

        ~SRDTexturesBasedEyeViewRenderer()
        {
        }

        public SrdXrResult UpdateFacePose(ISRDFaceTracker faceTracker, bool isBoxFrontNearClipActive)
        {
            if(_stereoTextureIO == null)
            {
                _stereoTextureIO = UnityEngine.Object.FindObjectOfType<SRDStereoTexture>();
            }

            if(_stereoTextureIO && _stereoTextureIO.Changed)
            {
                UpdateTextures();
            }
            return SrdXrResult.SUCCESS;
        }

        private void UpdateTextures()
        {
            if(_stereoTextureIO.leftTexture && _stereoTextureIO.rightTexture)
            {
                Graphics.ConvertTexture(_stereoTextureIO.leftTexture, _leftTexture);
                Graphics.ConvertTexture(_stereoTextureIO.rightTexture, _rightTexture);
            }
            _stereoTextureIO.ResolveChanges();
        }

        public void Render()
        {
            //do nothing
        }
        public Texture GetLeftEyeViewTexture()
        {
            return _leftTexture;
        }
        public Texture GetRightEyeViewTexture()
        {
            return _rightTexture;
        }

        public float GetNearClip()
        {
            return DefaultNearClip;
        }

        public float GetFarClip()
        {
            return DefaultFarClip;
        }

        public void Start()
        {
            // do nothing
        }

        public void Stop()
        {
            // do nothing
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}


