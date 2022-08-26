using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class test : MonoBehaviour
{
    public SphericalCamera camera;
    public SphGon gon;
    public SphShape shap;
    public UVTiles tiles;
    float Rad2Deg = 180.0f / Mathf.PI;

    SphericalUtilities su = new SphericalUtilities();

    void Start() {
        camera = GetComponent<SphericalCamera>();
        gon = GetComponent<SphGon>();
        shap = GetComponent<SphShape>();
    }

    void Update() {
        gon.Move(camera.position, (Rad2Deg * su.SphDistance(camera.position, gon.position) * 0.2f) * Time.deltaTime);
        gon.Rotate(50 * Time.deltaTime);

        shap.Move(camera.position, (Rad2Deg * su.SphDistance(camera.position, shap.position) * 0.3f) * Time.deltaTime);
        shap.Rotate(10 * Time.deltaTime);

        tiles.Move(camera.position, (Rad2Deg * su.SphDistance(camera.position, tiles.position) * 0.1f) * Time.deltaTime);
        tiles.ChangeColor(tiles.color * 0.9995f);
        tiles.Rotate(20 * Time.deltaTime);
    }
}
