using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class test : MonoBehaviour
{
    public SphericalCamera camera;
    public SphGon gon;
    public UVTiles uvs;
    public UVTiles hehe;
    float Rad2Deg = 180.0f / Mathf.PI;

    float tp = 0;

    SphSpaceManager ssm;

    SphericalUtilities su = new SphericalUtilities();

    void Start() {
        camera = GetComponent<SphericalCamera>();
        gon = GetComponent<SphGon>();
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void Update() {
        tp += Time.deltaTime;
        if (tp > 4) {//shap.MakeNonEmpty(); 
        uvs.MakeNonEmpty();
        hehe.ToggleCollider();
        gon.ToggleTrigger();
        ssm.PopulateTrianglesAndQuads();
        ssm.SortColliderTrigger();
        tp = -50;}



        if (su.SphDistance(camera.position, gon.position) > 0) {
            gon.Move(camera.position, (Rad2Deg * su.SphDistance(camera.position, gon.position)));
        }
        gon.Rotate(50 * Time.deltaTime);

        List<SphGon> gonss = ssm.GetTriggeredGons();
        for (int i = 0; i < gonss.Count; i++)
        {
            Debug.Log(gonss[i].gameObject.name);
        }



        //uvs.Move(camera.position, Rad2Deg * 0.1f * Time.deltaTime);
    }
}
