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

float4 q_lerp(float4 q1, float4 q2){
    if (dot(q1,q2) >=0)
        return q1 + q2;
    return q1-q2;
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

float4 q_from_R(float4x4 t){
    float trace = 1.0f + t._11 + t._22 + t._33;

    float s, x, y, z, w;

    if (trace > 0.000001f){
        s = sqrt(trace) * 2.0f;

        x = (t._23 - t._32) / s;
        y = (t._31 - t._13) / s;
        z = (t._12 - t._21) / s;
        w = 0.25f * s;
    }
    else if (t._11 > t._22 && t._11 > t._33){
        s  = sqrt(1.0f + t._11 - t._22 - t._33) * 2.0f;

        x = 0.25f * s;
        y = (t._12 + t._21) / s;
        z = (t._31 + t._13) / s;
        w = (t._23 - t._32) / s;
    }
    else if (t._22 > t._33){
        s  = sqrt(1.0f + t._22 - t._11 - t._33) * 2.0f;

        x = (t._12 + t._21) / s;
        y = 0.25f * s;
        z = (t._23 + t._32) / s;
        w = (t._31 - t._13) / s;
    }
    else {
        s  = sqrt(1.0f + t._33 - t._11 - t._22) * 2.0f;

        x = (t._31 + t._13) / s;
        y = (t._23 + t._32) / s;
        z = 0.25f * s;
        w = (t._12 - t._21) / s;
    }
    float4 q ={-x,-y,-z,w};
    return q;
}

float3x3 R_from_q(float4 q){
    q = q_normalize(q);
    float x = q.x;
    float y = q.y;
    float z = q.z;
    float s = q.w;
    float3x3 R = {{1-2*y*y-2*z*z, 2*x*y-2*s*z, 2*x*z+2*s*y},
                  {2*x*y+2*s*z, 1-2*x*x-2*z*z, 2*y*z-2*s*x},
                  {2*x*z-2*s*y, 2*y*z+2*s*x, 1-2*x*x-2*y*y}};
    /*
    float3x3 R = {{2 * (q0 * q0 + q1 * q1) - 1, 2 * (q1 * q2 - q0 * q3), 2 * (q1 * q3 + q0 * q2)},
                  {2 * (q1 * q2 + q0 * q3), 2 * (q0 * q0 + q2 * q2) - 1, 2 * (q2 * q3 - q0 * q1)},
                  {2 * (q1 * q3 - q0 * q2), 2 * (q2 * q3 + q0 * q1), 2 * (q0 * q0 + q3 * q3) - 1}};
    */
    return R;
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