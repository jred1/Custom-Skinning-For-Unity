using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

public class BoneDataEditor : EditorWindow
{
    public GameObject input;
    public string saveLocation;
    public Transform rootBone;

    private GameObject GOI;
    private Mesh replacementMesh; //empty mesh
    private List<BoneWeight> boneWeights;

    private string previousMeshPath;
    private string previousPrefabPath;

    [MenuItem("Window/Skinning/Bone Data Bake")]
    public static void ShowWindow()
    {
        GetWindow<BoneDataEditor>("Bone Data Bake");
    }
    private void OnGUI()
    {
        input = (GameObject)EditorGUILayout.ObjectField(input, typeof(GameObject), false);

        if (GUILayout.Button("Bake"))
        {
            Bake();
        }

    }

    private void Bake()
    {
        GOI = Instantiate(input);
        bool success = false;

        ///---Modify each Game Object on Prefab with a SkinnedMeshRenderer---///
        SkinnedMeshRenderer[] skinRenderers = GOI.GetComponentsInChildren<SkinnedMeshRenderer>();//modify to allow Mesh renderer on root object
        for (int i = 0; i < skinRenderers.Length; i++)
        {
            string relativePath = "";
            if (previousMeshPath != "")
            {
                //does not return relative path, causes error
                /*
                fullPath = EditorUtility.SaveFilePanel(
                     "Save Modifed Mesh",
                     previousPrefabPath,
                     skinRenderers[i].sharedMesh.name + " Modified",
                     "asset");
                */

                relativePath = EditorUtility.SaveFilePanelInProject(
                     "Save Modifed Mesh",
                     skinRenderers[i].sharedMesh.name + " Modified",
                     "asset",
                     "Save file");
            }
            else
            {

                relativePath = EditorUtility.SaveFilePanelInProject(
                     "Save Modifed Mesh",
                     skinRenderers[i].sharedMesh.name + " Modified",
                     "asset",
                     "Save file");

                previousMeshPath = Path.GetDirectoryName(relativePath);
            }

            Mesh mesh = new Mesh();
            if (relativePath != "")
            {
                mesh = ModifyMesh(skinRenderers[i].gameObject);
                AssetDatabase.CreateAsset(mesh, relativePath);
                AssetDatabase.SaveAssets();
                //Debug.Log(Application.dataPath);
                success = true;
            }
            else
            {
                success = false;
                break;
            }

            ModifyPrefab(skinRenderers[i].gameObject, mesh);
        }

        ///---Save Prompt---///
        if (success)
        {
            string fullPath;
            if (previousPrefabPath != "")
            {
                fullPath = EditorUtility.SaveFilePanel("Save Modifed Prefab", Path.GetDirectoryName(previousPrefabPath), input.name + " Modified", "prefab");
            }
            else
            {
                fullPath = EditorUtility.SaveFilePanelInProject("Save Modifed Prefab", input.name + " Modified", "prefab", "Save file");
                previousPrefabPath = fullPath;
            }

            if (fullPath != "")
            {
                PrefabUtility.SaveAsPrefabAsset(GOI, fullPath, out success);
            }
            if (success)
                Debug.Log("Prefab Save Successful");
            else
                Debug.LogWarning("Prefab Save Failed or Canceled");
        }
        DestroyImmediate(GOI);
    }

    private Mesh ModifyMesh(GameObject go)
    {

        SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
        Mesh mesh = Instantiate(skinnedMeshRenderer.sharedMesh);
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

        return mesh;
    }
    private void ModifyPrefab(GameObject go, Mesh mesh)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();

        go.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials;

        skinnedMeshRenderer.sharedMesh = replacementMesh;
        skinnedMeshRenderer.quality = SkinQuality.Bone1;
        skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
    }
}