using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PointLight : MonoBehaviour
{
    public Vector2 sphPosition = new Vector2();
    [Range(0, Mathf.PI)] public float radius = 1;
    [Range(0, 1.5f)] public float power = 1;
    public Color color = Color.white;
    public bool bakedLighting = false;
    [HideInInspector] public int bakingLayer = 0;

    [HideInInspector] public Vector3 position;
    float prevR = 0;

    SphericalUtilities su = new SphericalUtilities();

    void Setup() {
        position = su.Spherical2Cartesian(sphPosition);
    }

    void OnValidate() {Setup();}
    void OnEnable() {Setup();
                     Lighting.lights.Add(this);}
    void OnDisable() {Lighting.lights.Remove(this);}

    void OnDrawGizmos() {
        for (int i = 0; i < 10; i++) {
            Gizmos.color = color * (1.3f - (i*0.1f));
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), radius - (i * 0.02f)));
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(position, radius * 0.2f);
    }

    public void Move(Vector3 target, float angle) {
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.Cross(position, target));
        MoveQ(q);
    }

    public void MoveQ(Quaternion q) {
        position = q * position;
    }

    public void ChangeColor(Color c) {
        color = c;
    }

    public void Toggle() {
        if (radius == 0) {
            radius = prevR;
        } else {
            prevR = radius;
            radius = 0;
        }
    }

    public PointLightS GetStruct() {
        PointLightS pl = new PointLightS();
        pl.pos = position;
        pl.power = power;
        pl.slope = power/radius;
        pl.color = color;
        return pl;
    }
}

public struct PointLightS {
    public Vector3 pos;
    public float power;
    public float slope;
    public Color color;
}
