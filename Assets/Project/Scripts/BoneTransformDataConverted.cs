using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneTransformDataConverted : MonoBehaviour
{
    public float testing;
    private Material mat;
    private MaterialPropertyBlock properties;
    private MeshRenderer meshRenderer;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(properties);


    }

    // Update is called once per frame
    void Update()
    {

        properties.SetFloat("tester", testing);
        meshRenderer.SetPropertyBlock(properties);
        //Debug.Log();
    }
}
