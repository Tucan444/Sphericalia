using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class test : MonoBehaviour
{
    public SphericalCamera camera;
    public SphCircle circle;
    public SphGon gon;
    public SphShape shap;
    public UVTiles uvs;
    float Rad2Deg = 180.0f / Mathf.PI;

    SphSpaceManager ssm;

    SphericalUtilities su = new SphericalUtilities();

    void Start() {
        camera = GetComponent<SphericalCamera>();
        gon = GetComponent<SphGon>();
        circle = GetComponent<SphCircle>();
        shap = GetComponent<SphShape>();
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
        //uvs = GameObject.Find("hentai").GetComponent<UVTiles>();
    }

    void Update() {
        if (su.SphDistance(camera.position, gon.position) > 0) {
            gon.Move(camera.position, (Rad2Deg * su.SphDistance(camera.position, gon.position)));
        }
        gon.Rotate(50 * Time.deltaTime);

        circle.Move(camera.position, Rad2Deg * 0.1f * Time.deltaTime);
        shap.Move(camera.position, Rad2Deg * 0.1f * Time.deltaTime);
        shap.Rotate(10 * Time.deltaTime);
        shap.ChangeColor(shap.color * 1.001f);
        shap.Scale(1 + (0.1f * Time.deltaTime));
        //uvs.Move(camera.position, Rad2Deg * 0.1f * Time.deltaTime);
        List<SphCircle> circles = ssm.GetTriggeredCircles();
        for (int i = 0; i < circles.Count; i++)
        {
            Debug.Log(circles[i].transform.gameObject.name);
        }  

        List<SphShape> shapes = ssm.GetTriggeredShapes();
        for (int i = 0; i < shapes.Count; i++)
        {
            Debug.Log(shapes[i].transform.gameObject.name);
        }  

        List<UVTiles> uvts = ssm.GetTriggeredUVTiles();
        for (int i = 0; i < uvts.Count; i++)
        {
            Debug.Log(uvts[i].transform.gameObject.name);
        } 
    }
}
