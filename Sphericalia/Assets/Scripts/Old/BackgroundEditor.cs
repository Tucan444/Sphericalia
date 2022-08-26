/* using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SphericalBackground))]
public class BackgroundEditor : Editor
{
    SerializedObject so;
    SerializedProperty useT;
    SerializedProperty bgColor;
    SerializedProperty bgTex;

    void OnEnable() {
        so = serializedObject;
        useT = so.FindProperty("useTexture");
        bgColor = so.FindProperty("bgColor");
        bgTex = so.FindProperty("mainTex");
    }

    public override void OnInspectorGUI() {
        SphericalBackground sb = (SphericalBackground)target;
        so.Update();

        EditorGUILayout.PropertyField(useT);
        if(sb.useTexture) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("(no preview)", GUILayout.Width(80));

                EditorGUILayout.PropertyField(bgTex);
            }
        } else {
            EditorGUILayout.PropertyField(bgColor);
        }

        so.ApplyModifiedProperties();
    }
}
 */