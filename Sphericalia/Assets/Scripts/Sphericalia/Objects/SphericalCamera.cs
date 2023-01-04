using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalCamera : MonoBehaviour
{
    public enum Projection {
        Stereographic, Gnomic, Orthographic, Polar, Equirectangular, Gue, InfinitePolar
    };
    public Projection projection = Projection.Stereographic;
    public int[] resolution = new int[2] {512, 320};
    public Vector2 sphericalPosition = new Vector2();
    [SerializeField][Range(-Mathf.PI, Mathf.PI)]public float directionRotation = 0;
    [SerializeField] public float width = 2.6f;
    public bool pinScreenToPlayer = false;
    public bool noWidthControl = false;
    public RigidbodySpherical rigidbody;

    float height;

    [HideInInspector] public Vector3 position = new Vector3(1, 0, 0);
    Vector3 direction = new Vector3(0, 1, 0);
    [HideInInspector] public Quaternion screenQ = Quaternion.identity;
    Quaternion Q = Quaternion.identity;
    Quaternion qq = Quaternion.identity;

    Vector3[][] renderRays;
    [HideInInspector] public Vector3[] sendRays;
    Vector3[] corners;

    SphSpaceManager ssm;
    SphericalUtilities su = new SphericalUtilities();

    public void GetDefaultSetup() {

        if (sphericalPosition[1] == 0) {
            sphericalPosition[1] += 0.0001f;
        }
        // setting cartesian position and initial direction
        position = su.Spherical2Cartesian(sphericalPosition);
        direction = su.GetDirection(position);
        if (sphericalPosition[1] < 0) {
            direction = -direction;
        }
        Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
        direction = Q * direction;

        // setting render rays
        renderRays = new Vector3[resolution[0]][];

        if (!noWidthControl) {
            if (projection == Projection.Gue) { if (width > 3) {width = 3;} }
            if (projection == Projection.Equirectangular) { if (width > Mathf.PI * 2) {width = Mathf.PI * 2;} }
        }

        height = width * ((float)resolution[1] / (float)resolution[0]);

        if (!noWidthControl) {
            if (projection == Projection.Gue) { if (height >= 3) {width *= 3 / height; height = 3;} }
            if (projection == Projection.Equirectangular) { if (height > 3.14f) {width *= 3.14f / height; height = 3.14f;} }
        }


        if (projection == Projection.Gue || projection == Projection.Equirectangular) {

            corners = new Vector3[4] {
                new Vector3(-width * 0.5f, height * 0.5f),
                new Vector3(-width * 0.5f, -height * 0.5f),
                new Vector3(width * 0.5f, height * 0.5f),
                new Vector3(width * 0.5f, -height * 0.5f)
            };
            for (int i = 0; i < 4; i++) {
                corners[i] = Q * su.AddCartSpher(su.Spherical2Cartesian(corners[i]), sphericalPosition);
            }
        } else {
            corners = new Vector3[4] {
                new Vector3(1, -height * 0.5f, width * 0.5f),
                new Vector3(1, -height * 0.5f, -width * 0.5f),
                new Vector3(1, height * 0.5f, width * 0.5f),
                new Vector3(1, height * 0.5f, -width * 0.5f)
            };
            for (int i = 0; i < 4; i++) {
                corners[i] = Q * su.AddCartSpherR3(corners[i], sphericalPosition);
            }
        }
        
        // setting up rigidbody here so it looks the same in editor as indicator for camera
        rigidbody.SetUp(position, direction, directionRotation);
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        GetDefaultSetup();
        transform.position = position;

        SphSpaceManager.sc = this;
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }


    void OnDrawGizmos() {
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(position, 0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position  + (0.2f * direction));

        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++) {
            Gizmos.DrawSphere(screenQ * corners[i], 0.05f);
        }

        if (projection == Projection.Gue) {
            GizmosDrawLine(screenQ * corners[0], screenQ * corners[1]);
            GizmosDrawLine(screenQ * corners[1], screenQ * corners[3]);
            GizmosDrawLine(screenQ * corners[2], screenQ * corners[0]);
            GizmosDrawLine(screenQ * corners[3], screenQ * corners[2]);
        } else if (projection == Projection.Equirectangular) {

            Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);
            Vector3[] newCorners = new Vector3[4] {
                new Vector3(1, -width * 0.5f, height * 0.5f),
                new Vector3(1, -width * 0.5f, -height * 0.5f),
                new Vector3(1, width * 0.5f, -height * 0.5f),
                new Vector3(1, width * 0.5f, height * 0.5f)
            };
            for (int i = 0; i < 4; i++) {
                int ii = (i+1) % 4;
                Vector3[] line = GetLine(newCorners[ii], newCorners[i], 4);
                for (int j = 0; j < 4; j++)
                {
                    line[j] = screenQ * (Q * su.AddCartSpher(su.SphericalToCartesian(line[j]), sphericalPosition));
                }
                su.GizmosDrawPointsNoLoop(line);
            }

        } else {
            Gizmos.DrawLine(screenQ * corners[0], screenQ * corners[1]);
            Gizmos.DrawLine(screenQ * corners[1], screenQ * corners[3]);
            Gizmos.DrawLine(screenQ * corners[2], screenQ * corners[0]);
            Gizmos.DrawLine(screenQ * corners[3], screenQ * corners[2]);
        }
    }

    public void GizmosDrawLine(Vector3 a, Vector3 b, int n=4) {
        Vector3[] points = su.GetLinePoints(a, b, n);
        for (int i = 0; i < n-1; i++) {
            Gizmos.DrawLine(points[i], points[i+1]);
        }
    }

    void Start() {
        // sampling rays
        if (projection == Projection.Gue) {
            Vector3[] upLine = su.GetLinePoints(corners[0], corners[2], resolution[0]);
            Vector3[] downLine = su.GetLinePoints(corners[1], corners[3], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = su.GetLinePoints(upLine[i], downLine[i], resolution[1]);
            }
        } else if (projection == Projection.Gnomic) {
            Vector3[] upLine = GetLine(corners[3], corners[2], resolution[0]);
            Vector3[] downLine = GetLine(corners[1], corners[0], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = renderRays[i][j].normalized;
                }
            }
        } else if (projection == Projection.Orthographic) {
            Vector3[] upLine = GetLine(corners[3], corners[2], resolution[0]);
            Vector3[] downLine = GetLine(corners[1], corners[0], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = RaySphereIntersectionFirst(renderRays[i][j], -position);
                }
            }
        } else if (projection == Projection.Stereographic) {
            Vector3[] upLine = GetLine(corners[3], corners[2], resolution[0]);
            Vector3[] downLine = GetLine(corners[1], corners[0], resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = RaySphereIntersectionSecond(-position, (renderRays[i][j] + position).normalized);
                }
            }
        } else if (projection == Projection.Equirectangular) {
            Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);

            Vector3[] upLine = GetLine(new Vector3(1, -width * 0.5f, height * 0.5f), new Vector3(1, width * 0.5f, height * 0.5f), resolution[0]);
            Vector3[] downLine = GetLine(new Vector3(1, -width * 0.5f, -height * 0.5f), new Vector3(1, width * 0.5f, -height * 0.5f), resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    renderRays[i][j] = Q * su.AddCartSpher(su.SphericalToCartesian(renderRays[i][j]), sphericalPosition);
                }
            }
        } else if (projection == Projection.Polar || projection == Projection.InfinitePolar) { // polar projections scales size by 2
            Q = Quaternion.AngleAxis(directionRotation * (360.0f / (Mathf.PI * 2)), position);

            Vector3[] upLine = GetLine(new Vector3(1, -width, height), new Vector3(1, width, height), resolution[0]);
            Vector3[] downLine = GetLine(new Vector3(1, -width, -height), new Vector3(1, width, -height), resolution[0]);

            for (int i = 0; i < renderRays.Length; i++) {
                renderRays[i] = GetLine(upLine[i], downLine[i], resolution[1]);
                for (int j = 0; j < resolution[1]; j++) {
                    Vector2 vec2 = new Vector2(renderRays[i][j].y, renderRays[i][j].z);
                    Vector2 polarCord = new Vector2(vec2.magnitude, su.Rad2Deg * Mathf.Atan2(vec2.x, vec2.y));

                    if (polarCord.x > Mathf.PI && projection != Projection.InfinitePolar) {
                        renderRays[i][j] = new Vector3(10, 0, 0);
                    } else {
                        renderRays[i][j] = Q * su.AddCartSpher(su.Polar2Cartesian(polarCord), sphericalPosition);
                    }
                }
            }
        }

        sendRays = new Vector3[resolution[0] * resolution[1]];

        for (int i = 0; i < sendRays.Length; i++) {
            int j = (i - (i % resolution[1])) / resolution[1];
            int ii = renderRays[j].Length - (i % resolution[1]) - 1;

            sendRays[i] = renderRays[j][ii];
        }
    }

    Vector3[] GetLine(Vector3 a, Vector3 b, int n) {
        Vector3[] linePoints = new Vector3[n];

        float fraction = 1.0f / (float)(n-1);
        for (int i = 0; i < n; i++) {
            linePoints[i] = (a * (1-(i * fraction))) + (b * (i * fraction));
        } 

        return linePoints;
    }

    Vector3 RaySphereIntersectionFirst(Vector3 o, Vector3 dir) {
        float d = o.magnitude;
        float dtc = Mathf.Sqrt((d*d)-1);
        if (dtc > 1) {return new Vector3(10, 0, 0);}
        return o + (dir * (1 - Mathf.Sqrt(1-(dtc*dtc))));
    }
    Vector3 RaySphereIntersectionSecond(Vector3 o, Vector3 dir) {
        float od = Vector3.Dot(-o, dir);
        return o + (dir * (od * 2));
    }

    // Update is called once per frame
    void Update()
    {
        // movement
        transform.position = position;
        position = rigidbody.position;
        screenQ = rigidbody.movementQ;
    }

    public void QuitApp() {Application.Quit();}

    // returs mouse position in cartesian coordinates, (10, 0, 0) if its out of space
    public Vector3 GetMousePos() {
        Vector2 mousePos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Vector3 ray;

        try{
            ray = renderRays[(int)(mousePos.x * resolution[0])][(resolution[1] - 1) - (int)(mousePos.y * resolution[1])];
        } catch (Exception e) {
            return su.Spherical2Cartesian(new Vector2());
        }

        if (Mathf.Abs(ray.x) + Mathf.Abs(ray.y) + Mathf.Abs(ray.z) > 6) {return new Vector3(10, 0, 0);}
        return (screenQ * ray).normalized;
    }
}
