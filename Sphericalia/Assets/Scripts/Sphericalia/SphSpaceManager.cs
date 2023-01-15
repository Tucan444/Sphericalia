using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// main class
public class SphSpaceManager : MonoBehaviour
{
    public bool useObjectPool = false;

    public static SphBg sb;
    public static SphericalCamera sc;
    public static Lighting lighting;
    public static List<SphCircle> sphCircles = new List<SphCircle>();
    public static List<SphGon> sphGons = new List<SphGon>();
    public static List<SphShape> sphShapes = new List<SphShape>();
    [HideInInspector] public List<int> layers = new List<int>();
    [HideInInspector] public bool initialized = false;

    // getting triggers
    [HideInInspector] public List<SphCircle> cTrigger = new List<SphCircle>();
    [HideInInspector] public List<SphGon> gTrigger = new List<SphGon>();
    [HideInInspector] public List<SphShape> sTrigger = new List<SphShape>();

    // getting colliders
    [HideInInspector] public List<SphCircle> circleC = new List<SphCircle>();
    [HideInInspector] public List<SphGon> gonC = new List<SphGon>();
    [HideInInspector] public List<SphShape> shapeC = new List<SphShape>();

    // object pool
    public ObjectPool objectPool = new ObjectPool();

    // arrays for layer ordering
    Vector3[] layerSplits;

    List<CircleS> circles = new List<CircleS>();
    List<TriangleS> triangles = new List<TriangleS>();
    List<QuadS> quads = new List<QuadS>();

    public ComputeShader baseShader;
    public ComputeShader realtimeLightingShader;
    public ComputeShader mixedLightingShader;
    private RenderTexture renderTexture;

    Texture2D black;

    float tau = Mathf.PI * 2;

    EmptyObjects eo = new EmptyObjects();

    // properties
    int ambientLightID = Shader.PropertyToID("ambientLight");
    int gammaID = Shader.PropertyToID("gamma");

    int lightLayersID = Shader.PropertyToID("lightLayers");
    int lightmapID = Shader.PropertyToID("lightmaps");
    int lightmapStepID = Shader.PropertyToID("lightmapStep");
    int lightmapsDepthID = Shader.PropertyToID("lightmapsDepth");

    int lightsID = Shader.PropertyToID("lights");
    int lLengthID = Shader.PropertyToID("lLength");
    int nlLightsID = Shader.PropertyToID("nlLights");
    int nllLengthID = Shader.PropertyToID("nllLength");

    int circlesID = Shader.PropertyToID("circles");
    int trianglesID = Shader.PropertyToID("triangles");
    int quadsID = Shader.PropertyToID("quads");

    int layerNumsID = Shader.PropertyToID("layerNums");
    int layersID = Shader.PropertyToID("layers");
    int layLengthID = Shader.PropertyToID("layLength");

    int resultID = Shader.PropertyToID("Result");
    int bgColorID = Shader.PropertyToID("bgColor");
    int orthoBgID = Shader.PropertyToID("orthoBg");
    int useBgTextureID = Shader.PropertyToID("useBgTexture");
    int bgTextureID = Shader.PropertyToID("bgTexture");
    int bgStepID = Shader.PropertyToID("bgStep");

    int screenQID = Shader.PropertyToID("screenQ");
    int sendRaysID = Shader.PropertyToID("rays");
    int resolutionID = Shader.PropertyToID("resolution");

    // other

    #if UNITY_EDITOR
    void OnEnable() {
        Tools.hidden = true;
    }
    void OnDisable() {
        Tools.hidden = false;
    }
    #endif
    
    // Start is called before the first frame update
    void Start()
    {
        layers.Sort();

        renderTexture = new RenderTexture(sc.resolution[0], sc.resolution[1], 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();

        black = Texture2D.blackTexture;

        SortObjects();
        GetLayerVectors();
        PopulateAll();

        initialized = true;
    }

    void SortObjects() {
        SortColliderTrigger();

        // sorting lists
        Circle0Comparer o0c = new Circle0Comparer();
        Gon0Comparer g0c = new Gon0Comparer();
        Shape0Comparer s0c = new Shape0Comparer();
        sphCircles.Sort(o0c);
        sphGons.Sort(g0c);
        sphShapes.Sort(s0c);
    }

    void GetLayerVectors() {
        int[] ti = new int[3];

        layerSplits = new Vector3[layers.Count+1];

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
    }

    public void InsertLayer(int l) {
        if (layers.Count == 0) { // if there are no layers
            layers.Add(l);
            return;
        }

        int i = (int)((float)layers.Count * 0.5f);
        int s = i;

        while (s > 0) { // hopping till next hop has size of 0
            if (i >= layers.Count) {i = layers.Count - 1;}
            if (i < 0) {i = 0;}
            
            s = (int)(s/2);  // updating s
            int v = layers[i];
            
            if (v == l) {
                return;
            } else if (v < l) {
                i = i + s + 1;
            } else {
                i = i - s - 1;
            }
        }

        // dealing with last value thats closest to l as possible
        if (i == layers.Count) {layers.Add(l); return;}
        if (i == -1) {layers.Insert(0, l);; return;}

        int value = layers[i];
            
        if (value == l) {
            return;
        } else if (value < l) {
            layers.Insert(i+1, l);
        } else {
            layers.Insert(i, l);
        }
    }

    public void InsertCircle(SphCircle c) {
        if (sphCircles.Count == 0) { // if there are no objects
            sphCircles.Add(c);
            return;
        }

        int l = c.layer;
        int i = (int)((float)sphCircles.Count * 0.5f);
        int s = i;

        while (s > 0) { // hopping till next hop has size of 0
            if (i >= sphCircles.Count) {i = sphCircles.Count - 1;}
            if (i < 0) {i = 0;}

            s = (int)(s/2);  // updating s
            int v = sphCircles[i].layer;
            
            if (v == l) {
                sphCircles.Insert(i, c);
                return;
            } else if (v < l) {
                i = i + s + 1;
            } else {
                i = i - s - 1;
            }
        }

        // dealing with last value thats closest to l as possible
        if (i == sphCircles.Count) {sphCircles.Add(c); return;}
        if (i == -1) {sphCircles.Insert(0, c);; return;}

        int value = sphCircles[i].layer;
            
        if (value < l) {
            sphCircles.Insert(i+1, c);
            return;
        }
        sphCircles.Insert(i, c);
    }

    public void InsertGon(SphGon g) {
        if (sphGons.Count == 0) { // if there are no objects
            sphGons.Add(g);
            return;
        }

        int l = g.layer;
        int i = (int)((float)sphGons.Count * 0.5f);
        int s = i;

        while (s > 0) { // hopping till next hop has size of 0
            if (i >= sphGons.Count) {i = sphGons.Count - 1;}
            if (i < 0) {i = 0;}
            
            s = (int)(s/2);  // updating s
            int v = sphGons[i].layer;
            
            if (v == l) {
                sphGons.Insert(i, g);
                return;
            } else if (v < l) {
                i = i + s + 1;
            } else {
                i = i - s - 1;
            }
        }

        // dealing with last value thats closest to l as possible
        if (i == sphGons.Count) {sphGons.Add(g); return;}
        if (i == -1) {sphGons.Insert(0, g);; return;}

        int value = sphGons[i].layer;
            
        if (value < l) {
            sphGons.Insert(i+1, g);
            return;
        }
        sphGons.Insert(i, g);
    }

    public void InsertShape(SphShape shape) {
        if (sphShapes.Count == 0) { // if there are no objects
            sphShapes.Add(shape);
            return;
        }

        int l = shape.layer;
        int i = (int)((float)sphShapes.Count * 0.5f);
        int s = i;

        while (s > 0) { // hopping till next hop has size of 0
            if (i >= sphShapes.Count) {i = sphShapes.Count - 1;}
            if (i < 0) {i = 0;}

            s = (int)(s/2);  // updating s
            int v = sphShapes[i].layer;
            
            if (v == l) {
                sphShapes.Insert(i, shape);
                return;
            } else if (v < l) {
                i = i + s + 1;
            } else {
                i = i - s - 1;
            }
        }

        // dealing with last value thats closest to l as possible
        if (i == sphShapes.Count) {sphShapes.Add(shape); return;}
        if (i == -1) {sphShapes.Insert(0, shape);; return;}

        int value = sphShapes[i].layer;
            
        if (value < l) {
            sphShapes.Insert(i+1, shape);
            return;
        }
        sphShapes.Insert(i, shape);
    }

    // used for lightning (used for baking lighting in editor)
    public List<SphCircle> GetStaticCircles() {
        List<SphCircle> staticCircles = new List<SphCircle>();
        for (int i = 0; i < sphCircles.Count; i++)
        {
            if (sphCircles[i].Static) {
                staticCircles.Add(sphCircles[i]);
            }
        }
        return staticCircles;
    }

    public List<SphGon> GetStaticGons() {
        List<SphGon> staticGons = new List<SphGon>();
        for (int i = 0; i < sphGons.Count; i++)
        {
            if (sphGons[i].Static) {
                staticGons.Add(sphGons[i]);
            }
        }
        return staticGons;
    }

    public List<SphShape> GetStaticShapes() {
        List<SphShape> staticShapes = new List<SphShape>();
        for (int i = 0; i < sphShapes.Count; i++)
        {
            if (sphShapes[i].Static) {
                staticShapes.Add(sphShapes[i]);
            }
        }
        return staticShapes;
    }
    
    public void PopulateAll() { 
        PopulateCircles();

        PopulateTrianglesAndQuads();  
    }

    public void PopulateWithObjectPool() {
        circles = objectPool.GetCircles(circles, layers);
        triangles = objectPool.GetTriangles(triangles, layers);
        quads = objectPool.GetQuads(quads, layers);
    }

    public void PopulateCircles() {
        circles = new List<CircleS>();

        for (int i = 0; i < sphCircles.Count; i++) {
            if (sphCircles[i].empty || sphCircles[i].invisible) {continue;}

            circles.Add(sphCircles[i].collider_.circleS);
        }
    }

    public void PopulateTrianglesAndQuads() {
        triangles = new List<TriangleS>();
        quads = new List<QuadS>();

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
                } else {
                    for (int jj = 0; jj < sphShapes[ti[1]].collider_.triangles.Length; jj++)
                    {
                        triangles.Add(sphShapes[ti[1]].collider_.triangles[jj]);
                    }
                }

                ti[1]++;
            }
        }
    }

    public void PopulateTriangles() {
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
    }

    public void PopulateQuads() {
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
    }

    // functions to use outside of class

    // use when object changed from collider to non collider or trigger to non trigger
    public void SortColliderTrigger() {
        cTrigger = new List<SphCircle>();
        gTrigger = new List<SphGon>();
        sTrigger = new List<SphShape>();

        circleC = new List<SphCircle>();
        gonC = new List<SphGon>();
        shapeC = new List<SphShape>();

        // circles
        for (int j = 0; j < sphCircles.Count; j++) {
            if (sphCircles[j].isCollider) {
                circleC.Add(sphCircles[j]);
            }
            if (sphCircles[j].isTrigger) {
                cTrigger.Add(sphCircles[j]);
            }
        }

        // ngons
        for (int j = 0; j < sphGons.Count; j++) {
            if (sphGons[j].isCollider) {
                gonC.Add(sphGons[j]);
            }
            if (sphGons[j].isTrigger) {
                gTrigger.Add(sphGons[j]);
            }
        }

        // shapes
        for (int j = 0; j < sphShapes.Count; j++) {
            if (sphShapes[j].isCollider) {
                shapeC.Add(sphShapes[j]);
            }
            if (sphShapes[j].isTrigger) {
                sTrigger.Add(sphShapes[j]);
            }
        }
    }

    // checks if circle collides with colliders (returns with first collision found)
    public bool CollideCircle(Vector3 center, float r, bool triggerStuff=false) {
        for (int j = 0; j < circleC.Count; j++) {
            if (circleC[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && circleC[j].isTrigger) {
                    circleC[j].triggered = true;
                }
                return true;
            }
        }
        for (int j = 0; j < gonC.Count; j++) {
            if (gonC[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff && gonC[j].isTrigger) {
                    gonC[j].triggered = true;
                }
                return true;
            }
        }
        for (int j = 0; j < shapeC.Count; j++) {
            if (shapeC[j].isQuad) {
                if (shapeC[j].qcollider.CollideCircle(center, r)) {
                    if (triggerStuff && shapeC[j].isTrigger) {
                        shapeC[j].triggered = true;
                    }
                    return true;
                }
            } else {
                if (shapeC[j].collider_.CollideCircle(center, r)) {
                    if (triggerStuff && shapeC[j].isTrigger) {
                        shapeC[j].triggered = true;
                    }
                    return true;
                }
            }
        }
        return false;
    }

    // checks if circle collides with triggers (goes over all triggers)
    public bool CollideTriggerCircle(Vector3 center, float r, bool triggerStuff=true) {
        bool collidedWithTrigger = false;

        for (int j = 0; j < cTrigger.Count; j++) {
            if (cTrigger[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff) {
                    cTrigger[j].triggered = true;
                }
                collidedWithTrigger = true;
            }
        }
        for (int j = 0; j < gTrigger.Count; j++) {
            if (gTrigger[j].collider_.CollideCircle(center, r)) {
                if (triggerStuff) {
                    gTrigger[j].triggered = true;
                }
                collidedWithTrigger = true;
            }
        }
        for (int j = 0; j < sTrigger.Count; j++) {
            if (sTrigger[j].isQuad) {
                if (sTrigger[j].qcollider.CollideCircle(center, r)) {
                    if (triggerStuff) {
                        sTrigger[j].triggered = true;
                    }
                    collidedWithTrigger = true;
                }
            } else {
                if (sTrigger[j].collider_.CollideCircle(center, r)) {
                    if (triggerStuff) {
                        sTrigger[j].triggered = true;
                    }
                    collidedWithTrigger = true;
                }
            }
        }
        return collidedWithTrigger;
    }

    // returns length ray has to travel to hit collider
    public float RayCastColliders(Vector3 o, Vector3 d) {
        float minT = 10;
        for (int j = 0; j < circleC.Count; j++) {
            float t = circleC[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < gonC.Count; j++) {
            float t = gonC[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < shapeC.Count; j++) {
            if (shapeC[j].isQuad) {
                float t = shapeC[j].qcollider.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            } else {
                float t = shapeC[j].collider_.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            }
        }

        if (minT == 10) {return -1;} else {return minT;}
    }

    // returns length ray has to travel to hit trigger
    public float RayCastTriggers(Vector3 o, Vector3 d) {
        float minT = 10;
        for (int j = 0; j < cTrigger.Count; j++) {
            float t = cTrigger[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < gTrigger.Count; j++) {
            float t = gTrigger[j].collider_.RayCast(o, d);
            if (t != -1) {
                minT = Mathf.Min(minT, t);
            }
        }
        for (int j = 0; j < sTrigger.Count; j++) {
            if (sTrigger[j].isQuad) {
                float t = sTrigger[j].qcollider.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            } else {
                float t = sTrigger[j].collider_.RayCast(o, d);
                if (t != -1) {
                    minT = Mathf.Min(minT, t);
                }
            }
        }

        if (minT == 10) {return -1;} else {return minT;}
    }

    public List<SphCircle> GetTriggeredCircles() {
        List<SphCircle> circles = new List<SphCircle>();
        for (int i = 0; i < cTrigger.Count; i++) {
            if (cTrigger[i].triggered) { circles.Add(cTrigger[i]); }
        }
        return circles;
    }

    public List<SphGon> GetTriggeredGons() {
        List<SphGon> gons = new List<SphGon>();
        for (int i = 0; i < gTrigger.Count; i++) {
            if (gTrigger[i].triggered) { gons.Add(gTrigger[i]); }
        }
        return gons;
    }

    public List<SphShape> GetTriggeredShapes() {
        List<SphShape> shapes = new List<SphShape>();
        for (int i = 0; i < sTrigger.Count; i++) {
            if (sTrigger[i].triggered) { shapes.Add(sTrigger[i]); }
        }
        return shapes;
    }

    public List<UVTiles> GetTriggeredUVTiles() {
        List<UVTiles> uvts = new List<UVTiles>();
        for (int i = 0; i < sTrigger.Count; i++) {
            if (sTrigger[i].triggered) {
                GameObject parent_ = sTrigger[i].transform.parent.gameObject;
                if (parent_.GetComponent<UVTiles>() != null) {
                    UVTiles uvt = parent_.GetComponent<UVTiles>();
                    if (!uvts.Contains(uvt)) {
                        uvts.Add(uvt);
                    }
                }
            }
        }
        return uvts;
    }

    public void PrintLayers() {
        string l = "";
        foreach (var ll in layers)
        {
            l = l + ll.ToString() + ", ";
        }
        Debug.Log(l);
    }

    void ClearTriggered() {
        for (int i = 0; i < cTrigger.Count; i++)
        {
            cTrigger[i].triggered = false;
        }

        for (int i = 0; i < gTrigger.Count; i++)
        {
            gTrigger[i].triggered = false;
        }

        for (int i = 0; i < sTrigger.Count; i++)
        {
            sTrigger[i].triggered = false;
        }
    }

    // end
    // internal functions below

    // Update is called once per frame
    void Update()
    {
        if (useObjectPool) {
            objectPool.SortLists();

            layerSplits = objectPool.GetLayerVectors(layers);
            PopulateWithObjectPool();
            
            objectPool.Clear();
        } else {
            PopulateAll();
            GetLayerVectors();
        }

        ClearTriggered();
    }

    void RenderBaseShader() {

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Count > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Count, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        baseShader.SetBuffer(0, circlesID, circles_buffer);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Count > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Count, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        baseShader.SetBuffer(0, trianglesID, triangles_buffer);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Count > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Count, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        baseShader.SetBuffer(0, quadsID, quads_buffer);

        Debug.Log("Number of circles: " + circles.Count + " Triangles: " + triangles.Count + " Quads: " + quads.Count);

        // sending layers
        ComputeBuffer layers_buffer = new ComputeBuffer(1, sizeof(float)*3); // quads
        layers_buffer.SetData(new Vector3[1] {new Vector3(0, 0, 0)});
        if(layers.Count > 0) {
            layers_buffer.Dispose();
            layers_buffer = new ComputeBuffer(layerSplits.Length, sizeof(float)*3); // quads
            layers_buffer.SetData(layerSplits);
        }
        baseShader.SetBuffer(0, layersID, layers_buffer);
        baseShader.SetInt(layLengthID, layers.Count);

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
        layers_buffer.Dispose();
        rays_buffer.Dispose();
    }

    void RenderRealtimeLightingShader() {
        // setting lighting
        realtimeLightingShader.SetVector(ambientLightID, lighting.ambientLight);
        realtimeLightingShader.SetFloat(gammaID, 1 / ((1.2f * lighting.gammaCorrection) + 1));

        // linear point lights
        PointLightS[] lights = lighting.GetLinearStructs();
        ComputeBuffer lights_buffer = new ComputeBuffer(lights.Length, sizeof(float)*10 + sizeof(int)); // point lights
        lights_buffer.SetData(lights);
        realtimeLightingShader.SetBuffer(0, lightsID, lights_buffer);
        realtimeLightingShader.SetInt(lLengthID, lights.Length);

        // nonLinear point lights
        NlPointLightS[] lights_ = lighting.GetNonLinearStructs();
        ComputeBuffer nlLights_buffer = new ComputeBuffer(lights_.Length, sizeof(float)*9 + sizeof(int)*2); // point lights
        nlLights_buffer.SetData(lights_);
        realtimeLightingShader.SetBuffer(0, nlLightsID, nlLights_buffer);
        realtimeLightingShader.SetInt(nllLengthID, lights_.Length);

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Count > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Count, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        realtimeLightingShader.SetBuffer(0, circlesID, circles_buffer);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Count > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Count, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        realtimeLightingShader.SetBuffer(0, trianglesID, triangles_buffer);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Count > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Count, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        realtimeLightingShader.SetBuffer(0, quadsID, quads_buffer);

        Debug.Log("Number of circles: " + circles.Count + " Triangles: " + triangles.Count + " Quads: " + quads.Count);

        // sending layers
        ComputeBuffer layerNums_buffer = new ComputeBuffer(1, sizeof(int)); 
        layerNums_buffer.SetData(new int[1] {0});

        ComputeBuffer layers_buffer = new ComputeBuffer(1, sizeof(float)*3);
        layers_buffer.SetData(new Vector3[1] {new Vector3(0, 0, 0)});
        if(layers.Count > 0) {
            layers_buffer.Dispose();
            layers_buffer = new ComputeBuffer(layerSplits.Length, sizeof(float)*3); 
            layers_buffer.SetData(layerSplits);

            layerNums_buffer.Dispose();
            layerNums_buffer = new ComputeBuffer(layers.Count, sizeof(int));
            layerNums_buffer.SetData(layers);
        }
        realtimeLightingShader.SetBuffer(0, layersID, layers_buffer);
        realtimeLightingShader.SetBuffer(0, layerNumsID, layerNums_buffer);
        realtimeLightingShader.SetInt(layLengthID, layers.Count);

        // sending bg data
        realtimeLightingShader.SetVector(bgColorID, sb.bgColor);
        realtimeLightingShader.SetVector(orthoBgID, sb.orthoBg);
        if (sb.bgTexture) {
            realtimeLightingShader.SetBool(useBgTextureID, true);
            realtimeLightingShader.SetTexture(0, bgTextureID, sb.bgTexture);
            realtimeLightingShader.SetVector(bgStepID, new Vector2(tau / (float)sb.bgTexture.width, Mathf.PI / (float)sb.bgTexture.height));
        } else {
            realtimeLightingShader.SetBool(useBgTextureID, false);
            realtimeLightingShader.SetTexture(0, bgTextureID, black);
            realtimeLightingShader.SetVector(bgStepID, new Vector2(0, 0));
        }

        // sending rays
        realtimeLightingShader.SetMatrix(screenQID, Matrix4x4.TRS(new Vector3(), sc.screenQ, new Vector3(1, 1, 1)));

        ComputeBuffer rays_buffer = new ComputeBuffer(sc.sendRays.Length, sizeof(float)*3);
        rays_buffer.SetData(sc.sendRays);
        realtimeLightingShader.SetBuffer(0, sendRaysID, rays_buffer);

        realtimeLightingShader.SetInts(resolutionID, sc.resolution);

        // sending texture
        realtimeLightingShader.SetTexture(0, resultID, renderTexture);

        // dispatching
        realtimeLightingShader.Dispatch(0, sc.resolution[0] / 32, sc.resolution[1] / 32, 1);

        // disposing of buffers
        lights_buffer.Dispose();
        nlLights_buffer.Dispose();
        circles_buffer.Dispose();
        triangles_buffer.Dispose();
        quads_buffer.Dispose();
        layerNums_buffer.Dispose();
        layers_buffer.Dispose();
        rays_buffer.Dispose();
    }

    void RenderMixedLightingShader() {
        // setting lighting
        mixedLightingShader.SetVector(ambientLightID, lighting.ambientLight);
        mixedLightingShader.SetFloat(gammaID, 1 / ((1.2f * lighting.gammaCorrection) + 1));

        // lightmap
        ComputeBuffer lightLayers_buffer = new ComputeBuffer(lighting.lightLayers.Count, sizeof(int));
        lightLayers_buffer.SetData(lighting.lightLayers);
        mixedLightingShader.SetBuffer(0, lightLayersID, lightLayers_buffer);
        mixedLightingShader.SetTexture(0, lightmapID, lighting.lightmaps);
        mixedLightingShader.SetVector(lightmapStepID, lighting.lightmapStep);
        mixedLightingShader.SetInt(lightmapsDepthID, lighting.lightmaps.depth);

        // linear point lights
        PointLightS[] lights = lighting.GetLinearStructs();
        ComputeBuffer lights_buffer = new ComputeBuffer(lights.Length, sizeof(float)*10 + sizeof(int)); // point lights
        lights_buffer.SetData(lights);
        mixedLightingShader.SetBuffer(0, lightsID, lights_buffer);
        mixedLightingShader.SetInt(lLengthID, lights.Length);

        // nonLinear point lights
        NlPointLightS[] lights_ = lighting.GetNonLinearStructs();
        ComputeBuffer nlLights_buffer = new ComputeBuffer(lights_.Length, sizeof(float)*9 + sizeof(int)*2); // point lights
        nlLights_buffer.SetData(lights_);
        mixedLightingShader.SetBuffer(0, nlLightsID, nlLights_buffer);
        mixedLightingShader.SetInt(nllLengthID, lights_.Length);

        // sending objects
        // circles
        ComputeBuffer circles_buffer = new ComputeBuffer(1, sizeof(float)*8); // circles
        circles_buffer.SetData(new CircleS[1] {eo.GetEmptyCircle()});
        if (circles.Count > 0) {
            circles_buffer.Dispose();
            circles_buffer = new ComputeBuffer(circles.Count, sizeof(float)*8); // circles
            circles_buffer.SetData(circles);
        }
        mixedLightingShader.SetBuffer(0, circlesID, circles_buffer);

        // triangles
        ComputeBuffer triangles_buffer = new ComputeBuffer(1, sizeof(float)*22); // triangles
        triangles_buffer.SetData(new TriangleS[1] {eo.GetEmptyTriangle()});
        if (triangles.Count > 0) {
            triangles_buffer.Dispose();
            triangles_buffer = new ComputeBuffer(triangles.Count, sizeof(float)*22); // triangles
            triangles_buffer.SetData(triangles);
        }
        mixedLightingShader.SetBuffer(0, trianglesID, triangles_buffer);

        // quads
        ComputeBuffer quads_buffer = new ComputeBuffer(1, sizeof(float)*28); // quads
        quads_buffer.SetData(new QuadS[1] {eo.GetEmptyQuad()});
        if(quads.Count > 0) {
            quads_buffer.Dispose();
            quads_buffer = new ComputeBuffer(quads.Count, sizeof(float)*28); // quads
            quads_buffer.SetData(quads);
        }
        mixedLightingShader.SetBuffer(0, quadsID, quads_buffer);

        Debug.Log("Number of circles: " + circles.Count + " Triangles: " + triangles.Count + " Quads: " + quads.Count);

        // sending layers
        ComputeBuffer layerNums_buffer = new ComputeBuffer(1, sizeof(int)); 
        layerNums_buffer.SetData(new int[1] {0});

        ComputeBuffer layers_buffer = new ComputeBuffer(1, sizeof(float)*3);
        layers_buffer.SetData(new Vector3[1] {new Vector3(0, 0, 0)});
        if(layers.Count > 0) {
            layers_buffer.Dispose();
            layers_buffer = new ComputeBuffer(layerSplits.Length, sizeof(float)*3); 
            layers_buffer.SetData(layerSplits);

            layerNums_buffer.Dispose();
            layerNums_buffer = new ComputeBuffer(layers.Count, sizeof(int));
            layerNums_buffer.SetData(layers);
        }
        mixedLightingShader.SetBuffer(0, layersID, layers_buffer);
        mixedLightingShader.SetBuffer(0, layerNumsID, layerNums_buffer);
        mixedLightingShader.SetInt(layLengthID, layers.Count);

        // sending bg data
        mixedLightingShader.SetVector(bgColorID, sb.bgColor);
        mixedLightingShader.SetVector(orthoBgID, sb.orthoBg);
        if (sb.bgTexture) {
            mixedLightingShader.SetBool(useBgTextureID, true);
            mixedLightingShader.SetTexture(0, bgTextureID, sb.bgTexture);
            mixedLightingShader.SetVector(bgStepID, new Vector2(tau / (float)sb.bgTexture.width, Mathf.PI / (float)sb.bgTexture.height));
        } else {
            mixedLightingShader.SetBool(useBgTextureID, false);
            mixedLightingShader.SetTexture(0, bgTextureID, black);
            mixedLightingShader.SetVector(bgStepID, new Vector2(0, 0));
        }

        // sending rays
        mixedLightingShader.SetMatrix(screenQID, Matrix4x4.TRS(new Vector3(), sc.screenQ, new Vector3(1, 1, 1)));

        ComputeBuffer rays_buffer = new ComputeBuffer(sc.sendRays.Length, sizeof(float)*3);
        rays_buffer.SetData(sc.sendRays);
        mixedLightingShader.SetBuffer(0, sendRaysID, rays_buffer);

        mixedLightingShader.SetInts(resolutionID, sc.resolution);

        // sending texture
        mixedLightingShader.SetTexture(0, resultID, renderTexture);

        // dispatching
        mixedLightingShader.Dispatch(0, sc.resolution[0] / 32, sc.resolution[1] / 32, 1);

        // disposing of buffers
        lightLayers_buffer.Dispose();
        lights_buffer.Dispose();
        nlLights_buffer.Dispose();
        circles_buffer.Dispose();
        triangles_buffer.Dispose();
        quads_buffer.Dispose();
        layerNums_buffer.Dispose();
        layers_buffer.Dispose();
        rays_buffer.Dispose();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!lighting.useLighting) {
            RenderBaseShader();
        } else {
            if (!lighting.useBakedLighting) {
                RenderRealtimeLightingShader();
            } else {RenderMixedLightingShader();}
        }

        renderTexture = PostProcessing(renderTexture);
        Graphics.Blit(renderTexture, dest);
    }

    private RenderTexture PostProcessing(RenderTexture image) {
        return image;
    }
}
