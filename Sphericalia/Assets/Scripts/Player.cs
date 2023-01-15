using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour  // this is custom script not a part of engine
{
    public PointLight pl;
    SphericalCamera sc;
    SphGon player;

    SphericalUtilities su = new SphericalUtilities();
    SphSpaceManager ssm;
    // Start is called before the first frame update
    void Start()
    {
        sc = GetComponent<SphericalCamera>();
        ssm = GameObject.Find("___SphericalSpace___").GetComponent<SphSpaceManager>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mp = sc.GetMousePos();
        if (mp.x != 10) {
            pl.Move(mp, su.Rad2Deg *  su.SphDistance(mp, pl.position));
        }
    }
}
