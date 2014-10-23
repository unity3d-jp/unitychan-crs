
#pragma strict

@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Bloom and Glow/Bloom")
				
class Bloom extends PostEffectsBase {
	enum LensFlareStyle {
		Ghosting = 0,
		Anamorphic = 1,
		Combined = 2,
	}

	enum TweakMode {
		Basic = 0,
		Complex = 1,
	}

	enum HDRBloomMode {
		Auto = 0,
		On = 1,
		Off = 2,
	}

	enum BloomScreenBlendMode {
		Screen = 0,
		Add = 1,
	}

	enum BloomQuality {
		Cheap = 0,
		High = 1,
	}	

	public var tweakMode : TweakMode = 0;
	public var screenBlendMode : BloomScreenBlendMode = BloomScreenBlendMode.Add;
	
	public var hdr : HDRBloomMode = HDRBloomMode.Auto;
	private var doHdr : boolean = false;
	public var sepBlurSpread : float = 2.5f;

	public var quality : BloomQuality = BloomQuality.High;
	
	public var bloomIntensity : float = 0.5f;
	public var bloomThreshhold : float = 0.5f;
	public var bloomThreshholdColor : Color = Color.white;
	public var bloomBlurIterations : int = 2;	
		
	public var hollywoodFlareBlurIterations : int = 2;
	public var flareRotation : float = 0.0f;
	public var lensflareMode : LensFlareStyle = 1;
	public var hollyStretchWidth : float = 2.5f;
	public var lensflareIntensity : float = 0.0f;
	public var lensflareThreshhold : float = 0.3f;
	public var lensFlareSaturation : float = 0.75f;
	public var flareColorA : Color = Color (0.4f, 0.4f, 0.8f, 0.75f);
	public var flareColorB : Color = Color (0.4f, 0.8f, 0.8f, 0.75f);
	public var flareColorC : Color = Color (0.8f, 0.4f, 0.8f, 0.75f);
	public var flareColorD : Color = Color (0.8f, 0.4f, 0.0f, 0.75f);
	public var blurWidth : float = 1.0f;	
	public var lensFlareVignetteMask : Texture2D;
				
	public var lensFlareShader : Shader; 
	private var lensFlareMaterial : Material;

	public var screenBlendShader : Shader;
	private var screenBlend : Material;
	
	public var blurAndFlaresShader: Shader;
	private var blurAndFlaresMaterial : Material;
	
	public var brightPassFilterShader : Shader;
	private var brightPassFilterMaterial : Material;
	
	function CheckResources () : boolean {
		CheckSupport (false);
		
		screenBlend = CheckShaderAndCreateMaterial (screenBlendShader, screenBlend);
		lensFlareMaterial = CheckShaderAndCreateMaterial(lensFlareShader,lensFlareMaterial);
		blurAndFlaresMaterial = CheckShaderAndCreateMaterial (blurAndFlaresShader, blurAndFlaresMaterial);
		brightPassFilterMaterial = CheckShaderAndCreateMaterial(brightPassFilterShader, brightPassFilterMaterial);

		if(!isSupported)
			ReportAutoDisable ();
		return isSupported;
	}

	function OnRenderImage (source : RenderTexture, destination : RenderTexture) {			
		if(CheckResources()==false) {
			Graphics.Blit (source, destination);
			return;
		}		
				
		// screen blend is not supported when HDR is enabled (will cap values)
		
		doHdr = false;
		if(hdr == HDRBloomMode.Auto)
			doHdr = source.format == RenderTextureFormat.ARGBHalf && camera.hdr;
		else {
			doHdr = hdr == HDRBloomMode.On;
		}
		
		doHdr = doHdr && supportHDRTextures;
		
		var realBlendMode : BloomScreenBlendMode = screenBlendMode;
		if(doHdr)
			realBlendMode = BloomScreenBlendMode.Add;

		var rtFormat = (doHdr) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
		var rtW2 = source.width/2;
		var rtH2 = source.height/2;
		var rtW4 = source.width/4;
		var rtH4 = source.height/4;

		var widthOverHeight : float = (1.0f * source.width) / (1.0f * source.height);
		var oneOverBaseSize : float = 1.0f / 512.0f;
		
		// downsample
		var quarterRezColor : RenderTexture = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
		var halfRezColorDown : RenderTexture = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat);
		if(quality > BloomQuality.Cheap) {
			Graphics.Blit (source, halfRezColorDown, screenBlend, 2);
			var rtDown4 : RenderTexture = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
			Graphics.Blit (halfRezColorDown, rtDown4, screenBlend, 2);
			Graphics.Blit (rtDown4, quarterRezColor, screenBlend, 6);
			RenderTexture.ReleaseTemporary(rtDown4);
		}
		else {
			Graphics.Blit (source, halfRezColorDown);
			Graphics.Blit (halfRezColorDown, quarterRezColor, screenBlend, 6);
		}
		RenderTexture.ReleaseTemporary (halfRezColorDown);

		// cut colors (threshholding)			
		var secondQuarterRezColor : RenderTexture = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);		
		BrightFilter (bloomThreshhold * bloomThreshholdColor, quarterRezColor, secondQuarterRezColor);		
				
		// blurring
		
		if (bloomBlurIterations < 1) bloomBlurIterations = 1;
		else if (bloomBlurIterations > 10) bloomBlurIterations = 10;
				        
		for (var iter : int = 0; iter < bloomBlurIterations; iter++ ) {
			var spreadForPass : float = (1.0f + (iter * 0.25f)) * sepBlurSpread;

			// vertical blur
			var blur4 : RenderTexture = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
			blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 (0.0f, spreadForPass * oneOverBaseSize, 0.0f, 0.0f));
			Graphics.Blit (secondQuarterRezColor, blur4, blurAndFlaresMaterial, 4);
			RenderTexture.ReleaseTemporary(secondQuarterRezColor);
			secondQuarterRezColor = blur4;

			// horizontal blur
			blur4 = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);
			blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 ((spreadForPass / widthOverHeight) * oneOverBaseSize, 0.0f, 0.0f, 0.0f));	
			Graphics.Blit (secondQuarterRezColor, blur4, blurAndFlaresMaterial, 4);
			RenderTexture.ReleaseTemporary (secondQuarterRezColor);
			secondQuarterRezColor = blur4;

			if (quality > BloomQuality.Cheap) {
				if (iter == 0)
				{
					Graphics.SetRenderTarget(quarterRezColor);
					GL.Clear(false, true, Color.black); // Clear to avoid RT restore
					Graphics.Blit (secondQuarterRezColor, quarterRezColor);
				}
				else
				{
					quarterRezColor.MarkRestoreExpected(); // using max blending, RT restore expected
					Graphics.Blit (secondQuarterRezColor, quarterRezColor, screenBlend, 10);
				}
			}
		}

		if(quality > BloomQuality.Cheap)
		{
			Graphics.SetRenderTarget(secondQuarterRezColor);
			GL.Clear(false, true, Color.black); // Clear to avoid RT restore
			Graphics.Blit (quarterRezColor, secondQuarterRezColor, screenBlend, 6); 
		}

		// lens flares: ghosting, anamorphic or both (ghosted anamorphic flares) 
		
		if (lensflareIntensity > Mathf.Epsilon) {
			 
			var rtFlares4 : RenderTexture = RenderTexture.GetTemporary (rtW4, rtH4, 0, rtFormat);

			if (lensflareMode == 0) {
				// ghosting only
							
				BrightFilter (lensflareThreshhold, secondQuarterRezColor, rtFlares4);
					
				if(quality > BloomQuality.Cheap) {
					// smooth a little
					blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 (0.0f, (1.5f) / (1.0f * quarterRezColor.height), 0.0f, 0.0f));
					Graphics.SetRenderTarget(quarterRezColor);
					GL.Clear(false, true, Color.black); // Clear to avoid RT restore
					Graphics.Blit (rtFlares4, quarterRezColor, blurAndFlaresMaterial, 4);

					blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 ((1.5f) / (1.0f * quarterRezColor.width), 0.0f, 0.0f, 0.0f));
					Graphics.SetRenderTarget(rtFlares4);
					GL.Clear(false, true, Color.black); // Clear to avoid RT restore
					Graphics.Blit (quarterRezColor, rtFlares4, blurAndFlaresMaterial, 4);
				}

				// no ugly edges!
				Vignette (0.975f, rtFlares4, rtFlares4);
				BlendFlares (rtFlares4, secondQuarterRezColor);
			} 
			else {
				
				//Vignette (0.975f, rtFlares4, rtFlares4);	
				//DrawBorder(rtFlares4, screenBlend, 8);

				var flareXRot : float = 1.0f * Mathf.Cos(flareRotation);
				var flareyRot : float = 1.0f * Mathf.Sin(flareRotation);
				
				var stretchWidth : float = (hollyStretchWidth * 1.0f / widthOverHeight) * oneOverBaseSize;
				var stretchWidthY : float = hollyStretchWidth * oneOverBaseSize;				

				blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 (flareXRot, flareyRot, 0.0, 0.0));
				blurAndFlaresMaterial.SetVector ("_Threshhold", Vector4 (lensflareThreshhold, 1.0f, 0.0f, 0.0f));
				blurAndFlaresMaterial.SetVector ("_TintColor", Vector4 (flareColorA.r, flareColorA.g, flareColorA.b, flareColorA.a) * flareColorA.a * lensflareIntensity);
				blurAndFlaresMaterial.SetFloat ("_Saturation", lensFlareSaturation);

				// "pre and cut"
				quarterRezColor.DiscardContents();
				Graphics.Blit (rtFlares4, quarterRezColor, blurAndFlaresMaterial, 2);
				// "post"
				rtFlares4.DiscardContents();
				Graphics.Blit (quarterRezColor, rtFlares4, blurAndFlaresMaterial, 3);

				blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 (flareXRot * stretchWidth, flareyRot * stretchWidth, 0.0, 0.0));
				// stretch 1st
				blurAndFlaresMaterial.SetFloat ("_StretchWidth", hollyStretchWidth);
				quarterRezColor.DiscardContents();
				Graphics.Blit (rtFlares4, quarterRezColor, blurAndFlaresMaterial, 1);	
				// stretch 2nd
				blurAndFlaresMaterial.SetFloat ("_StretchWidth", hollyStretchWidth * 2.0f);
				rtFlares4.DiscardContents();
				Graphics.Blit (quarterRezColor, rtFlares4, blurAndFlaresMaterial, 1);	
				// stretch 3rd
				blurAndFlaresMaterial.SetFloat ("_StretchWidth", hollyStretchWidth * 4.0f);
				quarterRezColor.DiscardContents();
				Graphics.Blit (rtFlares4, quarterRezColor, blurAndFlaresMaterial, 1);	

				// additional blur passes
				for (iter = 0; iter < hollywoodFlareBlurIterations; iter++ ) {
					stretchWidth = (hollyStretchWidth * 2.0f / widthOverHeight) * oneOverBaseSize;

					blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 (stretchWidth * flareXRot, stretchWidth * flareyRot, 0.0, 0.0));
					rtFlares4.DiscardContents();
					Graphics.Blit (quarterRezColor, rtFlares4, blurAndFlaresMaterial, 4);

					blurAndFlaresMaterial.SetVector ("_Offsets", Vector4 (stretchWidth * flareXRot, stretchWidth * flareyRot, 0.0, 0.0));	
					quarterRezColor.DiscardContents();
					Graphics.Blit (rtFlares4, quarterRezColor, blurAndFlaresMaterial, 4); 						
				}	

				if (lensflareMode == 1)
					// anamorphic lens flares															
					AddTo (1.0, quarterRezColor, secondQuarterRezColor);
				else {

					// "combined" lens flares													
				
					Vignette (1.0, quarterRezColor, rtFlares4);
					BlendFlares (rtFlares4, quarterRezColor);
					AddTo (1.0, quarterRezColor, secondQuarterRezColor);
				}																						
			}
			RenderTexture.ReleaseTemporary (rtFlares4);
		}		

		var blendPass : int = realBlendMode;
		//if(Mathf.Abs(chromaticBloom) < Mathf.Epsilon) 
		//	blendPass += 4;

		screenBlend.SetFloat ("_Intensity", bloomIntensity);
		screenBlend.SetTexture ("_ColorBuffer", source);

		if(quality > BloomQuality.Cheap) {
			var halfRezColorUp : RenderTexture = RenderTexture.GetTemporary (rtW2, rtH2, 0, rtFormat);
			Graphics.Blit (secondQuarterRezColor, halfRezColorUp);
			Graphics.Blit (halfRezColorUp, destination, screenBlend, blendPass);
			RenderTexture.ReleaseTemporary (halfRezColorUp);
		}
		else
			Graphics.Blit (secondQuarterRezColor, destination, screenBlend, blendPass);

		RenderTexture.ReleaseTemporary (quarterRezColor);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);	
	}
	
	private function AddTo (intensity_ : float, from : RenderTexture, to : RenderTexture) {
		screenBlend.SetFloat ("_Intensity", intensity_);
		to.MarkRestoreExpected(); // additive blending, RT restore expected
		Graphics.Blit (from, to, screenBlend, 9); 		
	}
	
	private function BlendFlares (from : RenderTexture, to : RenderTexture) {
		lensFlareMaterial.SetVector ("colorA", Vector4 (flareColorA.r, flareColorA.g, flareColorA.b, flareColorA.a) * lensflareIntensity);
		lensFlareMaterial.SetVector ("colorB", Vector4 (flareColorB.r, flareColorB.g, flareColorB.b, flareColorB.a) * lensflareIntensity);
		lensFlareMaterial.SetVector ("colorC", Vector4 (flareColorC.r, flareColorC.g, flareColorC.b, flareColorC.a) * lensflareIntensity);
		lensFlareMaterial.SetVector ("colorD", Vector4 (flareColorD.r, flareColorD.g, flareColorD.b, flareColorD.a) * lensflareIntensity);
		to.MarkRestoreExpected(); // additive blending, RT restore expected
		Graphics.Blit (from, to, lensFlareMaterial);			
	}

	private function BrightFilter (thresh : float, from : RenderTexture, to : RenderTexture) {
		brightPassFilterMaterial.SetVector ("_Threshhold", Vector4 (thresh, thresh, thresh, thresh));
		Graphics.Blit (from, to, brightPassFilterMaterial, 0);			
	}

	private function BrightFilter (threshColor : Color, from : RenderTexture, to : RenderTexture) {
		brightPassFilterMaterial.SetVector ("_Threshhold", threshColor);
		Graphics.Blit (from, to, brightPassFilterMaterial, 1);			
	}	
	
	private function Vignette (amount : float, from : RenderTexture, to : RenderTexture) {
		if(lensFlareVignetteMask) {
			screenBlend.SetTexture ("_ColorBuffer", lensFlareVignetteMask);
			to.MarkRestoreExpected(); // using blending, RT restore expected
			Graphics.Blit (from == to ? null : from, to, screenBlend, from == to ? 7 : 3); 				
		} 
		else if (from != to)
		{
			Graphics.SetRenderTarget (to);
			GL.Clear(false, true, Color.black); // clear destination to avoid RT restore
			Graphics.Blit (from, to);
		}
	}

}