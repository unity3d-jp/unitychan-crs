/************************************************************************************

Filename    :   OVRCamera.cs
Content     :   Interface to camera class
Created     :   January 8, 2013
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
using OVR;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;

[RequireComponent(typeof(Camera))]

/// <summary>
/// OVRCamera is used to render into a Unity Camera class. 
/// This component handles reading the Rift tracker and positioning the camera position
/// and rotation. It also is responsible for properly rendering the final output, which
/// also the final lens correction pass.
/// </summary>
public class OVRCamera : MonoBehaviour
{
	/// <summary>
	/// Types of render events we can send to the native rendering plugin.
	/// </summary>
	private enum EventType
    {
		BeginFrame = 0,
        EndFrame = 1,
    };

	#region Plugin Imports
	private const string strOvrLib = "OculusPlugin";
	
	[DllImport(strOvrLib)]
    static extern bool OVR_SetTexture(int id, System.IntPtr texture, float scale = 1);
    [DllImport(strOvrLib)]
    static extern bool OVR_UnityGetModeChange();
    [DllImport(strOvrLib)]
    static extern bool OVR_UnitySetModeChange(bool isChanged);
	[DllImport(strOvrLib)]
	static extern void OVR_SetEndFrameInPresent(bool isEnabled);
	[DllImport(strOvrLib)]
	static extern ovrPosef OVR_GetRenderPose();
    [DllImport(strOvrLib)]
    static extern void OVR_SetMacEditorPlay(bool isEditorPlay);
    [DllImport(strOvrLib)]
    static extern void OVR_SetDX11EditorPlay(bool isEditorPlay);
	#endregion

	#region Private Member Variables
	// We will search for camera controller and set it here for access to its members
	private OVRCameraController CameraController = null;
	// 0 for left, 1 for right.
	internal int EyeId;
    // Tracks if we need to reset state due to mode switches or a lost graphics device
	internal bool NeedsSetTexture;
    // We want to check if the screen mode is changed
    internal bool wasFullScreen;
	#endregion

	#region Public Member Variables
	/// <summary>
	// camera position, from root of camera to neck (translation only)
	/// </summary>
	[HideInInspector]
	public Vector3 NeckPosition = Vector3.zero;
	/// <summary>
	// From neck to eye (rotation and translation; x will be different for each eye)
	/// </summary>
	[HideInInspector]
	public Vector3 EyePosition  = new Vector3(0.0f, Hmd.OVR_DEFAULT_NECK_TO_EYE_VERTICAL, Hmd.OVR_DEFAULT_NECK_TO_EYE_HORIZONTAL);

	/// <summary>
	/// True if this camera corresponds to the right eye.
	/// </summary>
	public bool RightEye = false;
	#endregion

	#region Static Members
	/// <summary>
	// We will grab the actual orientation that is used by the cameras in a shared location.
	// This will allow multiple OVRCameraControllers to eventually be used in a scene, and 
	// only one orientation will be used to syncronize all camera orientation
	/// </summary>
	static private Quaternion CameraOrientation = Quaternion.identity;
	/// <summary>
	//  This is absolute camera location from vision
	/// </summary>
	static private Vector3 CameraPosition = Vector3.zero;
	/// <summary>
	// Set an offset to the camera that will adjust our location from the 
	// cameras center of origin (set when reset is called)
	/// </summary>
	static private Vector3 CameraPositionOffset = Vector3.zero;	
	/// <summary>
	// Set an offset to the camera that will adjust our rotation from the 
	// camera's default orientation (set when reset is called)
	/// </summary>
	static private Quaternion CameraOrientationOffset = Quaternion.identity;
	/// <summary>
	// List of game objects to update with local camera center of origin
	// {allows for objects that are at 0,0,0 relative to the camera to stay 
	// rendered in camera space, to allow for visual aid).
	/// </summary>
	static private List<OVRCameraGameObject> CameraLocalSetList = new List<OVRCameraGameObject>();

	/// <summary>
	/// An optional texture to which the undistorted image will be rendered.
	/// </summary>
    static public RenderTexture[] CameraTexture = new RenderTexture[2];
	#endregion
	
	#region Monobehaviour Member Functions
	void Awake()
	{
		// Force right/left state for specifically-named objects.
		if (gameObject.name.Contains("Right"))
			RightEye = true;
		if (gameObject.name.Contains("Left"))
			RightEye = false;
		
		EyeId = (RightEye) ? 1 : 0;

		// Make sure we render to the whole target.
		camera.rect = new Rect(0, 0, 1, 1);
#if UNITY_EDITOR_OSX
		OVR_SetEndFrameInPresent(false);
		OVR_SetMacEditorPlay(true);
#elif UNITY_STANDALONE_OSX
		OVR_SetMacEditorPlay(false);
#elif UNITY_EDITOR_WIN
        OVR_SetDX11EditorPlay(true);
#endif
    }

	void Start()
	{
        NeedsSetTexture = true;
        wasFullScreen = Screen.fullScreen;
		StartCoroutine(CallbackCoroutine());

		// Get the OVRCameraController
		CameraController = gameObject.transform.parent.GetComponent<OVRCameraController>();
		
		if(CameraController == null)
		{
			Debug.LogWarning("OVRCameraController not found!");
			this.enabled = false;
			return;
		}

		if (!CameraController.UseCameraTexture)
			return;

#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
		// if AA & AF are disabled, step down RT scale for a FPS boost
		if (QualitySettings.anisotropicFiltering == 0 && QualitySettings.antiAliasing == 0)
			CameraController.CameraTextureScale = Mathf.Min(CameraController.CameraTextureScale, 0.7f);
#endif

		// This will scale the render texture based on ideal resolution
		CreateRenderTexture(EyeId, CameraController.CameraTextureScale);

		camera.targetTexture = CameraTexture[EyeId];
        OldScale = CameraController.ScaleRenderTarget;

        
	}

    static int PendingEyeCount = 0;

	void OnPreCull()
	{        
        if (CameraTexture[EyeId] == null)
			return;

		if (PendingEyeCount == 0)
		{
			GL.IssuePluginEvent((int)EventType.BeginFrame);
		}

		SetCameraOrientation();

		if (PendingEyeCount < 2)
        	PendingEyeCount++;

	}

	float OldScale = -1f;
	
	void OnCoroutine()
	{
		if (PendingEyeCount > 0)
        	PendingEyeCount--;

        NeedsSetTexture = NeedsSetTexture || ((CameraController.ScaleRenderTarget != OldScale) || (Screen.fullScreen != wasFullScreen) || OVR_UnityGetModeChange());

        
        if (NeedsSetTexture)
        {
            if (CameraTexture[EyeId].GetNativeTexturePtr() == System.IntPtr.Zero)
                return;             

            OVR_SetTexture(EyeId, CameraTexture[EyeId].GetNativeTexturePtr(), CameraController.ScaleRenderTarget);

			OldScale = CameraController.ScaleRenderTarget;
			wasFullScreen = Screen.fullScreen;

			if (PendingEyeCount == 0)
				OVR_UnitySetModeChange(false);

            NeedsSetTexture = false;
         
        }

        if (PendingEyeCount == 0)
			GL.IssuePluginEvent((int)EventType.EndFrame);
	}

	IEnumerator CallbackCoroutine()
	{
        OVRDevice.HMD = Hmd.GetHmd();
        while (true)
        {
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR_OSX && UNITY_STANDALONE_OSX)
            yield return new WaitForEndOfFrame();
#else
           yield return null;
#endif
            OnCoroutine();
          
        }
	}
	#endregion
	
	#region Internal Functions
	/// <summary>
	/// Sets the camera orientation.
	/// </summary>
	void SetCameraOrientation()
	{	
		// Main camera has a depth of 0, so it will be rendered first
		if(camera == CameraController.CameraMain)
		{
			if(CameraController.TrackerRotatesY == true)
			{
				Vector3 a = camera.transform.rotation.eulerAngles;
				a.x = 0; 
				a.z = 0;
				transform.parent.transform.eulerAngles = a;
			}

			ovrPosef renderPose = OVR_GetRenderPose();

			if (CameraController.EnablePosition)
				CameraPosition = renderPose.Position.ToVector3();

			bool useOrt = (CameraController.EnableOrientation &&
			               !(CameraController.TimeWarp && CameraController.FreezeTimeWarp));	
			if(useOrt)
				CameraOrientation = renderPose.Orientation.ToQuaternion();
		}
		
		// Calculate the rotation Y offset that is getting updated externally
		// (i.e. like a controller rotation)
		float yRotation = 0.0f;
		CameraController.GetYRotation(ref yRotation);
		Quaternion qp = Quaternion.Euler(0.0f, yRotation, 0.0f);
		Vector3 dir = qp * Vector3.forward;
		qp.SetLookRotation(dir, Vector3.up);
	
		// Multiply the camera controllers offset orientation (allow follow of orientation offset)
		Quaternion orientationOffset = Quaternion.identity;
		CameraController.GetOrientationOffset(ref orientationOffset);
		qp = orientationOffset * qp * CameraOrientationOffset;
		
		// Multiply in the current HeadQuat (q is now the latest best rotation)
		Quaternion q = qp * CameraOrientation;
		
		// * * *
		// Update camera rotation
		camera.transform.rotation = q;
		
		// * * *
		// Update camera position (first add Offset to parent transform)
		camera.transform.localPosition = NeckPosition;
	
		// Adjust neck by taking eye position and transforming through q
		// Get final camera position as well as the clipping difference 
		// (to allow for absolute location of center of camera grid space)
		Vector3 newCamPos = Vector3.zero;
		CameraPositionOffsetAndClip(ref CameraPosition, ref newCamPos);

		// Update list of game objects with new CameraOrientation / newCamPos here
		// For example, this location is used to update the GridCube
		foreach(OVRCameraGameObject obj in CameraLocalSetList)
		{
			if(obj.CameraController.GetCameraDepth() == camera.depth)
			{
				// Initial difference
				Vector3 newPos = -(qp * CameraPositionOffset);
				// Final position
				newPos += camera.transform.position;
			
				// Set the game object info
				obj.CameraGameObject.transform.position = newPos;
				obj.CameraGameObject.transform.rotation = qp;
			}
		}

		// Adjust camera position with offset/clipped cam location
		camera.transform.localPosition += Quaternion.Inverse(camera.transform.parent.rotation) * qp * newCamPos;

		Vector3 newEyePos = Vector3.zero;
		newEyePos.x = EyePosition.x;
		camera.transform.localPosition += camera.transform.localRotation * newEyePos;
	}

	/// <summary>
	/// Based on offset and clip values, adjust camera position
	/// </summary>
	/// <param name="inCam">In cam.</param>
	/// <param name="outCam">Out cam.</param>
	void CameraPositionOffsetAndClip(ref Vector3 inCam, ref Vector3 outCam)
	{
		outCam = inCam - CameraPositionOffset;
	}
	#endregion

	#region Public Functions
	/// <summary>
	/// Call this in CameraController to set up the ideal FOV as
	/// defined by the SDK
	/// </summary>
	/// <returns>The ideal FOV.</returns>
	public float GetIdealVFOV()
	{
		int resH = 0; int resV = 0; float fovH = 0; float fovV = 0;
		OVRDevice.GetImageInfo(ref resH, ref resV, ref fovH, ref fovV);

		return fovV;
	}

	/// <summary>
	/// Calculates the aspect ratio.
	/// </summary>
	/// <returns>The aspect ratio.</returns>
	public float CalculateAspectRatio()
	{
		int resH = 0; int resV = 0; float fovH = 0; float fovV = 0;
		OVRDevice.GetImageInfo(ref resH, ref resV, ref fovH, ref fovV);

        return (float)resH / (float)resV;
	}

	/// <summary>
	/// Gets the ideal resolution.
	/// </summary>
	/// <returns>The ideal resolution.</returns>
	public void GetIdealResolution(ref int w, ref int h)
	{
		float fovH = 0; float fovV = 0;
		OVRDevice.GetImageInfo(ref w, ref h, ref fovH, ref fovV);
	}

	/// <summary>
	/// Creates the render texture.
	/// </summary>
	/// <param name="scale">Scale.</param>
	public void CreateRenderTexture(int i, float scale)
	{
		// Set CameraTextureScale (increases the size of the texture we are rendering into
		// for a better pixel match when post processing the image through lens distortion)
		// If CameraTextureScale is not 1.0f, create a new texture and assign to target texture
		// Otherwise, fall back to normal camera rendering

		if (CameraTexture[i] != null)
			return;

		int w = 0;
		int h = 0;

		GetIdealResolution(ref w, ref h);

		w = (int)((float)w * scale);
		h = (int)((float)h * scale);

		if (CameraTexture[i] != null)
			DestroyImmediate(CameraTexture[i]);
			
		CameraTexture[i] = new RenderTexture(w, h, 24, (camera.hdr) ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.Default);
		CameraTexture[i].antiAliasing = (QualitySettings.antiAliasing == 0) ? 1 : QualitySettings.antiAliasing;

		CameraTexture[i].Create();
	}
	#endregion

	#region Vision Functions
	/// <summary>
	/// Mainly to be used to reset camera position orientation
	/// camOffset will move the center eye position to an optimal location
	/// clampX/Y/Z will zero out the offset that is used (restricts offset in a given axis)
	/// </summary>
	/// <param name="posScale">Scale for positional change.</param>
	/// <param name="posOffset">Positional offset.</param>
	/// <param name="posOffset">Positional offset.</param>
	/// <param name="posOffset">Positional offset.</param>
	static public void ResetCameraPositionOrientation(Vector3 posScale, Vector3 posOffset, Vector3 ortScale, Vector3 ortOffset)
	{
		Vector3 camPos  = Vector3.zero;
		Quaternion camO = Quaternion.identity;
		OVRDevice.GetCameraPositionOrientation(ref camPos, ref camO, OVRDevice.PredictionTime);

		CameraPositionOffset = Vector3.Scale(camPos, posScale) - posOffset;

		Vector3 euler = Quaternion.Inverse(camO).eulerAngles;
		CameraOrientationOffset = Quaternion.Euler(Vector3.Scale(euler, ortScale) - ortOffset);
	}
	
	/// <summary>
	/// Set offset directly (for initial positioning that reflects the players
	/// eye location
	/// </summary>
	/// <param name="offset">Offset.</param>
	static public void SetCameraPositionOffset(Vector3 offset)
	{
		CameraPositionOffset = -offset;
	}

	/// <summary>
	/// Adds to local camera set list.
	/// </summary>
	/// <param name="obj">Object.</param>
	static public void AddToLocalCameraSetList(ref OVRCameraGameObject obj)
	{
		CameraLocalSetList.Add(obj);
	}
	
	/// <summary>
	/// Removes from local camera set list.
	/// </summary>
	/// <param name="obj">Object.</param>
	static public void RemoveFromLocalCameraSetList(ref OVRCameraGameObject obj)
	{
		CameraLocalSetList.Remove(obj);
	}
	
	/// <summary>
	/// Gets the camera orientation.
	/// </summary>
	/// <param name="orientation">Orientation.</param>
	static public void GetCameraOrientation(ref Quaternion orientation)
	{
		orientation = CameraOrientation;
	}
	
	/// <summary>
	/// Gets the absolute camera from vision position.
	/// </summary>
	/// <param name="pos">Position.</param>
	static public void GetAbsoluteCameraFromVisionPosition(ref Vector3 pos)
	{
		pos = CameraPosition;
	}
	
	/// <summary>
	/// Gets the relative camera from vision position.
	/// Takes into account position offset.
	/// </summary>
	/// <param name="pos">Position.</param>
	static public void GetRelativeCameraFromVisionPosition(ref Vector3 pos)
	{
		pos = CameraPosition - CameraPositionOffset;
	}
	#endregion
}


/// <summary>
/// OVR camera game object.
/// Used to extend a GameObject for updates within an OVRCamera
/// </summary>
public class OVRCameraGameObject
{
	public GameObject          CameraGameObject = null;
	public OVRCameraController CameraController = null;
}

