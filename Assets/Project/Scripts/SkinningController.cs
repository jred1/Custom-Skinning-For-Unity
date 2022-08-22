using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinningController : MonoBehaviour
{
    [Tooltip("0 for linear; 1 for dual quaternion")]
    public int skinningType; 
    public SkinningData skinningData;

    public MaterialPropertyBlock properties;
    public MeshRenderer meshRenderer;
    public SkinnedMeshRenderer skinRenderer;

    public Transform[] boneTransforms;

    private DualQuat[] boneDQs;
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

            SetBuffers_M();
            bindPose.SetData(skinningData.bindMatrices);
            bones.SetData(boneMatrices);
            properties.SetBuffer("bones", bones);
            properties.SetBuffer("bindPose", bindPose);
        }
        if (fixedType == 1){
            boneDQs = new DualQuat[boneCount];

            SetBuffers_DQ();
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
                boneDQs[i] = new DualQuat(meshMatrix * boneTransforms[i].localToWorldMatrix);
            bones.SetData(boneDQs);
        }

    }
    private void SetBuffers_M()
    {
        bones = new ComputeBuffer(skinningData.bindMatrices.Length, 16 * sizeof(float));
        bindPose = new ComputeBuffer(skinningData.bindMatrices.Length, 16 * sizeof(float));
    }
    private void SetBuffers_DQ()
    {
        bones = new ComputeBuffer(skinningData.bindMatrices.Length, 8 * sizeof(float));
        bindPose = new ComputeBuffer(skinningData.bindMatrices.Length, 8 * sizeof(float));
    }

    private void OnDestroy()
    {
        bones?.Release();
        bindPose?.Release();
    }
}
