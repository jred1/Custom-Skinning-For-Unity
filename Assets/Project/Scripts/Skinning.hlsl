
#ifndef VOXEL_MESH_INFO
#define VOXEL_MESH_INFO

#include "Assets/Project/Scripts/DualQuaternion.hlsl"

StructuredBuffer<float4x4> bones;
StructuredBuffer<float4x4> bindPose;

float4x4 meshMatrix;

StructuredBuffer<DualQuat> bonesDQ;
StructuredBuffer<DualQuat> bindPoseDQ;

void Direct_float(float4 pos, float4 norm, float4 bone_ids, float4 bone_weights, out float4 v, out float4 n)
{
    float4x4 relativeTransform = mul(bones[bone_ids.x], bindPose[bone_ids.x]);
    v = mul(relativeTransform, pos);
    n = mul(relativeTransform, norm);

}

void Linear_float(float3 pos, float3 norm, float4 bone_ids, float4 bone_weights, float bone_count, out float3 v, out float3 n) 
{
    int i;
    float totalWeight = 0;
    for(i = 0; i < bone_count; i++){
        totalWeight +=  bone_weights[i];
    }

    float4x4 relativeTransform;
    for(i = 0; i < bone_count; i++){
        relativeTransform =  relativeTransform + (bone_weights[i] / totalWeight) * mul(bones[bone_ids[i]], bindPose[bone_ids[i]]);
    }

    v = mul(relativeTransform, float4(pos,1)).xyz;
    n = mul(relativeTransform, float4(norm,0)).xyz;
}

void Dual_Quaternion_float(float3 pos, float3 norm, float4 bone_ids, float4 bone_weights, float bone_count, out float3 v, out float3 n)
{
    int i;
    float totalWeight = 0;
    for(i = 0; i < bone_count; i++){
        totalWeight +=  bone_weights[i];
    }

    DualQuat dq = dq_mul(bonesDQ[bone_ids[0]], bindPoseDQ[bone_ids[0]]);
    float4 q0 = dq.qReal;

    DualQuat dq_blend = dq_mul_s((bone_weights[0] / totalWeight), dq);
    for(i = 1; i < bone_count; i++){
        dq = dq_mul(bonesDQ[bone_ids[i]], bindPoseDQ[bone_ids[i]]);
        float w = bone_weights[i];
        
        if(dot(dq.qReal,q0) < 0.0f )
                w *= -1.0f;

        dq_blend = dq_add(dq_blend, dq_mul_s((w / totalWeight), dq));
    }
    v = move(dq_blend,pos);
    n = dq_rotate(dq_blend,norm);
}

/*
Input: n vertices, vertex i includes:
    • Rest pose position vi ∈ R^3
    • Skinning weights wi ∈ R^m
    • CoR p*_i ∈ R^3 computed by Eq. (1) and Eq. (4)
   m bones, bone j transformation is [R_j t_j ] ∈ R^3×4
*/

/*
Output: Deformed position v′_i ∈ R^3 for all vertices i = 1..n
1: for each bone j do
2:  Convert rotation matrix Rj to unit quaternion qj
3: end for
4: for each vertex i do
5: q ← w_i1 * q1 ⊕ w_i2 * q2 ⊕ . . . ⊕ w_im * qm
where: qa ⊕ qb =
    qa + qb if qa · qb ≥ 0
    qa − qb if qa · qb < 0
(qa · qb denotes the vector dot product)

6: Normalize and convert q to rotation matrix R
7: LBS: [R' t'] ← ∑m j=1: w_ij * [R_j t_j]
8: Compute translation: t ← R'_p∗_i + t' − Rp∗_i (Eq. (3b))
9: v′_i ← Rv_i + t
10: end for
*/
void CoR_float(float3 pos, float3 norm, float4 bone_ids, float4 bone_weights, float4 opt_cor, float bone_count, out float3 v, out float3 n)
{
    if (opt_cor.w > .99){
        int j;
        float totalWeight = 0;
        float4 q_j[4];
        float3x3 R_j[4];
        float3 t_j[4];

        float4x4 M_j;
        for(j = 0; j < bone_count; j++){
            totalWeight +=  bone_weights[j];

            //Move to CPU?
            M_j = mul(bones[bone_ids[j]], bindPose[bone_ids[j]]);
            R_j[j] = float3x3(M_j._11_12_13,M_j._21_22_23,M_j._31_32_33);
            q_j[j] = q_from_R(M_j);
            t_j[j] = M_j._14_24_34;
        }

        float weights_normalized[4];
        for(j = 0; j < bone_count; j++){
            weights_normalized[j] = (bone_weights[j] / totalWeight);
        }

        //QLERP
        float4 q = weights_normalized[0] * q_j[0];
        for(j = 1; j < bone_count; j++){
            q = q_lerp(q,weights_normalized[j] * q_j[j]);
        }
        float3x3 R = R_from_q(q);

        //LBS
        float3x3 R_LBS;
        float3 t_LBS;
        for(j = 0; j < bone_count; j++){
            R_LBS = R_LBS + weights_normalized[j] * R_j[j];
            t_LBS = t_LBS + weights_normalized[j] * t_j[j];
        }

        float3 CoR = opt_cor.xyz;
        float3 t = mul(R_LBS, CoR) + t_LBS - mul(R, CoR);
        v = mul(R, pos) + t;
        n = mul(R, norm);

        //float3 t = mul(R_LBS, CoR) + t_LBS - q_rotate(q, CoR);
        //v = q_rotate(q, pos) + t;
        //n = q_rotate(q, norm);
    }
    else{
        float4x4 relativeTransform = mul(bones[bone_ids.x], bindPose[bone_ids.x]);
        v = (mul(relativeTransform, float4(pos,1))).xyz;
        n = mul(relativeTransform, float4(norm,0)).xyz;
    }
}

// slightly modified version, should be faster
void Opt_CoR_float(float3 pos, float3 norm, float4 bone_ids, float4 bone_weights, float4 opt_cor, float bone_count, out float3 v, out float3 n)
{
    if (opt_cor.w > .99){
        int j;
        float totalWeight = 0;
        float4 q_j[4];
        float3x3 R_j[4];
        float3 t_j[4];

        float4x4 M_j;
        for(j = 0; j < bone_count; j++){
            totalWeight +=  bone_weights[j];

            //Move to CPU?
            M_j = mul(bones[bone_ids[j]], bindPose[bone_ids[j]]);
            R_j[j] = float3x3(M_j._11_12_13,M_j._21_22_23,M_j._31_32_33);
            q_j[j] = q_from_R(M_j);
            t_j[j] = M_j._14_24_34;
        }

        float weights_normalized[4];
        for(j = 0; j < bone_count; j++){
            weights_normalized[j] = (bone_weights[j] / totalWeight);
        }

        //QLERP
        float4 q = weights_normalized[0] * q_j[0];
        for(j = 1; j < bone_count; j++){
            q = q_lerp(q,weights_normalized[j] * q_j[j]);
        }

        //LBS
        float3x3 R_LBS;
        float3 t_LBS;
        for(j = 0; j < bone_count; j++){
            R_LBS = R_LBS + weights_normalized[j] * R_j[j];
            t_LBS = t_LBS + weights_normalized[j] * t_j[j];
        }

        float3 CoR = opt_cor.xyz;
        float3 t = mul(R_LBS, CoR) + t_LBS - q_rotate(q, CoR);
        v = q_rotate(q, pos) + t;
        n = q_rotate(q, norm);
    }
    else{
        float4x4 relativeTransform = mul(bones[bone_ids.x], bindPose[bone_ids.x]);
        v = (mul(relativeTransform, float4(pos,1))).xyz;
        n = mul(relativeTransform, float4(norm,0)).xyz;
    }
}

#endif
