/*
 * Copyright 2019,2020 Sony Corporation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SRD.Utils;

namespace SRD.Core
{
    [System.Serializable]

    internal struct SRDSystemDescription
    {
        public SRDSystemDescription(FaceTrackerSystem faceTracker, EyeViewRendererSystem eyeViewRenderer, StereoCompositerSystem stereoCompositer)
        {
            faceTrackerSystem = faceTracker;
            eyeViewRendererSystem = eyeViewRenderer;
            stereoCompositerSystem = stereoCompositer;
        }
        public FaceTrackerSystem faceTrackerSystem;
        public EyeViewRendererSystem eyeViewRendererSystem;
        public StereoCompositerSystem stereoCompositerSystem;
    }

    internal class SRDCoreRenderer: ISRDSubsystem
    {
        public Action<bool> OnSRDFaceTrackStateEvent;

        private SRDSystemDescription _currSystemDesc;
        private ISRDFaceTracker _faceTracker;
        private ISRDEyeViewRenderer _eyeViewRenderer;
        private ISRDStereoCompositer _stereoCompositer;

        private Dictionary<FaceTrackerSystem, ISRDFaceTracker> _cachedFaceTracker;
        private Dictionary<EyeViewRendererSystem, ISRDEyeViewRenderer> _cachedEyeViewRenderer;
        private Dictionary<StereoCompositerSystem, ISRDStereoCompositer> _cachedStereoCompositer;

        private bool _isStereoTextureRegistered;

        public SRDCoreRenderer(SRDSystemDescription description)
        {
            _cachedFaceTracker = new Dictionary<FaceTrackerSystem, ISRDFaceTracker>();
            _cachedEyeViewRenderer = new Dictionary<EyeViewRendererSystem, ISRDEyeViewRenderer>();
            _cachedStereoCompositer = new Dictionary<StereoCompositerSystem, ISRDStereoCompositer>();

            _currSystemDesc = description;
        }

        public void Update(Transform srdOriginTransform, bool isBoxFrontNearClipActive)
        {
            _faceTracker.UpdateState(srdOriginTransform);
            var xrResult = _eyeViewRenderer.UpdateFacePose(_faceTracker, isBoxFrontNearClipActive);

            if(OnSRDFaceTrackStateEvent != null)
            {
                OnSRDFaceTrackStateEvent.Invoke(xrResult == SrdXrResult.SUCCESS);
            }
        }

        public void Composite(bool IsSRRenderingActive)
        {
            if(!_isStereoTextureRegistered)
            {
                _stereoCompositer.RegisterSourceStereoTextures(_eyeViewRenderer.GetLeftEyeViewTexture(),
                                                               _eyeViewRenderer.GetRightEyeViewTexture());
                _isStereoTextureRegistered = true;
            }
            else
            {
                _stereoCompositer.RenderStereoComposition(IsSRRenderingActive);
            }
        }

        public void Start()
        {
            InitializeFaceTracker(_currSystemDesc.faceTrackerSystem);
            InitializeEyeViewRenderer(_currSystemDesc.eyeViewRendererSystem);
            InitializeStereoCompositer(_currSystemDesc.stereoCompositerSystem);
        }

        public void Stop()
        {
            _faceTracker.Stop();
            _eyeViewRenderer.Stop();
            _stereoCompositer.Stop();
        }

        public void Dispose()
        {
            OnSRDFaceTrackStateEvent = null;

            foreach(var cache in _cachedFaceTracker)
            {
                cache.Value.Dispose();
            }
            foreach(var cache in _cachedEyeViewRenderer)
            {
                cache.Value.Dispose();
            }
            foreach(var cache in _cachedStereoCompositer)
            {
                cache.Value.Dispose();
            }
        }

        public void UpdateSRDSystemsAndRefresh(SRDSystemDescription srdSystemDesc)
        {
            UpdateFaceTrackerAndRefresh(srdSystemDesc.faceTrackerSystem);
            UpdateEyeViewRendererAndRefresh(srdSystemDesc.eyeViewRendererSystem);
            UpdateStereoCompositerAndRefresh(srdSystemDesc.stereoCompositerSystem);
        }

        private void InitializeFaceTracker(FaceTrackerSystem faceTrackerSystem)
        {
            if(!_cachedFaceTracker.TryGetValue(faceTrackerSystem, out _faceTracker))
            {
                _faceTracker = SRDFaceTrackerFactory.CreateFaceTracker(faceTrackerSystem);
                _cachedFaceTracker[faceTrackerSystem] = _faceTracker;
            }

            _currSystemDesc.faceTrackerSystem = faceTrackerSystem;
            _faceTracker.Start();
        }

        private void InitializeEyeViewRenderer(EyeViewRendererSystem eyeViewRendererSystem)
        {
            if(!_cachedEyeViewRenderer.TryGetValue(eyeViewRendererSystem, out _eyeViewRenderer))
            {
                _eyeViewRenderer = SRDEyeViewRendererFactory.CreateEyeViewRenderer(eyeViewRendererSystem);
                _cachedEyeViewRenderer[eyeViewRendererSystem] = _eyeViewRenderer;
            }

            _currSystemDesc.eyeViewRendererSystem = eyeViewRendererSystem;
            _isStereoTextureRegistered = false;

            _eyeViewRenderer.Start();
        }

        private void InitializeStereoCompositer(StereoCompositerSystem stereoCompositerSystem)
        {
            if(!_cachedStereoCompositer.TryGetValue(stereoCompositerSystem, out _stereoCompositer))
            {
                _stereoCompositer = SRDStereoCompositerFactory.CreateStereoCompositer(stereoCompositerSystem);
                _cachedStereoCompositer[stereoCompositerSystem] = _stereoCompositer;
            }

            _currSystemDesc.stereoCompositerSystem = stereoCompositerSystem;
            _stereoCompositer.Start();
        }



        private void UpdateFaceTrackerAndRefresh(FaceTrackerSystem faceTrackerSystem)
        {
            if(_currSystemDesc.faceTrackerSystem == faceTrackerSystem)
            {
                return;
            }
            _faceTracker.Stop();
            InitializeFaceTracker(faceTrackerSystem);
        }

        private void UpdateEyeViewRendererAndRefresh(EyeViewRendererSystem eyeViewRendererSystem)
        {
            if(_currSystemDesc.eyeViewRendererSystem == eyeViewRendererSystem)
            {
                return;
            }
            _eyeViewRenderer.Stop();
            InitializeEyeViewRenderer(eyeViewRendererSystem);
        }

        private void UpdateStereoCompositerAndRefresh(StereoCompositerSystem stereoCompositerSystem)
        {
            if(_currSystemDesc.stereoCompositerSystem == stereoCompositerSystem)
            {
                return;
            }
            _stereoCompositer.Stop();
            InitializeStereoCompositer(stereoCompositerSystem);
        }

    }
}
