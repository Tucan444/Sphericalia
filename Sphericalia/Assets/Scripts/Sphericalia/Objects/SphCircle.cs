using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SphCircle : MonoBehaviour
{
    public int layer = 0;
    public bool Static = true;
    public bool isCollider = false;
    public bool isTrigger = false;
    public Vector2 sphPosition = new Vector2();
    [Range(0.01f, -0.01f + Mathf.PI)] public float radius = 0.1f;
    public Color color =  new Color(0.69f, 0.48f, 0.41f, 1);
    public bool invisible = false;
    public bool empty = false;

    [HideInInspector] public Vector3 position = new Vector3(1, 0, 0);

    [HideInInspector] public CircleCollider collider_;

    [HideInInspector] public bool triggered = false;
    [HideInInspector] public bool terminated = false;

    SphericalUtilities su = new SphericalUtilities();
    SphSpaceManager ssm;

    public void GetDefaultSetup() {
        position = su.Spherical2Cartesian(sphPosition);
    }

    void OnValidate() {
        GetDefaultSetup();
        transform.position = position;
    }

    void OnEnable() {
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();

        if (transform.parent == null) {
            transform.parent = GameObject.Find("___SphericalSpace___").transform;
        }
        GetDefaultSetup();
        transform.position = position;
        terminated = false;
    }

    void OnDisable() {
        SphSpaceManager.sphCircles.Remove(this);
        if (isCollider) {ssm.circleC.Remove(this);}
        if (isTrigger) {ssm.cTrigger.Remove(this);}
        terminated = true;
    }
    

    void OnDrawGizmos() {
        if (!empty) {
            Gizmos.color = color * 1.4f;
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), radius));
            Gizmos.color = color * 1.2f;
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), radius * 0.9f));
            Gizmos.color = color;
            su.GizmosDrawPoints(su.GetCirclePoints(su.Cartesian2Spherical(position), radius * 0.8f));
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(position, radius * 0.2f);
    }


    void Start()
    {
        collider_ = new CircleCollider(position, radius, color, invisible, empty);

        if (!ssm.initialized) {
            SphSpaceManager.sphCircles.Add(this);
            if (!ssm.layers.Contains(layer)) {ssm.layers.Add(layer);}
        } else {
            ssm.InsertCircle(this);
            ssm.InsertLayer(layer);

            if (isCollider) {ssm.circleC.Add(this);}
            if (isTrigger) {ssm.cTrigger.Add(this);}
        }
    }

    public void ToggleEmpty() {
        empty = !empty;
        collider_.empty = empty;
    }

    public void ToggleInvisible() {
        invisible = !invisible;
        collider_.invisible = invisible;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Warning() {
        if (Static) {
            Debug.Log("attempting changes on static object, will not take effect");
        }
    }

    public void Move(Vector3 target, float angle) {
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.Cross(position, target));
        MoveQ(q);
    }

    public void MoveQ(Quaternion q) {
        if (!float.IsNaN(q.x)) {
            position = q * position;
            collider_.Update(position, radius, color, invisible, empty);
        }
        Warning();
    }

    public void ChangeColor(Color c) {
        color = c;
        collider_.Update(position, radius, color, invisible, empty);
        Warning();
    }

    public void Scale(float s) {
        radius *= s;
        collider_.Update(position, radius, color, invisible, empty);
        Warning();
    }

    public void ToggleCollider() {isCollider = !isCollider;}
    public void ToggleTrigger() {isTrigger = !isTrigger;}
}
