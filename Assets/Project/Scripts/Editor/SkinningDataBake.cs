using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEngine.Rendering;
using Unity.Mathematics;

public class SkinningDataBake : EditorWindow
{
    public GameObject input;
    public float sigma = 0.5f;
    public ComputeShader computeShader;

    private GameObject GOI;
    private Mesh replacementMesh;
    private List<BoneWeight> boneWeights;
    private int boneCount;

    private double timer;


    [MenuItem("Window/Skinning/Data Bake")]
    public static void ShowWindow()
    {
        GetWindow<SkinningDataBake>("Skinning Data Bake");
    }
    private void OnGUI()
    {
        input = (GameObject)EditorGUILayout.ObjectField("    Input Prefab",input, typeof(GameObject), false);
        sigma = EditorGUILayout.FloatField("    Sigma",sigma);

        if (GUILayout.Button("Bake"))
        {
            Bake();
        }

    }

    //https://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
    string GetRelativePath(string filespec, string folder){
        Uri pathUri = new Uri(filespec);
        // Folders must end in a slash
        if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            folder += Path.DirectorySeparatorChar;
        }
        Uri folderUri = new Uri(folder);
        return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    private void Bake()
    {
        GOI = Instantiate(input);
        string folderPath = EditorUtility.SaveFolderPanel(
                "Save Skinning Data",
                "Assets" + Path.DirectorySeparatorChar,
                "");
        if (folderPath != ""){
            timer = EditorApplication.timeSinceStartup;

            string relativeFolderPath = "Assets" + Path.DirectorySeparatorChar + GetRelativePath(folderPath, Application.dataPath);

            ///---Modify each Game Object on Prefab with a SkinnedMeshRenderer---///
            SkinnedMeshRenderer[] skinRenderers = GOI.GetComponentsInChildren<SkinnedMeshRenderer>();//modify to allow Mesh renderer on root object
            for (int i = 0; i < skinRenderers.Length; i++)
            {
                // Modify meshes to include bone ids and weights
                Mesh mesh = new Mesh();
                string objName = skinRenderers[i].sharedMesh.name;
                string meshPath = (relativeFolderPath + Path.DirectorySeparatorChar + objName + "_Modified.asset");
                mesh = ModifyMesh(skinRenderers[i].gameObject);
                AssetDatabase.CreateAsset(mesh, meshPath);

                // Precomputed data needed for runtime
                SkinningData skinningData = ScriptableObject.CreateInstance<SkinningData>();
                string scrObjPath = (relativeFolderPath + Path.DirectorySeparatorChar + objName + "_SkinningData.asset");

                // Modify prefab to prepare for custom skinning
                skinningData = ConvertAndCreate(skinRenderers[i].gameObject, mesh, skinningData);
                AssetDatabase.CreateAsset(skinningData, scrObjPath);

            }
            AssetDatabase.SaveAssets();
            bool success;
            string prefabPath = relativeFolderPath + Path.DirectorySeparatorChar + input.name + "_Modified.prefab";
            PrefabUtility.SaveAsPrefabAsset(GOI, prefabPath, out success);

            timer = EditorApplication.timeSinceStartup - timer;
            timer = Mathf.Round((float)(timer)*1000.0f)/1000.0f;
            if (success)
                Debug.Log("Save Successful!\nBake completed in " + timer + " seconds.");
        }
        else Debug.LogWarning("Save Failed or Canceled");

        DestroyImmediate(GOI);
    }

    private int[] triIndices;
    private Vector4[] boneIDs;
    private Vector4[] weights;
    private Vector3[] vertPos;

    private ComputeBuffer _triIndices, _boneIDs, _weights, _vertPos;
    private ComputeBuffer _similarity;
    private ComputeBuffer _opt_CoR;

    private int vertCount, triCount;
    private int similarityKernel;
    private int opt_CoRKernel;

    private Vector3[] opt_CoRs;

    void PassParameters(){
        computeShader.SetInt("_vertCount",vertCount);
        computeShader.SetInt("_triCount",triCount);
        computeShader.SetInt("_boneCount",boneCount);
        computeShader.SetFloat("_sigma",sigma);
    }
    void MakeBuffers(){
        _triIndices = new ComputeBuffer(triCount*3,sizeof(int));
        _boneIDs = new ComputeBuffer(vertCount,4*sizeof(float));
        _weights = new ComputeBuffer(vertCount,4*sizeof(float));
        _vertPos = new ComputeBuffer(vertCount,3*sizeof(float));

        _similarity = new ComputeBuffer(vertCount*triCount,sizeof(float));
        _opt_CoR = new ComputeBuffer(vertCount,3*sizeof(float));
        
        // read data
        _triIndices.SetData(triIndices);
        _boneIDs.SetData(boneIDs);
        _weights.SetData(weights);
        _vertPos.SetData(vertPos);

        // first kernel
        computeShader.SetBuffer(similarityKernel,"_triIndices", _triIndices);
        computeShader.SetBuffer(similarityKernel,"_boneIDs", _boneIDs);
        computeShader.SetBuffer(similarityKernel,"_weights", _weights);
        // output buffer
        computeShader.SetBuffer(similarityKernel,"_similarity", _similarity);

        // second kernel
        computeShader.SetBuffer(opt_CoRKernel,"_triIndices", _triIndices);
        computeShader.SetBuffer(opt_CoRKernel,"_weights", _weights);
        computeShader.SetBuffer(opt_CoRKernel,"_vertPos", _vertPos);
        computeShader.SetBuffer(opt_CoRKernel,"_similarity", _similarity);
        // output buffer
        computeShader.SetBuffer(opt_CoRKernel,"_opt_CoR", _opt_CoR);
    }

    private Mesh ModifyMesh(GameObject go)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = Instantiate(skinnedMeshRenderer.sharedMesh);
        boneCount = skinnedMeshRenderer.bones.Length;
        boneWeights = new List<BoneWeight>();

        //bone ids and weights
        Debug.Log("Extracting Bone Data...");
        mesh.GetBoneWeights(boneWeights);

        vertCount = mesh.vertexCount;
        boneIDs = new Vector4[vertCount];
        weights = new Vector4[vertCount];

        for (int i = 0; i < vertCount; i++)
        {
            BoneWeight w = boneWeights[i];

            boneIDs[i] = new Vector4(
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
        mesh.SetUVs(1, boneIDs);
        mesh.SetUVs(2, weights);

        //optCoR
        Debug.Log("Creating Optimized Centers of Rotation...");
        triIndices = mesh.GetIndices(0);
        triCount = triIndices.Length/3;

        opt_CoRs = new Vector3[vertCount];
        vertPos = mesh.vertices;

        similarityKernel = computeShader.FindKernel("Similarity");
        opt_CoRKernel = computeShader.FindKernel("CoR");

        MakeBuffers();

        PassParameters();
        int groups = Mathf.CeilToInt(((float)vertCount)/32.0f);
        computeShader.Dispatch(similarityKernel,groups,1,1);
        computeShader.Dispatch(opt_CoRKernel,groups,1,1);
        _opt_CoR.GetData(opt_CoRs);
        Vector4[] finalCoRs = new Vector4[vertCount];
        for (int i = 0; i < vertCount; i++)
        {
            // w will be used to find if there is an optimized center of rotation
            if (!float.IsNaN(opt_CoRs[i].x)){
                finalCoRs[i] = new Vector4(
                                opt_CoRs[i].x,
                                opt_CoRs[i].y,
                                opt_CoRs[i].z,
                                1);
            }
            else{
                finalCoRs[i] = new Vector4(0,0,0,0);
            }
        }
        mesh.SetUVs(3, finalCoRs);

        _triIndices?.Release();
        _boneIDs?.Release();
        _weights?.Release();
        _vertPos?.Release();
        _similarity?.Release();
        _opt_CoR?.Release();
        
        return mesh;
    }

    private SkinningData ConvertAndCreate(GameObject go, Mesh mesh, SkinningData skinningData)
    {
        Debug.Log("Modifying Prefab...");
        // Swap mesh renderers
        SkinnedMeshRenderer skinRenderer = go.GetComponent<SkinnedMeshRenderer>();
        go.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = skinRenderer.sharedMaterials;

        skinRenderer.sharedMesh = replacementMesh;
        skinRenderer.quality = SkinQuality.Bone1;
        skinRenderer.shadowCastingMode = ShadowCastingMode.Off;

        skinningData = TransformData(go, skinRenderer, skinningData);

        // Initialize skinning controller
        SkinningController controller = go.AddComponent<SkinningController>();
        controller.skinningData = skinningData;
        controller.meshRenderer = meshRenderer;
        controller.skinRenderer = skinRenderer;
        controller.boneTransforms = skinRenderer.bones;

        return skinningData;
    }

    SkinningData TransformData(GameObject go, SkinnedMeshRenderer skinRenderer, SkinningData skinningData){
        
        // Precompute skinning data
        Transform[] boneTransforms = skinRenderer.bones;
        boneCount = boneTransforms.Length;

        Matrix4x4[] bindMatrices = new Matrix4x4[boneCount];
        DualQuat[] bindDQs = new DualQuat[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            bindMatrices[i] = go.transform.worldToLocalMatrix * boneTransforms[i].localToWorldMatrix;
            bindMatrices[i] = bindMatrices[i].inverse;
            bindDQs[i] = new DualQuat(bindMatrices[i]);
        }
        skinningData.bindMatrices = bindMatrices;
        skinningData.bindDQs = bindDQs;
        return skinningData;
    }
}
