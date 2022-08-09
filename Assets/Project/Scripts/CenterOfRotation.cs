using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class CenterOfRotation : MonoBehaviour
{
    public Mesh testMesh;
    public int focusVertex;
    public float sigma = 1;

    //only needed for testing this value will be aquired properly for the full implementation
    public int bone_count = 64;
    public float gizmosScale = 0.1f;

    private int focusVertex_old = int.MaxValue;
    private float sigma_old = 1;

    private float[] emptyArray;
    private List<Vector4> bone_ids;
    private int[] tris;
    private List<Vector4> weights;
    private int vert_count;
    private int tri_count;

    private float[] similarity_results;
    private Vector3[] vert_positions;
    private float3 Opt_CoR;

    // Start is called before the first frame update
    void Start()
    {
        bone_ids = new List<Vector4>();
        testMesh.GetUVs(1,bone_ids);
        vert_count = bone_ids.Count; 

        tris = testMesh.GetIndices(0);
        tri_count = tris.Length/3;

        weights = new List<Vector4>();
        testMesh.GetUVs(2,weights);

        emptyArray = new float[bone_count];
        similarity_results = new float[tri_count];
        vert_positions = testMesh.vertices;
    }

    // Update is called once per frame
    void Update()
    {
        //update only when test variables change
        if (focusVertex != focusVertex_old || sigma != sigma_old){
            similarity_results = new float[tri_count];
            Debug.Log("updating similarity...");
            focusVertex_old = focusVertex;
            sigma_old = sigma;

            Opt_CoR = computeCoR(focusVertex);
            Debug.Log("CoR " + Opt_CoR);
        }
    }

    private void makeTriWeightsList(int tri_id, List<float> w_in){
        int id,vert_id;
        for (int i = 0; i < 3; i++){
            for (int j = 0; j < 4; j++){
                vert_id = tris[(tri_id*3)+i];
                id = Mathf.RoundToInt(bone_ids[vert_id][j]);
                w_in[id] += weights[vert_id][j];
            }
        }
        w_in.ForEach(i => i = i/3);
    }

    /*
    s(w_p,w_v,w_ids) 
    --> similarity between verticies p and v
    --> (global variable) sigma controls the width of the exponential kernel 
    --> w_ids: this implementation only needs to know about the 4 bones per p vertex 
        -->since if the skin weight for either j or k are 0, then the result of that component is 0

    j and k represent bones (skin weight array indices) 
    --> sum the similarity between p and v when j and k are not equal
    */
    private float s(List<float> w_p, List<float> w_v, List<int> w_ids){

        float similarity = 0;

        for(int j = 0; j < 4; j++){
            for(int k = 0; k < 4; k++){
                if (j != k && (w_p[j] > 0.0001f && w_p[k] > 0.0001f )){
                    float contribution = w_p[j] * w_p[k] * w_v[w_ids[j]] * w_v[w_ids[k]];
                    float distance = math.pow(math.E, -(math.pow(w_p[j] * w_v[w_ids[k]] - w_p[k] * w_v[w_ids[j]] ,2)/(sigma*sigma)));
                    similarity += contribution*distance;
                }
            }
        }
        return similarity;
    }

    private float3 computeCoR(int p_i){
        List<float> w_p = new List<float>{
                weights[p_i].x,
                weights[p_i].y,
                weights[p_i].z,
                weights[p_i].w};
        List<int> w_ids = new List<int>{
                Mathf.RoundToInt(bone_ids[p_i].x),
                Mathf.RoundToInt(bone_ids[p_i].y),
                Mathf.RoundToInt(bone_ids[p_i].z),
                Mathf.RoundToInt(bone_ids[p_i].w)};
        printList(w_ids);

        //precompute similarity
        for (int t = 0; t < tri_count; t++){
            //initialize comparison list
            List<float> w_v = new List<float>(emptyArray);
            makeTriWeightsList(t,w_v);

            //calculate similarity
            similarity_results[t] = s(w_p, w_v, w_ids);
        }
        
        //sum numerator and denominator separately
        //use similarity results now
        float3 numerator = 0;
        float denominator = 0;
        for (int t = 0; t < tri_count; t++){
            //verts in triangle
            float3 v1 = vert_positions[tris[(t*3)+0]];
            float3 v2 = vert_positions[tris[(t*3)+1]];
            float3 v3 = vert_positions[tris[(t*3)+2]];
            //area 
            float a = math.length(math.cross(v1-v2,v1-v3))/2.0f;

            numerator += similarity_results[t]*((v1+v2+v3)/3.0f)*a;
            denominator += similarity_results[t]*a;
        }

        //final calculation
        Debug.Log("denominator " + denominator);
        return numerator/denominator;
    }

    private void OnDrawGizmos() {
        //draw similarity results
        if (similarity_results != null){
            for (int i = 0; i < tri_count*3; i++){
                if (similarity_results[i/3] > 0.00001f)
                    if (tris[i] == focusVertex){
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(transform.TransformPoint(vert_positions[tris[i]]),gizmosScale*similarity_results[i/3]);
                    }
                    else{
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireSphere(transform.TransformPoint(vert_positions[tris[i]]),gizmosScale*similarity_results[i/3]);
                    }
            }
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(Opt_CoR),gizmosScale*0.1f);
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
