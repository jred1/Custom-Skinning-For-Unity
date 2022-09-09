using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEngine.Rendering;

public class SkinningDataEditor : EditorWindow
{
    public GameObject input;
    public string saveLocation;
    public Transform rootBone;

    private GameObject GOI;
    private Mesh replacementMesh;
    private List<BoneWeight> boneWeights;

    private string previousPrefabPath;

    [MenuItem("Window/Skinning/Data Edit")]
    public static void ShowWindow()
    {
        GetWindow<SkinningDataEditor>("Skinning Data Edit");
    }
    private void OnGUI()
    {
        input = (GameObject)EditorGUILayout.ObjectField(input, typeof(GameObject), false);

        if (GUILayout.Button("Bake"))
        {
            Bake();
        }

    }

    //https://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
    string GetRelativePath(string filespec, string folder)
{
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
                skinningData = ModifyPrefab(skinRenderers[i].gameObject, mesh,skinningData);
                AssetDatabase.CreateAsset(skinningData, scrObjPath);

            }
            AssetDatabase.SaveAssets();

            bool success;
            string prefabPath = relativeFolderPath + Path.DirectorySeparatorChar + input.name + "_Modified.prefab";
            PrefabUtility.SaveAsPrefabAsset(GOI, prefabPath, out success);
            if (success)
                Debug.Log("Save Successful");
        }
        else Debug.LogWarning("Prefab Save Failed or Canceled");

        DestroyImmediate(GOI);
    }

    private Mesh ModifyMesh(GameObject go)
    {

        SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = Instantiate(skinnedMeshRenderer.sharedMesh);
        boneWeights = new List<BoneWeight>();

        mesh.GetBoneWeights(boneWeights);

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

        return mesh;
    }
    private SkinningData ModifyPrefab(GameObject go, Mesh mesh, SkinningData skinningData)
    {
        // Swap mesh renderers
        SkinnedMeshRenderer skinRenderer = go.GetComponent<SkinnedMeshRenderer>();
        go.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = skinRenderer.sharedMaterials;

        skinRenderer.sharedMesh = replacementMesh;
        skinRenderer.quality = SkinQuality.Bone1;
        skinRenderer.shadowCastingMode = ShadowCastingMode.Off;

        // Precompute skinning data
        Transform[] boneTransforms = skinRenderer.bones;
        int boneCount = boneTransforms.Length;

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

        // Initialize skinning controller
        SkinningController controller = go.AddComponent<SkinningController>();
        controller.skinningData = skinningData;
        controller.meshRenderer = meshRenderer;
        controller.skinRenderer = skinRenderer;
        controller.boneTransforms = skinRenderer.bones;

        return skinningData;
    }

}
