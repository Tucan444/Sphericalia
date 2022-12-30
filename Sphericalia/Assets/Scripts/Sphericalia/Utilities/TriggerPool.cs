using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerPool
{
    public List<SphCircle> sphCircles = new List<SphCircle>();
    public List<SphGon> sphGons = new List<SphGon>();
    public List<SphShape> sphShapes = new List<SphShape>();
    // all objects are considered to be triggers
    
    public void Clear() {
        sphCircles = new List<SphCircle>();
        sphGons = new List<SphGon>();
        sphShapes = new List<SphShape>();
    }

    // checks if circle collides with triggers (goes over all triggers)
    public bool CollideTriggerCircle(Vector3 center, float r, bool triggerStuff=true) {
        bool collidedWithTrigger = false;

        for (int j = 0; j < sphCircles.Count; j++) {
            if (sphCircles[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff) {
                    sphCircles[j].triggered = true;
                }
                collidedWithTrigger = true;
            }
        }
        for (int j = 0; j < sphGons.Count; j++) {
            if (sphGons[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff) {
                    sphGons[j].triggered = true;
                }
                collidedWithTrigger = true;
            }
        }
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].isQuad) {
                if (sphShapes[j].qcollider.CollideCircle(center, r)) {
                    if (triggerStuff) {
                        sphShapes[j].triggered = true;
                    }
                    collidedWithTrigger = true;
                }
            } else {
                if (sphShapes[j].collider_.CollideCircle(center, r)) {
                    if (triggerStuff) {
                        sphShapes[j].triggered = true;
                    }
                    collidedWithTrigger = true;
                }
            }
        }
        return collidedWithTrigger;
    }

    // returns length ray has to travel to hit trigger
    public float RayCastTriggers(Vector3 o, Vector3 d) {
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
