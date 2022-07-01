
#ifndef VOXEL_MESH_INFO
#define VOXEL_MESH_INFO
StructuredBuffer<float4x4> volume;
float tester;

void Direct_float(float test_id, float3 pos, float4 bone_id, float4 bone_w, out float3 v)
{
    //float4x4 t = volume[bone_id.x];

    v = float3(1, 1, 1);
    
    if ((int)(bone_id.x) == test_id) {
        v *= pos + (.1*bone_w.x *bone_w.x * tester);
    }
    else {
        v *= pos;
    }
    
}
#endif
