using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AddObject : EditorWindow
{
    GameObject space;

    public enum SphericalObjects {
        Circle, NGon, GeneralShape, UVTiles
    }
    SphericalObjects objects;

    // generally used variables
    bool draw = true;

    bool Static = true;
    string namE = "duck";
    Vector2 sphericalPosition = new Vector2();
    Color color = new Color(0.69f, 0.48f, 0.41f, 1);

    // for circle
    float radius = 0.1f;

    // for ngon  
    int ngon = 5;
    float rotation = 0;
    float scale = 1; 

    // for general shape
    int nshape = 20;

    // other
    SphericalUtilities su = new SphericalUtilities();
    
    [MenuItem("Spherical/AddObject")]
    public static void OpenAddObjectWindow() => GetWindow<AddObject>("Object adder");

    void OnGUI() {

        using (new GUILayout.HorizontalScope()) {
            GUILayout.Label("Object : ");
            objects = (SphericalObjects)EditorGUILayout.EnumPopup(objects);
        }

        draw = EditorGUILayout.Toggle("Draw: ", draw);
        Static = EditorGUILayout.Toggle("Static: ", Static);
        namE = EditorGUILayout.TextField("Name: ", namE);
        sphericalPosition = EditorGUILayout.Vector2Field("SphPosition: ", sphericalPosition);
        color = EditorGUILayout.ColorField("Color: ", color);

        // dealing with circle
        if (objects == SphericalObjects.Circle) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Radius : " + radius, GUILayout.Width(120));

                radius = GUILayout.HorizontalSlider(radius, 0.01f, -0.01f + Mathf.PI);
            }
            
            if (GUILayout.Button("Create circle")) {
                GameObject obj = new GameObject(namE);
                SphCircle sc = obj.AddComponent(typeof(SphCircle)) as SphCircle;
                Undo.RecordObject(sc, "Configured Circle");
                sc.Static = Static;
                sc.sphPosition = sphericalPosition;
                sc.radius = radius;
                sc.color = color;
                sc.GetDefaultSetup();
            }
        } // dealing with ngon
        else if (objects == SphericalObjects.NGon) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("N : " + ngon, GUILayout.Width(60));

                ngon = EditorGUILayout.IntSlider(ngon, 3, 20);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Rotation : " + rotation, GUILayout.Width(140));

                rotation = GUILayout.HorizontalSlider(rotation, -180, 180);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Scale : " + scale, GUILayout.Width(140));

                scale = GUILayout.HorizontalSlider(scale, -0.01f, -0.001f + Mathf.PI*0.5f);
            }

            if (GUILayout.Button("Create ngon")) {
                GameObject obj = new GameObject(namE);
                SphGon sg = obj.AddComponent(typeof(SphGon)) as SphGon;
                Undo.RecordObject(sg, "Configured NGon");
                sg.Static = Static;
                sg.n = ngon;
                sg.sphPosition = sphericalPosition;
                sg.rotation = rotation;
                sg.scale = scale;
                sg.color = color;
                sg.GetDefaultSetup();
            }
        }// dealing with general shapes
        else if (objects == SphericalObjects.GeneralShape) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("N : " + nshape, GUILayout.Width(60));

                nshape = EditorGUILayout.IntSlider(nshape, 3, 60);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Rotation : " + rotation, GUILayout.Width(140));

                rotation = GUILayout.HorizontalSlider(rotation, -180, 180);
            }
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Scale : " + scale, GUILayout.Width(140));

                scale = GUILayout.HorizontalSlider(scale, -0.01f, 2);
            }

            if (GUILayout.Button("Create general shape")) {
                GameObject obj = new GameObject(namE);
                SphShape ss = obj.AddComponent(typeof(SphShape)) as SphShape;
                Undo.RecordObject(ss, "Configured General Shape");
                ss.Static = Static;
                ss.sphPosition = sphericalPosition;
                ss.rotation = rotation;
                ss.scale = scale;
                ss.color = color;
                ss.polarVertices = new Vector2[nshape];
                ss.SetToNGon();
                ss.GetDefaultSetup();
            }
        } // dealing with uv tiles
        else if (objects == SphericalObjects.UVTiles) {
            using (new GUILayout.HorizontalScope()) {
                GUILayout.Label("Rotation : " + rotation, GUILayout.Width(140));

                rotation = GUILayout.HorizontalSlider(rotation, -180, 180);
            }

            if (GUILayout.Button("Create UVTiles")) {
                GameObject obj = new GameObject(namE);
                UVTiles uvt = obj.AddComponent(typeof(UVTiles)) as UVTiles;
                Undo.RecordObject(uvt, "Configured UVTiles");
                uvt.Static = Static;
                uvt.sphPosition = sphericalPosition;
                uvt.rotation = rotation;
                uvt.color = color;
                uvt.OnEnable();
            }
        }
    }

    void DuringSceneGUI(SceneView view) {
        if (draw) {
            if (objects == SphericalObjects.Circle) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, radius));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, radius * 0.9f));
                Handles.color = color;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, radius * 0.8f));
            } else if (objects == SphericalObjects.NGon) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale, ngon)));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.9f, ngon)));
                Handles.color = color;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.8f, ngon)));
            } else if (objects == SphericalObjects.GeneralShape) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale, nshape)));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.9f, nshape)));
                Handles.color = color;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, scale * 0.8f, nshape)));
            } else if (objects == SphericalObjects.UVTiles) {
                Handles.color = color * 1.4f;
                su.HandlesDrawPoints(ProcessVertices(su.GetCirclePoints(sphericalPosition, 0.5f, 4), 45));
                Handles.color = color * 1.2f;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, 0.45f, 10));
                Handles.color = color;
                su.HandlesDrawPoints(su.GetCirclePoints(sphericalPosition, 0.4f, 10));
            }
        }
        
    }

    void OnEnable() {
        space = GameObject.Find("___SphericalSpace___");
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable() {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    Vector3[] ProcessVertices(Vector3[] verts, float add=0) {
        Quaternion q = Quaternion.AngleAxis(rotation + add, su.Spherical2Cartesian(sphericalPosition));
        for (int i = 0; i < verts.Length; i++) {
            verts[i] = q * verts[i];
        }
        return verts;
    }
}
