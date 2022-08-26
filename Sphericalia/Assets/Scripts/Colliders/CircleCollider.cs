using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCollider
{
    Vector3 center;
    float r;
    public CircleS circleS;

    public CircleCollider(Vector3 center_, float r_, Color c) {
        center = center_;
        r = r_;
        circleS = new CircleS();
        circleS.center = center;
        circleS.r = r;
        circleS.color = c;
    }

    public bool CollidePoint(Vector3 p) {
        return (Mathf.Acos(Vector3.Dot(center, p)) < r);
    }

    public void Update(Vector3 center_, float r_, Color c) {
        center = center_;
        r = r_;
        circleS.center = center;
        circleS.r = r;
        circleS.color = c;
    }
}

public struct CircleS {
    public Vector3 center;
    public float r;
    public Color color;
}
