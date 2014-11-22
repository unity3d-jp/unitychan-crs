/************************************************************************************

Filename    :   OVRDevice.cs
Content     :   Interface for the Oculus Rift Device
Created     :   February 14, 2013
Authors     :   Peter Giokaris, David Borel

Copyright   :   Copyright 2014 Oculus VR, Inc. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.1 (the "License"); 
you may not use the Oculus VR Rift SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.1 

Unless required by applicable law or agreed to in writing, the Oculus VR SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OVR;

//-------------------------------------------------------------------------------------
// ***** OVRDevice
//
/// <summary>
/// OVRDevice is the main interface to the Oculus Rift hardware. It includes wrapper functions
/// for  all exported C++ functions, as well as helper functions that use the stored Oculus
/// variables to help set up camera behavior.
///
/// This component is added to the OVRCameraController prefab. It can be part of any 
/// game object that one sees fit to place it. However, it should only be declared once,
/// since there are public members that allow for tweaking certain Rift values in the
/// Unity inspector.
///
/// </summary>
public class OVRDevice : MonoBehaviour 
{
	/// <summary>
	/// The current HMD's nominal refresh rate.
	/// </summary>
	public static float SimulationRate = 60f;

	public static Hmd HMD;
	
	// PUBLIC
	/// <summary>
	/// Only used if prediction is on and timewarp is disabled (see OVRCameraController).
	/// </summary>
	public static float PredictionTime 								= 0.03f; // 30 ms
	// if off, tracker will not reset when new scene is loaded
	public static bool  ResetTrackerOnLoad							= true;

    // Records whether or not we're running on an unsupported platform
    [HideInInspector]
    public static bool SupportedPlatform;

	#region MonoBehaviour Message Handlers
	void Awake()
	{
        // Detect whether this platform is a supported platform
        RuntimePlatform currPlatform = Application.platform;
        SupportedPlatform |= currPlatform == RuntimePlatform.Android;
        SupportedPlatform |= currPlatform == RuntimePlatform.LinuxPlayer;
        SupportedPlatform |= currPlatform == RuntimePlatform.OSXEditor;
        SupportedPlatform |= currPlatform == RuntimePlatform.OSXPlayer;
        SupportedPlatform |= currPlatform == RuntimePlatform.WindowsEditor;
        SupportedPlatform |= currPlatform == RuntimePlatform.WindowsPlayer;
        if (!SupportedPlatform)
        {
            Debug.LogWarning("This platform is unsupported");
            return;
        }

        if (HMD != null)
            return;
        HMD = Hmd.GetHmd();

        SetLowPersistenceMode(true);
	}

	void Update()
	{
		if (HMD != null && Input.anyKeyDown && HMD.GetHSWDisplayState().Displayed)
			HMD.DismissHSWDisplay();
	}
	#endregion

	/// <summary>
	/// Destroy this instance.
	/// </summary>
    void OnDestroy()
    {
        // We may want to turn this off so that values are maintained between level / scene loads
        if (!ResetTrackerOnLoad || HMD == null)
            return;
    }

	// * * * * * * * * * * * *
	// PUBLIC FUNCTIONS
	// * * * * * * * * * * * *
	
	/// <summary>
	/// Determines if is HMD present.
	/// </summary>
	/// <returns><c>true</c> if is HMD present; otherwise, <c>false</c>.</returns>
	public static bool IsHMDPresent()
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrTrackingState ss = HMD.GetTrackingState();
		
		return (ss.StatusFlags & (uint)ovrStatusBits.ovrStatus_HmdConnected) != 0;
	}

	/// <summary>
	/// Determines if is sensor present.
	/// </summary>
	/// <returns><c>true</c> if is sensor present; otherwise, <c>false</c>.</returns>
	public static bool IsSensorPresent()
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrHmdDesc desc = HMD.GetDesc();

		return (desc.HmdCaps & (uint)ovrHmdCaps.ovrHmdCap_Present) != 0;
	}

	/// <summary>
	/// Resets the orientation.
	/// </summary>
	/// <returns><c>true</c>, if orientation was reset, <c>false</c> otherwise.</returns>
	public static bool ResetOrientation()
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		HMD.RecenterPose();

		return true;
	}
	
	// Latest absolute sensor readings (note: in right-hand co-ordinates)
	
	/// <summary>
	/// Gets the acceleration.
	/// </summary>
	/// <returns><c>true</c>, if acceleration was gotten, <c>false</c> otherwise.</returns>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="z">The z coordinate.</param>
	public static bool GetAcceleration(ref float x, ref float y, ref float z)
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrTrackingState ss = HMD.GetTrackingState();
		x = ss.HeadPose.LinearAcceleration.x;
		y = ss.HeadPose.LinearAcceleration.y;
		z = ss.HeadPose.LinearAcceleration.z;
		
		return true;
	}

	/// <summary>
	/// Gets the angular velocity.
	/// </summary>
	/// <returns><c>true</c>, if angular velocity was gotten, <c>false</c> otherwise.</returns>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="z">The z coordinate.</param>
	public static bool GetAngularVelocity(ref float x, ref float y, ref float z)
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrTrackingState ss = HMD.GetTrackingState();
		x = ss.HeadPose.AngularVelocity.x;
		y = ss.HeadPose.AngularVelocity.y;
		z = ss.HeadPose.AngularVelocity.z;

		return true;
	}
					
	/// <summary>
	/// Gets the IPD.
	/// </summary>
	/// <returns><c>true</c>, if IP was gotten, <c>false</c> otherwise.</returns>
	/// <param name="IPD">IP.</param>
	public static bool GetIPD(ref float IPD)
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		IPD = HMD.GetFloat(Hmd.OVR_KEY_IPD, Hmd.OVR_DEFAULT_IPD);
		
		return true;
	}

	/// <summary>
	/// Orients the sensor.
	/// </summary>
	/// <param name="q">Q.</param>
	public static void OrientSensor(ref Quaternion q)
	{
		// Change the co-ordinate system from right-handed to Unity left-handed
		/*
		q.x =  x; 
		q.y =  y;
		q.z =  -z; 
		q = Quaternion.Inverse(q);
		*/
			
		// The following does the exact same conversion as above
		q.x = -q.x; 
		q.y = -q.y;	
	}

	/// <summary>
	/// Gets the height of the player eye.
	/// </summary>
	/// <returns><c>true</c>, if player eye height was gotten, <c>false</c> otherwise.</returns>
	/// <param name="eyeHeight">Eye height.</param>
	public static bool GetPlayerEyeHeight(ref float eyeHeight)
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		eyeHeight = HMD.GetFloat(Hmd.OVR_KEY_EYE_HEIGHT, Hmd.OVR_DEFAULT_PLAYER_HEIGHT);

		return true;
	}
	
	// CAMERA VISION FUNCTIONS

	/// <summary>
	/// Determines if is camera present.
	/// </summary>
	/// <returns><c>true</c> if is camera present; otherwise, <c>false</c>.</returns>
	public static bool IsCameraPresent()
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrTrackingState ss = HMD.GetTrackingState();
		
		return (ss.StatusFlags & (uint)ovrStatusBits.ovrStatus_PositionConnected) != 0;
	}
	
	/// <summary>
	/// Determines if is camera tracking.
	/// </summary>
	/// <returns><c>true</c> if is camera tracking; otherwise, <c>false</c>.</returns>
	public static bool IsCameraTracking()
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrTrackingState ss = HMD.GetTrackingState();
		
		return (ss.StatusFlags & (uint)ovrStatusBits.ovrStatus_PositionTracked) != 0;
	}
	
	/// <summary>
	/// Gets the camera position orientation.
	/// </summary>
	/// <returns><c>true</c>, if camera position orientation was gotten, <c>false</c> otherwise.</returns>
	/// <param name="p">P.</param>
	/// <param name="o">O.</param>
	public static bool 
	GetCameraPositionOrientation(ref Vector3 p, ref Quaternion o, double predictionTime = 0f)
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		float px = 0, py = 0, pz = 0, ow = 0, ox = 0, oy = 0, oz = 0;

		double abs_time_plus_pred = Hmd.GetTimeInSeconds() + predictionTime;

		ovrTrackingState ss = HMD.GetTrackingState(abs_time_plus_pred);
		
		px = ss.HeadPose.ThePose.Position.x;
		py = ss.HeadPose.ThePose.Position.y;
		pz = ss.HeadPose.ThePose.Position.z;
		
		ox = ss.HeadPose.ThePose.Orientation.x;
		oy = ss.HeadPose.ThePose.Orientation.y;
		oz = ss.HeadPose.ThePose.Orientation.z;
		ow = ss.HeadPose.ThePose.Orientation.w;
		
		p.x = px; p.y = py; p.z = -pz;
		o.w = ow; o.x = ox; o.y = oy; o.z = oz;

		// Convert to Left hand CS
		OrientSensor(ref o);
		
		return true;
	}

	/// <summary>
	/// Gets the camera projection matrix.
	/// </summary>
	/// <returns><c>true</c>, if camera projection matrix was gotten, <c>false</c> otherwise.</returns>
	/// <param name="eyeId">Eye Id - Left = 0, Right = 1.</param>
	/// <param name="nearClip">Near Clip Plane of the camera.</param>
	/// <param name="farClip">Far Clip Plane of the camera.</param>
	/// <param name="mat">The generated camera projection matrix.</param>
	public static bool GetCameraProjection(int eyeId, float nearClip, float farClip, ref Matrix4x4 mat)
	{
        if (HMD == null || !SupportedPlatform)
            return false;

		ovrFovPort fov = HMD.GetDesc().DefaultEyeFov[eyeId];
		mat = Hmd.GetProjection(fov, nearClip, farClip, true).ToMatrix4x4();
		
		return true;
	}

	/// <summary>
	/// Sets the vision enabled.
	/// </summary>
	/// <param name="on">If set to <c>true</c> on.</param>
	public static void SetVisionEnabled(bool on)
	{
        if (HMD == null || !SupportedPlatform)
            return;

		uint trackingCaps = (uint)ovrTrackingCaps.ovrTrackingCap_Orientation | (uint)ovrTrackingCaps.ovrTrackingCap_MagYawCorrection;

		if (on)
			trackingCaps |= (uint)ovrTrackingCaps.ovrTrackingCap_Position;
		
		HMD.RecenterPose();
		HMD.ConfigureTracking(trackingCaps, 0);
	}
	
	/// <summary>
	/// Sets the low Persistence mode.
	/// </summary>
	/// <param name="on">If set to <c>true</c> on.</param>
	public static void SetLowPersistenceMode(bool on)
	{
        if (HMD == null || !SupportedPlatform)
            return; 

		uint caps = HMD.GetEnabledCaps();
		
		if(on)
			caps |= (uint)ovrHmdCaps.ovrHmdCap_LowPersistence;
		else
			caps &= ~(uint)ovrHmdCaps.ovrHmdCap_LowPersistence;
		
		HMD.SetEnabledCaps(caps);
	}

	/// <summary>
	/// Gets the FOV and resolution.
	/// </summary>
	public static void GetImageInfo(ref int resH, ref int resV, ref float fovH, ref float fovV)
	{
		// Always set to safe values :)
		resH = 1280;
		resV = 800;
		fovH = fovV = 90.0f;
		
        if (HMD == null || !SupportedPlatform)
            return;

		ovrHmdDesc desc = HMD.GetDesc();
		ovrFovPort fov = desc.DefaultEyeFov[0];
		fov.LeftTan = fov.RightTan = Mathf.Max(fov.LeftTan, fov.RightTan);
		fov.UpTan   = fov.DownTan  = Mathf.Max(fov.UpTan,   fov.DownTan);

		// Configure Stereo settings. Default pixel density is 1.0f.
		float desiredPixelDensity = 1.0f;
		ovrSizei texSize = HMD.GetFovTextureSize(ovrEyeType.ovrEye_Left, fov, desiredPixelDensity);

		resH = texSize.w;
		resV = texSize.h;
		fovH = 2f * Mathf.Rad2Deg * Mathf.Atan( fov.LeftTan );
		fovV = 2f * Mathf.Rad2Deg * Mathf.Atan( fov.UpTan );
	}

    /// <summary>
    /// Get resolution of eye texture
    /// </summary>
    /// <param name="w">Width</param>
    /// <param name="h">Height</param>
    public static void GetResolutionEyeTexture(ref int w, ref int h)
    {
        if (HMD == null || !SupportedPlatform)
		    return;
        
        ovrHmdDesc desc = HMD.GetDesc();
        ovrFovPort[] eyeFov = new ovrFovPort[2];

	    eyeFov[0] = desc.DefaultEyeFov[0];
	    eyeFov[1] = desc.DefaultEyeFov[1];

        ovrSizei recommenedTex0Size = HMD.GetFovTextureSize(ovrEyeType.ovrEye_Left, desc.DefaultEyeFov[0], 1.0f);
        ovrSizei recommenedTex1Size = HMD.GetFovTextureSize(ovrEyeType.ovrEye_Left, desc.DefaultEyeFov[1], 1.0f);

	    w = recommenedTex0Size.w + recommenedTex1Size.w;
	    h = (recommenedTex0Size.h + recommenedTex1Size.h)/2;
    }

    /// <summary>
    /// Get latency values
    /// </summary>
    /// <param name="Ren">Ren</param>
    /// <param name="TWrp">TWrp</param>
    /// <param name="PostPresent">PostPresent</param>
    public static void GetLatencyValues(ref float Ren, ref float TWrp, ref float PostPresent)
    {
        if (HMD == null || !SupportedPlatform)
            return;

        float[] values = { 0.0f, 0.0f, 0.0f };
        float[] latencies = HMD.GetFloatArray("DK2Latency", values);
        Ren = latencies[0];
        TWrp = latencies[1];
        PostPresent = latencies[2];
    }

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
	[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
	public static extern IntPtr GetActiveWindow();
#endif

	public const string LibFile = "OculusPlugin";
	[DllImport(LibFile)]
	public static extern void OVR_SetHMD(ref IntPtr hmdPtr);
	[DllImport(LibFile)]
	private static extern void OVR_GetHMD(ref IntPtr hmdPtr);
}
