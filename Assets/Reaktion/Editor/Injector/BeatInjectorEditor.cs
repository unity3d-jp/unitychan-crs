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
using UnityEditor;
using System.Collections;

namespace Reaktion {

[CustomEditor(typeof(BeatInjector)), CanEditMultipleObjects]
public class BeatInjectorEditor : Editor
{
    SerializedProperty propBpm;
    SerializedProperty propCurve;
    SerializedProperty propTapNote;
    SerializedProperty propTapChannel;
    SerializedProperty propTapButton;
    GUIContent labelBpm;

    AnimationCurve preset1;
    AnimationCurve preset2;
    AnimationCurve preset3;
    AnimationCurve preset4;

    void OnEnable()
    {
        propBpm = serializedObject.FindProperty("bpm");
        propCurve = serializedObject.FindProperty("curve");
        propTapNote = serializedObject.FindProperty("tapNote");
        propTapChannel = serializedObject.FindProperty("tapChannel");
        propTapButton = serializedObject.FindProperty("tapButton");
        labelBpm = new GUIContent("BPM");

        preset1 = new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(0.5f, 0), new Keyframe(1, 0)
        );

        preset2 = new AnimationCurve(
            new Keyframe(0.0f, 1.0f), new Keyframe(0.499f, 0),
            new Keyframe(0.5f, 0.2f), new Keyframe(0.999f, 0)
        );

        preset3 = new AnimationCurve(
            new Keyframe(0.00f, 1.0f), new Keyframe(0.249f, 0),
            new Keyframe(0.25f, 0.1f), new Keyframe(0.499f, 0),
            new Keyframe(0.50f, 0.1f), new Keyframe(0.749f, 0),
            new Keyframe(0.75f, 0.1f), new Keyframe(0.999f, 0)
        );

        preset4 = new AnimationCurve(
            new Keyframe(0.00f, 1.0f), new Keyframe(0.249f, 0),
            new Keyframe(0.25f, 0.1f), new Keyframe(0.499f, 0),
            new Keyframe(0.50f, 0.3f), new Keyframe(0.749f, 0),
            new Keyframe(0.75f, 0.1f), new Keyframe(0.999f, 0)
        );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(propBpm, labelBpm);
        EditorGUILayout.PropertyField(propCurve);

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kick Only"))
            propCurve.animationCurveValue = preset1;
        if (GUILayout.Button("Kick-Hat"))
            propCurve.animationCurveValue = preset2;
        if (GUILayout.Button("K-C-C-C"))
            propCurve.animationCurveValue = preset3;
        if (GUILayout.Button("K-C-O-C"))
            propCurve.animationCurveValue = preset4;
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(propTapNote);
        EditorGUILayout.PropertyField(propTapChannel);
        EditorGUILayout.PropertyField(propTapButton);

        if (GUILayout.Button("TAP"))
            foreach (var t in targets)
                (t as BeatInjector).SendMessage("Tap");

        serializedObject.ApplyModifiedProperties();
    }
}

} // namespace Reaktion
