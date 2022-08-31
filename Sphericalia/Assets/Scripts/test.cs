using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class test : MonoBehaviour
{
    public SphericalCamera camera;
    public SphCircle cc;
    public SphGon gon;
    public UVTiles uvs;
    float Rad2Deg = 180.0f / Mathf.PI;

    float tp = 0;

    SphSpaceManager ssm;

    SphericalUtilities su = new SphericalUtilities();

    void Start() {
        camera = GetComponent<SphericalCamera>();
        gon = GetComponent<SphGon>();
        //cc = GetComponent<SphCircle>();
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    void Update() {
        tp += Time.deltaTime;
        if (tp > 4) {//shap.MakeNonEmpty(); 
        /* uvs.ToggleEmpty();
        gon.ToggleTrigger();
        ssm.PopulateAll();
        ssm.SortColliderTrigger(); */
        tp = -20;}



        if (su.SphDistance(camera.position, gon.position) > 0) {
            gon.Move(camera.position, (Rad2Deg * su.SphDistance(camera.position, gon.position)));
        }
        gon.Rotate(50 * Time.deltaTime);

        List<SphGon> gonss = ssm.GetTriggeredGons();
        for (int i = 0; i < gonss.Count; i++)
        {
            Debug.Log(gonss[i].gameObject.name);
        }
    }
}
