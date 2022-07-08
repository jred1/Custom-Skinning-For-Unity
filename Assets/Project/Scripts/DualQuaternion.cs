using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct dualQuat
{
    private quat qReal;
    private quat qDual;

    #region Constructors
    public dualQuat(quat real, quat dual){
        qReal = real;
        qDual = dual;
    }
    public dualQuat(Matrix4x4 transform){
        qReal = new quat(transform);
        float3 translation = new float3(transform.m03,transform.m13,transform.m23);
        qDual = quat_dual_from(qReal,translation);
    }
    #endregion

    #region Methods
    private static quat quat_dual_from( quat q, float3 t){
        float x =  0.5f*( t.x * q.w() + t.y * q.z() - t.z * q.y());
        float y =  0.5f*(-t.x * q.z() + t.y * q.w() + t.z * q.x());
        float z =  0.5f*( t.x * q.y() - t.y * q.x() + t.z * q.w());
        float w = -0.5f*( t.x * q.x() + t.y * q.y() + t.z * q.z());

        return new quat(x,y,z,w);
    }

    public static dualQuat identity(){
        return new dualQuat(new quat(0, 0, 0, 1), new quat(0, 0, 0, 0));
    }

    public float3 move(float3 v3){
        //Normalize
        float norm = qReal.norm();
        quat qBlendReal = qReal / norm;
        quat qBlendDual = qDual / norm;

        //Translation: 2.f * qblend_e * conjugate(qblend_0)
        float3 vReal = qBlendReal.xyz();
        float3 vDual = qBlendDual.xyz();
        float3 trans = (vDual * qBlendReal.w() -
                        vReal * qBlendDual.w() +
                        math.cross(vReal,vDual))
                        * 2.0f;

        // Rotate
        return qBlendReal.rotate(v3) + trans;
    }

    public float3 rotate(float3 n3){
        quat tmp = qReal;
        tmp.normalize();
        return tmp.rotate(n3);
    }
    #endregion

    #region Operators
    public static dualQuat operator +(dualQuat dq1, dualQuat dq2) {
        return new dualQuat(dq1.qReal + dq2.qReal, dq1.qDual + dq2.qDual);
    }

    public static dualQuat operator *(dualQuat dq1, float f1) {
        return new dualQuat(dq1.qReal * f1, dq1.qDual * f1);
    }

    public static dualQuat operator *(float f1, dualQuat dq1) {
        return new dualQuat(dq1.qReal * f1, dq1.qDual * f1);
    }
    #endregion
}
