/************************************************************************************
	
	Filename    	:   OVRGridCube.cs
		Content     :   Renders a grid of cubes (useful for positional tracking)
		Created     :   February 14, 2014
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
/// OVR grid cube.
/// </summary>
public class OVRGridCube : MonoBehaviour 
{
	public KeyCode GridKey                     = KeyCode.G;
	private GameObject 	CubeGrid			   = null;
	private OVRCameraGameObject CameraCubeGrid = null;

	private bool 	CubeGridOn		    	   = false;
	private bool 	CubeSwitchColorOld  	   = false;
	private bool 	CubeSwitchColor     	   = false;

	private int   gridSizeX  = 6;
	private int   gridSizeY  = 4;
	private int   gridSizeZ  = 6;
	private float gridScale  = 0.3f;
	private float cubeScale  = 0.03f;

	// Handle to OVRCameraController
	private OVRCameraController CameraController = null;

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () 
	{
	}
	
	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update () 
	{
		UpdateCubeGrid();
	}

	/// <summary>
	/// Sets the OVR camera controller.
	/// </summary>
	/// <param name="cameraController">Camera controller.</param>
	public void SetOVRCameraController(ref OVRCameraController cameraController)
	{
		CameraController = cameraController;
	}

	/// <summary>
	/// Updates the cube grid.
	/// </summary>
	void UpdateCubeGrid()
	{
		// Toggle the grid cube display on 'G'
		if(Input.GetKeyDown(GridKey))
		{
			if(CubeGridOn == false)
			{
				CubeGridOn = true;
				Debug.LogWarning("CubeGrid ON");
				if(CubeGrid != null)
					CubeGrid.SetActive(true);	
				else
					CreateCubeGrid();

				// Add the CameraCubeGrid to the camera list for update
				OVRCamera.AddToLocalCameraSetList(ref CameraCubeGrid);
			}
			else
			{
				CubeGridOn = false;
				Debug.LogWarning("CubeGrid OFF");
				
				if(CubeGrid != null)
					CubeGrid.SetActive(false);

				// Remove the CameraCubeGrid from the camera list
				OVRCamera.RemoveFromLocalCameraSetList(ref CameraCubeGrid);
			}
		}
		
		if(CubeGrid != null)
		{
			// Set cube colors to let user know if camera is tracking
			CubeSwitchColor = !OVRDevice.IsCameraTracking();
			
			if(CubeSwitchColor != CubeSwitchColorOld)
				CubeGridSwitchColor(CubeSwitchColor);
			CubeSwitchColorOld = CubeSwitchColor;
		}
	}
	
	/// <summary>
	/// Creates the cube grid.
	/// </summary>
	void CreateCubeGrid()
	{
		Debug.LogWarning("Create CubeGrid");

		// Create the visual cube grid
		CubeGrid = new GameObject("CubeGrid");
		// Set a layer to target a specific camera
		CubeGrid.layer = CameraController.gameObject.layer;

		// Create a CameraGameObject to update within Camera
		CameraCubeGrid = new OVRCameraGameObject();
		// Set CubeGrid GameObject and CameraController 
		// to allow for targeting depth within OVRCamera update
		CameraCubeGrid.CameraGameObject = CubeGrid;
		CameraCubeGrid.CameraController = CameraController;

		for (int x = -gridSizeX; x <= gridSizeX; x++)
			for (int y = -gridSizeY; y <= gridSizeY; y++)
				for (int z = -gridSizeZ; z <= gridSizeZ; z++)
			{
				// Set the cube type:
				// 0 = non-axis cube
				// 1 = axis cube
				// 2 = center cube
				int CubeType = 0;
				if ((x == 0 && y == 0) || (x == 0 && z == 0) || (y == 0 && z == 0))
				{
					if((x == 0) && (y == 0) && (z == 0))
						CubeType = 2;
					else
						CubeType = 1;
				}
				
				GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
				
				BoxCollider bc = cube.GetComponent<BoxCollider>();
				bc.enabled = false;
				
				cube.layer = CameraController.gameObject.layer;
				
				// No shadows
				cube.renderer.castShadows    = false;
				cube.renderer.receiveShadows = false;
				
				// Cube line is white down the middle
				if (CubeType == 0)
					cube.renderer.material.color = Color.red;
				else if (CubeType == 1)	
					cube.renderer.material.color = Color.white;
				else
					cube.renderer.material.color = Color.yellow;
				
				cube.transform.position = 
					new Vector3(((float)x * gridScale), 
					            ((float)y * gridScale), 
					            ((float)z * gridScale));
				
				float s = 0.7f;
				
				// Axis cubes are bigger
				if(CubeType == 1)
					s = 1.0f;				
				// Center cube is the largest
				if(CubeType == 2)
					s = 2.0f;
				
				cube.transform.localScale = 
					new Vector3(cubeScale * s, cubeScale * s, cubeScale * s);
				
				cube.transform.parent = CubeGrid.transform;
			}
	}
	
	/// <summary>
	/// Switch the Cube grid color.
	/// </summary>
	/// <param name="CubeSwitchColor">If set to <c>true</c> cube switch color.</param>
	void CubeGridSwitchColor(bool CubeSwitchColor)
	{
		Color c = Color.red;
		if(CubeSwitchColor == true)
			c = Color.blue;
		
		foreach(Transform child in CubeGrid.transform)
		{
			// Cube line is white down the middle
			if((child.renderer.material.color == Color.red) ||
			   (child.renderer.material.color == Color.blue))
				child.renderer.material.color = c;
		}
	}
}
