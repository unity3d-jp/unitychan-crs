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

[CustomEditor(typeof(LightGear)), CanEditMultipleObjects]
public class LightGearEditor : Editor
{
    SerializedProperty propReaktor;
    SerializedProperty propIntensity;
    SerializedProperty propEnableColor;
    SerializedProperty propColorGradient;

    GUIContent labelColor;
    GUIContent labelGradient;

    void OnEnable()
    {
        propReaktor       = serializedObject.FindProperty("reaktor");
        propIntensity     = serializedObject.FindProperty("intensity");
        propEnableColor   = serializedObject.FindProperty("enableColor");
        propColorGradient = serializedObject.FindProperty("colorGradient");

        labelColor    = new GUIContent("Color");
        labelGradient = new GUIContent("Gradient");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(propReaktor);

        EditorGUILayout.PropertyField(propIntensity);

        EditorGUILayout.PropertyField(propEnableColor, labelColor);
        if (propEnableColor.hasMultipleDifferentValues || propEnableColor.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(propColorGradient, labelGradient);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}

} // namespace Reaktion
