using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BoneData : MonoBehaviour
{
    public Transform rootBone;
    public int testBoneID;

    private Mesh replacementMesh; //empty mesh
    private List<BoneWeight> boneWeights;
    void Start()
    {
        SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        boneWeights = new List<BoneWeight>();

        //use GetAllBoneWeights and the associated api's for more bones per vertex
        mesh.GetBoneWeights(boneWeights);
        //Debug.Log("index: " + boneWeights[0].boneIndex0 + " weight: " + boneWeights[0].weight0);

        int size = mesh.vertexCount;
        Vector4[] indices = new Vector4[size];
        Vector4[] weights = new Vector4[size];

        for (int i = 0; i < size; i++)
        {
            BoneWeight w = boneWeights[i];

            indices[i] = new Vector4(
                            w.boneIndex0,
                            w.boneIndex1,
                            w.boneIndex2,
                            w.boneIndex3
                            );
            weights[i] = new Vector4(
                            w.weight0,
                            w.weight1,
                            w.weight2,
                            w.weight3
                            );
        }
        mesh.SetUVs(1, indices);
        mesh.SetUVs(2, weights);

        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.materials = skinnedMeshRenderer.materials;

        skinnedMeshRenderer.sharedMesh = replacementMesh;
        skinnedMeshRenderer.quality = SkinQuality.Bone1;
        skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;

        /*
        for (int i = 0; i < size; i++)
        {
            Debug.Log(weights[i] +" "+ mesh.uv3[i].x);
        }
        */

        //boneWeights.Clear();


    }

    // Update is called once per frame
    void Update()
    {
        //store localToWorldMatrix to get the 4x4 matrix defining the bone
        Transform[] boneTransforms = GetComponent<SkinnedMeshRenderer>().bones;
        Matrix4x4 worldTRS = boneTransforms[boneWeights[testBoneID].boneIndex0].localToWorldMatrix;
        Matrix4x4 rootTRS = rootBone.worldToLocalMatrix;
        Debug.Log(rootTRS.MultiplyPoint3x4(worldTRS.MultiplyPoint3x4(new Vector3(0, 0, 0))) );
    }
}
