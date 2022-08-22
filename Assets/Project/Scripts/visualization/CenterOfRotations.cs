using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class CenterOfRotations : MonoBehaviour
{
    #region public variables
    public Mesh testMesh;
    public float sigma = 1;
    public float weightThreshold = 0.01f;

    //only needed for testing this value will be aquired properly for the full implementation
    public int bone_count = 64;
    public float gizmosScale = 0.1f;

    public bool update = true;
    #endregion

    #region private variables
    private float[] emptyArray;
    private List<Vector4> boneIDs;
    private int[] tris;
    private List<Vector4> weights;
    private int vertCount;
    private int triCount;

    private float[] similarityResults;
    private Vector3[] vertPos;
    private float3[] Opt_CoRs;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        boneIDs = new List<Vector4>();
        testMesh.GetUVs(1,boneIDs);
        vertCount = boneIDs.Count; 

        tris = testMesh.GetIndices(0);
        triCount = tris.Length/3;

        weights = new List<Vector4>();
        testMesh.GetUVs(2,weights);

        emptyArray = new float[bone_count];
        similarityResults = new float[triCount];
        Opt_CoRs = new float3[vertCount];
        vertPos = testMesh.vertices;
    }

    // Update is called once per frame
    void Update()
    {
        //update only when test variables change
        if (update){
            Debug.Log("updating Centers of Rotation...");
            for(int v = 0; v < vertCount; v++)
                Opt_CoRs[v] = ComputeCoR(v);
            update = false;
        }
    }

    private void MakeTriWeightsList(int tri_id, List<float> w_in){
        int id,vert_id;
        for (int i = 0; i < 3; i++){
            for (int j = 0; j < 4; j++){
                vert_id = tris[(tri_id*3)+i];
                id = Mathf.RoundToInt(boneIDs[vert_id][j]);
                w_in[id] += weights[vert_id][j];
            }
        }
        w_in.ForEach(i => i = i/3);
    }

    ///<summary>
    /// Pair-wise bone weighting similarity between verticies p and v
    ///</summary>
    private float S(List<float> w_p, List<float> w_v, List<int> w_pIDs){
        // the global variable sigma controls the width of the exponential kernel
        // w_ids: this implementation only needs to know about the 4 bones per p vertex 
        // --> since if the skin weight for either j or k are 0, then the result of that term is 0

        // j and k represent bones (skin weight array indices) 
        // sum the similarity between p and v when j and k are not equal
        float similarity = 0;
        for(int j = 0; j < 4; j++){
            for(int k = 0; k < 4; k++){
                //math: each term equals (w_pj * w_pk * w_vj * w_vk) * e^( -((w_pj * w_vk - w_pk * w_vj)^2)/(sigma^2) )
                if (j != k && (w_p[j] > 0.0001f && w_p[k] > 0.0001f )){
                    float contribution = w_p[j] * w_p[k] * w_v[w_pIDs[j]] * w_v[w_pIDs[k]];
                    float distance = math.pow(math.E, -(math.pow(w_p[j] * w_v[w_pIDs[k]] - w_p[k] * w_v[w_pIDs[j]] ,2)/(sigma*sigma)));
                    similarity += contribution*distance;
                }
            }
        }
        return similarity;
    }

    private float3 ComputeCoR(int p_i){
        List<float> w_p = new List<float>{
                weights[p_i].x,
                weights[p_i].y,
                weights[p_i].z,
                weights[p_i].w};
        List<int> w_pIDs = new List<int>{
                Mathf.RoundToInt(boneIDs[p_i].x),
                Mathf.RoundToInt(boneIDs[p_i].y),
                Mathf.RoundToInt(boneIDs[p_i].z),
                Mathf.RoundToInt(boneIDs[p_i].w)};
        similarityResults = new float[triCount];

        // only computed if there is more than one bone for the given vertex
        if (w_p[1] > weightThreshold){

            // precompute similarity
            for (int t = 0; t < triCount; t++){
                // initialize comparison list
                List<float> w_v = new List<float>(emptyArray);
                MakeTriWeightsList(t,w_v);

                // calculate similarity
                similarityResults[t] = S(w_p, w_v, w_pIDs);
            }

            // sum numerator and denominator separately
            float3 numerator = 0;
            float denominator = 0;
            for (int t = 0; t < triCount; t++){
                // verts in the triangle
                float3 v1 = vertPos[tris[(t*3)+0]];
                float3 v2 = vertPos[tris[(t*3)+1]];
                float3 v3 = vertPos[tris[(t*3)+2]];
                // area of the trianhle
                float a = math.length(math.cross(v1-v2,v1-v3))/2.0f;

                numerator += similarityResults[t]*((v1+v2+v3)/3.0f)*a;
                denominator += similarityResults[t]*a;
            }

            return numerator/denominator;
        }
        else return new float3(float.NaN,float.NaN,float.NaN);
    }

    private void OnDrawGizmos() {
        // draw similarity results
        if (!update){
            int counter = 0;
            for (int i = 0; i < vertCount; i++){
                if (!(float.IsNaN(Opt_CoRs[i].x) || float.IsNaN(Opt_CoRs[i].y) || float.IsNaN(Opt_CoRs[i].z))){
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(transform.TransformPoint(Opt_CoRs[i]),gizmosScale*0.1f);
                    counter++;
                }
            }
            Debug.Log(counter);
        }
    }
    private void printList<T>(List<T> l){
        string str = "[";
        for(int i = 0; i < l.Count; i++){
            str += l[i] + ", ";
        }
        str += "]";
        Debug.Log(str);
    }
}
