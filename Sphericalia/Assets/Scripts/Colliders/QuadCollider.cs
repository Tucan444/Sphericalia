using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadCollider
{
    Vector3 center;
    Vector3[] points;
    Vector3[] normals;
    Vector3[] mids;
    Color c;

    public QuadS q;

    bool empty = false;

    SphericalUtilities su = new SphericalUtilities();
    EmptyObjects eo = new EmptyObjects();

    public QuadCollider(Vector3[] verts, Color c_, bool empty_=false) {
        c = c_;
        points = (Vector3[])verts.Clone();
        normals = new Vector3[points.Length];
        mids = new Vector3[points.Length];
        center = ComputeCenter(points);

        ComputeNormalsAndMids();
        CreateQuad();

        empty = empty_;
        if (empty) {q = eo.GetEmptyQuad();}
    }

    public void Update(Vector3[] verts, Color c_) {
        empty = false;
        c = c_;
        points = (Vector3[])verts.Clone();
        normals = new Vector3[points.Length];
        mids = new Vector3[points.Length];
        center = ComputeCenter(points);

        ComputeNormalsAndMids();
        CreateQuad();
    }

    public void MoveRotate(Quaternion qua) {
        for (int i = 0; i < points.Length; i++) {
            points[i] = qua * points[i];
            normals[i] = qua * normals[i];
            mids[i] = qua * mids[i];
            center = qua * center;

            CreateQuad();
        }
    }

    public void ChangeColor(Color c_) {
        c = c_;
        q.color = c;
    }

    Vector3 ComputeCenter(Vector3[] points_) {
        Vector3 center_ = new Vector3();
        for (int i = 0; i < points_.Length; i++) {
            center_ += points_[i];
        }
        center_ = center_.normalized;
        return center_;
    }

    void ComputeNormalsAndMids() {
        for (int i = 0; i < points.Length; i++) {
            int ii = (i+1) % points.Length;

            mids[i] = (points[i] + points[ii]).normalized;

            normals[i] = Vector3.Cross(mids[i], (points[ii] - points[i]).normalized);

            if (Vector3.Dot(normals[i], (mids[i] - center)) < 0) {
                normals[i] *= -1;
            }
        }
    }

    // all things must be calculated first
    void CreateQuad() {
        q = new QuadS();
        q.color = c;

        q.a = normals[0]; q.b = normals[1]; q.c = normals[2]; q.d = normals[3];
        q.midAB = mids[0]; q.midBC = mids[1]; q.midCD = mids[2]; q.midDA = mids[3];
    }

    public bool CollidePoint(Vector3 p) {
        if (empty) {return false;}

        for (int i = 0; i < points.Length; i++) {
            if (Vector3.Dot(normals[i], p - mids[i]) > 0) {
                return false;
            }
        }

        return true;
    }

    public bool CollideCircle(Vector3 center, float r) {
        if (empty) {return false;}
        if (CollidePoint(center)) {return true;}

        for (int i = 0; i < points.Length; i++) {
            if(su.CircleLineCollision(center, r, points[i], points[(i+1) % points.Length])) {return true;}
        }

        return false;
    }
}

public struct QuadS {
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;
    public Vector3 d;

    public Vector3 midAB;
    public Vector3 midBC;
    public Vector3 midCD;
    public Vector3 midDA;

    public Color color;
}
