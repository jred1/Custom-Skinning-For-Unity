using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Similarity : MonoBehaviour
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
    private List<Vector4> ids;
    private int[] tris;
    private List<Vector4> weights;
    private int vert_count;
    private int tri_count;

    private float[] similarity_results;
    private Vector3[] vert_positions;

    // Start is called before the first frame update
    void Start()
    {
        ids = new List<Vector4>();
        testMesh.GetUVs(1,ids);
        vert_count = ids.Count; 

        tris = testMesh.GetIndices(0);
        tri_count = tris.Length/3;

        weights = new List<Vector4>();
        testMesh.GetUVs(2,weights);

        emptyArray = new float[bone_count];
        //similarity_results = new float[vert_count];
        similarity_results = new float[tri_count];
        vert_positions = testMesh.vertices;
    }

    // Update is called once per frame
    void Update()
    {
        //vert version
        /*
        //update only when test variables change
        if (focusVertex != focusVertex_old || sigma != sigma_old){
            similarity_results = new float[vert_count];
            Debug.Log("updating similarity...");
            focusVertex_old = focusVertex;
            sigma_old = sigma;
            
            List<float> w_p = new List<float>{
                    weights[focusVertex].x,
                    weights[focusVertex].y,
                    weights[focusVertex].z,
                    weights[focusVertex].w};
            List<int> w_ids = new List<int>{
                    Mathf.RoundToInt(ids[focusVertex].x),
                    Mathf.RoundToInt(ids[focusVertex].y),
                    Mathf.RoundToInt(ids[focusVertex].z),
                    Mathf.RoundToInt(ids[focusVertex].w)};
            printList(w_ids);

            for (int v = 0; v < vert_count; v++){
                //initialize comparison list
                List<float> w_v = new List<float>(emptyArray);
                makeVertWeightsList(v,w_v);

                //calculate similarity
                similarity_results[v] = s(w_p, w_v, w_ids);
            }
        }
        */
        //update only when test variables change
        if (focusVertex != focusVertex_old || sigma != sigma_old){
            similarity_results = new float[tri_count];
            Debug.Log("updating similarity...");
            focusVertex_old = focusVertex;
            sigma_old = sigma;
            
            List<float> w_p = new List<float>{
                    weights[focusVertex].x,
                    weights[focusVertex].y,
                    weights[focusVertex].z,
                    weights[focusVertex].w};
            List<int> w_ids = new List<int>{
                    Mathf.RoundToInt(ids[focusVertex].x),
                    Mathf.RoundToInt(ids[focusVertex].y),
                    Mathf.RoundToInt(ids[focusVertex].z),
                    Mathf.RoundToInt(ids[focusVertex].w)};
            printList(w_ids);

            for (int t = 0; t < tri_count; t++){
                //initialize comparison list
                List<float> w_v = new List<float>(emptyArray);
                makeTriWeightsList(t,w_v);

                //calculate similarity
                similarity_results[t] = s(w_p, w_v, w_ids);
            }
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

    private void makeVertWeightsList(int vert_id, List<float> w_in){
        int[] ids_INT = {Mathf.RoundToInt(ids[vert_id].x),
                        Mathf.RoundToInt(ids[vert_id].y),
                        Mathf.RoundToInt(ids[vert_id].z),
                        Mathf.RoundToInt(ids[vert_id].w)};
        w_in[ids_INT[0]] = weights[vert_id].x;
        w_in[ids_INT[1]] = weights[vert_id].y;
        w_in[ids_INT[2]] = weights[vert_id].z;
        w_in[ids_INT[3]] = weights[vert_id].w;
    }
    
    private void makeTriWeightsList(int tri_id, List<float> w_in){
        int id,vert_id;
        for (int i = 0; i < 3; i++){
            for (int j = 0; j < 4; j++){
                vert_id = tris[(tri_id*3)+i];
                id = Mathf.RoundToInt(ids[vert_id][j]);
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
        //printList(w_p);
        //printList(w_v);

        for(int j = 0; j < 4; j++){
            for(int k = 0; k < 4; k++){
                if (j != k && (w_p[j] > 0.0001f && w_p[k] > 0.0001f )){
                    float contribution = w_p[j] * w_p[k] * w_v[w_ids[j]] * w_v[w_ids[k]];
                    //Debug.Log(w_p[j] + " * " + w_p[k] + " * " + w_v[w_ids[j]] + " * " + w_v[w_ids[k]]);
                    float distance = math.pow(math.E, -(math.pow(w_p[j] * w_v[w_ids[k]] - w_p[k] * w_v[w_ids[j]] ,2)/(sigma*sigma)));
                    //Debug.Log(distance);
                    similarity += contribution*distance;
                }
            }
        }
        //Debug.Log(similarity);
        return similarity;
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
        }
    }
}
