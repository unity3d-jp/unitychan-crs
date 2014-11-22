/************************************************************************************

Filename    :   OVRDeviceEditor.cs
Content     :   Rift editor interface. 
				This script adds editor functionality to the OVRRift class
Created     :   February 14, 2013
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
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(OVRDevice))]

//-------------------------------------------------------------------------------------
// ***** OVRDeviceEditor
//
// OVRDeviceEditor adds extra functionality into the inspector for the currently selected
// OVRRift component.
//
public class OVRDeviceEditor : Editor
{
	// OnInspectorGUI
	public override void OnInspectorGUI()
	{
		GUI.color = Color.white;
		{
			OVRDevice.PredictionTime     = EditorGUILayout.Slider("Prediction Time", 		OVRDevice.PredictionTime,	0, 0.1f);
			OVRDevice.ResetTrackerOnLoad = EditorGUILayout.Toggle("Reset Tracker On Load",  OVRDevice.ResetTrackerOnLoad);			
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(target);
		}
	}		
}

