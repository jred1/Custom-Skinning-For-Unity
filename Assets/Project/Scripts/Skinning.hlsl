
#ifndef VOXEL_MESH_INFO
#define VOXEL_MESH_INFO

StructuredBuffer<float4x4> bones;
StructuredBuffer<float4x4> bindPose;

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

    float totalWeight = bone_weights.x + bone_weights.y + bone_weights.z + bone_weights.w;

    float4x4 relativeTransform;
    /*
    relativeTransform = (bone_weights[0]/ totalWeight) * mul(bones[bone_ids[0]], bindPose[bone_ids[0]]); 
    for(int i = 1; i < bone_count && bone_weights[i] > 0.001; i++){
        relativeTransform =  relativeTransform + (bone_weights[i] / totalWeight) * mul(bones[bone_ids[i]], bindPose[bone_ids[i]]);
    }*/
    relativeTransform = (bone_weights.x/ totalWeight) * mul(bones[bone_ids.x], bindPose[bone_ids.x]);  
    relativeTransform =  relativeTransform + (bone_weights.y / totalWeight) * mul(bones[bone_ids.y], bindPose[bone_ids.y]);
    relativeTransform =  relativeTransform + (bone_weights.z / totalWeight) * mul(bones[bone_ids.z], bindPose[bone_ids.z]);
    relativeTransform =  relativeTransform + (bone_weights.w / totalWeight) * mul(bones[bone_ids.w], bindPose[bone_ids.w]);
    v = mul(relativeTransform, pos);
    n = mul(relativeTransform, norm);
}

#endif
