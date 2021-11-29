/*
 * Copyright 2019,2020,2021 Sony Corporation
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using SRD.Utils;

namespace SRD.Core
{
    internal static class SRDCorePlugin
    {
        public static bool LinkXrLibraryWin64()
        {
            return XRRuntimeAPI.LinkXrLibraryWin64();
        }

        public static int ShowMessageBox(string title, string message, Action<string> debugLogFunc = null)
        {
            if(debugLogFunc != null)
            {
                debugLogFunc(message);
            }
            return XRRuntimeAPI.ShowMessageBox(SRDApplicationWindow.GetSelfWindowHandle(), title, message);
        }

        public static void ShowNativeLog()
        {
            var callback = new XRRuntimeAPI.DebugLogDelegate((message, log_levels) =>
            {
                switch(log_levels)
                {
                    case SrdXrLogLevels.LOG_LEVELS_TRACE:
                    case SrdXrLogLevels.LOG_LEVELS_DEBUG:
                    case SrdXrLogLevels.LOG_LEVELS_INFO:
                        Debug.Log(message);
                        break;
                    case SrdXrLogLevels.LOG_LEVELS_WARN:
                        Debug.LogWarning(message);
                        break;
                    case SrdXrLogLevels.LOG_LEVELS_ERR:
                        Debug.LogError(message);
                        break;
                    default:
                        break;
                }
            });
            XRRuntimeAPI.SetDebugLogCallback(Marshal.GetFunctionPointerForDelegate(callback));
        }

        public static void UnlinkXrLibraryWin64()
        {
            XRRuntimeAPI.UnlinkXrLibraryWin64();
        }

        public static SrdXrResult CreateSession(out IntPtr session)
        {
            var devices = new SrdXrDeviceInfo[1];
            var resultED = SRDCorePlugin.EnumerateDevices(devices, (uint)devices.Length);
            SrdXrSessionCreateInfo info;
            {
                info.platform_id = SrdXrPlatformId.PLATFORM_ID_SRD;
                info.coordinate_system = SrdXrCoordinateSystem.COORDINATE_SYSTEM_LEFT_Y_UP_Z_FORWARD;
                info.coordinate_unit = SrdXrCoordinateUnit.COORDINATE_UNIT_METER;
                info.device = devices[0];
            }
            var resultCS = XRRuntimeAPI.CreateSession(ref info, out session);
            return (resultED != SrdXrResult.SUCCESS) ? resultED : resultCS;
        }

        public static SrdXrResult DestroySession(out IntPtr session)
        {
            return XRRuntimeAPI.DestroySession(out session);
        }

        public static SrdXrResult BeginSession(IntPtr session)
        {
            SrdXrSessionBeginInfo begin_info;
            {
                begin_info.primary_view_configuration_type = SrdXrViewConfigurationType.VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;
            }
            return XRRuntimeAPI.BeginSession(session, ref begin_info);
        }

        public static SrdXrResult EndSession(IntPtr session)
        {
            return XRRuntimeAPI.EndSession(session);
        }

        public static SrdXrResult BeginFrame(IntPtr session, bool callInMainThread = true, bool callinRenderThread = false)
        {
            if(callinRenderThread)
            {
                GL.IssuePluginEvent(XRRuntimeAPI.GetBeginFramePtr(session), 0);
            }
            if(callInMainThread)
            {
                return XRRuntimeAPI.BeginFrame(session);
            }
            return SrdXrResult.SUCCESS;
        }

        public static SrdXrResult EndFrame(IntPtr session, bool callInMainThread = true, bool callinRenderThread = false)
        {
            var end_info = new SrdXrFrameEndInfo();
            if(callinRenderThread)
            {
                GL.IssuePluginEvent(XRRuntimeAPI.GetEndFramePtr(session, ref end_info), 0);
            }
            if(callInMainThread)
            {
                return XRRuntimeAPI.EndFrame(session, ref end_info);
            }
            return SrdXrResult.SUCCESS;
        }

        public static SrdXrResult PollEvent(IntPtr session, out SrdXrEventDataBuffer eventData)
        {
            return XRRuntimeAPI.PollEvent(session, out eventData);
        }

        public static SrdXrResult SetGraphicsAPI(IntPtr session, UnityEngine.Rendering.GraphicsDeviceType graphicsAPI)
        {
            return XRRuntimeAPI.SetGraphicsAPI(session, XRRuntimeGraphicsDeviceType[graphicsAPI]);
        }

        public static void GenerateTextureAndShaders(IntPtr session, ref SrdXrTexture leftTextureData, ref SrdXrTexture rightTextureData, ref SrdXrTexture outTextureData)
        {
            GL.IssuePluginEvent(XRRuntimeAPI.GetGenerateTextureAndShadersPtr(session, ref leftTextureData, ref rightTextureData, ref outTextureData), 0);
        }

        public static SrdXrResult ShowCameraWindow(IntPtr session, bool show)
        {
            return XRRuntimeAPI.ShowCameraWindow(session, show);
        }

        public static SrdXrResult GetFacePose(IntPtr session,
                                              out Pose headPose, out Pose eyePoseL, out Pose eyePoseR)
        {
            var views = new SrdXrView[3];
            var viewLocateInfo = new SrdXrViewLocateInfo();
            {
                viewLocateInfo.view_configuration_type = SrdXrViewConfigurationType.VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;
            }

            var xrResult = XRRuntimeAPI.LocateViews(session, ref viewLocateInfo, views);
            headPose = ToUnityPose(views[0].pose);
            eyePoseL = ToUnityPose(views[1].pose);
            eyePoseR = ToUnityPose(views[2].pose);
            return xrResult;
        }

        public static SrdXrResult GetProjectionMatrix(IntPtr session, float nearClip, float farClip,
                                                      out Matrix4x4 leftProjectionMatrix, out Matrix4x4 rightProjectionMatrix)
        {
            var projMat = new SrdXrProjectionMatrix();
            var projectionMatrixInfo = new SrdXrProjectionMatrixInfo
            {
                graphics_api = XRRuntimeGraphicsDeviceType[SystemInfo.graphicsDeviceType],
                coordinate_system = SrdXrCoordinateSystem.COORDINATE_SYSTEM_RIGHT_Y_UP_Z_FORWARD,
                near_clip = nearClip,
                far_clip = farClip,
                reversed_z = false
            };

            var xrResult = XRRuntimeAPI.GetProjectionMatrix(session, ref projectionMatrixInfo, out projMat);
            leftProjectionMatrix = ToUnityMatrix4x4(projMat.left_projection);
            rightProjectionMatrix = ToUnityMatrix4x4(projMat.right_projection);

            return xrResult;
        }

        public static SrdXrResult GetDeviceState(IntPtr session, out SrdXrDeviceState device_state)
        {
            return XRRuntimeAPI.GetDeviceState(session, out device_state);
        }

        public static bool GetSRDBodyBounds(IntPtr session, out SRDSettings.BodyBounds bodyBounds)
        {
            SrdXrSRDData data;
            if(XRRuntimeAPI.GetPlatformSpecificData(session, out data) != SrdXrResult.SUCCESS)
            {
                bodyBounds = new SRDSettings.BodyBounds();
                return false;
            }
            bodyBounds = new SRDSettings.BodyBounds(data.display_size.width_m,
                                                    data.display_size.height_m * Mathf.Sin(data.display_tilt_rad),
                                                    data.display_size.height_m * Mathf.Cos(data.display_tilt_rad));
            return true;
        }

        public static bool GetSRDScreenRect(out SRDSettings.ScreenRect screenRect)
        {
            var size = SRDCorePlugin.CountDevices();
            if(size == 0)
            {
                screenRect = new SRDSettings.ScreenRect();
                return false;
            }

            SrdXrDeviceInfo[] devices = { new SrdXrDeviceInfo(), };
            SRDCorePlugin.EnumerateDevices(devices, 1);

            var target = devices[0].target_monitor_rectangle;
            var width = target.right - target.left;
            var height = target.bottom - target.top;
            screenRect = new SRDSettings.ScreenRect(target.left, target.top, width, height);
            return true;
        }

        public static SrdXrResult EnumerateDevices([In, Out] SrdXrDeviceInfo[] devices, UInt32 loadCount)
        {
            var result = XRRuntimeAPI.EnumerateDevices(devices, loadCount);
            if(result != SrdXrResult.SUCCESS)
            {
                for(int i = 0; i < loadCount; i++)
                {
                    devices[i].device_index = 0;
                    devices[i].device_serial_number = "";
                    devices[i].primary_monitor_rectangle = new SrdXrRect(0, 0, 0, 0);
                    devices[i].target_monitor_rectangle = new SrdXrRect(0, 0, 0, 0);
                }
            }
            return result;
        }

        public static int CountDevices()
        {
            return (int)XRRuntimeAPI.CountDevices();
        }

        public const SrdXrCrosstalkCorrectionType DefaultCrosstalkCorrectionType = SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_MEDIUM;

        public static SrdXrResult SetActiveStateCrosstalkCorrection(IntPtr session, bool state, SrdXrCrosstalkCorrectionType type = DefaultCrosstalkCorrectionType)
        {
            try
            {
                var settings = new SrdXrCrosstalkCorrectionSettings_v1_1_0(state, type);
                return XRRuntimeAPI.SetCrosstalkCorrectionSettings_v1_1_0(session, ref settings);
            }
            catch(EntryPointNotFoundException)
            {
                var settings = new SrdXrCrosstalkCorrectionSettings(state);
                return XRRuntimeAPI.SetCrosstalkCorrectionSettings(session, ref settings);
            }
        }

        public static SrdXrResult GetActiveStateCrosstalkCorrection(IntPtr session, out bool state, out SrdXrCrosstalkCorrectionType type)
        {
            try
            {
                var result = XRRuntimeAPI.GetCrosstalkCorrectionSettings_v1_1_0(session, out var settings);
                state = settings.compensation_enabled;
                type = settings.correction_type;
                return result;
            }
            catch(EntryPointNotFoundException)
            {
                var result = XRRuntimeAPI.GetCrosstalkCorrectionSettings(session, out var settings);
                state = settings.compensation_enabled;
                type = DefaultCrosstalkCorrectionType;
                return result;
            }
        }

        public static SrdXrResult SetColorSpaceSettings(IntPtr session, ColorSpace colorSpace)
        {
            var unityGamma = 2.2f;
            var settings = new SrdXrColorManagementSettings(colorSpace == ColorSpace.Gamma, unityGamma);
            return XRRuntimeAPI.SetColorManagementSettings(session, ref settings);
        }

        private struct XRRuntimeAPI
        {
            const string dll_path = SRD.Utils.SRDHelper.SRDConstants.XRRuntimeDLLName;
            const string wrapper_dll_path = SRD.Utils.SRDHelper.SRDConstants.XRRuntimeWrapperDLLName;

            [DllImport(wrapper_dll_path, EntryPoint = "srd_xrLinkXrLibraryWin64")]
            public static extern bool LinkXrLibraryWin64();

            [DllImport(wrapper_dll_path, EntryPoint = "srd_xrUnlinkXrLibraryWin64")]
            public static extern void UnlinkXrLibraryWin64();

            [DllImport(dll_path, EntryPoint = "srd_xrCreateSession")]
            public static extern SrdXrResult CreateSession([In] ref SrdXrSessionCreateInfo create_info, out IntPtr session);

            [DllImport(dll_path, EntryPoint = "srd_xrDestroySession")]
            public static extern SrdXrResult DestroySession(out IntPtr session);

            [DllImport(dll_path, EntryPoint = "srd_xrBeginSession")]
            public static extern SrdXrResult BeginSession(IntPtr session, [In] ref SrdXrSessionBeginInfo begin_info);

            [DllImport(dll_path, EntryPoint = "srd_xrEndSession")]
            public static extern SrdXrResult EndSession(IntPtr session);

            [DllImport(dll_path, EntryPoint = "srd_xrPollEvent")]
            public static extern SrdXrResult PollEvent(IntPtr session, out SrdXrEventDataBuffer event_data);

            [DllImport(dll_path, EntryPoint = "srd_xrBeginFrame")]
            public static extern SrdXrResult BeginFrame(IntPtr session);

            [DllImport(wrapper_dll_path, EntryPoint = "get_BeginFrame_func")]
            public static extern IntPtr GetBeginFramePtr(IntPtr session);

            [DllImport(dll_path, EntryPoint = "srd_xrEndFrame")]
            public static extern SrdXrResult EndFrame(IntPtr session, [In] ref SrdXrFrameEndInfo frame_end_info);

            [DllImport(wrapper_dll_path, EntryPoint = "get_EndFrame_func")]
            public static extern IntPtr GetEndFramePtr(IntPtr session, [In] ref SrdXrFrameEndInfo frame_end_info);

            [DllImport(wrapper_dll_path, EntryPoint = "get_GenerateTextureAndShaders_func")]
            public static extern IntPtr GetGenerateTextureAndShadersPtr(IntPtr session, [In] ref SrdXrTexture left_texture, [In] ref SrdXrTexture right_texture, [In] ref SrdXrTexture render_target);

            [DllImport(dll_path, EntryPoint = "srd_xr_extSetGraphicsAPI")]
            public static extern SrdXrResult SetGraphicsAPI(IntPtr session, SrdXrGraphicsAPI graphics_api);

            [DllImport(dll_path, EntryPoint = "srd_xr_extShowCapturedImageWindow")]
            public static extern SrdXrResult ShowCameraWindow(IntPtr session, bool show);

            [DllImport(dll_path, EntryPoint = "srd_xrLocateViews")]
            public static extern SrdXrResult LocateViews(IntPtr session, [In] ref SrdXrViewLocateInfo view_locate_info, [Out] SrdXrView[] views);

            [DllImport(dll_path, EntryPoint = "srd_xr_extGetProjectionMatrix")]
            public static extern SrdXrResult GetProjectionMatrix(IntPtr session, [In] ref SrdXrProjectionMatrixInfo projection_matrix_info, out SrdXrProjectionMatrix projection_matrix);

            [DllImport(dll_path, EntryPoint = "srd_xr_extGetDeviceState")]
            public static extern SrdXrResult GetDeviceState(IntPtr session, out SrdXrDeviceState device_state);

            [DllImport(wrapper_dll_path, EntryPoint = "srd_xr_extEnumerateDevices")]
            public static extern SrdXrResult EnumerateDevices([In, Out] SrdXrDeviceInfo[] devices, UInt32 load_count);

            [DllImport(wrapper_dll_path, EntryPoint = "srd_xr_extCountDevices")]
            public static extern UInt32 CountDevices();

            [DllImport(wrapper_dll_path, EntryPoint = "srd_xrShowMessageBox", CharSet = CharSet.Unicode)]
            public static extern int ShowMessageBox(IntPtr hWnd, string title, string msg);

            [DllImport(dll_path, EntryPoint = "srd_xr_extGetPlatformSpecificData")]
            public static extern SrdXrResult GetPlatformSpecificData(IntPtr session, out SrdXrSRDData data);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void DebugLogDelegate([MarshalAs(UnmanagedType.LPStr)] string str, SrdXrLogLevels log_levels);
            [DllImport(dll_path, EntryPoint = "srd_xr_extSetDebugLogCallback")]
            public static extern void SetDebugLogCallback(IntPtr ptr);

            [DllImport(dll_path, EntryPoint = "srd_xr_extGetCrosstalkCorrectionSettings")]
            public static extern SrdXrResult GetCrosstalkCorrectionSettings(IntPtr session, out SrdXrCrosstalkCorrectionSettings crosstalk_correction_settings);

            [DllImport(dll_path, EntryPoint = "srd_xr_extSetCrosstalkCorrectionSettings")]
            public static extern SrdXrResult SetCrosstalkCorrectionSettings(IntPtr session, [In] ref SrdXrCrosstalkCorrectionSettings crosstalk_correction_settings);

            [DllImport(dll_path, EntryPoint = "srd_xr_extGetCrosstalkCorrectionSettings_v1_1_0")]
            public static extern SrdXrResult GetCrosstalkCorrectionSettings_v1_1_0(
                IntPtr session,
                out SrdXrCrosstalkCorrectionSettings_v1_1_0 crosstalk_correction_settings);

            [DllImport(dll_path, EntryPoint = "srd_xr_extSetCrosstalkCorrectionSettings_v1_1_0")]
            public static extern SrdXrResult SetCrosstalkCorrectionSettings_v1_1_0(
                IntPtr session,
                [In] ref SrdXrCrosstalkCorrectionSettings_v1_1_0 crosstalk_correction_settings);

            [DllImport(dll_path, EntryPoint = "srd_xr_extSetColorManagementSettings")]
            public static extern SrdXrResult SetColorManagementSettings(IntPtr session, [In] ref SrdXrColorManagementSettings color_management_settings);
        }


        private static readonly Dictionary<UnityEngine.Rendering.GraphicsDeviceType, SrdXrGraphicsAPI> XRRuntimeGraphicsDeviceType = new Dictionary<UnityEngine.Rendering.GraphicsDeviceType, SrdXrGraphicsAPI>()
        {
            { UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore,  SrdXrGraphicsAPI.GRAPHICS_API_GL},
            { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,  SrdXrGraphicsAPI.GRAPHICS_API_DirectX}
        };

        private static Pose ToUnityPose(SrdXrPosef p)
        {
            return new Pose(ToUnityVector(p.position), ToUnityQuaternion(p.orientation));
        }

        private static Vector3 ToUnityVector(SrdXrVector3f v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        private static Quaternion ToUnityQuaternion(SrdXrQuaternionf q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }

        private static Matrix4x4 ToUnityMatrix4x4(SrdXrMatrix4x4f m)
        {
            var ret = new Matrix4x4();
            for(var i = 0; i < 16; i++)
            {
                ret[i] = m.matrix[i];
            }
            return ret;
        }

    }

    public enum SrdXrResult
    {
        SUCCESS = 0,
        TIMEOUT_EXPIRED = 1,
        SESSION_LOSS_PENDING = 3,
        EVENT_UNAVAILABLE = 4,
        SPACE_BOUNDS_UNAVAILABLE = 7,
        SESSION_NOT_FOCUSED = 8,
        FRAME_DISCARDED = 9,
        ERROR_VALIDATION_FAILURE = -1,
        ERROR_RUNTIME_FAILURE = -2,
        ERROR_OUT_OF_MEMORY = -3,
        ERROR_API_VERSION_UNSUPPORTED = -4,
        ERROR_INITIALIZATION_FAILED = -6,
        ERROR_FUNCTION_UNSUPPORTED = -7,
        ERROR_FEATURE_UNSUPPORTED = -8,
        ERROR_EXTENSION_NOT_PRESENT = -9,
        ERROR_LIMIT_REACHED = -10,
        ERROR_SIZE_INSUFFICIENT = -11,
        ERROR_HANDLE_INVALID = -12,
        ERROR_INSTANCE_LOST = -13,
        ERROR_SESSION_RUNNING = -14,
        ERROR_SESSION_NOT_RUNNING = -16,
        ERROR_SESSION_LOST = -17,
        ERROR_SYSTEM_INVALID = -18,
        ERROR_PATH_INVALID = -19,
        ERROR_PATH_COUNT_EXCEEDED = -20,
        ERROR_PATH_FORMAT_INVALID = -21,
        ERROR_PATH_UNSUPPORTED = -22,
        ERROR_LAYER_INVALID = -23,
        ERROR_LAYER_LIMIT_EXCEEDED = -24,
        ERROR_SWAPCHAIN_RECT_INVALID = -25,
        ERROR_SWAPCHAIN_FORMAT_UNSUPPORTED = -26,
        ERROR_ACTION_TYPE_MISMATCH = -27,
        ERROR_SESSION_NOT_READY = -28,
        ERROR_SESSION_NOT_STOPPING = -29,
        ERROR_TIME_INVALID = -30,
        ERROR_REFERENCE_SPACE_UNSUPPORTED = -31,
        ERROR_FILE_ACCESS_ERROR = -32,
        ERROR_FILE_CONTENTS_INVALID = -33,
        ERROR_FORM_FACTOR_UNSUPPORTED = -34,
        ERROR_FORM_FACTOR_UNAVAILABLE = -35,
        ERROR_API_LAYER_NOT_PRESENT = -36,
        ERROR_CALL_ORDER_INVALID = -37,
        ERROR_GRAPHICS_DEVICE_INVALID = -38,
        ERROR_POSE_INVALID = -39,
        ERROR_INDEX_OUT_OF_RANGE = -40,
        ERROR_VIEW_CONFIGURATION_TYPE_UNSUPPORTED = -41,
        ERROR_ENVIRONMENT_BLEND_MODE_UNSUPPORTED = -42,
        ERROR_NAME_DUPLICATED = -44,
        ERROR_NAME_INVALID = -45,
        ERROR_ACTIONSET_NOT_ATTACHED = -46,
        ERROR_ACTIONSETS_ALREADY_ATTACHED = -47,
        ERROR_LOCALIZED_NAME_DUPLICATED = -48,
        ERROR_LOCALIZED_NAME_INVALID = -49,

        ERROR_RUNTIME_NOT_FOUND = -1001,
        ERROR_DEVICE_NOT_FOUND = -1011,
        ERROR_DISPLAY_NOT_FOUND = -1012,
        ERROR_USB_NOT_CONNECTED = -1013,
        ERROR_USB_3_NOT_CONNECTED = -1014,

        RESULT_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrPlatformId
    {
        PLATFORM_ID_SRD = 0,
        PLATFORM_ID_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrViewConfigurationType
    {
        VIEW_CONFIGURATION_TYPE_PRIMARY_MONO = 1,
        VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO = 2,
        VIEW_CONFIGURATION_TYPE_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrEnvironmentBlendMode
    {
        ENVIRONMENT_BLEND_MODE_OPAQUE = 1,
        ENVIRONMENT_BLEND_MODE_ADDITIVE = 2,
        ENVIRONMENT_BLEND_MODE_ALPHA_BLEND = 3,
        ENVIRONMENT_BLEND_MODE_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrSessionState
    {
        SESSION_STATE_UNKNOWN = 0,
        SESSION_STATE_IDLE = 1,
        SESSION_STATE_READY = 2,
        SESSION_STATE_SYNCHRONIZED = 3,
        SESSION_STATE_VISIBLE = 4,
        SESSION_STATE_FOCUSED = 5,
        SESSION_STATE_STOPPING = 6,
        SESSION_STATE_LOSS_PENDING = 7,
        SESSION_STATE_EXITING = 8,
        SESSION_STATE_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrCoordinateSystem
    {
        COORDINATE_SYSTEM_RIGHT_Y_UP_Z_BACK = 0,
        COORDINATE_SYSTEM_RIGHT_Y_UP_Z_FORWARD = 1,
        COORDINATE_SYSTEM_LEFT_Y_UP_Z_FORWARD = 2,
        COORDINATE_SYSTEM_LEFT_Z_UP_X_FORWARD = 3,
        COORDINATE_SYSTEM_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrCoordinateUnit
    {
        COORDINATE_UNIT_METER = 0,
        COORDINATE_UNIT_CENTIMETER = 1,
        COORDINATE_UNIT_MILLIMETER = 2,
        COORDINATE_UNIT_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrGraphicsAPI
    {
        GRAPHICS_API_GL = 0,
        GRAPHICS_API_DirectX = 1,
        GRAPHICS_API_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrCompositionLayerFlags
    {
        COMPOSITION_LAYER_CORRECT_CHROMATIC_ABERRATION_BIT = 0x00000001,
        COMPOSITION_LAYER_BLEND_TEXTURE_SOURCE_ALPHA_BIT = 0x00000002,
        COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT = 0x00000004
    }

    public enum SrdXrLogLevels
    {
        LOG_LEVELS_TRACE = 0,
        LOG_LEVELS_DEBUG = 1,
        LOG_LEVELS_INFO = 2,
        LOG_LEVELS_WARN = 3,
        LOG_LEVELS_ERR = 4,
        LOG_LEVELS_CRITICAL = 5,
        LOG_LEVELS_OFF = 6,
        LOG_LEVELS_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrDeviceConnectionState
    {
        DEVICE_NOT_CONNECTED = 0,
        DEVICE_CONNECTED = 1,
    };

    public enum SrdXrDevicePowerState
    {
        DEVICE_POWER_OFF = 0,
        DEVICE_POWER_ON = 1,
    };

    public enum SrdXrCrosstalkCorrectionType
    {
        GRADATION_CORRECTION_MEDIUM = 0,
        GRADATION_CORRECTION_ALL = 1,
        GRADATION_CORRECTION_HIGH_PRECISE = 2,
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrSessionCreateInfo
    {
        public SrdXrPlatformId platform_id;
        public SrdXrCoordinateSystem coordinate_system;
        public SrdXrCoordinateUnit coordinate_unit;
        public SrdXrDeviceInfo device;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrSessionBeginInfo
    {
        public SrdXrViewConfigurationType primary_view_configuration_type;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrEventDataBuffer
    {
        public SrdXrSessionState state;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrCompositionLayerBaseHeader
    {
        public SrdXrCompositionLayerFlags layer_flags;
        public IntPtr space;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrFrameEndInfo
    {
        public Int64 display_time;
        public UInt32 layer_count;
        public IntPtr layers;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrViewLocateInfo
    {
        public SrdXrViewConfigurationType view_configuration_type;
        public Int64 display_time;
        public IntPtr space;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrPosef
    {
        public SrdXrQuaternionf orientation;
        public SrdXrVector3f position;

        public SrdXrPosef(SrdXrQuaternionf in_orientation, SrdXrVector3f in_position)
        {
            orientation = in_orientation;
            position = in_position;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrFovf
    {
        public float angle_left;
        public float angle_right;
        public float angle_up;
        public float angle_down;

        public SrdXrFovf(float in_angle)
        {
            angle_left = in_angle;
            angle_right = in_angle;
            angle_up = in_angle;
            angle_down = in_angle;
        }
        public SrdXrFovf(float in_angle_left, float in_angle_right, float in_angle_up, float in_angle_down)
        {
            angle_left = in_angle_left;
            angle_right = in_angle_right;
            angle_up = in_angle_up;
            angle_down = in_angle_down;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrView
    {
        public SrdXrPosef pose;
        public SrdXrFovf fov;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrProjectionMatrixInfo
    {
        public SrdXrGraphicsAPI graphics_api;
        public SrdXrCoordinateSystem coordinate_system;
        public float near_clip;  // The unit is arbitrary
        public float far_clip;  // The unit is arbitrary
        public bool reversed_z;

        public SrdXrProjectionMatrixInfo(SrdXrGraphicsAPI in_graphics_api, SrdXrCoordinateSystem in_coordinate_system, float in_near_clip, float in_far_clip, bool in_reversed_z)
        {
            graphics_api = in_graphics_api;
            coordinate_system = in_coordinate_system;
            near_clip = in_near_clip;
            far_clip = in_far_clip;
            reversed_z = in_reversed_z;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplaySize
    {
        public float width_m;
        public float height_m;

        public SrdXrDisplaySize(float in_width_m, float in_height_m)
        {
            width_m = in_width_m;
            height_m = in_height_m;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public SrdXrRect(int in_left, int in_top, int in_right, int in_bottom)
        {
            left = in_left;
            top = in_top;
            right = in_right;
            bottom = in_bottom;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplayResolution
    {
        public UInt32 width;
        public UInt32 height;
        public UInt32 area;

        public SrdXrDisplayResolution(UInt32 in_width, UInt32 in_height)
        {
            width = in_width;
            height = in_height;
            area = width * height;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrProjectionMatrix
    {
        public SrdXrMatrix4x4f projection;
        public SrdXrMatrix4x4f left_projection;
        public SrdXrMatrix4x4f right_projection;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplayLocateInfo
    {
        public Int64 display_time;
        public IntPtr space;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplay
    {
        public SrdXrDisplaySize display_size;
        public SrdXrDisplayResolution display_resolution;
        public SrdXrPosef display_pose;

        public SrdXrDisplay(SrdXrDisplaySize in_display_size, SrdXrDisplayResolution in_display_resolution, SrdXrPosef in_display_pose)
        {
            display_size = in_display_size;
            display_resolution = in_display_resolution;
            display_pose = in_display_pose;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrTexture
    {
        public IntPtr texture;
        public UInt32 width;
        public UInt32 height;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrQuaternionf
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SrdXrQuaternionf(float in_x, float in_y, float in_z, float in_w)
        {
            x = in_x;
            y = in_y;
            z = in_z;
            w = in_w;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrVector3f
    {
        public float x;
        public float y;
        public float z;

        public SrdXrVector3f(float in_x, float in_y, float in_z)
        {
            x = in_x;
            y = in_y;
            z = in_z;
        }

        public static SrdXrVector3f operator +(SrdXrVector3f a, SrdXrVector3f b)
        => new SrdXrVector3f(a.x + b.x, a.y + b.y, a.z + b.z);

        public static SrdXrVector3f operator -(SrdXrVector3f a, SrdXrVector3f b)
        => new SrdXrVector3f(a.x - b.x, a.y - b.y, a.z - b.z);

        public static SrdXrVector3f operator *(SrdXrVector3f a, float b)
        => new SrdXrVector3f(a.x * b, a.y * b, a.z * b);

        public static SrdXrVector3f operator /(SrdXrVector3f a, float b)
        => new SrdXrVector3f(a.x / b, a.y / b, a.z / b);

        public float Dot(SrdXrVector3f a)
        {
            return x * a.x + y * a.y + z * a.z;
        }

        public void Normalize()
        {
            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            x /= length;
            y /= length;
            z /= length;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrVector4f
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SrdXrVector4f(float in_x, float in_y, float in_z, float in_w)
        {
            x = in_x;
            y = in_y;
            z = in_z;
            w = in_w;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrMatrix4x4f
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4 * 4)]
        public float[] matrix;

        public SrdXrMatrix4x4f(SrdXrVector4f in_x, SrdXrVector4f in_y, SrdXrVector4f in_z, SrdXrVector4f in_w)
        {
            matrix = new float[4 * 4];

            matrix[4 * 0 + 0] = in_x.x;
            matrix[4 * 0 + 1] = in_x.y;
            matrix[4 * 0 + 2] = in_x.z;
            matrix[4 * 0 + 3] = in_x.w;
            matrix[4 * 1 + 0] = in_y.x;
            matrix[4 * 1 + 1] = in_y.y;
            matrix[4 * 1 + 2] = in_y.z;
            matrix[4 * 1 + 3] = in_y.w;
            matrix[4 * 2 + 0] = in_z.x;
            matrix[4 * 2 + 1] = in_z.y;
            matrix[4 * 2 + 2] = in_z.z;
            matrix[4 * 2 + 3] = in_z.w;
            matrix[4 * 3 + 0] = in_w.x;
            matrix[4 * 3 + 1] = in_w.y;
            matrix[4 * 3 + 2] = in_w.z;
            matrix[4 * 3 + 3] = in_w.w;
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SrdXrDeviceInfo
    {
        public UInt32 device_index;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string device_serial_number;
        public SrdXrRect target_monitor_rectangle;
        public SrdXrRect primary_monitor_rectangle;

        public SrdXrDeviceInfo(UInt32 in_device_index, string in_device_serial_number
                               , SrdXrRect in_target_monitor_rectangle, SrdXrRect in_primary_monitor_rectangle)
        {
            device_index = in_device_index;
            device_serial_number = in_device_serial_number;
            target_monitor_rectangle = in_target_monitor_rectangle;
            primary_monitor_rectangle = in_primary_monitor_rectangle;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrSRDData
    {
        public SrdXrDisplaySize display_size;
        public float display_tilt_rad;
        public SrdXrDisplayResolution display_resolution;

        public SrdXrSRDData(SrdXrDisplaySize in_display_size, float in_display_tilt_rad
                            , SrdXrDisplayResolution in_display_resolution)
        {
            display_size = in_display_size;
            display_tilt_rad = in_display_tilt_rad;
            display_resolution = in_display_resolution;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrCrosstalkCorrectionSettings
    {
        public bool compensation_enabled;

        public SrdXrCrosstalkCorrectionSettings(bool in_compensation_enabled)
        {
            compensation_enabled = in_compensation_enabled;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrCrosstalkCorrectionSettings_v1_1_0
    {
        public bool compensation_enabled;
        public SrdXrCrosstalkCorrectionType correction_type;

        public SrdXrCrosstalkCorrectionSettings_v1_1_0(bool in_compensation_enabled, SrdXrCrosstalkCorrectionType in_correction_type)
        {
            compensation_enabled = in_compensation_enabled;
            correction_type = in_correction_type;
        }
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrColorManagementSettings
    {
        public bool is_input_texture_gamma_corrected;
        public float gamma;

        public SrdXrColorManagementSettings(bool in_is_input_texture_gamma_corrected, float in_gamma)
        {
            is_input_texture_gamma_corrected = in_is_input_texture_gamma_corrected;
            gamma = in_gamma;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDeviceState
    {
        public SrdXrDeviceConnectionState connection_state;
        public SrdXrDevicePowerState power_state;

        public SrdXrDeviceState(SrdXrDeviceConnectionState in_connection_state, SrdXrDevicePowerState in_power_state)
        {
            connection_state = in_connection_state;
            power_state = in_power_state;
        }
    };

}
