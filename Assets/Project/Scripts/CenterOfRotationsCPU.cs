using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class CenterOfRotationsCPU : MonoBehaviour
{
    #region public variables
    public Mesh testMesh;
    public bool displayGizmos = true;
    public float gizmosScale = 0.1f;
    public float sigma = 1;
    private float sigma_old;

    //only needed for testing this value will be aquired properly for the full implementation
    public int boneCount = 64;

    public ComputeShader computeShader;
    public bool update = true;
    #endregion

    #region private variables
    private int vertCount;
    private int triCount;
    private int[] triIndices;
    private List<Vector4> boneIDs;
    private List<Vector4> weights;
    private Vector3[] vertPos;

    private float3[] opt_CoRs;

    private ComputeBuffer _triIndices, _boneIDs, _weights, _vertPos;

    private ComputeBuffer _similarity;
    private ComputeBuffer _opt_CoR;

    private int similarityKernel;
    private int opt_CoRKernel;
    #endregion

    void Start()
    {
        boneIDs = new List<Vector4>();
        testMesh.GetUVs(1,boneIDs);
        vertCount = boneIDs.Count; 

        triIndices = testMesh.GetIndices(0);
        triCount = triIndices.Length/3;

        weights = new List<Vector4>();
        testMesh.GetUVs(2,weights);

        opt_CoRs = new float3[vertCount];
        vertPos = testMesh.vertices;

        similarityKernel = computeShader.FindKernel("Similarity");
        opt_CoRKernel = computeShader.FindKernel("CoR");

        MakeBuffers();
    }

    void Update()
    {
        //update only when test variables change
        if (update || sigma_old != sigma){
            update = false;
            sigma_old = sigma;
            Debug.Log("updating Centers of Rotation...");
            PassParameters();
            int groups = Mathf.CeilToInt(((float)vertCount)/32.0f);
            computeShader.Dispatch(similarityKernel,groups,1,1);
            computeShader.Dispatch(opt_CoRKernel,groups,1,1);
            _opt_CoR.GetData(opt_CoRs);
            //printList(new List<float3>(opt_CoRs),1000);
        }
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

    void PassParameters(){
        computeShader.SetInt("_vertCount",vertCount);
        computeShader.SetInt("_triCount",triCount);
        computeShader.SetInt("_boneCount",boneCount);
        computeShader.SetFloat("_sigma",sigma);
    }

    private void OnDrawGizmos() {
        // draw similarity results
        if (!update && displayGizmos){
            int counter = 0;
            for (int i = 0; i < vertCount; i++){
                if (!(float.IsNaN(opt_CoRs[i].x) || float.IsNaN(opt_CoRs[i].y) || float.IsNaN(opt_CoRs[i].z))){
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(transform.TransformPoint(opt_CoRs[i]),gizmosScale*0.1f);
                    counter++;
                }
            }
            Debug.Log(counter);
        }
    }

    private void printList<T>(List<T> l, int count){
        string str = "[";
        for(int i = 0; i < l.Count && i < count; i++){
            str += l[i] + ", ";
        }
        str += "]";
        Debug.Log(str);
    }

    private void OnDestroy() {
        _triIndices?.Release();
        _boneIDs?.Release();
        _weights?.Release();
        _vertPos?.Release();
        _similarity?.Release();
        _opt_CoR?.Release();
    }
}
