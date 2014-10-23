//
// Reaktion - An audio reactive animation toolkit for Unity.
//
// Copyright (C) 2013, 2014 Keijiro Takahashi
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

namespace Reaktion {

[AddComponentMenu("Reaktion/Injector/Beat Injector")]
public class BeatInjector : InjectorBase
{
    public float bpm = 120;
    public AnimationCurve curve = AnimationCurve.Linear(0, 1, 0.5f, 0);

    public int tapNote = -1;
    public MidiChannel tapChannel = MidiChannel.All;
    public string tapButton;

    float time;
    float tapTime;

    void Update()
    {
        if (tapNote >= 0)
            if (MidiJack.GetKeyDown(tapChannel, tapNote))
                Tap();

        if (!string.IsNullOrEmpty(tapButton))
            if (Input.GetButtonDown(tapButton))
                Tap();

        var interval = 60.0f / bpm;

        time = (time + Time.deltaTime) % interval;

        dbLevel = (curve.Evaluate(time / interval) - 1) * 18;
    }

    public void Tap()
    {
        var delta = Time.time - tapTime;
        if (tapTime > 0.2f && delta < 3.0f)
        {
            bpm = Mathf.Lerp(bpm, 60.0f / delta, 0.15f);
            time = (time > 0.2f) ? 0.0f : time * 0.5f;
        }
        tapTime = Time.time;
    }
}

} // namespace Reaktion
