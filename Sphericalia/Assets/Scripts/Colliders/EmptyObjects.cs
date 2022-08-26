using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyObjects
{
    SphericalUtilities su = new SphericalUtilities();
    public CircleS GetEmptyCircle() {
        CircleS circle = new CircleS();
        circle.center = new Vector3(1, 0, 0);
        circle.r = 0;
        circle.color = Color.gray;
        return circle;
    }

    public TriangleS GetEmptyTriangle() {
        Vector3[] points = su.GetCirclePoints(new Vector3(1, 0, 0), 0.00001f, 3);
        ConvexCollider c = new ConvexCollider(points, Color.gray);
        TriangleS t = c.triangles[0];
        t.a += new Vector3(10, 0, 0); t.b += new Vector3(10, 0, 0); t.c += new Vector3(10, 0, 0);
        t.midAB += new Vector3(10, 0, 0); t.midBC += new Vector3(10, 0, 0); t.midCA += new Vector3(10, 0, 0);
        return t;
    }

    public QuadS GetEmptyQuad() {
        Vector3[] points = su.GetCirclePoints(new Vector3(1, 0, 0), 0.00001f, 4);
        ConvexCollider c = new ConvexCollider(points, Color.gray);
        QuadS q = c.quads[0];
        q.a += new Vector3(10, 0, 0); q.b += new Vector3(10, 0, 0); q.c += new Vector3(10, 0, 0); q.d += new Vector3(10, 0, 0);
        q.midAB += new Vector3(10, 0, 0); q.midBC += new Vector3(10, 0, 0); q.midCD += new Vector3(10, 0, 0); q.midDA += new Vector3(10, 0, 0);
        return q;
    }
}
