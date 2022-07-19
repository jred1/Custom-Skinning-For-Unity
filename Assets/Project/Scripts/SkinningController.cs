using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinningController : MonoBehaviour
{
    [Tooltip("0 for direct; 1 for linear; 2 for dual quaternion")]
    public int skinningType; 
    public SkinningData skinningData;

    public MaterialPropertyBlock properties;
    public MeshRenderer meshRenderer;
    public SkinnedMeshRenderer skinRenderer;

    public Transform[] boneTransforms;

    private dualQuat[] boneDQs;
    private Matrix4x4[] boneMatrices;

    private ComputeBuffer bones;
    private ComputeBuffer bindPose;

    private int boneCount;
    private int fixedType; 
    void Start()
    {
        boneCount = skinningData.bindMatrices.Length;
        fixedType = skinningType;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(properties);

        if (fixedType == 0){
            boneMatrices = new Matrix4x4[boneCount];

            SetBuffersUnsafe();
            bindPose.SetData(skinningData.bindMatrices);
            bones.SetData(boneMatrices);
            properties.SetBuffer("bones", bones);
            properties.SetBuffer("bindPose", bindPose);
        }
        if (fixedType == 1){
            boneDQs = new dualQuat[boneCount];

            SetBuffersUnsafe_DQ();
            bindPose.SetData(skinningData.bindDQs);
            bones.SetData(boneDQs);
            properties.SetBuffer("bonesDQ", bones);
            properties.SetBuffer("bindPoseDQ", bindPose);
        }

        meshRenderer.SetPropertyBlock(properties);
    }

    void Update()
    {
        boneTransforms = skinRenderer.bones;
        Matrix4x4 meshMatrix = transform.worldToLocalMatrix;
        if (fixedType == 0){
            for (int i = 0; i < boneCount; i++)
                boneMatrices[i] = meshMatrix * boneTransforms[i].localToWorldMatrix;
            bones.SetData(boneMatrices);
        }
        if (fixedType == 1){
            for (int i = 0; i < boneCount; i++)
                boneDQs[i] = new dualQuat(meshMatrix * boneTransforms[i].localToWorldMatrix);
            bones.SetData(boneDQs);
        }

    }
    private unsafe void SetBuffersUnsafe()
    {
        bones = new ComputeBuffer(skinningData.bindMatrices.Length, sizeof(Matrix4x4));
        bindPose = new ComputeBuffer(skinningData.bindMatrices.Length, sizeof(Matrix4x4));
    }
    private unsafe void SetBuffersUnsafe_DQ()
    {
        bones = new ComputeBuffer(skinningData.bindMatrices.Length, sizeof(dualQuat));
        bindPose = new ComputeBuffer(skinningData.bindMatrices.Length, sizeof(dualQuat));
    }

    private void OnDestroy()
    {
        bones.Release();
        bindPose.Release();
    }
}
