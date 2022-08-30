using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class Lighting : MonoBehaviour
{
    public bool useLighting = false; 
    [Range(0, 1)] public float ambientLight = 0.3f;
    public bool useBakedLighting = false;
    public Texture2D bakedLightmap;

    public static List<PointLight> lights = new List<PointLight>();
    int count = 0;
    
    SphSpaceManager ssm;

    void OnEnable() {
        SphSpaceManager.lighting = this;
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    public void BakeLighting() {
        Debug.Log("baking");
    }

    public void AddPointLight() {
        GameObject obj = new GameObject(count.ToString());
        PointLight pl = obj.AddComponent(typeof(PointLight)) as PointLight;
        Undo.RecordObject(pl, "Created point light");
        obj.transform.parent = gameObject.transform;

        count++;
    }

    public PointLightS[] GetStructs() {
        PointLightS[] lightsS = new PointLightS[lights.Count];

        for (int i = 0; i < lights.Count; i++) {
            lightsS[i] = lights[i].GetStruct();
        }

        return lightsS;
    }
}
