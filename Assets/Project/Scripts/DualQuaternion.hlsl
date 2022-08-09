#ifndef DUAL_QUAT
#define DUAL_QUAT

struct DualQuat{
    float4 qReal;
    float4 qDual;
};

//quaternion methods
float4 q_mul(float4 q1,float4 q2){
    return float4(
        q1.w*q2.x + q1.x*q2.w + q1.y*q2.z - q1.z*q2.y,
        q1.w*q2.y + q1.y*q2.w + q1.z*q2.x - q1.x*q2.z,
        q1.w*q2.z + q1.z*q2.w + q1.x*q2.y - q1.y*q2.x,
        q1.w*q2.w - q1.x*q2.x - q1.y*q2.y - q1.z*q2.z);
}

float q_norm(float4 q){
    return sqrt(q.x * q.x +
                q.y * q.y +
                q.z * q.z +
                q.w * q.w );
}

float4 q_normalize(float4 q){
    float n = q_norm(q);
    q /= n;
    return q;
}

float3 q_rotate(float4 q, float3 v3){
    return v3 + cross(q.xyz*2.0f, cross(q.xyz,v3) + (v3 * q.w));
}

//dual quaternion methods
DualQuat dq_mul(DualQuat dq1, DualQuat dq2){
    DualQuat tmp = dq1;
    float4 qReal = q_mul(dq1.qReal,dq2.qReal);
    float4 qDual = q_mul(dq1.qDual,dq2.qReal) + q_mul(dq1.qReal,dq2.qDual);
    tmp.qReal = qReal;
    tmp.qDual = qDual;
    return tmp;
}

DualQuat dq_mul_s(float s, DualQuat dq1){
    DualQuat tmp = dq1;
    tmp.qReal *= s;
    tmp.qDual *= s;
    return tmp;
}
DualQuat dq_add(DualQuat dq1, DualQuat dq2){
    DualQuat tmp = dq1;
    tmp.qReal += dq2.qReal;
    tmp.qDual += dq2.qDual;
    return tmp;
}

DualQuat dq_identity(){
    DualQuat dq = {float4(0,0,0,1),float4(0,0,0,0)};
    return dq;
}

float3 move(DualQuat dq, float3 v3){
    // Normalize
    float norm = q_norm(dq.qReal);
    float4 qBlendReal = dq.qReal / norm;
    float4 qBlendDual = dq.qDual / norm;

    // Translation: 2.f * qblend_e * conjugate(qblend_0)
    float3 vReal = qBlendReal.xyz;
    float3 vDual = qBlendDual.xyz;
    float3 trans = (vDual * qBlendReal.w - vReal * qBlendDual.w + cross(vReal,vDual)) * 2.0f;

    // Rotate
    return q_rotate(qBlendReal,v3) + trans;
}

float3 dq_rotate(DualQuat dq, float3 n3){
    float4 tmp = q_normalize(dq.qReal);
    return q_rotate(tmp,n3);
}



#endif