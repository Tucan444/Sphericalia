/* using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteAlways]
public class SphericalBackground : MonoBehaviour
{
    public bool useTexture = false;
    public Texture2D mainTex = new Texture2D(1, 1);
    public Color bgColor = Color.gray;

    Material m;

    void OnEnable() {
        SphSpaceManager.sb = this;
        mainTex = Texture2D.grayTexture;
        m = GetComponent<Renderer>().sharedMaterial;
    }
}
 */