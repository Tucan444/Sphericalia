using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Composite : MonoBehaviour
{
    public RigidbodySpherical rigidbody;

    [HideInInspector] public SphCircle[] circles;
    [HideInInspector] public SphGon[] gons;
    [HideInInspector] public SphShape[] shapes;

    [HideInInspector] public Dictionary<string, Marker> markers = new Dictionary<string, Marker>();

    SphericalUtilities su = new SphericalUtilities();
    bool firstFrame = true;
    
    // can be used anytime
    public void FindChildren() {
        circles = GetComponentsInChildren<SphCircle>();
        gons = GetComponentsInChildren<SphGon>();
        shapes = GetComponentsInChildren<SphShape>();
    }

    // used in onEnable
    public void SmackChildren() {
        Quaternion q = rigidbody.Q; // quaternion used in onEnable of rigidbody for direction rotation

        for (int i = 0; i < circles.Length; i++) {
            circles[i].sphPosition = su.AddSpherSpher(circles[i].sphPosition, rigidbody.sphericalPosition);
        }

        for (int i = 0; i < gons.Length; i++) {
            gons[i].sphPosition = su.AddSpherSpher(gons[i].sphPosition, rigidbody.sphericalPosition);
        }

        for (int i = 0; i < shapes.Length; i++) {
            shapes[i].sphPosition = su.AddSpherSpher(shapes[i].sphPosition, rigidbody.sphericalPosition);
        }

        foreach (string key in markers.Keys) {
            markers[key].position = su.AddCartSpher(markers[key].position, rigidbody.sphericalPosition);
            markers[key].direction = su.AddCartSpher(markers[key].direction, rigidbody.sphericalPosition);
        }
    }

    // can be used from first Update
    public void ManipulateChildren(Quaternion q) {

        for (int i = 0; i < circles.Length; i++) {
            circles[i].MoveQ(q);
        }

        for (int i = 0; i < gons.Length; i++) {
            gons[i].MoveQ(q);
        }

        for (int i = 0; i < shapes.Length; i++) {
            shapes[i].MoveQ(q);
        }

        foreach (string key in markers.Keys) {
            markers[key].position = q * markers[key].position;
            markers[key].direction = q * markers[key].direction;
        }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        FindChildren();
        SmackChildren();
    }

    void Start() {
    }

    // Update is called once per frame
    void Update()
    {
        if (firstFrame) {ManipulateChildren(rigidbody.Q);};
        ManipulateChildren(rigidbody.lframeQ);

        firstFrame = false;
    }
}

public class Marker {

    public string name;
    public Vector3 position;
    public Vector3 direction; 

    public Marker(string name_, Vector3 position_, Vector3 direction_) {
        name = name_;
        position = position_;
        direction = direction_;
    }
}
