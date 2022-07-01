using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneTransformDataConverted : MonoBehaviour
{
    public float testing;

    private MaterialPropertyBlock properties;
    private MeshRenderer meshRenderer;

    private SkinnedMeshRenderer skinRenderer;
    private Transform[] boneTransforms;
    private int boneCount;

    private Matrix4x4[] bindMatrices;
    private Matrix4x4[] boneMatrices;

    private ComputeBuffer bones;
    private ComputeBuffer bindPose;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        skinRenderer = GetComponent<SkinnedMeshRenderer>();
        properties = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(properties);

        boneTransforms = skinRenderer.bones; // bind pose for now. implement code to save this
        boneCount = boneTransforms.Length;
        bindMatrices = new Matrix4x4[boneCount];
        boneMatrices = new Matrix4x4[boneCount];
        SetBuffersUnsafe();

        //get and save bind pose
        for (int i = 0; i < boneCount; i++)
        {
            bindMatrices[i] = transform.worldToLocalMatrix * boneTransforms[i].localToWorldMatrix;
            bindMatrices[i] = bindMatrices[i].inverse;
        }
        bindPose.SetData(bindMatrices);

        bones.SetData(boneMatrices);
        properties.SetBuffer("bones", bones);
        properties.SetBuffer("bindPose", bindPose);
        meshRenderer.SetPropertyBlock(properties);

    }

    // Update is called once per frame
    void Update()
    {
        boneTransforms = skinRenderer.bones;
        for (int i = 0; i < boneCount; i++)
        {
            boneMatrices[i] = transform.worldToLocalMatrix * boneTransforms[i].localToWorldMatrix;
        }

        bones.SetData(boneMatrices);

    }

    private unsafe void SetBuffersUnsafe()
    {
        bones = new ComputeBuffer(boneTransforms.Length, sizeof(Matrix4x4));
        bindPose = new ComputeBuffer(boneTransforms.Length, sizeof(Matrix4x4));
    }

    private void OnDestroy()
    {
        bones.Release();
        bindPose.Release();
    }
}
