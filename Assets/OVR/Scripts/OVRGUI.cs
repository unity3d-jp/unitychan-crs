/************************************************************************************

Filename    :   OVRGUI.cs
Content     :   OVR GUI helper classclass
Created     :   May 1, 2013
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
using System.Collections.Generic;

//-------------------------------------------------------------------------------------
// ***** OVRGUI
//

/// <summary>
/// OVRGUI is a GUI help class that provides functions for drawing text and other elements
/// to the screen
/// </summary>
public class OVRGUI
{
	private Font  FontReplace    = null;
		
	private float PixelWidth     = 1280.0f;
	private float PixelHeight    =  800.0f;
	
	// DK1 resolution of W: (1280 / 2) H: 800
	private float DisplayWidth   = 1280.0f;
	private float DisplayHeight  =  800.0f;

	private Rect  DrawRect;

	/// <summary>
	/// Gets the replacement font.
	/// </summary>
	/// <param name="fontReplace">Font replace.</param>
	public void GetFontReplace(ref Font fontReplace)
	{
		fontReplace = FontReplace;
	}
	/// <summary>
	/// Sets the font replace.
	/// </summary>
	/// <param name="fontReplace">Font replace.</param>
	public void SetFontReplace(Font fontReplace)
	{
		FontReplace = fontReplace;
	}
	
	/// <summary>
	/// Gets the pixel resolution.
	/// </summary>
	/// <param name="pixelWidth">Pixel width.</param>
	/// <param name="pixelHeight">Pixel height.</param>
	public void GetPixelResolution(ref float pixelWidth, ref float pixelHeight)
	{
		pixelWidth = PixelWidth;
		pixelHeight = PixelHeight;
	}
	/// <summary>
	/// Sets the pixel resolution.
	/// </summary>
	/// <param name="pixelWidth">Pixel width.</param>
	/// <param name="pixelHeight">Pixel height.</param>
	public void SetPixelResolution(float pixelWidth, float pixelHeight)
	{
		PixelWidth = pixelWidth;
		PixelHeight = pixelHeight;
	}
	
	/// <summary>
	/// Gets the display resolution.
	/// </summary>
	/// <param name="Width">Width.</param>
	/// <param name="Height">Height.</param>
	public void GetDisplayResolution(ref float Width, ref float Height)
	{
		Width = DisplayWidth;
		Height = DisplayHeight;
	}
	/// <summary>
	/// Sets the display resolution.
	/// </summary>
	/// <param name="Width">Width.</param>
	/// <param name="Height">Height.</param>
	public void SetDisplayResolution(float Width, float Height)
	{
		DisplayWidth = Width;
		DisplayHeight = Height;
	}

	/// <summary>
	/// StereoBox  
	/// Values go from 0 - PixelSizeX/Y 
	/// </summary>
	/// <param name="X">X.</param>
	/// <param name="Y">Y.</param>
	/// <param name="wX">W x.</param>
	/// <param name="hY">H y.</param>
	/// <param name="text">Text.</param>
	/// <param name="color">Color.</param>
	public void StereoBox(int X, int Y, int wX, int hY, ref string text, Color color)
	{
		Font prevFont = GUI.skin.font;
		GUI.color = color;
		// Make sure to change font if it needs replacement
		if(GUI.skin.font != FontReplace) GUI.skin.font = FontReplace;
	
		float s = PixelWidth / DisplayWidth;
	
		CalcPositionAndSize(X * s, Y * s, wX * s, hY * s, ref DrawRect);
		
		GUI.Box(DrawRect, text);
		
		GUI.skin.font = prevFont;
	}

	/// <summary>
	/// Stereos the box.
	/// Values go from 0.0 - 1.0f; normalized approach to rendering GUI objects
	/// </summary>
	/// <param name="X">X.</param>
	/// <param name="Y">Y.</param>
	/// <param name="wX">W x.</param>
	/// <param name="hY">H y.</param>
	/// <param name="text">Text.</param>
	/// <param name="color">Color.</param>
	public void StereoBox(float X, float Y, float wX, float hY, ref string text, Color color)
	{
		StereoBox ((int)(X  * PixelWidth), 
				   (int)(Y  * PixelHeight),
				   (int)(wX * PixelWidth),
				   (int)(hY * PixelHeight),
					ref text, color);
	}

	/// <summary>
	/// Draw a stereo texture.
	/// </summary>
	/// <param name="X">X.</param>
	/// <param name="Y">Y.</param>
	/// <param name="wX">W x.</param>
	/// <param name="hY">H y.</param>
	/// <param name="image">Image.</param>
	/// <param name="color">Color.</param>
	public void StereoDrawTexture(int X, int Y, int wX, int hY, ref Texture image, Color color)
	{
		GUI.color = color;
		// Make sure to change font if it needs replacement
		if(GUI.skin.font != FontReplace) GUI.skin.font = FontReplace;
	
		float s = PixelWidth / DisplayWidth;
	
		CalcPositionAndSize(X * s, Y * s, wX * s, hY * s, ref DrawRect);
		
		GUI.DrawTexture(DrawRect, image);
	}
	/// <summary>
	/// Draw a stereo texture.
	/// </summary>
	/// <param name="X">X.</param>
	/// <param name="Y">Y.</param>
	/// <param name="wX">W x.</param>
	/// <param name="hY">H y.</param>
	/// <param name="image">Image.</param>
	/// <param name="color">Color.</param>
	public void StereoDrawTexture(float X, float Y, float wX, float hY, ref Texture image, Color color)
	{
		StereoDrawTexture ((int)(X  * PixelWidth), 
				   		   (int)(Y  * PixelHeight),
				   		   (int)(wX * PixelWidth),
				   		   (int)(hY * PixelHeight),
						    ref image, color);
	}
	
	/// <summary>
	/// Calculates the size of the position.
	/// </summary>
	/// <param name="X">X.</param>
	/// <param name="Y">Y.</param>
	/// <param name="wX">W x.</param>
	/// <param name="hY">H y.</param>
	/// <param name="calcPosSize">Calculate position size.</param>
	private void CalcPositionAndSize(float X, float Y, float wX, float hY,
									 ref Rect calcPosSize)
	{
		float sSX = (float)Screen.width / PixelWidth;	
		float sSY = (float)Screen.height / PixelHeight;
				
		calcPosSize.x  = X * sSX;
		calcPosSize.width = wX * sSX;
		calcPosSize.y  = Y * sSY;
		calcPosSize.height = hY * sSY;
	}
	
}


