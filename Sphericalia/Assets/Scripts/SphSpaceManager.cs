using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SphSpaceManager : MonoBehaviour
{
    public bool useChunks = false;

    public static SphBg sb;
    public static SphericalCamera sc;
    public static List<SphCircle> sphCircles = new List<SphCircle>();
    public static List<SphGon> sphGons = new List<SphGon>();
    public static List<SphShape> sphShapes = new List<SphShape>();
    
    // sorting by static
    List<SphCircle> sphCirclesS = new List<SphCircle>();
    List<SphGon> sphGonsS = new List<SphGon>();
    List<SphShape> sphShapesS = new List<SphShape>();

    int[] staticSplit = new int[6];
    CircleS[] circles;
    TriangleS[] triangles;
    QuadS[] quads;

    public ComputeShader baseShader;
    private RenderTexture renderTexture;

    Texture2D black;

    float tau = Mathf.PI * 2;

    EmptyObjects eo = new EmptyObjects();

    // properties

    int circlesID = Shader.PropertyToID("circles");
    int clengthID = Shader.PropertyToID("cLength");

    int trianglesID = Shader.PropertyToID("triangles");
    int tlengthID = Shader.PropertyToID("tLength");

    int quadsID = Shader.PropertyToID("quads");
    int qlengthID = Shader.PropertyToID("qLength");

    int resultID = Shader.PropertyToID("Result");
    int bgColorID = Shader.PropertyToID("bgColor");
    int orthoBgID = Shader.PropertyToID("orthoBg");
    int useBgTextureID = Shader.PropertyToID("useBgTexture");
    int bgTextureID = Shader.PropertyToID("bgTexture");
    int bgStepID = Shader.PropertyToID("bgStep");

    int screenQID = Shader.PropertyToID("screenQ");
    int sendRaysID = Shader.PropertyToID("rays");
    int resolutionID = Shader.PropertyToID("resolution");

    void OnEnable() {
        Tools.hidden = true;
    }
    void OnDisable() {
        Tools.hidden = false;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        renderTexture = new RenderTexture(sc.resolution[0], sc.resolution[1], 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        black = Texture2D.blackTexture;

        SortObjects();

        // setting circles
        circles = new CircleS[sphCircles.Count];
        for (int i = 0; i < sphCirclesS.Count; i++) {
            circles[i] = sphCirclesS[i].collider_.circleS;
        }

        // setting ngons & shapes - triangles and quads
        int tcount = 0;
        int qcount = 0;
        for (int i = 0; i < sphGons.Count; i++)
        {
            tcount += sphGons[i].collider_.triangles.Length;
            qcount += sphGons[i].collider_.quads.Length;
        }
        for (int i = 0; i < sphShapes.Count; i++) {
            if (sphShapes[i].isQuad) {
                qcount++;
            } else {
                tcount += sphShapes[i].collider_.triangles.Length;
            }
        }

        triangles = new TriangleS[tcount];
        quads = new QuadS[qcount];

        int processedT = 0;
        int processedQ = 0;
        for (int j = 0; j < sphGonsS.Count; j++){
            for (int jj = 0; jj < sphGonsS[j].collider_.triangles.Length; jj++)
            {
                triangles[processedT] = sphGonsS[j].collider_.triangles[jj];
                processedT++;
            }
            for (int jj = 0; jj < sphGonsS[j].collider_.quads.Length; jj++)
            {
                quads[processedQ] = sphGonsS[j].collider_.quads[jj];
                processedQ++;
            }
        }

        for (int j = 0; j < sphShapesS.Count; j++) {
            if (sphShapesS[j].isQuad) {
                quads[processedQ] = sphShapesS[j].qcollider.q;
                processedQ++;
            } else {
                for (int jj = 0; jj < sphShapesS[j].collider_.triangles.Length; jj++)
                {
                    triangles[processedT] = sphShapesS[j].collider_.triangles[jj];
                    processedT++;
                }
            }
        } 

    }

    void SortObjects() {
        // circles
        for (int j = 0; j < sphCircles.Count; j++) {
            if (sphCircles[j].Static) {
                sphCirclesS.Add(sphCircles[j]);
                staticSplit[0] += 1; // for circles
                staticSplit[3]++; // for objects
            }
        }

        for (int j = 0; j < sphCircles.Count; j++) {
            if (!sphCircles[j].Static) {
                sphCirclesS.Add(sphCircles[j]);
            }
        }

        // ngons
        for (int j = 0; j < sphGons.Count; j++) {
            if (sphGons[j].Static) {
                sphGonsS.Add(sphGons[j]);
                staticSplit[4]++; // for objects
                staticSplit[1] += sphGons[j].collider_.triangles.Length;
                staticSplit[2] += sphGons[j].collider_.quads.Length;
            }
        }

        for (int j = 0; j < sphGons.Count; j++) {
            if (!sphGons[j].Static) {
                sphGonsS.Add(sphGons[j]);
            }
        }

        // shapes
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].Static) {
                sphShapesS.Add(sphShapes[j]);
                staticSplit[5]++; // for objects
                if (sphShapes[j].isQuad) { // for quads
                    staticSplit[2] += 1;
                } else { // for triangles
                    staticSplit[1] += sphShapes[j].collider_.triangles.Length;
                }
            }
        }

        for (int j = 0; j < sphShapes.Count; j++) {
            if (!sphShapes[j].Static) {
                sphShapesS.Add(sphShapes[j]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // updating non-static objects
        int acount = staticSplit[0];
        for (int i = staticSplit[3]; i < sphCirclesS.Count; i++) {  // circles
            circles[acount] = sphCirclesS[i].collider_.circleS;
            acount++;
        }

        acount = staticSplit[1];
        int bcount = staticSplit[2];
        for (int i = staticSplit[4]; i < sphGonsS.Count; i++) {  // sphgons
            for (int j = 0; j < sphGonsS[i].collider_.triangles.Length; j++) {
                triangles[acount] = sphGonsS[i].collider_.triangles[j];
                acount++;
            }
            for (int j = 0; j < sphGonsS[i].collider_.quads.Length; j++) {
                quads[bcount] = sphGonsS[i].collider_.quads[j];
                bcount++;
            }
        }

        for (int i = staticSplit[5]; i < sphShapesS.Count; i++) {  // sphshapes
            if (sphShapesS[i].isQuad) {
                quads[bcount] = sphShapesS[i].qcollider.q;
                bcount++;
            } else {
                for (int j = 0; j < sphShapesS[i].collider_.triangles.Length; j++) {
                    triangles[acount] = sphShapesS[i].collider_.triangles[j];
                    acount++;
                }
            }
        }

    }

     void RenderBaseShader() {

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Length > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Length, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        baseShader.SetBuffer(0, circlesID, circles_buffer);
        baseShader.SetInt(clengthID, circles.Length);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Length > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Length, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        baseShader.SetBuffer(0, trianglesID, triangles_buffer);
        baseShader.SetInt(tlengthID, triangles.Length);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Length > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Length, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        baseShader.SetBuffer(0, quadsID, quads_buffer);
        baseShader.SetInt(qlengthID, quads.Length);

        Debug.Log("Number of circles: " + circles.Length + " Triangles: " + triangles.Length + " Quads: " + quads.Length);

        // sending bg data
        baseShader.SetVector(bgColorID, sb.bgColor);
        baseShader.SetVector(orthoBgID, sb.orthoBg);
        if (sb.bgTexture) {
            baseShader.SetBool(useBgTextureID, true);
            baseShader.SetTexture(0, bgTextureID, sb.bgTexture);
            baseShader.SetVector(bgStepID, new Vector2(tau / (float)sb.bgTexture.width, Mathf.PI / (float)sb.bgTexture.height));
        } else {
            baseShader.SetBool(useBgTextureID, false);
            baseShader.SetTexture(0, bgTextureID, black);
            baseShader.SetVector(bgStepID, new Vector2(0, 0));
        }

        // sending rays
        baseShader.SetMatrix(screenQID, Matrix4x4.TRS(new Vector3(), sc.screenQ, new Vector3(1, 1, 1)));

        ComputeBuffer rays_buffer = new ComputeBuffer(sc.sendRays.Length, sizeof(float)*3);
        rays_buffer.SetData(sc.sendRays);
        baseShader.SetBuffer(0, sendRaysID, rays_buffer);

        baseShader.SetInts(resolutionID, sc.resolution);

        // sending texture
        baseShader.SetTexture(0, resultID, renderTexture);

        // dispatching
        baseShader.Dispatch(0, sc.resolution[0] / 32, sc.resolution[1] / 32, 1);

        // disposing of buffers
        circles_buffer.Dispose();
        triangles_buffer.Dispose();
        quads_buffer.Dispose();
        rays_buffer.Dispose();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RenderBaseShader();
        Graphics.Blit(renderTexture, dest);
    }
}
