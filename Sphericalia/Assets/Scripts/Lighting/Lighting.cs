using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class Lighting : MonoBehaviour
{
    public bool useLighting = false; 
    public Color ambientLight = new Color(0.3f, 0.3f, 0.3f, 1);
    [Range(0, 1)] public float gammaCorrection = 1;
    public bool useBakedLighting = false;
    public Texture2D bakedLightmap;
    public int bakingDetail = 5;

    public List<PointLight> lights;

    public List<PointLight> linearLights;
    public List<PointLight> nonLinearLights;

    int count = 0;
    
    SphSpaceManager ssm;

    void OnEnable() {
        SphSpaceManager.lighting = this;
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void Start() {
        lights = new List<PointLight>();

        PointLight[] pointLights = GetComponentsInChildren<PointLight>();
        for (int i = 0; i <pointLights.Length; i++) {lights.Add(pointLights[i]);}

        SortLinear();
    }

    public void SortLinear() {
        linearLights = new List<PointLight>();
        nonLinearLights = new List<PointLight>();

        for (int i = 0; i < lights.Count; i++)
        {
            if (!lights[i].bakedLighting && lights[i].linear) {
                linearLights.Add(lights[i]);
            } else if (!lights[i].bakedLighting && !lights[i].linear) {
                nonLinearLights.Add(lights[i]);
            }
        }
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

    public PointLightS[] GetLinearStructs() {
        PointLightS[] lightsS = new PointLightS[1] {new PointLightS()};
        if (linearLights.Count > 0) {
            lightsS = new PointLightS[linearLights.Count];

            for (int i = 0; i < linearLights.Count; i++) {
                lightsS[i] = linearLights[i].GetLinearStruct();
            }
        } else {
            lightsS[0].layer = 0;
            lightsS[0].pos = new Vector3(1, 0, 0);
            lightsS[0].power = 0;
            lightsS[0].top = 0;
            lightsS[0].slope = 0;
            lightsS[0].color = Color.black;
        }

        return lightsS;
    }

    public NlPointLightS[] GetNonLinearStructs() {
        NlPointLightS[] lightsS = new NlPointLightS[1] {new NlPointLightS()};
        if (nonLinearLights.Count > 0) {
            lightsS = new NlPointLightS[nonLinearLights.Count];

            for (int i = 0; i < nonLinearLights.Count; i++) {
                lightsS[i] = nonLinearLights[i].GetNonLinearStruct();
            }
        } else {
            lightsS[0].layer = 0;
            lightsS[0].pos = new Vector3(1, 0, 0);
            lightsS[0].radius = 0;
            lightsS[0].power = 0;
            lightsS[0].color = Color.black;
            lightsS[0].fallout = 1;
        }

        return lightsS;
    }
}
