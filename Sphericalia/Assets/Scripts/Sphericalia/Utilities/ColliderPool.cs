using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderPool
{
    public List<SphCircle> sphCircles = new List<SphCircle>();
    public List<SphGon> sphGons = new List<SphGon>();
    public List<SphShape> sphShapes = new List<SphShape>();
    // all objects are considered to be colliders
    
    public void Clear() {
        sphCircles = new List<SphCircle>();
        sphGons = new List<SphGon>();
        sphShapes = new List<SphShape>();
    }

    // checks if circle collides with colliders (returns with first collision found)
    public bool CollideCircle(Vector3 center, float r, bool triggerStuff=false) {
        for (int j = 0; j < sphCircles.Count; j++) {
            if (sphCircles[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && sphCircles[j].isTrigger) {
                    sphCircles[j].triggered = true;
                }
                return true;
            }
        }
        for (int j = 0; j < sphGons.Count; j++) {
            if (sphGons[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && sphGons[j].isTrigger) {
                    sphGons[j].triggered = true;
                }
                return true;
            }
        }
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].isQuad) {
                if (sphShapes[j].qcollider.CollideCircle(center, r)) {
                    if (triggerStuff && sphShapes[j].isTrigger) {
                        sphShapes[j].triggered = true;
                    }
                    return true;
                }
            } else {
                if (sphShapes[j].collider_.CollideCircle(center, r)) {
                    if (triggerStuff && sphShapes[j].isTrigger) {
                        sphShapes[j].triggered = true;
                    }
                    return true;
                }
            }
        }
        return false;
    }

    // returns length ray has to travel to hit collider
    public float RayCast(Vector3 o, Vector3 d) {
        float minT = 10;
        for (int j = 0; j < sphCircles.Count; j++) {
            float t = sphCircles[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < sphGons.Count; j++) {
            float t = sphGons[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].isQuad) {
                float t = sphShapes[j].qcollider.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            } else {
                float t = sphShapes[j].collider_.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            }
        }

        if (minT == 10) {return -1;} else {return minT;}
    }
}
