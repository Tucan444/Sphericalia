using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    public List<SphCircle> sphCircles = new List<SphCircle>();
    public List<SphGon> sphGons = new List<SphGon>();
    public List<SphShape> sphShapes = new List<SphShape>();

    Circle0Comparer o0c = new Circle0Comparer();
    Gon0Comparer g0c = new Gon0Comparer();
    Shape0Comparer s0c = new Shape0Comparer();

    public void Clear() {
        sphCircles = new List<SphCircle>();
        sphGons = new List<SphGon>();
        sphShapes = new List<SphShape>();
    }

    public void SortLists() {
        // sorting lists
        sphCircles.Sort(o0c);
        sphGons.Sort(g0c);
        sphShapes.Sort(s0c);
    }

    public Vector3[] GetLayerVectors(List<int> layers) {
        int[] ti = new int[3];

        Vector3[] layerSplits = new Vector3[layers.Count+1];

        for (int i = 0; i < layers.Count; i++)
        {
            // circles
            while (ti[0] < sphCircles.Count && sphCircles[ti[0]].layer == layers[i]) {
                if (sphCircles[ti[0]].empty || sphCircles[ti[0]].invisible) {ti[0]++; continue;}
                layerSplits[i+1][0]++;

                ti[0]++;
            }

            int[] tqc = new int[2];
            // gons
            while (ti[1] < sphGons.Count && sphGons[ti[1]].layer == layers[i]) {
                if (sphGons[ti[1]].empty || sphGons[ti[1]].invisible) {ti[1]++; continue;}
                tqc[0] += sphGons[ti[1]].collider_.triangles.Length;
                tqc[1] += sphGons[ti[1]].collider_.quads.Length;

                ti[1]++;
            }

            // shapes
            while (ti[2] < sphShapes.Count && sphShapes[ti[2]].layer == layers[i]) {
                if (sphShapes[ti[2]].empty || sphShapes[ti[2]].invisible) {ti[2]++; continue;}
                if (sphShapes[ti[2]].isQuad) { tqc[1]++; } 
                else { tqc[0] += sphShapes[ti[2]].collider_.triangles.Length; }

                ti[2]++;
            }

            layerSplits[i+1][1] = tqc[0];
            layerSplits[i+1][2] = tqc[1];
        }

        for (int i = 1; i < layerSplits.Length; i++) {
            layerSplits[i] = layerSplits[i] + layerSplits[i-1];
        }

        return layerSplits;
    }

    public List<CircleS> GetCircles(List<CircleS> circles, List<int> layers) {
        circles = new List<CircleS>();

        for (int i = 0; i < sphCircles.Count; i++) {
            if (sphCircles[i].empty || sphCircles[i].invisible) {continue;}

            circles.Add(sphCircles[i].collider_.circleS);
        }

        return circles;
    }

    public List<TriangleS> GetTriangles(List<TriangleS> triangles, List<int> layers) {
        triangles = new List<TriangleS>();

        // updating non-static objects
        int[] ti = new int[2] {0, 0};
        for (int i = 0; i < layers.Count; i++)
        {
            // gons
            while (ti[0] < sphGons.Count && sphGons[ti[0]].layer == layers[i]) {
                if (sphGons[ti[0]].empty || sphGons[ti[0]].invisible) {ti[0]++; continue;}

                for (int jj = 0; jj < sphGons[ti[0]].collider_.triangles.Length; jj++)
                {
                    triangles.Add(sphGons[ti[0]].collider_.triangles[jj]);
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapes.Count && sphShapes[ti[1]].layer == layers[i]) {
                if (sphShapes[ti[1]].empty || sphShapes[ti[1]].invisible) {ti[1]++; continue;}

                if (!sphShapes[ti[1]].isQuad) {
                    for (int jj = 0; jj < sphShapes[ti[1]].collider_.triangles.Length; jj++)
                    {
                        triangles.Add(sphShapes[ti[1]].collider_.triangles[jj]);
                    }
                }

                ti[1]++;
            }
        }

        return triangles;
    }

    public List<QuadS> GetQuads(List<QuadS> quads, List<int> layers) {
        quads = new List<QuadS>();

        // updating non-static objects
        int[] ti = new int[2] {0, 0};
        for (int i = 0; i < layers.Count; i++)
        {
            // gons
            while (ti[0] < sphGons.Count && sphGons[ti[0]].layer == layers[i]) {
                if (sphGons[ti[0]].empty || sphGons[ti[0]].invisible) {ti[0]++; continue;}

                for (int jj = 0; jj < sphGons[ti[0]].collider_.quads.Length; jj++)
                {
                    quads.Add(sphGons[ti[0]].collider_.quads[jj]);
                }

                ti[0]++;
            }

            // shapes
            while (ti[1] < sphShapes.Count && sphShapes[ti[1]].layer == layers[i]) {
                if (sphShapes[ti[1]].empty || sphShapes[ti[1]].invisible) {ti[1]++; continue;}

                if (sphShapes[ti[1]].isQuad) {
                    quads.Add(sphShapes[ti[1]].qcollider.q);
                }

                ti[1]++;
            }
        }

        return quads;
    }
}
