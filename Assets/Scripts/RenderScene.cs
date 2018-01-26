using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderScene : MonoBehaviour {
    public GameObject obj;
    public Mesh meshObj;
    public Material material;
    public LayerMask layer;

    public void OnPostRender()
    {
        Graphics.DrawMesh(meshObj, obj.transform.position, obj.transform.rotation, material, layer.value);
    }
}
