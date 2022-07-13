using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DualQuaternionSkinning : MonoBehaviour
{
    public float testing;

    private MaterialPropertyBlock properties;
    private MeshRenderer meshRenderer;

    private SkinnedMeshRenderer skinRenderer;
    private Transform[] boneTransforms;
    private int boneCount;

    private dualQuat[] bindDQ;
    private dualQuat[] boneDQ;

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
        bindDQ = new dualQuat[boneCount];
        boneDQ = new dualQuat[boneCount];
        SetBuffersUnsafe();

        //get and save bind pose
        Matrix4x4 temp;
        for (int i = 0; i < boneCount; i++)
        {
            // change to use quaternions before the multiplication
            temp = transform.worldToLocalMatrix * boneTransforms[i].localToWorldMatrix;
            bindDQ[i] = new dualQuat(temp.inverse);
        }
        bindPose.SetData(bindDQ);

        bones.SetData(boneDQ);
        properties.SetBuffer("bonesDQ", bones);
        properties.SetBuffer("bindPoseDQ", bindPose);
        meshRenderer.SetPropertyBlock(properties);

    }

    // Update is called once per frame
    void Update()
    {
        boneTransforms = skinRenderer.bones;
        for (int i = 0; i < boneCount; i++)
        {
            boneDQ[i] = new dualQuat(transform.worldToLocalMatrix * boneTransforms[i].localToWorldMatrix);
        }

        bones.SetData(boneDQ);

    }

    private unsafe void SetBuffersUnsafe()
    {
        bones = new ComputeBuffer(boneTransforms.Length, sizeof(dualQuat));
        bindPose = new ComputeBuffer(boneTransforms.Length, sizeof(dualQuat));
    }

    private void OnDestroy()
    {
        bones.Release();
        bindPose.Release();
    }
}
