/************************************************************************************

Filename    :   OVRCameraController.cs
Content     :   Camera controller interface. 
				This script is used to interface the OVR cameras.
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

//#define OVR_USE_PROJ_MATRIX

using UnityEngine;
using OVR;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// OVR camera controller.
/// OVRCameraController is a component that allows for easy handling of the lower level cameras.
/// It is the main interface between Unity and the cameras. 
/// This is attached to a prefab that makes it easy to add a Rift into a scene.
///
/// All camera control should be done through this component.
///
/// </summary>
public class OVRCameraController : MonoBehaviour
{
	#region Plugin Imports
	public const string strOvrLib = "OculusPlugin";
	[DllImport(strOvrLib)]
	static extern void OVR_EnableTimeWarp(bool isEnabled);
	[DllImport(strOvrLib)]
	static extern void OVR_ForceSymmetricProj(bool isEnabled);
	[DllImport(strOvrLib)]
	static extern bool OVR_SetViewport(int x, int y, int w, int h);
	#endregion

	#region Private Members
	private bool   UpdateCamerasDirtyFlag = false;
	private bool   UpdateDistortionDirtyFlag = false;
	private Camera CameraLeft, CameraRight = null;
	private float  AspectRatio = 1.0f;						
	// Initial orientation of the camera, can be used to always set the 
	// zero orientation of the cameras to follow a set forward facing orientation.
	private Quaternion OrientationOffset = Quaternion.identity;	
	// Set Y rotation here; this will offset the y rotation of the cameras. 
	private float   YRotation = 0.0f;
	#endregion

	#region Public Members
	public Camera CameraMain { get; private set; }
	#endregion

	#region Stereo Properties
	/// <summary>
	/// The distance between the Unity cameras, also known as the "Inter-Camera Distance" or ICD.
	/// Affects stereo level, not distortion.
	/// </summary>
	public 	float 		IPD
	{
		get{return ipd;}
		set
		{
			if (ipd == value)
				return;

			ipd = value;
			UpdateDistortionDirtyFlag = true;
		}
	}
	[SerializeField]
	private float  		ipd 		= Hmd.OVR_DEFAULT_IPD; 				// in millimeters

	/// <summary>
	/// Gets or sets the vertical FOV.
	/// </summary>
	public 	float		VerticalFOV
	{
		get{return verticalFOV;}
		set
		{
			float newVerticalFOV = Mathf.Clamp(value, 40.0f, 170.0f);

			if (newVerticalFOV == verticalFOV)
				return;

			verticalFOV = newVerticalFOV;
			UpdateDistortionDirtyFlag = true;
		}
	}
	[SerializeField]
	private float  		verticalFOV = 90.0f;	 			// in degrees

	/// <summary>
	// If true, renders to a RenderTexture to allow super-sampling.
	/// </summary>
	internal bool UseCameraTexture = true;

	/// <summary>
	// A constant multiple of the ideal resolution, which enables supersampling for higher image quality.
	/// </summary>
	public float CameraTextureScale = 1.0f;

	/// <summary>
	/// Gets or sets the render target scale.
	/// </summary>
	public	float 		ScaleRenderTarget
	{
		get{return scaleRenderTarget;}
		set
		{
			scaleRenderTarget = value;
			if(scaleRenderTarget > 1.0f)
				scaleRenderTarget = 1.0f;
			else if (scaleRenderTarget < 0.01f)
				scaleRenderTarget = 0.01f;

			// We will call this initially to grab the serialized value
			SetScaleRenderTarget();
		}
	}
	[SerializeField]
	private float		scaleRenderTarget = 1.0f;

	/// <summary>
	// Camera positioning:
	// CameraRootPosition will be used to calculate NeckPosition and Eye Height
	/// </summary>
	public Vector3 		CameraRootPosition = new Vector3(0.0f, 1.0f, 0.0f);					
	/// <summary>
	// From CameraRootPosition to neck
	/// </summary>
	public Vector3 		NeckPosition      = new Vector3(0.0f, 0.7f,  0.0f);	
	/// <summary>
	// Use player eye height as set in the Rift config tool
	/// </summary>
	public  bool 		UsePlayerEyeHeight     = false;
	private bool 		PrevUsePlayerEyeHeight = false;
	/// <summary>
	// Set this transform with an object that the camera orientation should follow.
	// NOTE: Best not to set this with the OVRCameraController IF TrackerRotatesY is
	// on, since this will lead to uncertain output
	/// </summary>
	public Transform 	FollowOrientation = null;
	/// <summary>
	// Set to true if we want the rotation of the camera controller to be influenced by tracker
	/// </summary>
	public bool  		TrackerRotatesY	= false;
	/// <summary>
	// Use this to enable / disable Tracker orientation
	/// </summary>
	public bool         EnableOrientation = true;
	/// <summary>
	// Use this to enable / disable Tracker position
	/// </summary>
	public bool         EnablePosition = true;
	/// <summary>
	// Use this to turn on/off Prediction
	/// </summary>
	public bool			PredictionOn 	= true;

	/// <summary>
	// Automatically adjusts output to compensate for rendering latency.
	/// </summary>
	public bool 		TimeWarp
	{
		get { return timeWarp; }
		set
		{
			if (value == timeWarp)
				return;

			timeWarp = value;
			UpdateDistortionDirtyFlag = true;
		}
	}
	[SerializeField]
	private bool		timeWarp = false;

	public bool			Mirror
	{
		get { return mirror; }
		set
		{
			if (value == mirror)
				return;

			mirror = value;
			UpdateDistortionDirtyFlag = true;
		}
	}
	[SerializeField]
	private bool		mirror = false;

	/// <summary>
	// If true, then TimeWarp freezes the start view.
	/// </summary>
	public bool 		FreezeTimeWarp		= false;
	#endregion

	#region Camera Properties
	/// <summary>
	// Gets or sets the background color for both cameras
	/// </summary>
	/// <value>The color of the background.</value>
	public  Color       BackgroundColor
	{
		get{return backgroundColor;}
		set{backgroundColor = value; UpdateCamerasDirtyFlag = true;}
	}
	[SerializeField]
	private Color 		backgroundColor = new Color(0.192f, 0.302f, 0.475f, 1.0f);

	/// <summary>
	// Gets or sets the near clip plane for both cameras
	/// </summary>
	public  float 		NearClipPlane
	{
		get{return nearClipPlane;}
		set{nearClipPlane = value; UpdateCamerasDirtyFlag = true;}
	}
	[SerializeField]
	private float 		nearClipPlane   = 0.15f;

	/// <summary>
	// Gets or sets the far clip plane for both cameras
	/// </summary>
	/// <value>The far clip plane.</value>
	public  float 		FarClipPlane
	{
		get{return farClipPlane;}
		set{farClipPlane = value; UpdateCamerasDirtyFlag = true;}
	}
	[SerializeField]
	private float 		farClipPlane    = 1000.0f;  
	#endregion

	#region MonoBehaviour Message Handlers
	void Awake()
	{
		// Get the cameras
		OVRCamera[] cameras = gameObject.GetComponentsInChildren<OVRCamera>();
		
		for (int i = 0; i < cameras.Length; i++)
		{
			if(cameras[i].RightEye)
				SetCameras(CameraLeft, cameras[i].camera);
			else
				SetCameras(cameras[i].camera, CameraRight);
		}
		
		if(CameraLeft == null)
			Debug.LogWarning("No left camera found for OVRCameraController!");
		if(CameraRight == null)
			Debug.LogWarning("No right camera found for OVRCameraController!");
	}

	void Start()
	{
		if (camera == null)
		{
			gameObject.AddComponent<Camera>();
		}

        // In the event of an unsupported platform, remove our cameras and replace with a dummy
        if (!OVRDevice.SupportedPlatform)
        {
            OVRCamera[] ovrCameras = gameObject.GetComponentsInChildren<OVRCamera>();
            for (int i = 0; i < ovrCameras.Length; i++)
            {
                ovrCameras[i].enabled = false;
            }
        }
		else
		{
			//HACK: Use the camera to force rendering of the left and right eyes.
			camera.cullingMask = 0;
			camera.clearFlags = CameraClearFlags.Nothing;
			camera.renderingPath = RenderingPath.Forward;
			camera.orthographic = true;
		}

		// Get the required Rift infromation needed to set cameras
		InitCameraControllerVariables();
		
		// Initialize the cameras
		UpdateCamerasDirtyFlag = true;
		UpdateDistortionDirtyFlag = true;
		SetScaleRenderTarget();
		
		if (Application.isEditor)
			OVRDevice.SetLowPersistenceMode(false);
	}
	#endregion
		
	#region Internal Functions
	/// <summary>
	/// Sets the scale render target.
	/// </summary>
	void SetScaleRenderTarget()
	{
		if((CameraLeft != null && CameraRight != null))
		{
			float scale = (UseCameraTexture) ? ScaleRenderTarget : 1f;

			// Aquire and scale the cameras
			OVRCamera[] cameras = gameObject.GetComponentsInChildren<OVRCamera>();
			for (int i = 0; i < cameras.Length; i++)
			{
				float w = (UseCameraTexture) ? scale : 0.5f * scale;
				float x = (UseCameraTexture) ? 0f : (cameras[i].RightEye) ? 0.5f : 0f;
				cameras[i].camera.rect = new Rect(x, 0f, w, scale);
			}

			//TODO: Set scale on OVR eye textures.
		}
	}
	
	/// <summary>
	/// Updates the cameras.
	/// </summary>
	void Update()
	{
		// Values that influence the stereo camera orientation up and above the tracker
		if(FollowOrientation != null)
			OrientationOffset = FollowOrientation.rotation;

		// Handle positioning of eye height and other things here
		UpdatePlayerEyeHeight();

		if (UpdateCamerasDirtyFlag)
		{
			// Configure left and right cameras
			float eyePositionOffset = IPD * 0.5f;
			ConfigureCamera(CameraRight, eyePositionOffset);
			ConfigureCamera(CameraLeft, -eyePositionOffset);
			UpdateCamerasDirtyFlag = false;
		}
		
		if (UpdateDistortionDirtyFlag)
		{
			OVR_EnableTimeWarp(timeWarp);

			uint caps = OVRDevice.HMD.GetEnabledCaps();
			
			if(mirror)
				caps &= ~(uint)ovrHmdCaps.ovrHmdCap_NoMirrorToWindow;
			else
				caps |= (uint)ovrHmdCaps.ovrHmdCap_NoMirrorToWindow;

			OVRDevice.HMD.SetEnabledCaps(caps);

			UpdateDistortionDirtyFlag = false;
		}
		
		OVR_SetViewport(0, 0, Screen.width, Screen.height);
    }
	    
    /// <summary>
	/// Configures the camera.
	/// </summary>
	/// <returns><c>true</c>, if camera was configured, <c>false</c> otherwise.</returns>
	/// <param name="camera">Camera.</param>
	/// <param name="eyePositionOffset">Eye position offset.</param>
	void ConfigureCamera(Camera camera, float eyePositionOffset)
	{
		OVRCamera cam = camera.GetComponent<OVRCamera>();

		// Always set  camera fov and aspect ratio
		camera.fieldOfView = VerticalFOV;
		camera.aspect      = AspectRatio;

		// Background color
		camera.backgroundColor = BackgroundColor;
		
		// Clip Planes
		camera.nearClipPlane = NearClipPlane;
		camera.farClipPlane = FarClipPlane;

#if OVR_USE_PROJ_MATRIX
		// Projection Matrix
		Matrix4x4 camMat = Matrix4x4.identity;
		OVRDevice.GetCameraProjection(cam.EyeId, NearClipPlane, FarClipPlane, ref camMat);
		camera.projectionMatrix = camMat;
		OVR_ForceSymmetricProj(false);
#else
		OVR_ForceSymmetricProj(true);
#endif
		
		// Set camera variables that pertain to the neck and eye position
		// NOTE: We will want to add a scale vlue here in the event that the player 
		// grows or shrinks in the world. This keeps head modelling behaviour
		// accurate
		cam.NeckPosition = NeckPosition;
		cam.EyePosition = new Vector3(eyePositionOffset, 0f, 0f);
	}
	
	/// <summary>
	/// Updates the height of the player eye.
	/// </summary>
	void UpdatePlayerEyeHeight()
	{
		if((UsePlayerEyeHeight == true) && (PrevUsePlayerEyeHeight == false))
		{
			// Calculate neck position to use based on Player configuration
			float  peh = 0.0f;
			
			if(OVRDevice.GetPlayerEyeHeight(ref peh) != false)
			{
				NeckPosition.y = peh - CameraRootPosition.y;
			}
		}
		
		PrevUsePlayerEyeHeight = UsePlayerEyeHeight;
	}
	#endregion

	#region Public Functions
	/// <summary>
	/// Inits the camera controller variables.
	/// Made public so that it can be called by classes that require information about the
	/// camera to be present when initing variables in 'Start'
	/// </summary>
	public void InitCameraControllerVariables()
	{
		// Get the IPD value (distance between eyes in meters)
		OVRDevice.GetIPD(ref ipd);
		
		// Using the calculated FOV, based on distortion parameters, yeilds the best results.
		// However, public functions will allow to override the FOV if desired
		VerticalFOV = CameraMain.GetComponent<OVRCamera>().GetIdealVFOV();
		// Get aspect ratio as well
		AspectRatio = CameraMain.GetComponent<OVRCamera>().CalculateAspectRatio();
		
		// Get our initial world orientation of the cameras from the scene (we can grab it from 
		// the set FollowOrientation object or this OVRCameraController gameObject)
		if(FollowOrientation != null)
			OrientationOffset = FollowOrientation.rotation;
		else
			OrientationOffset = transform.rotation;
	}

	/// <summary>
	/// Sets the cameras - Should we want to re-target the cameras
	/// </summary>
	/// <param name="cameraLeft">Camera left.</param>
	/// <param name="cameraRight">Camera right.</param>
	public void SetCameras(Camera cameraLeft, Camera cameraRight)
	{
		CameraLeft  = cameraLeft;
		CameraRight = cameraRight;

		CameraMain = CameraLeft ?? CameraRight;

		if (CameraLeft != null && CameraRight != null)
			CameraMain = (CameraLeft.depth < CameraRight.depth) ? CameraLeft : CameraRight;

		UpdateCamerasDirtyFlag = true;
	}
	
	/// <summary>
	/// Gets the IPD.
	/// </summary>
	/// <param name="ipd">Ipd.</param>
	public void GetIPD(ref float ipd)
	{
		ipd = IPD;
	}
	/// <summary>
	/// Sets the IPD.
	/// </summary>
	/// <param name="ipd">Ipd.</param>
	public void SetIPD(float ipd)
	{
		IPD = ipd;
		UpdateCamerasDirtyFlag = true;
	}
			
	/// <summary>
	/// Gets the vertical FOV.
	/// </summary>
	/// <param name="verticalFOV">Vertical FO.</param>
	public void GetVerticalFOV(ref float verticalFOV)
	{
		verticalFOV = VerticalFOV;
	}
	/// <summary>
	/// Sets the vertical FOV.
	/// </summary>
	/// <param name="verticalFOV">Vertical FO.</param>
	public void SetVerticalFOV(float verticalFOV)
	{
		VerticalFOV = verticalFOV;
		UpdateCamerasDirtyFlag = true;
	}
	
	/// <summary>
	/// Gets the aspect ratio.
	/// </summary>
	/// <param name="aspectRatio">Aspect ratio.</param>
	public void GetAspectRatio(ref float aspectRatio)
	{
		aspectRatio = AspectRatio;
	}
	/// <summary>
	/// Sets the aspect ratio.
	/// </summary>
	/// <param name="aspectRatio">Aspect ratio.</param>
	public void SetAspectRatio(float aspectRatio)
	{
		AspectRatio = aspectRatio;
		UpdateCamerasDirtyFlag = true;
	}
		
	/// <summary>
	/// Gets the camera root position.
	/// </summary>
	/// <param name="cameraRootPosition">Camera root position.</param>
	public void GetCameraRootPosition(ref Vector3 cameraRootPosition)
	{
		cameraRootPosition = CameraRootPosition;
	}
	/// <summary>
	/// Sets the camera root position.
	/// </summary>
	/// <param name="cameraRootPosition">Camera root position.</param>
	public void SetCameraRootPosition(ref Vector3 cameraRootPosition)
	{
		CameraRootPosition = cameraRootPosition;
		UpdateCamerasDirtyFlag = true;
	}

	/// <summary>
	/// Gets the neck position.
	/// </summary>
	/// <param name="neckPosition">Neck position.</param>
	public void GetNeckPosition(ref Vector3 neckPosition)
	{
		neckPosition = NeckPosition;
	}
	/// <summary>
	/// Sets the neck position.
	/// </summary>
	/// <param name="neckPosition">Neck position.</param>
	public void SetNeckPosition(Vector3 neckPosition)
	{
		// This is locked to the NeckPosition that is set by the
		// Player profile.
		if(UsePlayerEyeHeight != true)
		{
			NeckPosition = neckPosition;
			UpdateCamerasDirtyFlag = true;
		}
	}

	/// <summary>
	/// Gets the orientation offset.
	/// </summary>
	/// <param name="orientationOffset">Orientation offset.</param>
	public void GetOrientationOffset(ref Quaternion orientationOffset)
	{
		orientationOffset = OrientationOffset;
	}
	/// <summary>
	/// Sets the orientation offset.
	/// </summary>
	/// <param name="orientationOffset">Orientation offset.</param>
	public void SetOrientationOffset(Quaternion orientationOffset)
	{
		OrientationOffset = orientationOffset;
	}
	
	/// <summary>
	/// Gets the Y rotation.
	/// </summary>
	/// <param name="yRotation">Y rotation.</param>
	public void GetYRotation(ref float yRotation)
	{
		yRotation = YRotation;
	}
	/// <summary>
	/// Sets the Y rotation.
	/// </summary>
	/// <param name="yRotation">Y rotation.</param>
	public void SetYRotation(float yRotation)
	{
		YRotation = yRotation;
	}
	
	/// <summary>
	/// Gets the tracker rotates y flag.
	/// </summary>
	/// <param name="trackerRotatesY">Tracker rotates y.</param>
	public void GetTrackerRotatesY(ref bool trackerRotatesY)
	{
		trackerRotatesY = TrackerRotatesY;
	}
	/// <summary>
	/// Sets the tracker rotates y flag.
	/// </summary>
	/// <param name="trackerRotatesY">If set to <c>true</c> tracker rotates y.</param>
	public void SetTrackerRotatesY(bool trackerRotatesY)
	{
		TrackerRotatesY = trackerRotatesY;
	}
	
	// GetCameraOrientationEulerAngles
	/// <summary>
	/// Gets the camera orientation euler angles.
	/// </summary>
	/// <returns><c>true</c>, if camera orientation euler angles was gotten, <c>false</c> otherwise.</returns>
	/// <param name="angles">Angles.</param>
	public bool GetCameraOrientationEulerAngles(ref Vector3 angles)
	{
		if(CameraMain == null)
			return false;
		
		angles = CameraMain.transform.rotation.eulerAngles;
		return true;
	}
	
	/// <summary>
	/// Gets the camera orientation.
	/// </summary>
	/// <returns><c>true</c>, if camera orientation was gotten, <c>false</c> otherwise.</returns>
	/// <param name="quaternion">Quaternion.</param>
	public bool GetCameraOrientation(ref Quaternion quaternion)
	{
		if(CameraMain == null)
			return false;
		
		quaternion = CameraMain.transform.rotation;
		return true;
	}
	
	/// <summary>
	/// Gets the camera position.
	/// </summary>
	/// <returns><c>true</c>, if camera position was gotten, <c>false</c> otherwise.</returns>
	/// <param name="position">Position.</param>
	public bool GetCameraPosition(ref Vector3 position)
	{
		if(CameraMain == null)
			return false;
		
		position = CameraMain.transform.position;
	
		return true;
	}
	
	/// <summary>
	/// Gets the camera.
	/// </summary>
	/// <param name="camera">Camera.</param>
	public void GetCamera(ref Camera camera)
	{
		camera = CameraMain;
	}
	
	/// <summary>
	/// Attachs a game object to the right (main) camera.
	/// </summary>
	/// <returns><c>true</c>, if game object to camera was attached, <c>false</c> otherwise.</returns>
	/// <param name="gameObject">Game object.</param>
	public bool AttachGameObjectToCamera(ref GameObject gameObject)
	{
		if(CameraMain == null)
			return false;

		gameObject.transform.parent = CameraMain.transform;
	
		return true;
	}

	/// <summary>
	/// Detachs the game object from the right (main) camera.
	/// </summary>
	/// <returns><c>true</c>, if game object from camera was detached, <c>false</c> otherwise.</returns>
	/// <param name="gameObject">Game object.</param>
	public bool DetachGameObjectFromCamera(ref GameObject gameObject)
	{
		if((CameraMain != null) && (CameraMain.transform == gameObject.transform.parent))
		{
			gameObject.transform.parent = null;
			return true;
		}				
		
		return false;
	}

	/// <summary>
	/// Gets the camera depth.
	/// </summary>
	/// <returns>The camera depth.</returns>
	public float GetCameraDepth()
	{
		return CameraMain.depth;
	}

	// Get Misc. values from CameraController
	
	/// <summary>
	/// Gets the height of the player eye.
	/// </summary>
	/// <returns><c>true</c>, if player eye height was gotten, <c>false</c> otherwise.</returns>
	/// <param name="eyeHeight">Eye height.</param>
	public bool GetPlayerEyeHeight(ref float eyeHeight)
	{
		eyeHeight = CameraRootPosition.y + NeckPosition.y;  
		
		return true;
	}
	#endregion
}

