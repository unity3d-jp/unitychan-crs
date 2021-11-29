/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using SRD.Utils;

namespace SRD.Core
{

    internal interface ISRDFaceTracker : ISRDSubsystem
    {
        void UpdateState(Transform srdWorldOrigin);
        // return a current user face pose in the Unity World Coordinate
        SrdXrResult GetCurrentFacePose(out FacePose facePose);
        SrdXrResult GetCurrentProjMatrices(float nearClip, float farClip, out EyeProjectionMatrices projMatrices);
    }

    internal class SRDFaceTracker : ISRDFaceTracker
    {
        private FacePose _prevFacePose;
        private EyeProjectionMatrices _prevProjMatrix;
        private Transform _currentOrigin;

        public SRDFaceTracker()
        {
        }

        public void UpdateState(Transform srdWorldOrigin)
        {
            _currentOrigin = srdWorldOrigin;
            SRDCorePlugin.BeginFrame(SRDSessionHandler.SessionHandle,
                                     callInMainThread: true, callinRenderThread: true);
        }

        public SrdXrResult GetCurrentFacePose(out FacePose facePose)
        {
            facePose = (_prevFacePose != null) ? _prevFacePose : SRDFaceTracker.CreateDefaultFacePose().GetTransformedBy(_currentOrigin);

            var headPose = facePose.HeadPose;
            var eyePoseL = facePose.EyePoseL;
            var eyePoseR = facePose.EyePoseR;
            var xrResult = SRDCorePlugin.GetFacePose(SRDSessionHandler.SessionHandle,
                                                     out headPose, out eyePoseL, out eyePoseR);

            facePose = (new FacePose(headPose, eyePoseL, eyePoseR)).GetTransformedBy(_currentOrigin);
            if(xrResult == SrdXrResult.ERROR_HANDLE_INVALID || xrResult == SrdXrResult.ERROR_SESSION_NOT_RUNNING)
            {
                facePose = SRDFaceTracker.CreateDefaultFacePose().GetTransformedBy(_currentOrigin);
            }
            _prevFacePose = facePose;
            return xrResult;
        }

        public SrdXrResult GetCurrentProjMatrices(float nearClip, float farClip, out EyeProjectionMatrices projMatrices)
        {
            projMatrices = (_prevProjMatrix != null) ? _prevProjMatrix : SRDFaceTracker.CreateDefaultProjMatrices();

            var leftMatrix = projMatrices.LeftMatrix;
            var rightMatrix = projMatrices.RightMatrix;
            var xrResult = SRDCorePlugin.GetProjectionMatrix(SRDSessionHandler.SessionHandle, nearClip, farClip,
                                                             out leftMatrix, out rightMatrix);

            projMatrices = new EyeProjectionMatrices(leftMatrix, rightMatrix);
            if(xrResult == SrdXrResult.ERROR_HANDLE_INVALID || xrResult == SrdXrResult.ERROR_SESSION_NOT_RUNNING)
            {
                projMatrices = SRDFaceTracker.CreateDefaultProjMatrices();
            }
            _prevProjMatrix = projMatrices;
            return xrResult;
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

        public static FacePose CreateDefaultFacePose()
        {
            var facePose = new FacePose();
            facePose.HeadPose.position = new Vector3(0f, 0.2f, -0.3f);

            var dispCenter = Utils.SRDSettings.DeviceInfo.BodyBounds.Center;
            var forward = dispCenter - facePose.HeadPose.position;
            var up = Vector3.Cross(Vector3.right, forward);
            facePose.HeadPose.rotation = Quaternion.LookRotation(forward, up);

            facePose.UpdateWithNewHeadPose(facePose.HeadPose, dispCenter);
            return facePose;
        }

        public static EyeProjectionMatrices CreateDefaultProjMatrices()
        {
            var eyeProjMatrices = new EyeProjectionMatrices();
            var aspect = (float)SRD.Utils.SRDSettings.DeviceInfo.ScreenRect.Width / (float)SRD.Utils.SRDSettings.DeviceInfo.ScreenRect.Height;
            eyeProjMatrices.LeftMatrix = Matrix4x4.Perspective(40f, aspect, 0.3f, 100f);
            eyeProjMatrices.RightMatrix = Matrix4x4.Perspective(40f, aspect, 0.3f, 100f);

            return eyeProjMatrices;
        }
    }


    internal class MouseBasedFaceTracker : ISRDFaceTracker
    {
        private FacePose _facePose;
        private Transform _currentOrigin;

        private Vector3 _focus;
        private Vector3 _prevMousePos;

        private Matrix4x4 _posTrackCoordTdispCenerCoord;
        private Matrix4x4 _dispCenerCoordTposTrackCoord;

        private readonly float MinFocusToPosition = 0.35f;
        private readonly float MaxFocusToPosition = 1.2f;
        private readonly float MovableConeHalfAngleDeg = 35.0f;

        enum MouseButtonDown
        {
            MBD_LEFT = 0, MBD_RIGHT, MBD_MIDDLE,
        };

        public MouseBasedFaceTracker()
        {
            _facePose = SRDFaceTracker.CreateDefaultFacePose();
            _focus = Utils.SRDSettings.DeviceInfo.BodyBounds.Center;

            var dispCenterPose = new Pose(Utils.SRDSettings.DeviceInfo.BodyBounds.Center, Quaternion.Euler(-45f, 180f, 0f));
            _posTrackCoordTdispCenerCoord = SRDHelper.PoseToMatrix(dispCenterPose);
            _dispCenerCoordTposTrackCoord = SRDHelper.PoseToMatrix(SRDHelper.InvPose(dispCenterPose));
        }

        public void UpdateState(Transform srdWorldOrigin)
        {
            _currentOrigin = srdWorldOrigin;
            var deltaWheelScroll = Input.GetAxis("Mouse ScrollWheel");
            var focusToPosition = _facePose.HeadPose.position - _focus;
            var updatedFocusToPosition = focusToPosition * (1.0f - deltaWheelScroll);
            if(updatedFocusToPosition.magnitude > MinFocusToPosition && updatedFocusToPosition.magnitude < MaxFocusToPosition)
            {
                _facePose.HeadPose.position = _focus + updatedFocusToPosition;
            }

            if(Input.GetMouseButtonDown((int)MouseButtonDown.MBD_RIGHT))
            {
                _prevMousePos = Input.mousePosition;
            }
            if(Input.GetMouseButton((int)MouseButtonDown.MBD_RIGHT))
            {
                var currMousePos = Input.mousePosition;
                var diff = currMousePos - _prevMousePos;
                diff.z = 0f;
                if(diff.magnitude > Vector3.kEpsilon)
                {
                    diff /= 1000f;
                    var posInDispCoord = _dispCenerCoordTposTrackCoord.MultiplyPoint3x4(_facePose.HeadPose.position);

                    var coneAngleFromNewPos = Vector3.Angle(Vector3.forward, posInDispCoord + diff);
                    if(Mathf.Abs(coneAngleFromNewPos) > MovableConeHalfAngleDeg)
                    {
                        var tangentLineInMovableCone = (new Vector2(-posInDispCoord.y, posInDispCoord.x)).normalized;
                        var diffXY = new Vector2(diff.x, diff.y);
                        if(Vector2.Angle(diffXY, tangentLineInMovableCone) > 90f)
                        {
                            tangentLineInMovableCone = -tangentLineInMovableCone;
                        }
                        diff = Vector2.Dot(tangentLineInMovableCone, diffXY) * tangentLineInMovableCone;
                    }
                    posInDispCoord += diff;
                    var coneRadianInCurrentZ = posInDispCoord.z * Mathf.Tan(Mathf.Deg2Rad * MovableConeHalfAngleDeg);
                    var radian = (new Vector2(posInDispCoord.x, posInDispCoord.y)).magnitude;
                    if(radian > coneRadianInCurrentZ)
                    {
                        posInDispCoord.x *= (coneRadianInCurrentZ / radian);
                        posInDispCoord.y *= (coneRadianInCurrentZ / radian);
                    }
                    posInDispCoord.z = Mathf.Sqrt(Mathf.Pow(updatedFocusToPosition.magnitude, 2f) - Mathf.Pow(((Vector2)posInDispCoord).magnitude, 2f));
                    _facePose.HeadPose.position = _posTrackCoordTdispCenerCoord.MultiplyPoint3x4(posInDispCoord);
                }

                _prevMousePos = currMousePos;
            }

            _facePose.HeadPose.rotation = Quaternion.LookRotation(_focus - _facePose.HeadPose.position, Vector3.up);
            _facePose.UpdateWithNewHeadPose(_facePose.HeadPose, _focus);
        }

        public SrdXrResult GetCurrentFacePose(out FacePose facePose)
        {
            facePose = _facePose.GetTransformedBy(_currentOrigin);
            return SrdXrResult.SUCCESS;
        }

        public SrdXrResult GetCurrentProjMatrices(float nearClip, float farClip, out EyeProjectionMatrices projMatrices)
        {
            var posInDispCoord = _dispCenerCoordTposTrackCoord.MultiplyPoint3x4(_facePose.HeadPose.position);
            var maxAng = 0f;
            foreach(var edge in Utils.SRDSettings.DeviceInfo.BodyBounds.EdgePositions)
            {
                var toEdge = edge - posInDispCoord;
                var toCenter = Utils.SRDSettings.DeviceInfo.BodyBounds.Center - posInDispCoord;
                var ang = Vector3.Angle(toCenter, toEdge);
                if(ang > maxAng)
                {
                    maxAng = ang;
                }
            }
            var projMat = Matrix4x4.Perspective(maxAng * 2.0f, 1f, nearClip, farClip);
            projMatrices = new EyeProjectionMatrices(projMat, projMat);
            return SrdXrResult.SUCCESS;
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

