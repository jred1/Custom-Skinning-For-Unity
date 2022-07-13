
#ifndef VOXEL_MESH_INFO
#define VOXEL_MESH_INFO

#include "Assets/Project/Scripts/DualQuaternion.hlsl"

StructuredBuffer<float4x4> bones;
StructuredBuffer<float4x4> bindPose;

StructuredBuffer<dualQuat> bonesDQ;
StructuredBuffer<dualQuat> bindPoseDQ;

void Direct_float(float4 pos, float4 norm, float4 bone_ids, float4 bone_weights, out float4 v, out float4 n)
{
    
    float4x4 relativeTransform = mul(bones[bone_ids.x], bindPose[bone_ids.x]);
    v = mul(relativeTransform, pos);
    n = mul(relativeTransform, norm);

}

void Linear_float(float4 pos, float4 bone_ids, float4 bone_weights, out float4 v) 
{

    float totalWeight = bone_weights.x + bone_weights.y + bone_weights.z + bone_weights.w;

    float4x4 relativeTransform = mul(bones[bone_ids.x], bindPose[bone_ids.x]);
    v = (bone_weights.x / totalWeight) * mul(relativeTransform, pos);

    relativeTransform = mul(bones[bone_ids.y], bindPose[bone_ids.y]);
    v += (bone_weights.y / totalWeight) * mul(relativeTransform, pos);

    relativeTransform = mul(bones[bone_ids.z], bindPose[bone_ids.z]);
    v += (bone_weights.z / totalWeight) * mul(relativeTransform, pos);

    relativeTransform = mul(bones[bone_ids.w], bindPose[bone_ids.w]);
    v += (bone_weights.w / totalWeight) * mul(relativeTransform, pos);
}

void Linear_Opt_float(float4 pos, float4 norm, float4 bone_ids, float4 bone_weights, float bone_count, out float4 v, out float4 n) 
{
    int i;
    float totalWeight = 0;
    for(i = 0; i < bone_count; i++){
        totalWeight +=  bone_weights[i];
    }

    float4x4 relativeTransform = (bone_weights[0]/ totalWeight) * mul(bones[bone_ids[0]], bindPose[bone_ids[0]]);
    for(i = 1; i < bone_count; i++){
        relativeTransform =  relativeTransform + (bone_weights[i] / totalWeight) * mul(bones[bone_ids[i]], bindPose[bone_ids[i]]);
    }

    v = mul(relativeTransform, pos);
    n = mul(relativeTransform, norm);
}

void Dual_Quaternion_float(float3 pos, float3 norm, float4 bone_ids, float4 bone_weights, float bone_count, out float3 v, out float3 n)
{
    int i;
    float totalWeight = 0;
    for(i = 0; i < bone_count; i++){
        totalWeight +=  bone_weights[i];
    }

    dualQuat dq = dq_mul(bonesDQ[bone_ids[0]], bindPoseDQ[bone_ids[0]]);
    float4 q0 = dq.qReal;
    
    dualQuat dq_blend = dq_mul_s((bone_weights[0] / totalWeight), dq);
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

#endif
