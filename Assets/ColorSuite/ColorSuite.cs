//
// Copyright (C) 2014 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Color Adjustments/Color Suite")]
public class ColorSuite : MonoBehaviour
{
    // Curve objects.
    [SerializeField] AnimationCurve _rCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _gCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _bCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _lCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public AnimationCurve redCurve {
        get { return _rCurve; }
        set { _rCurve = value; UpdateParameters(); }
    }
    public AnimationCurve greenCurve {
        get { return _gCurve; }
        set { _gCurve = value; UpdateParameters(); }
    }
    public AnimationCurve blueCurve {
        get { return _bCurve; }
        set { _bCurve = value; UpdateParameters(); }
    }
    public AnimationCurve luminanceCurve {
        get { return _lCurve; }
        set { _lCurve = value; UpdateParameters(); }
    }

    // Adjustment parameters.
    [SerializeField] float _brightness = 0.0f;
    [SerializeField] float _contrast   = 1.0f;
    [SerializeField] float _saturation = 1.0f;

    public float brightness {
        get { return _brightness; }
        set { _brightness = value; UpdateParameters(); }
    }
    public float contrast {
        get { return _contrast; }
        set { _contrast = value; UpdateParameters(); }
    }
    public float saturation {
        get { return _saturation; }
        set { _saturation = value; } // no UpdateParameters
    }

    // Tonemapping parameters.
    [SerializeField] bool _tonemapping = false;
    [SerializeField] float _exposure   = 1.8f;

    public bool tonemapping {
        get { return _tonemapping; }
        set { _tonemapping = value; }
    }
    public float exposure {
        get { return _exposure; }
        set { _exposure = value; }
    }

    // Vignette parameters.
    [SerializeField] float _vignette = 0.0f;

    public float vignette {
        get { return _vignette; }
        set { _vignette = value; }
    }

    // Reference to the shader.
    [SerializeField] Shader shader;

    // Temporary objects.
    Material material;
    Texture2D texture;

    void SetUpObjects()
    {
        if (material != null && texture != null) return;

        material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;

        texture = new Texture2D(256, 1, TextureFormat.ARGB32, false, true);
        texture.hideFlags = HideFlags.DontSave;
        texture.wrapMode = TextureWrapMode.Clamp;

        UpdateParameters();
    }

    void UpdateParameters()
    {
        // Variables for brightness adjustment.
        var bt = _brightness > 0 ? 1.0f : -1.0f;
        var bp = Mathf.Abs(_brightness);

        for (var x = 0; x < texture.width; x++)
        {
            var u = 1.0f / (texture.width - 1) * x;
            var r = Mathf.Lerp(_lCurve.Evaluate((_rCurve.Evaluate(u) - 0.5f) * _contrast + 0.5f), bt, bp);
            var g = Mathf.Lerp(_lCurve.Evaluate((_gCurve.Evaluate(u) - 0.5f) * _contrast + 0.5f), bt, bp);
            var b = Mathf.Lerp(_lCurve.Evaluate((_bCurve.Evaluate(u) - 0.5f) * _contrast + 0.5f), bt, bp);
            texture.SetPixel(x, 0, new Color(r, g, b, 0));
        }

        texture.Apply();
    }

    void Start()
    {
        SetUpObjects();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetUpObjects();

        material.SetTexture("_Curves", texture);
        material.SetFloat("_Saturation", _saturation);

        if (_tonemapping)
        {
            material.EnableKeyword("TONEMAPPING_ON");
            material.SetFloat("_Exposure", _exposure);
        }
        else
            material.DisableKeyword("TONEMAPPING_ON");

        if (_vignette > 0.0f)
        {
            material.EnableKeyword("VIGNETTE_ON");
            material.SetFloat("_Vignette", _vignette);
        }
        else
            material.DisableKeyword("VIGNETTE_ON");

        Graphics.Blit(source, destination, material);
    }
}
