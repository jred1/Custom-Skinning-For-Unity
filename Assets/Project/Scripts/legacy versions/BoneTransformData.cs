using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneTransformData : MonoBehaviour
{
    public float testing;
    private Material mat;
    private MaterialPropertyBlock properties;
    private SkinnedMeshRenderer SkinRenderer;
    // Start is called before the first frame update
    void Start()
    {
        SkinRenderer = GetComponent<SkinnedMeshRenderer>();
        properties = new MaterialPropertyBlock();
        SkinRenderer.GetPropertyBlock(properties);


    }

    // Update is called once per frame
    void Update()
    {

        properties.SetFloat("tester", testing);
        SkinRenderer.SetPropertyBlock(properties);
        //Debug.Log();
    }
}
