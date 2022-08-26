using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class SphericalUtilities : SphericalAdder{

    public float SphDistance(Vector3 a, Vector3 b) {
        return Mathf.Acos(Vector3.Dot(a, b));
    }
    
    public Vector3 SphLerp(Vector3 a, Vector3 b, float t) {
        float d = Mathf.Acos(Vector3.Dot(a, b)) * t;
        Quaternion q = Quaternion.AngleAxis(Rad2Deg * d, Vector3.Cross(a, b));
        return q * a;
    }

    // calculates default normal for spherical coordinates
    public Vector3 GetDirection(Vector3 v) { // working
        return (new Vector3(0, 1.0f / Vector3.Dot(new Vector3(0, 1, 0), v), 0) - v).normalized;
    }

    // returns points on line
    public Vector3[] GetLinePoints(Vector3 v0, Vector3 v1, int points_n=20, bool shortest_path=true) {
        float length = Mathf.Acos(Vector3.Dot(v0, v1));
        if (shortest_path == false) {
            length = -(Mathf.PI * 2) + length;
        }
        
        Quaternion q = Quaternion.AngleAxis(-(length * (180.0f / Mathf.PI)) / ((float)points_n - 1),
                                             Vector3.Cross(v1, v0));
        Quaternion newQ = q;
        Vector3[] points = new Vector3[points_n];

        points[0] = v0;
        for (int n = 0; n < points_n-1; n++) {
            points[n+1] = newQ * v0;
            newQ = newQ * q;
        }

        return points;
    }

    // returns points on circle
    public Vector3[] GetCirclePoints(Vector2 center, float r, int points_n=20) {
        Vector3 v1 = Spherical2Cartesian(center);
        Vector3 v2 = Spherical2Cartesian(new Vector2(center.x, center.y + 0.1f));

        Quaternion q = Quaternion.AngleAxis(r * Rad2Deg, Vector3.Cross(v1, v2));
        
        Vector3 sample = q * v1;

        q = Quaternion.AngleAxis(360.0f / points_n, v1);
        Quaternion iter = Quaternion.identity;

        Vector3[] points = new Vector3[points_n];

        for (int i = 0; i < points_n; i++) {
            points[i] = iter * sample;
            iter = q * iter;
        }

        return points;
    }

    // Functions made for unconvex shapes

    // return vector thats tangent to space pointing from a to b in the shortest path
    public Vector3 GetPlaneVector(Vector3 a, Vector3 b) { // working

        // I HAVE NO IDEA WHY THESE 3 LINES DONT WORK

        float angle = Mathf.Acos(Vector3.Dot(a, (a - b).normalized));
        Quaternion q = Quaternion.AngleAxis(((Mathf.PI * 0.5f) - angle) * Rad2Deg, Vector3.Cross(b, a)); // radians !!!!!!!
        return (q * (b-a)).normalized;

        // these work
        /* float angle = Mathf.Acos(Vector3.Dot(a, b));
        Vector3 v = (b * (1.0f / Mathf.Cos(angle))) - a;
        return v.normalized; */
    }

    // returns smaller angle on point b (returns positive value)
    public float GetAngleBetween(Vector3 a, Vector3 b, Vector3 c) { // working
        return Mathf.Acos(Vector3.Dot(GetPlaneVector(b, a), GetPlaneVector(b, c)));
    }

    // check if convexity was interupted
    public bool CheckConvexity(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { // working *
        Vector3[] planeVectors = new Vector3[4] {GetPlaneVector(b, a), GetPlaneVector(b, c), GetPlaneVector(c, b), GetPlaneVector(c, d)};
        Vector3 normal = Quaternion.AngleAxis(90, b) * planeVectors[1];
        Quaternion q = Quaternion.AngleAxis(-Mathf.Acos(Vector3.Dot(b, c)) * Rad2Deg, Vector3.Cross(b, c));

        planeVectors[2] = q * planeVectors[2];
        planeVectors[3] = q * planeVectors[3];

        float det1 = Vector3.Dot((planeVectors[0] + planeVectors[1]).normalized, normal);
        float det2 = Vector3.Dot((planeVectors[2] + planeVectors[3]).normalized, normal);

        if (((det1 < 0) && (det2 < 0)) || ((det1 > 0) && (det2 > 0))) {
            return true;
        }
        return false;

    }

    // gets angles for all points
    public float[] GetAngles(Vector3[] points) {

        // filtering 180 angles and getting angles
        List<int> usefullPoints = new List<int>();
        float[] angles = new float[points.Length];

        for (int i = 0; i < points.Length; i++) {
            int[] ii = new int[3] {i, (i+1) % points.Length, (i+2) % points.Length};
            float nextAngle = GetAngleBetween(points[ii[0]], points[ii[1]], points[ii[2]]);
            angles[ii[1]] = nextAngle;
            if (Mathf.Abs(nextAngle - Mathf.PI) > 0.0001f) {
                usefullPoints.Add(ii[1]);
            }
        }

        // flagging unconvexity
        bool convexity = true;
        for (int i = 0; i < usefullPoints.Count; i++) {
            // doing prep work
            int[] ii = new int[2] {i, (i+1) % usefullPoints.Count};

            int[] ids = new int[4] {usefullPoints[ii[0]] - 1, usefullPoints[ii[0]], usefullPoints[ii[1]], (usefullPoints[ii[1]]+1) % points.Length};
            if (ids[0] == -1) {ids[0] = points.Length - 1;}

            // checking convexivity
            if (!CheckConvexity(points[ids[0]], points[ids[1]], points[ids[2]], points[ids[3]])) {
                convexity = !convexity;
            }


            if (convexity == false) {
                angles[ids[2]] = TAU - angles[ids[2]];
            }
        }

        // finding smaller area
        float[] sums = new float[2];
        for (int i = 0; i < points.Length; i++) {
            sums[0] += angles[i];
            sums[1] += TAU - angles[i];
        }

        if (sums[0] > sums[1]) {
            for (int i = 0; i < points.Length; i++) {
                angles[i] = TAU - angles[i];
            }
        }

        return angles;
    }

    // returns vector pointing from b to inside of shape
    public Vector3 GetInsetVector(Vector3 a, Vector3 b, Vector3 c, float angle) { // working
        Vector3[] planeVectors = new Vector3[2] {GetPlaneVector(b, a), GetPlaneVector(b, c)};
        Vector3 v = (planeVectors[0] + planeVectors[1]).normalized;
        if (angle > Mathf.PI) {
            v *= -1;
        }
        return v;
    }

    // gets inset version of shape
    public Vector3[] GetInsetShape(Vector3[] points, float[] angles, float d) {

        // getting inset vectors and flagging 180 angles
        Vector3[] insetVectors = new Vector3[points.Length];
        int working = 0;

        for (int i = 0; i < points.Length; i++) {
            int[] ii = new int[3] {i, (i+1) % points.Length, (i+2) % points.Length};
            if (angles[ii[1]] - Mathf.PI > 0.001f) {
                insetVectors[ii[1]] = new Vector3(10, 0, 0);
            } else {
                insetVectors[ii[1]] = GetInsetVector(points[ii[0]], points[ii[1]], points[ii[2]], angles[ii[1]]);
                working = ii[1];
            }
        }

        // getting inset vectors for 180 ones
        for (int i = 0; i < points.Length; i++) {
            int[] j = new int[3] {(i + working) % points.Length, (i+1 + working) % points.Length, (i+2 + working) % points.Length};
            if (insetVectors[j[1]].x == 10) {
                insetVectors[j[1]] = GetInsetVector(points[j[0]], points[j[1]], points[j[2]], angles[j[1]] - (0.5f * Mathf.PI));
                if (Vector3.Dot(insetVectors[j[0]], insetVectors[j[1]]) < 0) {
                    insetVectors[j[1]] *= -1;
                }
            }
        }

        // getting new points
        Vector3[] newPoints = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) {
            newPoints[i] = points[i] + (d * insetVectors[i]);
        }
        return newPoints;
    }

    // gizmos functions
    public void GizmosDrawLine(Vector3 a, Vector3 b, int n=4, bool short_=true) {
        Vector3[] points = this.GetLinePoints(a, b, n, short_);
        for (int i = 0; i < n-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    public void GizmosDrawPoints(Vector3[] points) {
        for (int i = 0; i < points.Length; i++) {
            Gizmos.DrawLine(points[i], points[(i+1) % points.Length]);
        }
    }
    
    public void GizmosDrawPointsNoLoop(Vector3[] points) {
        for (int i = 0; i < points.Length-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    public void GizmosDrawNGon(Vector2[] points, Vector2 spherPos, Color color, int rings = 3, float brighten = 1.4f) {
        float multip = 1;
        for (int k = 0; k < rings; k++) {
            Vector3[] newP = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) {
                newP[i] = Polar2Cartesian(AddPolarSpher(new Vector2(points[i][0] * multip, points[i][1]), spherPos));
            }

            Gizmos.color = brighten * color * multip * multip;

            for (int i = 0; i < points.Length; i++) {
                GizmosDrawLine(newP[i], newP[(i+1)%newP.Length]);
            }

            multip -= 0.1f;
        }
    }

    public void GizmosDrawShape(Vector3[] points, float[] angles, float d, float scale, Color color) {
        Gizmos.color = color * 1.4f;
        GizmosDrawPoints(points);
        Gizmos.color = color * 1.2f;
        GizmosDrawPoints(GetInsetShape(points, angles, d * scale));
        Gizmos.color = color;
        GizmosDrawPoints(GetInsetShape(points, angles, (2*d) * scale));
    }

    // handles functions
    public void HandlesDrawPoints(Vector3[] points) {
        for (int i = 0; i < points.Length; i++) {
            Handles.DrawLine(points[i], points[(i+1) % points.Length]);
        }
    }

    public void HandlesDrawLine(Vector3 a, Vector3 b, int n=4, bool short_=true) {
        Vector3[] points = this.GetLinePoints(a, b, n, short_);
        for (int i = 0; i < n-1; i++) {
            Handles.DrawLine(points[i], points[i+1]);
        }
    }

    public void HandlesDrawNGon(Vector2[] points, Vector2 spherPos, Color color, int rings = 3, float brighten = 1.4f) {
        float multip = 1;
        for (int k = 0; k < rings; k++) {
            Vector3[] newP = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) {
                newP[i] = Polar2Cartesian(AddPolarSpher(new Vector2(points[i][0] * multip, points[i][1]), spherPos));
            }

            Handles.color = brighten * color * multip * multip;

            for (int i = 0; i < points.Length; i++) {
                HandlesDrawLine(newP[i], newP[(i+1)%newP.Length]);
            }

            multip -= 0.1f;
        }
    }
}


public class Spherical_utilities : MonoBehaviour{

    Vector3 v0 = new Vector3(1, 0, 1);
    Vector3 v1 = new Vector3(1, 4, -2);

    Vector3 v2 = new Vector3(1, 1, 0.6f);
    Vector3 v3 = new Vector3(1, 3, 0.2f);

    SphericalUtilities su = new SphericalUtilities();

    public void Start() {
    }
}
