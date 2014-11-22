/************************************************************************************

Filename    :   OVRHmd.cs
Content     :   C# Interface for the Oculus C API
Created     :   February 24, 2014
Authors     :   Michael Antonov, David Borel, James Hillery

Copyright   :   Copyright 2014 Oculus VR, Inc. All Rights reserved.

Use of this software is subject to the terms of the Oculus LLC license
agreement provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

************************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OVR {

//-------------------------------------------------------------------------------------
// ***** OVRHmd
//
// Wraps access to CAPI

// ovrVector2
[StructLayout(LayoutKind.Sequential)]
public struct ovrVector2i
{
    public int x, y;

    public ovrVector2i(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
};

// ovrSizei
[StructLayout(LayoutKind.Sequential)]
public struct ovrSizei
{
    public int w, h;

    public ovrSizei(int _w, int _h)
    {
        w = _w;
        h = _h;
    }
};

// ovrRecti
[StructLayout(LayoutKind.Sequential)]
public struct ovrRecti
{
    public ovrVector2i Pos;
    public ovrSizei    Size;
};

// ovrQuatf
[StructLayout(LayoutKind.Sequential)]
public struct ovrQuatf
{
    public float x, y, z, w;

    public ovrQuatf(float _x, float _y, float _z, float _w)
    {
        x = _x;
        y = _y;
        z = _z;
        w = _w;
    }
};

// ovrVector2f
[StructLayout(LayoutKind.Sequential)]
public struct ovrVector2f
{
    public float x, y;

    public ovrVector2f(float _x, float _y)
    {
        x = _x;
        y = _y;
    }
};

// ovrVector3f
[StructLayout(LayoutKind.Sequential)]
public struct ovrVector3f
{
    public float x, y, z;

    public ovrVector3f(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
};

// ovrMatrix4f
[StructLayout(LayoutKind.Sequential)]
public struct ovrMatrix4f
{
    public float[,] m;

    public ovrMatrix4f(ovrMatrix4f_Raw raw)
    {
        this.m = new float[,] {
			{ raw.m00, raw.m01, raw.m02, raw.m03 },
			{ raw.m10, raw.m11, raw.m12, raw.m13 },
			{ raw.m20, raw.m21, raw.m22, raw.m23 },
			{ raw.m30, raw.m31, raw.m32, raw.m33 } };
    }
};

[StructLayout(LayoutKind.Sequential)]
public struct ovrMatrix4f_Raw
{
    public float m00;
    public float m01;
    public float m02;
    public float m03;

    public float m10;
    public float m11;
    public float m12;
    public float m13;

    public float m20;
    public float m21;
    public float m22;
    public float m23;

    public float m30;
    public float m31;
    public float m32;
    public float m33;
};

// ovrPosef
// Position and orientation together.
[StructLayout(LayoutKind.Sequential)]
public struct ovrPosef
{
    public ovrQuatf    Orientation;
    public ovrVector3f Position;

    public ovrPosef(ovrQuatf q, ovrVector3f p)
    {
        Orientation = q;
        Position    = p;
    }
};

// ovrPoseStatef
// Full pose (rigid body) configuration with first and second derivatives.
[StructLayout(LayoutKind.Sequential)]
public struct ovrPoseStatef
{
    public ovrPosef     ThePose;
    public ovrVector3f  AngularVelocity;
    public ovrVector3f  LinearVelocity;
    public ovrVector3f  AngularAcceleration;
    public ovrVector3f  LinearAcceleration;
    public double       TimeInSeconds;         // Absolute time of this state sample.
};

// ovrFovPort
// Field Of View (FOV) in tangent of the angle units.
// As an example, for a standard 90 degree vertical FOV, we would 
// have: { UpTan = tan(90 degrees / 2), DownTan = tan(90 degrees / 2) }.
[StructLayout(LayoutKind.Sequential)]
public struct ovrFovPort
{
    public float UpTan;
    public float DownTan;
    public float LeftTan;
    public float RightTan;
};


// ***** HMD Types

// Enumerates all HMD types that we support.
public enum ovrHmdType
{
	ovrHmd_None             = 0,
	ovrHmd_DK1              = 3,
	ovrHmd_DKHD             = 4,
	ovrHmd_DK2              = 6,
	ovrHmd_Other                        // Some HMD other then the one in the enumeration.
};

// HMD capability bits reported by device.
public enum ovrHmdCaps
{
	// Read-only flags.
	ovrHmdCap_Present           = 0x0001,   //  This HMD exists (as opposed to being unplugged).
	ovrHmdCap_Available         = 0x0002,   //  HMD and is sensor is available for Ownership use, i.e.
	//  is not owned by another app.
	ovrHmdCap_Captured          = 0x0004,   //  'true' if we captured ownership for this Hmd.
	
	// These flags are intended for use with the new driver display mode.
	ovrHmdCap_ExtendDesktop     = 0x0008,   // Read only, means display driver is in compatibility mode.
	
	// Modifiable flags (through ovrHmd_SetEnabledCaps).
	ovrHmdCap_NoMirrorToWindow  = 0x2000,   // Disables mirrowing of HMD output to the window;
	// may improve rendering performance slightly (only if ExtendDesktop is off).
	ovrHmdCap_DisplayOff        = 0x0040,   // Turns off Oculus HMD screen and output (only if ExtendDesktop is off).
	
	ovrHmdCap_LowPersistence    = 0x0080,   //  Supports low persistence mode.
	ovrHmdCap_DynamicPrediction = 0x0200,   //  Adjust prediction dynamically based on DK2 Latency.
	// Support rendering without VSync for debugging
	ovrHmdCap_NoVSync           = 0x1000,
	
	// These bits can be modified by ovrHmd_SetEnabledCaps.
	ovrHmdCap_Writable_Mask     = 0x33F0,
	// These flags are currently passed into the service. May change without notice.
	ovrHmdCap_Service_Mask      = 0x23F0
};

// Tracking capability bits reported by device.
// Used with ovrHmd_ConfigureTracking.
public enum ovrTrackingCaps
{
	ovrTrackingCap_Orientation       = 0x0010,   //  Supports orientation tracking (IMU).
	ovrTrackingCap_MagYawCorrection  = 0x0020,   //  Supports yaw correction through magnetometer or other means.
	ovrTrackingCap_Position          = 0x0040,   //  Supports positional tracking.
	ovrTrackingCap_Idle              = 0x0100,   //  Overwrites other flags; indicates that implementation
	                                             //  doesn't care about tracking settings. Default before
	                                             //  ovrHmd_ConfigureTracking is called.
};

// Distortion capability bits reported by device.
// Used with ovrHmd_ConfigureRendering and ovrHmd_CreateDistortionMesh.
public enum ovrDistortionCaps
{
    ovrDistortionCap_Chromatic  = 0x01,     //  Supports chromatic aberration correction.
    ovrDistortionCap_TimeWarp   = 0x02,     //  Supports timewarp.
    ovrDistortionCap_Vignette   = 0x08,     //  Supports vignetting around the edges of the view.
	ovrDistortionCap_NoRestore  = 0x10,     //  Do not save and restore the graphics state when rendering distortion.
	ovrDistortionCap_FlipInput  = 0x20,     //  Flip the vertical texture coordinate of input images.
	ovrDistortionCap_SRGB       = 0x40,     //  Assume input images are in SRGB gamma-corrected color space.
	ovrDistortionCap_Overdrive  = 0x80,     //  Overdrive brightness transitions to dampen high contrast artifacts on DK2+ displays
    ovrDistortionCap_ProfileNoTimewarpSpinWaits = 0x10000,  // Use when profiling with timewarp to remove false positives
};

// Specifies which eye is being used for rendering.
// This type explicitly does not include a third "NoStereo" option, as such is
// not required for an HMD-centered API.
public enum ovrEyeType
{
    ovrEye_Left  = 0,
    ovrEye_Right = 1,
    ovrEye_Count = 2
};

// This is a complete descriptor of the HMD.
public struct ovrHmdDesc
{
    public IntPtr       Handle;  // Handle of this HMD.
    public ovrHmdType   Type;

	// Name string describing the product: "Oculus Rift DK1", etc.
    public string       ProductName;
    public string       Manufacturer;
    // HID Vendor and ProductId of the device.
    public short        VendorId;
    public short        ProductId;
    // Sensor (and display) serial number.
    public string       SerialNumber;
	// Sensor firmware
	public short        FirmwareMajor;
	public short        FirmwareMinor;
	// Fixed camera frustum dimensions, if present
    public float        CameraFrustumHFovInRadians;
    public float        CameraFrustumVFovInRadians;
    public float        CameraFrustumNearZInMeters;
    public float        CameraFrustumFarZInMeters;

    // Capability bits described by ovrHmdCaps.
    public uint         HmdCaps;
    // Capability bits described by ovrTrackingCaps.
    public uint         TrackingCaps;
    // Capability bits described by ovrDistortionCaps.
    public uint         DistortionCaps;

    // These define the recommended and maximum optical FOVs for the HMD.
    public ovrFovPort[] DefaultEyeFov;
    public ovrFovPort[] MaxEyeFov;
	public ovrEyeType[] EyeRenderOrder;

    // Resolution of the entire HMD screen (for both eyes) in pixels.
    public ovrSizei     Resolution;
    // Where monitor window should be on screen or (0,0).
    public ovrVector2i  WindowsPos;
    
    // Display that HMD should present on.
    // TBD: It may be good to remove this information relying on WidowPos instead.
    // Ultimately, we may need to come up with a more convenient alternative,
    // such as a API-specific functions that return adapter, or something that will
    // work with our monitor driver.

    // Windows: "\\\\.\\DISPLAY3", etc. Can be used in EnumDisplaySettings/CreateDC.
    public string       DisplayDeviceName;
    // MacOS
    public int          DisplayId;

    internal ovrHmdDesc(ovrHmdDesc_Raw raw)
    {
        this.Handle                     = raw.Handle;
        this.Type                       = (ovrHmdType)raw.Type;
        this.ProductName                = Marshal.PtrToStringAnsi(raw.ProductName);
        this.Manufacturer               = Marshal.PtrToStringAnsi(raw.Manufacturer);
		this.VendorId                   = raw.VendorId;
		this.ProductId                  = raw.ProductId;
		this.SerialNumber               = raw.SerialNumber;
		this.FirmwareMajor              = raw.FirmwareMajor;
		this.FirmwareMinor              = raw.FirmwareMinor;
        this.CameraFrustumHFovInRadians = raw.CameraFrustumHFovInRadians;
        this.CameraFrustumVFovInRadians = raw.CameraFrustumVFovInRadians;
        this.CameraFrustumNearZInMeters = raw.CameraFrustumNearZInMeters;
        this.CameraFrustumFarZInMeters  = raw.CameraFrustumFarZInMeters;
        this.HmdCaps                    = raw.HmdCaps;
        this.TrackingCaps               = raw.TrackingCaps;
        this.DistortionCaps             = raw.DistortionCaps;
        this.Resolution                 = raw.Resolution;
        this.WindowsPos                 = raw.WindowsPos;
        this.DefaultEyeFov              = new ovrFovPort[2] { raw.DefaultEyeFov_0, raw.DefaultEyeFov_1 };
        this.MaxEyeFov                  = new ovrFovPort[2] { raw.MaxEyeFov_0, raw.MaxEyeFov_1 };
		this.EyeRenderOrder = new ovrEyeType[2] { ovrEyeType.ovrEye_Left, ovrEyeType.ovrEye_Right };
        this.DisplayDeviceName          = Marshal.PtrToStringAnsi(raw.DisplayDeviceName);
        this.DisplayId                  = raw.DisplayId;
    }
};

// Internal description for HMD; must match C 'ovrHmdDesc' layout.
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct ovrHmdDesc_Raw
{
    public IntPtr       Handle;
    public uint         Type;
    // Use IntPtr so that CLR doesn't try to deallocate string.
    public IntPtr       ProductName;
    public IntPtr       Manufacturer;
    // HID Vendor and ProductId of the device.
    public short        VendorId;
    public short        ProductId;
    // Sensor (and display) serial number.
    [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 24)]
    public string       SerialNumber;
	// Sensor firmware
	public short        FirmwareMajor;
	public short        FirmwareMinor;
	// Fixed camera frustum dimensions, if present
    public float        CameraFrustumHFovInRadians;
    public float        CameraFrustumVFovInRadians;
    public float        CameraFrustumNearZInMeters;
    public float        CameraFrustumFarZInMeters;
    public uint         HmdCaps;
    public uint         TrackingCaps;
    public uint         DistortionCaps;
    // C# arrays are dynamic and thus not supported as return values, so just expand the struct.
    public ovrFovPort   DefaultEyeFov_0;
    public ovrFovPort   DefaultEyeFov_1;
	public ovrFovPort   MaxEyeFov_0;
    public ovrFovPort   MaxEyeFov_1;
	public ovrEyeType   EyeRenderOrder_0;
	public ovrEyeType   EyeRenderOrder_1;
	public ovrSizei     Resolution;
	public ovrVector2i  WindowsPos;
    public IntPtr       DisplayDeviceName;
    public int          DisplayId;
};

// Bit flags describing the current status of sensor tracking.
public enum ovrStatusBits
{
    ovrStatus_OrientationTracked    = 0x0001,   // Orientation is currently tracked (connected and in use).
    ovrStatus_PositionTracked       = 0x0002,   // Position is currently tracked (false if out of range).
    ovrStatus_CameraPoseTracked     = 0x0004,   // Camera pose is currently tracked.
    ovrStatus_PositionConnected     = 0x0020,   // Position tracking HW is conceded.
    ovrStatus_HmdConnected          = 0x0080    // HMD Display is available & connected.
};

public struct ovrSensorData
{
    public ovrVector3f    Accelerometer;    // Acceleration reading in in m/s^2.
    public ovrVector3f    Gyro;             // Rotation rate in rad/s.
    public ovrVector3f    Magnetometer;     // Magnetic field in Gauss.
    public float          Temperature;      // Temperature of sensor in degrees Celsius.
    public float          TimeInSeconds;    // Time when reported IMU reading took place, in seconds.
};

// Tracking state at a given absolute time (describes HMD location, etc).
// Returned by ovrHmd_GetTrackingState.
[StructLayout(LayoutKind.Sequential)]
public struct ovrTrackingState
{
    // Predicted pose configuration at requested absolute time.
	// The look-ahead interval is equal to (HeadPose.TimeInSeconds - RawSensorData.TimeInSeconds).
    public ovrPoseStatef HeadPose;
	// Current orientation and position of the external camera, if present.
    // This pose will include camera tilt (roll and pitch). For a leveled coordinate
    // system use LeveledCameraPose instead.
	public ovrPosef      CameraPose;
    // Camera frame aligned with gravity.
    // This value includes position and yaw of the camera, but not roll and pitch.
    // Can be used as a reference point to render real-world objects in the correct place.
    public ovrPosef       LeveledCameraPose;
    // Most recent sensor data received from the HMD.
    public ovrSensorData  RawSensorData;
    // Sensor status described by ovrStatusBits.
    public uint          StatusFlags;
};

// Frame timing data reported by ovrHmd_BeginFrameTiming() or ovrHmd_BeginFrame().
[StructLayout(LayoutKind.Sequential)]
public struct ovrFrameTiming
{
    // The amount of time that has passed since the previous frame returned
    // BeginFrameSeconds value, usable for movement scaling.
    // This will be clamped to no more than 0.1 seconds to prevent
    // excessive movement after pauses for loading or initialization.
    public float    DeltaSeconds;

    // It is generally expected that the following hold:
    // ThisFrameSeconds < TimewarpPointSeconds < NextFrameSeconds <
    // EyeScanoutSeconds[EyeOrder[0]] <= ScanoutMidpointSeconds <= EyeScanoutSeconds[EyeOrder[1]]

    // Absolute time value of when rendering of this frame began or is expected to
    // begin; generally equal to NextFrameSeconds of the previous frame. Can be used
    // for animation timing.
    public double   ThisFrameSeconds;
    // Absolute point when IMU expects to be sampled for this frame.
    public double   TimewarpPointSeconds;
    // Absolute time when frame Present + GPU Flush will finish, and the next frame starts.
    public double   NextFrameSeconds;

    // Time when when half of the screen will be scanned out. Can be passes as a prediction
    // value to ovrHmd_GetTrackingState() go get general orientation.
    public double   ScanoutMidpointSeconds;
    // Timing points when each eye will be scanned out to display. Used for rendering each eye. 
    public double[] EyeScanoutSeconds;

    internal ovrFrameTiming(ovrFrameTiming_Raw raw)
    {
        this.DeltaSeconds           = raw.DeltaSeconds;
        this.ThisFrameSeconds       = raw.ThisFrameSeconds;
        this.TimewarpPointSeconds   = raw.TimewarpPointSeconds;
        this.NextFrameSeconds       = raw.NextFrameSeconds;
        this.ScanoutMidpointSeconds = raw.ScanoutMidpointSeconds;
        this.EyeScanoutSeconds      = new double[2] { raw.EyeScanoutSeconds_0, raw.EyeScanoutSeconds_1 };
    }
};

// Internal description for ovrFrameTiming; must match C 'ovrFrameTiming' layout.
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct ovrFrameTiming_Raw
{
    public float  DeltaSeconds;
    public double ThisFrameSeconds;
    public double TimewarpPointSeconds;
    public double NextFrameSeconds;
    public double ScanoutMidpointSeconds;
    // C# arrays are dynamic and thus not supported as return values, so just expand the struct.
    public double EyeScanoutSeconds_0;
    public double EyeScanoutSeconds_1;
};

// Rendering information for each eye, computed by either ovrHmd_ConfigureRendering().
// or ovrHmd_GetRenderDesc() based on the specified Fov.
// Note that the rendering viewport is not included here as it can be 
// specified separately and modified per frame though:
//    (a) calling ovrHmd_GetRenderScaleAndOffset with game-rendered api,
// or (b) passing different values in ovrTexture in case of SDK-rendered distortion.
[StructLayout(LayoutKind.Sequential)]
public struct ovrEyeRenderDesc
{
    ovrEyeType  Eye;
    ovrFovPort  Fov;
	ovrRecti    DistortedViewport;          // Distortion viewport
    ovrVector2f PixelsPerTanAngleAtCenter;  // How many display pixels will fit in tan(angle) = 1.
    ovrVector3f ViewAdjust;                 // Translation to be applied to view matrix.
};

//-----------------------------------------------------------------------------------
// ***** Platform-independent Rendering Configuration

// These types are used to hide platform-specific details when passing
// render device, OS and texture data to the APIs.
//
// The benefit of having these wrappers vs. platform-specific API functions is
// that they allow game glue code to be portable. A typical example is an
// engine that has multiple back ends, say GL and D3D. Portable code that calls
// these back ends may also use LibOVR. To do this, back ends can be modified
// to return portable types such as ovrTexture and ovrRenderAPIConfig.

public enum ovrRenderAPIType
{
    ovrRenderAPI_None,
    ovrRenderAPI_OpenGL,
    ovrRenderAPI_Android_GLES,  // May include extra native window pointers, etc.
    ovrRenderAPI_D3D9,
    ovrRenderAPI_D3D10,
    ovrRenderAPI_D3D11,
    ovrRenderAPI_Count
};

// Platform-independent part of rendering API-configuration data.
// It is a part of ovrRenderAPIConfig, passed to ovrHmd_Configure.
[StructLayout(LayoutKind.Sequential)]
public struct ovrRenderAPIConfigHeader
{
    public ovrRenderAPIType API;
    public ovrSizei         RTSize;
    public int              Multisample;
};

[StructLayout(LayoutKind.Sequential)]
public struct ovrRenderAPIConfig
{
    public ovrRenderAPIConfigHeader Header;
    public IntPtr[]                 PlatformData;

	internal ovrRenderAPIConfig(ovrRenderAPIConfig_Raw raw)
	{
		this.Header = raw.Header;
		this.PlatformData = new IntPtr[8] {
			raw.PlatformData_0,
		   	raw.PlatformData_1,
			raw.PlatformData_2,
		   	raw.PlatformData_3,
		   	raw.PlatformData_4,
		   	raw.PlatformData_5,
		   	raw.PlatformData_6,
		   	raw.PlatformData_7 };
	}
};

// Internal description for ovrFrameTiming; must match C 'ovrFrameTiming' layout.
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct ovrRenderAPIConfig_Raw
{
    public ovrRenderAPIConfigHeader Header;
    public IntPtr                   PlatformData_0;
    public IntPtr                   PlatformData_1;
    public IntPtr                   PlatformData_2;
    public IntPtr                   PlatformData_3;
    public IntPtr                   PlatformData_4;
    public IntPtr                   PlatformData_5;
    public IntPtr                   PlatformData_6;
    public IntPtr                   PlatformData_7;
};

// Platform-independent part of eye texture descriptor.
// It is a part of ovrTexture, passed to ovrHmd_EndFrame.
//  - If RenderViewport is all zeros, will be used.
[StructLayout(LayoutKind.Sequential)]
public struct ovrTextureHeader
{
    public ovrRenderAPIType API;
    public ovrSizei         TextureSize;
    public ovrRecti         RenderViewport;  // Pixel viewport in texture that holds eye image.
};

[StructLayout(LayoutKind.Sequential)]
public struct ovrTexture
{
    public ovrTextureHeader Header;
    public IntPtr[]         PlatformData;

	internal ovrTexture(ovrTexture_Raw raw)
	{
		this.Header = raw.Header;
		this.PlatformData = new IntPtr[8] {
			raw.PlatformData_0,
		   	raw.PlatformData_1,
			raw.PlatformData_2,
		   	raw.PlatformData_3,
		   	raw.PlatformData_4,
		   	raw.PlatformData_5,
		   	raw.PlatformData_6,
		   	raw.PlatformData_7 };
	}
};

[StructLayout(LayoutKind.Sequential)]
public struct ovrTexture_Raw
{
    public ovrTextureHeader Header;
    public IntPtr           PlatformData_0;
    public IntPtr           PlatformData_1;
    public IntPtr           PlatformData_2;
    public IntPtr           PlatformData_3;
    public IntPtr           PlatformData_4;
    public IntPtr           PlatformData_5;
    public IntPtr           PlatformData_6;
    public IntPtr           PlatformData_7;
};

// Describes a vertex used for distortion; this is intended to be converted into
// the engine-specific format.
// Some fields may be unused based on ovrDistortionCaps selected. TexG and TexB, for example,
// are not used if chromatic correction is not requested.
[StructLayout(LayoutKind.Sequential)]
public struct ovrDistortionVertex
{
    public ovrVector2f ScreenPosNDC;    // [-1,+1],[-1,+1] over the entire framebuffer.
    public float       TimeWarpFactor;  // Lerp factor between time-warp matrices. Can be encoded in Pos.z.
    public float       VignetteFactor;  // Vignette fade factor. Can be encoded in Pos.w.
    public ovrVector2f TanEyeAnglesR;
    public ovrVector2f TanEyeAnglesG;
    public ovrVector2f TanEyeAnglesB;
};

public struct ovrDistortionMesh
{
    public ovrDistortionVertex[] pVertexData;
    public short[]               pIndexData;
    public uint                  VertexCount;
    public uint                  IndexCount;

    internal ovrDistortionMesh(ovrDistortionMesh_Raw raw)
    {
        this.VertexCount = raw.VertexCount;
        this.pVertexData = new ovrDistortionVertex[this.VertexCount];
        this.IndexCount  = raw.IndexCount;
        this.pIndexData  = new short[this.IndexCount];

        // Copy data
        System.Type vertexType = typeof(ovrDistortionVertex);
        Int32       vertexSize = Marshal.SizeOf(vertexType);
        Int32       indexSize  = sizeof(short);
        Int64       pvertices  = raw.pVertexData.ToInt64();
        Int64       pindices   = raw.pIndexData.ToInt64();

		// TODO: Investigate using Marshal.Copy() or Buffer.BlockCopy() for improved performance

        for (int i = 0; i < raw.VertexCount; i++)
        {
            pVertexData[i] = (ovrDistortionVertex)Marshal.PtrToStructure(new IntPtr(pvertices), vertexType);
            pvertices += vertexSize;
        }
        // Indices are stored as shorts.
        for (int j = 0; j < raw.IndexCount; j++)
        {
            pIndexData[j] = Marshal.ReadInt16(new IntPtr(pindices));
            pindices += indexSize;
        }
    }
};

// Describes a full set of distortion mesh data, filled in by ovrHmd_CreateDistortionMesh.
// Contents of this data structure, if not null, should be freed by ovrHmd_DestroyDistortionMesh.
[StructLayout(LayoutKind.Sequential)]
internal struct ovrDistortionMesh_Raw
{
    public IntPtr pVertexData;
    public IntPtr pIndexData;
    public uint   VertexCount;
    public uint   IndexCount;
};

// Used by ovrHmd_GetHSWDisplayState to report the current display state.
[StructLayout(LayoutKind.Sequential)]
public struct ovrHSWDisplayState
{
    public bool    Displayed;       // If true then the warning should be currently visible
                                    // and the following variables have meaning. Else there is no
                                    // warning being displayed for this application on the given HMD.
    public double  StartTime;       // Absolute time when the warning was first displayed. See ovr_GetTimeInSeconds().
    public double  DismissibleTime; // Earliest absolute time when the warning can be dismissed. May be a time in the past.
};

//-------------------------------------------------------------------------------------
//  ****** ovrHmd Class
// ovrHmd provides an interface to an HMD object.  The ovrHmd instance is normally
// created by ovrHmd::Create, after which its other methods can be called.
// The typical process would involve calling:
//
// Setup:
//   - Initialize() to initialize the OVR SDK.
//   - Create() to create an HMD.
//   - Use hmd members and ovrHmd_GetFovTextureSize() to determine graphics configuration.
//   - ConfigureTracking() to configure and initialize tracking.
//   - ConfigureRendering() to setup graphics for SDK rendering.
//   - If ovrHmdCap_ExtendDesktop is not set, use ovrHmd_AttachToWindow to associate the window with an Hmd.
//   - Allocate textures as needed.
//
// Game Loop:
//   - Call ovrHmd_BeginFrame() to get frame timing and orientation information.
//   - Render each eye in between, using ovrHmd_GetEyePose to get frame poses
//     for rendering.
//   - Call ovrHmd_EndFrame() to render distorted textures to the back buffer
//     and present them on the Hmd.
//
// Shutdown:
//   - Destroy() to release the HMD.
//   - ovr_Shutdown() to shutdown the OVR SDK.
	public class Hmd 
	{
		public const string OVR_KEY_USER                   = "User";
		public const string OVR_KEY_NAME                   = "Name";
		public const string OVR_KEY_GENDER                 = "Gender";
		public const string OVR_KEY_PLAYER_HEIGHT          = "PlayerHeight";
		public const string OVR_KEY_EYE_HEIGHT             = "EyeHeight";
		public const string OVR_KEY_IPD                    = "IPD";
		public const string OVR_KEY_NECK_TO_EYE_DISTANCE   = "NeckEyeDistance";

		// Default measurements empirically determined at Oculus to make us happy
		// TODO: Need to sync these with ANSUR-88 models and tests
		public const string OVR_DEFAULT_GENDER                 = "Unknown";
		public const float OVR_DEFAULT_PLAYER_HEIGHT           = 1.778f;
		public const float OVR_DEFAULT_EYE_HEIGHT              = 1.675f;
		public const float OVR_DEFAULT_IPD                     = 0.064f;
		public const float OVR_DEFAULT_NECK_TO_EYE_HORIZONTAL  = 0.09f;
		public const float OVR_DEFAULT_NECK_TO_EYE_VERTICAL    = 0.15f;
		public const float OVR_DEFAULT_EYE_RELIEF_DIAL         = 3;

		IntPtr HmdPtr;

		// Used to return color result to avoid per-frame allocation.
		byte[] LatencyTestRgb = new byte[3];
		
		// -----------------------------------------------------------------------------------
		// Static Methods

		// Library init/shutdown, must be called around all other OVR code.
		// No other functions calls are allowed before Initialize succeeds or after Shutdown.
		public static bool Initialize()
		{
			return ovr_Initialize();
		}

		public static void Shutdown()
		{
			ovr_Shutdown();
		}

		// Detects or re-detects HMDs and reports the total number detected.
		// Users can get information about each HMD by calling ovrHmd_Create with an index.

		public static int Detect()
		{
			return ovrHmd_Detect();
		}

		public static Hmd Create(int index)
		{
			IntPtr hmdPtr = ovrHmd_Create(index);
			if (hmdPtr == IntPtr.Zero)
				return null;

            OVR_SetHMD(ref hmdPtr);
			return new Hmd(hmdPtr);
		}

		public static Hmd CreateDebug(ovrHmdType type)
		{
			IntPtr hmdPtr = ovrHmd_CreateDebug(type);
			if (hmdPtr == IntPtr.Zero)
				return null;

            OVR_SetHMD(ref hmdPtr);
			return new Hmd(hmdPtr);
		}

        public static Hmd GetHmd()
        {
            IntPtr hmdPtr = IntPtr.Zero;
            OVR_GetHMD(ref hmdPtr);
            return (hmdPtr != IntPtr.Zero) ? new Hmd(hmdPtr) : null;
        }

		// Used to generate projection from ovrEyeDesc::Fov.
		public static ovrMatrix4f GetProjection(ovrFovPort fov, float znear, float zfar, bool rightHanded)
		{
			return new ovrMatrix4f(ovrMatrix4f_Projection(fov, znear, zfar, rightHanded));
		}

		// Used for 2D rendering, Y is down
		// orthoScale = 1.0f / pixelsPerTanAngleAtCenter
		// orthoDistance = distance from camera, such as 0.8m
		public static ovrMatrix4f GetOrthoSubProjection(ovrMatrix4f projection, ovrVector2f orthoScale, float orthoDistance, float eyeViewAdjustX)
		{
			return new ovrMatrix4f(ovrMatrix4f_OrthoSubProjection(projection, orthoScale, orthoDistance, eyeViewAdjustX));
		}

		// Absolute time.
		public static double GetTimeInSeconds()
		{
			return ovr_GetTimeInSeconds();
		}
		
		// Waits until the specified absolute time.
		public static double WaitTillTime(double absTime)
		{
			return ovr_WaitTillTime(absTime);
		}
		
		// -----------------------------------------------------------------------------------
		// **** Constructor
		
		public Hmd(IntPtr hmdPtr)
		{
			this.HmdPtr = hmdPtr;
		}

        //~Hmd()
        //{
        //    Dispose();
        //}

        public void Destroy()
		{
            //ovrHmd_Destroy(HmdPtr);
            //HmdPtr = IntPtr.Zero;
            //OVR_SetHMD(ref HmdPtr);
        }

		// Returns last error for HMD state. Returns null for no error.
		public string GetLastError()
		{
			return ovrHmd_GetLastError(HmdPtr);
		}

#if false
        public bool AttachToWindow(ovrRecti destMirrorRect, ovrRecti sourceRenderTargetRect)
        {
            return ovrHmd_AttachToWindow(HmdPtr, IntPtr.Zero, destMirrorRect, sourceRenderTargetRect);
        }
#endif

        public uint GetEnabledCaps()
		{
			return ovrHmd_GetEnabledCaps(HmdPtr);
		}
		
		public void SetEnabledCaps(uint capsBits)
		{
			ovrHmd_SetEnabledCaps(HmdPtr, capsBits);
		}

		public ovrHmdDesc GetDesc()
		{
			ovrHmdDesc_Raw rawDesc = (ovrHmdDesc_Raw)Marshal.PtrToStructure(HmdPtr, typeof(ovrHmdDesc_Raw));
			return new ovrHmdDesc(rawDesc);
		}

		//-------------------------------------------------------------------------------------
		// ***** Tracking Interface
		
		// All tracking interface functions are thread-safe, allowing sensor state to be sampled
		// from different threads.
		
		// ConfigureTracking starts sensor sampling, enabling specified capabilities, described by ovrTrackingCaps.
		//  - supportedTrackingCaps specifies support that is requested. The function will succeed even
		//    if these caps are not available (i.e. sensor or camera is unplugged). Support will
		//    automatically be enabled if such device is plugged in later. Software should check
		//    ovrTrackingState.StatusFlags for real-time status.
		// - requiredTrackingCaps specify sensor capabilities required at the time of the call. If they
		//   are not available, the function will fail. Pass 0 if only specifying SupportedTrackingCaps.
		// - Pass 0 for both supportedTrackingCaps and requiredTrackingCaps to disable tracking.
		public bool ConfigureTracking(uint supportedTrackingCaps, uint requiredTrackingCaps)
		{
			return ovrHmd_ConfigureTracking(HmdPtr, supportedTrackingCaps, requiredTrackingCaps);
		}

		// Re-centers the sensor orientation.
		// Normally it will recenter the (x,y,z) translational components,
		// and it will recenter the yaw component of orientation.
		public void RecenterPose()
		{
			ovrHmd_RecenterPose(HmdPtr);
		}
		
		// Returns sensor state reading based on the specified absolute system time.
		// Pass absTime value of 0.0 to request the most recent sensor reading; in this case
		// both HeadPose and SamplePose will have the same value.
		// ovrHmd_GetEyePose relies on this internally.
		// This may also be used for more refined timing of FrontBuffer rendering logic, etc.
		public ovrTrackingState GetTrackingState(double absTime = 0.0d)
		{
			return ovrHmd_GetTrackingState(HmdPtr, absTime);
		}
		
		//-------------------------------------------------------------------------------------
		// ***** Graphics Setup
		
		// Calculates texture size recommended for rendering one eye within HMD, given FOV cone.
		// Higher FOV will generally require larger textures to maintain quality.
		//  - pixelsPerDisplayPixel specifies that number of render target pixels per display
		//    pixel at center of distortion; 1.0 is the default value. Lower values
		//    can improve performance.
		public ovrSizei GetFovTextureSize(ovrEyeType eye, ovrFovPort fov, float pixelsPerDisplayPixel = 1.0f)
		{
			return ovrHmd_GetFovTextureSize(HmdPtr, eye, fov, pixelsPerDisplayPixel);
		}
		
		public ovrEyeRenderDesc[] ConfigureRendering(ovrFovPort[] eyeFovIn, uint distortionCaps)
		{
			ovrEyeRenderDesc[] eyeRenderDesc = new ovrEyeRenderDesc[] { new ovrEyeRenderDesc(), new ovrEyeRenderDesc() };
			ovrRenderAPIConfig renderAPIConfig = new ovrRenderAPIConfig();

			if (ovrHmd_ConfigureRendering(HmdPtr, ref renderAPIConfig, distortionCaps, eyeFovIn, eyeRenderDesc))
				return eyeRenderDesc;
			return null;
		}

		// Begins a frame, returning timing and orientation information.
		// This should be called in the very beginning of game rendering loop (on render thread),
		// ideally immediately after prior frame buffer swap (Present) was flushed and synced.
		// Pass 0 for for frame index if not using GetFrameTiming.
		public ovrFrameTiming BeginFrame(uint frameIndex = 0)
		{
			ovrFrameTiming_Raw raw = ovrHmd_BeginFrame(HmdPtr, frameIndex);
			return new ovrFrameTiming(raw);
		}

		// Ends frame, rendering textures to frame buffer. This performs distortion and scaling internally.
		// Must be called on the same thread as BeginFrame.
		public void EndFrame()
		{
			ovrHmd_EndFrame(HmdPtr);
		}
		
		public ovrEyeRenderDesc GetRenderDesc(ovrEyeType eyeType, ovrFovPort fov)
		{
			return ovrHmd_GetRenderDesc(HmdPtr, eyeType, fov);
		}

		// Generate distortion mesh per eye.
		// Distortion capabilities will depend on 'distortionCaps' flags; user should rely on
		// appropriate shaders based on their settings.
		// Distortion mesh data will be allocated and stored into the ovrDistortionMesh data structure,
		// which should be explicitly freed with ovrHmd_DestroyDistortionMesh.
		// Users should call ovrHmd_GetRenderScaleAndOffset to get uvScale and Offset values for rendering.
		// The function shouldn't fail unless theres is a configuration or memory error, in which case
		// ovrDistortionMesh values will be set to null.
		// This is the only function in the SDK reliant on eye relief, currently imported from profiles, 
		// or overriden here.
		public ovrDistortionMesh? CreateDistortionMesh(ovrEyeType eye,
													   ovrFovPort fov,
													   uint distortionCaps)
		{
			ovrDistortionMesh_Raw rawMesh = new ovrDistortionMesh_Raw();

			if (!ovrHmd_CreateDistortionMesh(HmdPtr, eye, fov, distortionCaps, out rawMesh))
			{
				return null;
			}

			ovrDistortionMesh mesh = new ovrDistortionMesh(rawMesh);
			ovrHmd_DestroyDistortionMesh(ref rawMesh);
			return mesh;
		}

		// Computes updated 'uvScaleOffsetOut' to be used with a distortion if render target size or
		// viewport changes after the fact. This can be used to adjust render size every frame, if desired.
		public ovrVector2f[] GetRenderScaleAndOffset(ovrFovPort fov,
													 ovrSizei textureSize,
													 ovrRecti renderViewport)
		{
			ovrVector2f[] uvScaleOffsetOut;
			ovrHmd_GetRenderScaleAndOffset(fov, textureSize, renderViewport, out uvScaleOffsetOut);
			return uvScaleOffsetOut;
		}

		// Thread-safe timing function for the main thread. Caller should increment frameIndex
		// with every frame and pass the index to RenderThread for processing.
		public ovrFrameTiming GetFrameTiming(uint frameIndex)
		{
			ovrFrameTiming_Raw raw = ovrHmd_GetFrameTiming(HmdPtr, frameIndex);
			return new ovrFrameTiming(raw);
		}

		// Called at the beginning of the frame on the Render Thread.
		// Pass frameIndex == 0 if ovrHmd_GetFrameTiming isn't being used. Otherwise,
		// pass the same frame index as was used for GetFrameTiming on the main thread.
		public ovrFrameTiming BeginFrameTiming(uint frameIndex)
		{
			ovrFrameTiming_Raw raw = ovrHmd_BeginFrameTiming(HmdPtr, frameIndex);
			return new ovrFrameTiming(raw);
		}

		// Marks the end of game-rendered frame, tracking the necessary timing information. This
		// function must be called immediately after Present/SwapBuffers + GPU sync. GPU sync is important
		// before this call to reduce latency and ensure proper timing.
		public void EndFrameTiming()
		{
			ovrHmd_EndFrameTiming(HmdPtr);
		}

		// Initializes and resets frame time tracking. This is typically not necessary, but
		// is helpful if game changes vsync state or video mode. vsync is assumed to be on if this
		// isn't called. Resets internal frame index to the specified number.
		public void ResetFrameTiming(uint frameIndex)
		{
			ovrHmd_ResetFrameTiming(HmdPtr, frameIndex);
		}

		// Predicts and returns Pose that should be used rendering the specified eye.
		// Must be called between ovrHmd_BeginFrameTiming & ovrHmd_EndFrameTiming.
		public ovrPosef GetEyePose(ovrEyeType eye)
		{
			return ovrHmd_GetEyePose(HmdPtr, eye);
		}

		// Computes timewarp matrices used by distortion mesh shader, these are used to adjust
		// for orientation change since the last call to ovrHmd_GetEyePose for this eye.
		// The ovrDistortionVertex::TimeWarpFactor is used to blend between the matrices,
		// usually representing two different sides of the screen.
		// Must be called on the same thread as ovrHmd_BeginFrameTiming.
		public ovrMatrix4f[] ovrHmd_GetEyeTimewarpMatrices(ovrEyeType eye, ovrPosef renderPose)
		{
			ovrMatrix4f_Raw[] rawMats = {new ovrMatrix4f_Raw(), new ovrMatrix4f_Raw()};
			ovrHmd_GetEyeTimewarpMatrices(HmdPtr, eye, renderPose, out rawMats);

			ovrMatrix4f[] mats = {new ovrMatrix4f(rawMats[0]), new ovrMatrix4f(rawMats[1])};
			return mats;
		}

		// -----------------------------------------------------------------------------------
		// ***** Latency Test interface

		// Does latency test processing, returning rgb[3] color array if color should
		// be used to clear the screen. Return null if no clear is needed.
		public byte[] ProcessLatencyTest()
		{
			if (ovrHmd_ProcessLatencyTest(HmdPtr, out LatencyTestRgb))
				return LatencyTestRgb;
			return null;
		}

		// Returns non-null string once with latency test result, when it is available.
		// Returns null for no result.
		public string GetLatencyTestResult()
		{
			IntPtr p = ovrHmd_GetLatencyTestResult(HmdPtr);
			return (p == IntPtr.Zero) ? null : Marshal.PtrToStringAnsi(p);
		}

		//-------------------------------------------------------------------------------------
		// ***** Health and Safety Warning Display interface

		// Returns the current state of the HSW display. If the application is doing the rendering of
		// the HSW display then this function serves to indicate that the the warning should be 
		// currently displayed. If the application is using SDK-based eye rendering then the SDK by 
		// default automatically handles the drawing of the HSW display. An application that uses 
		// application-based eye rendering should use this function to know when to start drawing the
		// HSW display itself and can optionally use it in conjunction with ovrHmd_DismissHSWDisplay
		// as described below.
		//
		// Example usage for application-based rendering:
		//    bool HSWDisplayCurrentlyDisplayed = false; // global or class member variable
		//    ovrHSWDisplayState hswDisplayState;
		//    ovrHmd_GetHSWDisplayState(Hmd, &hswDisplayState);
		//
		//    if (hswDisplayState.Displayed && !HSWDisplayCurrentlyDisplayed) {
		//        <insert model into the scene that stays in front of the user>
		//        HSWDisplayCurrentlyDisplayed = true;
		//    }
		public ovrHSWDisplayState GetHSWDisplayState()
		{
			ovrHSWDisplayState hswDisplayState;
            OVR_GetHMD(ref HmdPtr);
			ovrHmd_GetHSWDisplayState(HmdPtr, out hswDisplayState);
			return hswDisplayState;
		}

		// Dismisses the HSW display if the warning is dismissible and the earliest dismissal time 
		// has occurred. Returns true if the display is valid and could be dismissed. The application 
		// should recognize that the HSW display is being displayed (via ovrHmd_GetHSWDisplayState)
		// and if so then call this function when the appropriate user input to dismiss the warning
		// occurs.
		//
		// Example usage :
		//    void ProcessEvent(int key) {
		//        if(key == escape) {
		//            ovrHSWDisplayState hswDisplayState;
		//            ovrHmd_GetHSWDisplayState(hmd, &hswDisplayState);
		//
		//            if(hswDisplayState.Displayed && ovrHmd_DismissHSWDisplay(hmd)) {
		//                <remove model from the scene>
		//                HSWDisplayCurrentlyDisplayed = false;
		//            }
		//        }
		//    }
		public bool DismissHSWDisplay()
		{
			return ovrHmd_DismissHSWDisplay(HmdPtr);
		}

		// -----------------------------------------------------------------------------------
		// ***** Property Access

		// These allow accessing different properties of the HMD and profile.
		// Some of the properties may go away with profile/HMD versions, so software should
		// use defaults and/or proper fallbacks.

		// Get bool property. Returns first element if property is a bool array.
		// Returns defaultValue if property doesn't exist.
		public bool GetBool(string propertyName, bool defaultVal = false)
		{
			return ovrHmd_GetBool(HmdPtr, propertyName, defaultVal);
		}

		// Get int property. Returns first element if property is an int array.
		// Returns defaultValue if property doesn't exist.
		public int GetInt(string propertyName, int defaultVal = 0)
		{
			return ovrHmd_GetInt(HmdPtr, propertyName, defaultVal);
		}

		// Get float property. Returns first element if property is a float array.
		// Returns defaultValue if property doesn't exist.
		public float GetFloat(string propertyName, float defaultVal = 0.0f)
		{
			return ovrHmd_GetFloat(HmdPtr, propertyName, defaultVal);
		}

		// Get float[] property. Returns null array if property doesn't exist.
		// Maximum of arraySize elements will be written.
		public float[] GetFloatArray(string propertyName, float[] values)
		{
			if (values == null)
				return null;

			ovrHmd_GetFloatArray(HmdPtr, propertyName, values, (uint)values.Length);
			return values;
		}

		// Get string property. Returns first element if property is a string array.
		// Returns defaultValue if property doesn't exist.
		// String memory is guaranteed to exist until next call to GetString or GetStringArray, or HMD is destroyed.
		public string GetString(string propertyName, string defaultVal = null)
		{
			IntPtr p = ovrHmd_GetString(HmdPtr, propertyName, null);
			if (p == IntPtr.Zero)
				return defaultVal;
			return Marshal.PtrToStringAnsi(p);
		}

		public const string LibFile = "OculusPlugin";

		// Imported functions from
		// OVRPlugin.dll 	(PC)
		// OVRPlugin.bundle (OSX)
		// OVRPlugin.so 	(Linux, Android)

		// -----------------------------------------------------------------------------------
		// ***** Private Interface to OculusPlugin
        [DllImport(LibFile)]
        private static extern void OVR_SetHMD(ref IntPtr hmdPtr);
        [DllImport(LibFile)]
        private static extern void OVR_GetHMD(ref IntPtr hmdPtr);


		// -----------------------------------------------------------------------------------
		// ***** Private Interface to libOVR

		[DllImport(LibFile)]
		private static extern bool ovr_Initialize();
		[DllImport(LibFile)]
		private static extern void ovr_Shutdown();
		[DllImport(LibFile)]
		private static extern int ovrHmd_Detect();
		[DllImport(LibFile)]
		private static extern IntPtr ovrHmd_Create(int index);
		[DllImport(LibFile)]
		private static extern void ovrHmd_Destroy(IntPtr hmd);
		[DllImport(LibFile)]
		private static extern IntPtr ovrHmd_CreateDebug(ovrHmdType type);
		[DllImport(LibFile)]
		private static extern string ovrHmd_GetLastError(IntPtr hmd);
        [DllImport(LibFile)]
        private static extern bool ovrHmd_AttachToWindow(IntPtr hmd, IntPtr window, ovrRecti destMirrorRect, ovrRecti sourceRenderTargetRect);
		[DllImport(LibFile)]
		private static extern uint ovrHmd_GetEnabledCaps(IntPtr hmd);
		[DllImport(LibFile)]
		private static extern void ovrHmd_SetEnabledCaps(IntPtr hmd, uint capsBits);

		//-------------------------------------------------------------------------------------
		// ***** Sensor Interface

		[DllImport(LibFile)]
		private static extern bool ovrHmd_ConfigureTracking(IntPtr hmd, uint supportedTrackingCaps, uint requiredTrackingCaps);
		[DllImport(LibFile)]
		private static extern void ovrHmd_RecenterPose(IntPtr hmd);
		[DllImport(LibFile)]
		private static extern ovrTrackingState ovrHmd_GetTrackingState(IntPtr hmd, double absTime);

		//-------------------------------------------------------------------------------------
		// ***** Graphics Setup
		
		[DllImport(LibFile)]
		private static extern ovrSizei ovrHmd_GetFovTextureSize(IntPtr hmd, ovrEyeType eye, ovrFovPort fov, float pixelsPerDisplayPixel);
		[DllImport(LibFile)]
		private static extern bool ovrHmd_ConfigureRendering(IntPtr hmd,
						                                     ref ovrRenderAPIConfig apiConfig,
						                                     uint distortionCaps,
					                                         ovrFovPort[] eyeFovIn,
						                                     ovrEyeRenderDesc[] eyeRenderDescOut);
		[DllImport(LibFile)]
		private static extern ovrFrameTiming_Raw ovrHmd_BeginFrame(IntPtr hmd, uint frameIndex);
		[DllImport(LibFile)]
		private static extern void ovrHmd_EndFrame(IntPtr hmd);
		[DllImport(LibFile)]
		private static extern ovrEyeRenderDesc ovrHmd_GetRenderDesc(IntPtr hmd, ovrEyeType eye, ovrFovPort fov);

		//-------------------------------------------------------------------------------------
		// Stateless math setup functions

		// Used to generate projection from ovrEyeDesc::Fov
		[DllImport(LibFile)]
		private static extern ovrMatrix4f_Raw ovrMatrix4f_Projection(ovrFovPort fov, float znear, float zfar, bool rightHanded);
		[DllImport(LibFile)]
		private static extern ovrMatrix4f_Raw ovrMatrix4f_OrthoSubProjection(ovrMatrix4f projection,
																		 ovrVector2f orthoScale,
																		 float orthoDistance,
																		 float eyeViewAdjustX);
		[DllImport(LibFile)]
		private static extern double ovr_GetTimeInSeconds();
		[DllImport(LibFile)]
		private static extern double ovr_WaitTillTime(double absTime);

		// -----------------------------------------------------------------------------------
		// **** Game-side rendering API

		[DllImport(LibFile)]
		private static extern bool ovrHmd_CreateDistortionMesh(IntPtr hmd,
															   ovrEyeType eye,
															   ovrFovPort fov,
															   uint distortionCaps,
															   [Out] out ovrDistortionMesh_Raw meshData);
		[DllImport(LibFile)]
		private static extern void ovrHmd_DestroyDistortionMesh(ref ovrDistortionMesh_Raw meshData);
		[DllImport(LibFile)]
		private static extern void ovrHmd_GetRenderScaleAndOffset(ovrFovPort fov,
																  ovrSizei textureSize,
																  ovrRecti renderViewport,
																  [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)]
																  [Out] out ovrVector2f[] uvScaleOffsetOut);
		[DllImport(LibFile)]
		private static extern ovrFrameTiming_Raw ovrHmd_GetFrameTiming(IntPtr hmd, uint frameIndex);
		[DllImport(LibFile)]
		private static extern ovrFrameTiming_Raw ovrHmd_BeginFrameTiming(IntPtr hmd, uint frameIndex);
		[DllImport(LibFile)]
		private static extern void ovrHmd_EndFrameTiming(IntPtr hmd);
		[DllImport(LibFile)]
		private static extern void ovrHmd_ResetFrameTiming(IntPtr hmd, uint frameIndex);
		[DllImport(LibFile)]
		private static extern ovrPosef ovrHmd_GetEyePose(IntPtr hmd, ovrEyeType eye);
		[DllImport(LibFile)]
		private static extern void ovrHmd_GetEyeTimewarpMatrices(IntPtr hmd, ovrEyeType eye, ovrPosef renderPose,
																 [MarshalAs(UnmanagedType.LPArray, SizeConst = 2)]
																 [Out] out ovrMatrix4f_Raw[] twnOut);

		// -----------------------------------------------------------------------------------
		// ***** Latency Test interface

		[DllImport(LibFile)]
		private static extern bool ovrHmd_ProcessLatencyTest(IntPtr hmd,
															 [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]
															 [Out] out byte[] rgbColorOut);
		// Returns IntPtr to avoid allocation.
		[DllImport(LibFile)]
		private static extern IntPtr ovrHmd_GetLatencyTestResult(IntPtr hmd);

		//-------------------------------------------------------------------------------------
		// ***** Health and Safety Warning Display interface

		[DllImport(LibFile)]
		private static extern void ovrHmd_GetHSWDisplayState(IntPtr hmd, [Out] out ovrHSWDisplayState hasWarningState);
		[DllImport(LibFile)]
		private static extern bool ovrHmd_DismissHSWDisplay(IntPtr hmd);
		
		// -----------------------------------------------------------------------------------
		// ***** Property Access

		// TODO: Expose SetBool,SetFloat,etc.

		[DllImport(LibFile)]
		private static extern bool ovrHmd_GetBool(IntPtr hmd,
												  [MarshalAs(UnmanagedType.LPStr)]
												  string propertyName,
												  bool defaultVal);
		[DllImport(LibFile)]
		private static extern int ovrHmd_GetInt(IntPtr hmd,
												[MarshalAs(UnmanagedType.LPStr)]
												string propertyName,
												int defaultVal);
		[DllImport(LibFile)]
		private static extern float ovrHmd_GetFloat(IntPtr hmd,
													[MarshalAs(UnmanagedType.LPStr)]
													string propertyName,
													float defaultVal);
		[DllImport(LibFile)]
		private static extern uint ovrHmd_GetFloatArray(IntPtr hmd,
														[MarshalAs(UnmanagedType.LPStr)]
														string propertyName,
														float[] values, // TBD: Passing var size?
														uint arraySize);
		[DllImport(LibFile)]
		private static extern IntPtr ovrHmd_GetString(IntPtr hmd,
													  [MarshalAs(UnmanagedType.LPStr)]
													  string propertyName,
													  [MarshalAs(UnmanagedType.LPStr)]
													  string defaultVal);
	}
}
