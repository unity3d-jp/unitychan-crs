/************************************************************************************
	
Filename    :   OVRVisionGuide.cs
Content     :   Guides user back to optimal vision volume 
Created     :   February 19, 2014
Authors     :   Peter Giokaris
		
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
using System.Collections;

/// <summary>
/// OVR vision guide.
/// </summary>
public class OVRVisionGuide : MonoBehaviour 
{
	// Manual fade (used when out of view; will add textures on top)
	private Texture FadeTexture 			= null;
	private float 	FadeTextureAlpha 		= 0.0f;

	// Clip Camera (takes position offset into account)
	private Vector3 CameraPositionClampMin = new Vector3(-0.45f, -0.25f, -0.5f);
	private Vector3 CameraPositionClampMax = new Vector3( 0.45f,  1.35f,  1.0f);
	private float   CameraPositionOverlap  = 0.125f;
	private float   CameraPositionMaxFade  = 0.65f;

	// Handle to OVRCameraController
	private OVRCameraController CameraController = null;
	 
	// Handle to Vision Guide 
	private GameObject VisionGuide = null;
	private float VisionGuideFlashSpeed = 5.0f; // Radians / sec

	// Layer to render to
	private string LayerName = "Default";

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () 
	{	
		if(CameraController != null)
		{
			// Set the GUI target 
			VisionGuide = GameObject.Instantiate(Resources.Load("OVRVisionGuideMessage")) as GameObject;
			// Grab transform of GUI object
			Transform t = VisionGuide.transform;
			// Attach the GUI object to the camera
			CameraController.AttachGameObjectToCamera(ref VisionGuide);
			// Reset the transform values
			OVRUtils.SetLocalTransform(ref VisionGuide, ref t);
			// Deactivate the object
			VisionGuide.SetActive(false);
			// Set layer on object
			VisionGuide.layer = LayerMask.NameToLayer(LayerName);
		}
	}
	
	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update () 
	{
		Vector3 absVisionCam = Vector3.zero;
		OVRCamera.GetAbsoluteCameraFromVisionPosition(ref absVisionCam);
		Vector3 relVisionCam = Vector3.zero;
		OVRCamera.GetRelativeCameraFromVisionPosition(ref relVisionCam);
/*
		Debug.LogWarning(System.String.Format("ABS: {0:F2} {1:F2} {2:F2}",
		                                      absVisionCam.x, absVisionCam.y, absVisionCam.z));
		Debug.LogWarning(System.String.Format("REL: {0:F2} {1:F2} {2:F2}",
		                                      relVisionCam.x, relVisionCam.y, relVisionCam.z));
*/
		// R will reset the orientation based on player input ('R' key)
		UpdateResetOrientation();
		// Fade screen out based on location of relative Vision Camera
		UpdateFadeValueFromRelCamPosition(ref relVisionCam);
				
		if (Input.GetKeyDown(KeyCode.T))
			CameraController.TimeWarp = !CameraController.TimeWarp;
		
		if (Input.GetKeyDown(KeyCode.F))
			CameraController.FreezeTimeWarp = !CameraController.FreezeTimeWarp;
	}

	/// <summary>
	/// Updates the reset orientation.
	/// </summary>
	void UpdateResetOrientation()
	{
		// Reset the view on 'R'
		if (Input.GetKeyDown(KeyCode.R) == true)
		{
			// Reset tracker position.
			OVRCamera.ResetCameraPositionOrientation(Vector3.one, Vector3.zero, Vector3.up, Vector3.zero);
		}
	}

	/// <summary>
	/// Updates the fade value from rel cam position.
	/// </summary>
	/// <returns><c>true</c>, if fade value from rel cam position was updated, <c>false</c> otherwise.</returns>
	/// <param name="relCamPosition">Rel cam position.</param>
	bool UpdateFadeValueFromRelCamPosition(ref Vector3 relCamPosition)
	{
		bool result = false;
		FadeTextureAlpha = 0.0f;	

		// Clip camera to min amd max values
		// MIN
		if((relCamPosition.x < CameraPositionClampMin.x) && 
		   (CalculateFadeValue(ref FadeTextureAlpha, 
		                       relCamPosition.x, CameraPositionClampMin.x) == true))
			result = true;

		if((relCamPosition.y < CameraPositionClampMin.y) && 
		   (CalculateFadeValue(ref FadeTextureAlpha, 
		                    relCamPosition.y, CameraPositionClampMin.y) == true))
			result = true;

		if((relCamPosition.z < CameraPositionClampMin.z) && 
		   (CalculateFadeValue(ref FadeTextureAlpha, 
		                    relCamPosition.z, CameraPositionClampMin.z) == true))
			result = true;

		// MAX
		if((relCamPosition.x > CameraPositionClampMax.x) && 
		   (CalculateFadeValue(ref FadeTextureAlpha, 
		                    CameraPositionClampMax.x, relCamPosition.x ) == true))
			result = true;

		if((relCamPosition.y > CameraPositionClampMax.y) && 
		   (CalculateFadeValue(ref FadeTextureAlpha, 
		                    CameraPositionClampMax.y, relCamPosition.y ) == true))
			result = true;

		if((relCamPosition.z > CameraPositionClampMax.z) && 
		   (CalculateFadeValue(ref FadeTextureAlpha, 
		                    CameraPositionClampMax.z, relCamPosition.z ) == true))
			result = true;

		return result;
	}

	/// <summary>
	/// CalculateFadeValue
	/// return value tells us which axis is the furthest out, so we 
	/// can tell the user which direction to go
	/// </summary>
	/// <returns><c>true</c>, if fade value was calculated, <c>false</c> otherwise.</returns>
	/// <param name="curFade">Current fade.</param>
	/// <param name="a">The alpha component.</param>
	/// <param name="b">The blue component.</param>
	bool CalculateFadeValue(ref float curFade, float a, float b)
	{
		bool result = false;
		
		float tmpFade = (b - a) / CameraPositionOverlap;
		
		if(tmpFade > 1.0f) tmpFade = 1.0f;
		tmpFade *= CameraPositionMaxFade;
		
		if(tmpFade > curFade)
		{
			curFade = tmpFade;
			
			// We want to show a bit more then the fade
			if(tmpFade >= CameraPositionMaxFade)
				result = true;
		}
		
		return result;
	}


	//
	// PUBLIC FUNCTIONS
	//

	/// <summary>
	/// Sets the camera controller.
	/// </summary>
	/// <param name="cameraController">Camera controller.</param>
	public void SetOVRCameraController(ref OVRCameraController cameraController)
	{
		CameraController = cameraController;
	}

	/// <summary>
	/// Sets the fade texture.
	/// </summary>
	/// <param name="fadeTexture">Fade texture.</param>
	public void SetFadeTexture(ref Texture fadeTexture)
	{
		FadeTexture = fadeTexture;
	}

	/// <summary>
	/// Gets the fade alpha value.
	/// </summary>
	/// <returns>The fade alpha value.</returns>
	public float GetFadeAlphaValue()
	{
		return FadeTextureAlpha;
	}

	/// <summary>
	/// Sets the vision guide layer.
	/// </summary>
	/// <param name="layer">Layer.</param>
	public void SetVisionGuideLayer(ref string layer)
	{
		LayerName = layer;
	}

	/// <summary>
	/// Raises the GUI vision guide event.
	/// </summary>
	public void OnGUIVisionGuide()
	{	
		// Separate fade value (externally driven)
		if((FadeTexture != null) && (FadeTextureAlpha > 0.0f))
		{
			GUI.color = new Color(0.1f, 0.1f, 0.1f, FadeTextureAlpha);
			GUI.DrawTexture( new Rect(0, 0, Screen.width, Screen.height ), FadeTexture );	
			GUI.color = Color.white;

			if(VisionGuide != null)
			{
				// Activate the message
				VisionGuide.SetActive(true);

				// Sharper curve for fading text
				float fade = FadeTextureAlpha / CameraPositionMaxFade;
				fade *= fade;

				// Fade and flash the VisionGuide message
				float VisionGuideAlpha = 
				fade * ((Mathf.Sin(Time.time * VisionGuideFlashSpeed) + 1.0f) * 0.5f);
				          
				Color c = VisionGuide.renderer.material.GetColor("_Color");
				c.a = VisionGuideAlpha;
				VisionGuide.renderer.material.SetColor("_Color", c);
			}
		}
		else
		{
			if(VisionGuide != null)
				VisionGuide.SetActive(false);
		}
	}
}
