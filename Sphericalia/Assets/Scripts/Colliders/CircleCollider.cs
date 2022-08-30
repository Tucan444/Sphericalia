using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleCollider
{
    Vector3 center;
    float r;
    public CircleS circleS;
    bool empty;

    EmptyObjects eo = new EmptyObjects();

    public CircleCollider(Vector3 center_, float r_, Color c, bool empty_=false) {
        center = center_;
        r = r_;
        circleS = new CircleS();
        circleS.center = center;
        circleS.r = r;
        circleS.color = c;

        empty = empty_;

        if (empty) {
            circleS = eo.GetEmptyCircle();
        }
    }

    public bool CollidePoint(Vector3 p) {
        return (Mathf.Acos(Vector3.Dot(center, p)) < r) && !empty;
    }

    public bool CollideCircle(Vector3 center_, float r_) {
        float d = Mathf.Acos(Vector3.Dot(center, center_));
        if (d <= r_ + r) {return true && !empty;} else {return false;}
    }

    public void Update(Vector3 center_, float r_, Color c, bool empty_ = false) {
        center = center_;
        r = r_;
        circleS.center = center;
        circleS.r = r;
        circleS.color = c;
        
        empty = empty_;

        if (empty) {
            circleS = eo.GetEmptyCircle();
        }
    }
}

public struct CircleS {
    public Vector3 center;
    public float r;
    public Color color;
}
