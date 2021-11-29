/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    public enum EyeType
    {
        Left = 0,
        Right = 1
    }

    /// <summary>
    /// A class to keep Spatial Reality Display's edge positions
    /// </summary>
    public class DisplayEdges
    {
        /// <summary>
        /// A constructor of DisplayEdges.
        /// </summary>
        /// <param name="leftUp"> A Trnsform of LeftUp edge. </param>
        /// <param name="leftBottom">  A Trnsform of LeftBottom edge. </param>
        /// <param name="rightBottom"> A Trnsform of RightBottom edge. </param>
        /// <param name="rightUp"> A Trnsform of RightUp edge. </param>
        public DisplayEdges(Transform leftUp, Transform leftBottom, Transform rightBottom, Transform rightUp)
        {
            this.leftUp = leftUp;
            this.leftBottom = leftBottom;
            this.rightUp = rightUp;
            this.rightBottom = rightBottom;
        }

        private Transform leftUp;
        /// <summary>
        /// A Trnsform of LeftUp edge.
        /// </summary>
        public Transform LeftUp { get { return leftUp; } }

        private Transform leftBottom;
        /// <summary>
        /// A Trnsform of LeftBottom edge.
        /// </summary>
        public Transform LeftBottom { get {return leftBottom; } }

        private Transform rightUp;
        /// <summary>
        /// A Trnsform of RightUp edge.
        /// </summary>
        public Transform RightUp { get { return rightUp; } }

        private Transform rightBottom;
        /// <summary>
        /// A Trnsform of RightBottom edge.
        /// </summary>
        public Transform RightBottom { get { return rightBottom; } }

        /// <summary>
        /// Center position of Spatial Reality Display
        /// </summary>
        public Vector3 CenterPosition
        {
            get
            {
                return (this.leftBottom.position + this.rightUp.position) / 2f;
            }
        }

        /// <summary>
        /// Normal vector of Spatial Reality Display
        /// </summary>
        public Vector3 Normal
        {
            get
            {
                var lhs = this.RightBottom.position - this.leftBottom.position;
                var rhs = this.leftUp.position - this.leftBottom.position;
                return -Vector3.Cross(lhs, rhs);
            }
        }

        /// <summary>
        /// An array of edge positions. The order is counterclockwise from LeftUp (i.e. LeftUp, LeftBottom, RightBottom, and RightUp).
        /// </summary>
        public Vector3[] Positions
        {
            get
            {
                return new Vector3[]
                {
                    this.leftUp.position, this.leftBottom.position,
                    this.rightBottom.position, this.rightUp.position
                };
            }
        }
    }

    internal class FacePose : IEquatable<FacePose>
    {
        public Pose HeadPose;
        public Pose EyePoseL;
        public Pose EyePoseR;

        private readonly float IPD = 0.065f;

        public FacePose()
        {
            this.HeadPose = new Pose();
            this.EyePoseL = new Pose();
            this.EyePoseR = new Pose();
        }
        public FacePose(Pose headPose, Pose eyePoseL, Pose eyePoseR)
        {
            this.HeadPose = headPose;
            this.EyePoseL = eyePoseL;
            this.EyePoseR = eyePoseR;
        }

        public static FacePose operator *(FacePose fp, float f)
        {
            fp.HeadPose.position *= f;
            fp.EyePoseL.position *= f;
            fp.EyePoseR.position *= f;
            return fp;
        }

        public FacePose GetTransformedBy(Transform lhs)
        {
            return new FacePose(this.HeadPose.GetTransformedBy(lhs),
                                this.EyePoseL.GetTransformedBy(lhs),
                                this.EyePoseR.GetTransformedBy(lhs));
        }

        public void UpdateWithNewHeadPose(Pose newHeadPose, Vector3 lookAtTarget)
        {
            this.HeadPose = newHeadPose;
            this.EyePoseL = (new Pose(Vector3.left * IPD / 2f, Quaternion.identity)).GetTransformedBy(newHeadPose);
            this.EyePoseL.rotation = Quaternion.LookRotation(lookAtTarget - this.EyePoseL.position, Vector3.up);
            this.EyePoseR = (new Pose(Vector3.right * IPD / 2f, Quaternion.identity)).GetTransformedBy(newHeadPose);
            this.EyePoseR.rotation = Quaternion.LookRotation(lookAtTarget - this.EyePoseR.position, Vector3.up);
        }

        public bool Equals(FacePose other)
        {
            return this.HeadPose == other.HeadPose && this.EyePoseL == other.EyePoseL && this.EyePoseR == other.EyePoseR;
        }

        public Pose GetEyePose(EyeType type)
        {
            return (type == EyeType.Left) ? this.EyePoseL : this.EyePoseR;
        }
    }

    internal class EyeProjectionMatrices
    {
        public Matrix4x4 LeftMatrix;
        public Matrix4x4 RightMatrix;

        public EyeProjectionMatrices()
        {
            this.LeftMatrix = Matrix4x4.identity;
            this.RightMatrix = Matrix4x4.identity;
        }

        public EyeProjectionMatrices(Matrix4x4 leftProjectionMatrix, Matrix4x4 rightProjectionMatrix)
        {
            this.LeftMatrix = leftProjectionMatrix;
            this.RightMatrix = rightProjectionMatrix;
        }

        public Matrix4x4 GetProjectionMatrix(EyeType type)
        {
            return (type == EyeType.Left) ? this.LeftMatrix : this.RightMatrix;
        }
    }

    internal static partial class SRDHelper
    {
        public static class SRDConstants
        {
            public const string WatcherGameObjDefaultName = "WatcherAnchor";
            public const string WatcherCameraGameObjDefaultName = "WatcherCamera";
            public const string EyeCamGameObjDefaultName = "EyeCamera";
            public const string EyeAnchorGameObjDefaultName = "EyeAnchor";
            public const string EyeCamRenderTexDefaultName = "EyeCamRenderTex";
            public const string HomographyCommandBufferName = "SRDHomographyCommandBuffer";

            public const string SRDProjectSettingsAssetPath = "Assets/SRDisplayUnityPlugin/Resources/SRDProjectSettings.asset";

            public const string XRRuntimeDLLName = "xr_runtime";
            public const string XRRuntimeWrapperDLLName = "xr_runtime_unity_wrapper";
        }

        public static readonly Dictionary<EyeType, string> EyeSideName = new Dictionary<EyeType, string>
        {
            { EyeType.Left, "Left" },
            { EyeType.Right, "Right" }
        };

        public static void PopupMessageAndForceToTerminate(string message, bool forceToTerminate = true)
        {
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                return;
            }

            if(forceToTerminate && Application.isPlaying)
            {
                message += ("\n" + SRDHelper.SRDMessages.AppCloseMessage);
                SRDCorePlugin.ShowMessageBox("Error", message, Debug.LogError);

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            else
            {
                SRDCorePlugin.ShowMessageBox("Error", message, Debug.LogError);
            }
        }


        public static bool HasNanOrInf(Matrix4x4 m)
        {
            for(var i = 0; i < 16; i++)
            {
                if(float.IsNaN(m[i]) || float.IsInfinity(m[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static Pose InvPose(Pose p)
        {
            var invq = Quaternion.Inverse(p.rotation);
            return new Pose((invq * -p.position), invq);
        }

        public static Matrix4x4 PoseToMatrix(Pose p)
        {
            return Matrix4x4.TRS(p.position, p.rotation, Vector3.one);
        }

        public static Pose MatrixToPose(Matrix4x4 m)
        {
            return new Pose(m.GetColumn(3), m.rotation);
        }


        public static float[] CalcHomographyMatrix(Vector3 leftUp, Vector3 leftBottom, Vector3 rightBottom, Vector3 rightUp, UnityEngine.Camera camera)
        {
            Vector2 p00 = camera.WorldToViewportPoint(leftBottom);
            Vector2 p01 = camera.WorldToViewportPoint(leftUp);
            Vector2 p10 = camera.WorldToViewportPoint(rightBottom);
            Vector2 p11 = camera.WorldToViewportPoint(rightUp);

            var x00 = p00.x;
            var y00 = p00.y;
            var x01 = p01.x;
            var y01 = p01.y;
            var x10 = p10.x;
            var y10 = p10.y;
            var x11 = p11.x;
            var y11 = p11.y;

            var a = x10 - x11;
            var b = x01 - x11;
            var c = x00 - x01 - x10 + x11;
            var d = y10 - y11;
            var e = y01 - y11;
            var f = y00 - y01 - y10 + y11;

            var h13 = x00;
            var h23 = y00;
            var h32 = (c * d - a * f) / (b * d - a * e);
            var h31 = (c * e - b * f) / (a * e - b * d);
            var h11 = x10 - x00 + h31 * x10;
            var h12 = x01 - x00 + h32 * x01;
            var h21 = y10 - y00 + h31 * y10;
            var h22 = y01 - y00 + h32 * y01;

            return new float[] { h11, h12, h13, h21, h22, h23, h31, h32, 1f };
        }

        public static float[] CalcInverseMatrix3x3(float[] mat)
        {
            var i11 = mat[0];
            var i12 = mat[1];
            var i13 = mat[2];
            var i21 = mat[3];
            var i22 = mat[4];
            var i23 = mat[5];
            var i31 = mat[6];
            var i32 = mat[7];
            var i33 = mat[8];
            var a = 1f / (
                        +(i11 * i22 * i33)
                        + (i12 * i23 * i31)
                        + (i13 * i21 * i32)
                        - (i13 * i22 * i31)
                        - (i12 * i21 * i33)
                        - (i11 * i23 * i32)
                    );

            var o11 = (i22 * i33 - i23 * i32) / a;
            var o12 = (-i12 * i33 + i13 * i32) / a;
            var o13 = (i12 * i23 - i13 * i22) / a;
            var o21 = (-i21 * i33 + i23 * i31) / a;
            var o22 = (i11 * i33 - i13 * i31) / a;
            var o23 = (-i11 * i23 + i13 * i21) / a;
            var o31 = (i21 * i32 - i22 * i31) / a;
            var o32 = (-i11 * i32 + i12 * i31) / a;
            var o33 = (i11 * i22 - i12 * i21) / a;

            return new float[] { o11, o12, o13, o21, o22, o23, o31, o32, o33 };
        }
    }

}

