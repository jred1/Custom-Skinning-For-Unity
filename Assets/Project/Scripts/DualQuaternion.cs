using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[System.Serializable]
public struct DualQuat
{
    [field: SerializeField]
    public Quat qReal { get; private set; }

    [field: SerializeField]
    public Quat qDual { get; private set; }

    #region Constructors
    public DualQuat(Quat real, Quat dual){
        qReal = real;
        qDual = dual;
    }

    public DualQuat(Matrix4x4 transform){
        qReal = new Quat(transform);
        float3 translation = new float3(transform.m03,transform.m13,transform.m23);
        qDual = qDualFrom(qReal,translation);
    }
    #endregion

    #region Methods
    //Expensive?
    private static Quat qDualFrom(Quat q, float3 t){
        float x =  0.5f*( t.x * q.coeff.w + t.y * q.coeff.z - t.z * q.coeff.y);
        float y =  0.5f*(-t.x * q.coeff.z + t.y * q.coeff.w + t.z * q.coeff.x);
        float z =  0.5f*( t.x * q.coeff.y - t.y * q.coeff.x + t.z * q.coeff.w);
        float w = -0.5f*( t.x * q.coeff.x + t.y * q.coeff.y + t.z * q.coeff.z);

        return new Quat(x,y,z,w);
    }

    public static DualQuat Identity(){
        return new DualQuat(new Quat(0, 0, 0, 1), new Quat(0, 0, 0, 0));
    }

    public static DualQuat Normalize(DualQuat q)
    {
        float mag = q.qReal.Dot(q.qReal);
        DualQuat tmp = q;
        tmp.qReal *= 1.0f / mag;
        tmp.qDual *= 1.0f / mag;
        return tmp;
    }

    public static Matrix4x4 DualQuaternionToMatrix(DualQuat q)
    {
        q = DualQuat.Normalize( q );
        Matrix4x4 M = Matrix4x4.identity;
        float w = q.qReal.coeff.w;
        float x = q.qReal.coeff.x;
        float y = q.qReal.coeff.y;
        float z = q.qReal.coeff.z;
        // Extract rotational information
        M.m00 = w*w + x*x - y*y - z*z;
        M.m10 = 2*x*y + 2*w*z;
        M.m20 = 2*x*z - 2*w*y;

        M.m01 = 2*x*y - 2*w*z;
        M.m11 = w*w + y*y - x*x - z*z;
        M.m21 = 2*y*z + 2*w*x;
        M.m02 = 2*x*z + 2*w*y;
        M.m12 = 2*y*z - 2*w*x;
        M.m22 = w*w + z*z - x*x - y*y;
        // Extract translation information
        Quat t = (q.qDual * 2.0f) * q.qReal.Conjugate();
        M.m03 = t.coeff.x;
        M.m13 = t.coeff.y;
        M.m23 = t.coeff.z;
        return M;
    }

    public float3 Move(float3 v3){
        //Normalize
        float norm = qReal.Norm();
        Quat qBlendReal = qReal / norm;
        Quat qBlendDual = qDual / norm;

        //Translation: 2.f * qblend_e * conjugate(qblend_0)
        float3 vReal = qBlendReal.coeff.xyz;
        float3 vDual = qBlendDual.coeff.xyz;
        float3 trans = (vDual * qBlendReal.coeff.w -
                        vReal * qBlendDual.coeff.w +
                        math.cross(vReal,vDual))
                        * 2.0f;

        // Rotate
        return qBlendReal.Rotate(v3) + trans;
    }

    public float3 Rotate(float3 n3){
        Quat tmp = qReal;
        tmp.Normalize();
        return tmp.Rotate(n3);
    }
    #endregion

    #region Operators
    public static DualQuat operator +(DualQuat dq1, DualQuat dq2) {
        return new DualQuat(dq1.qReal + dq2.qReal, dq1.qDual + dq2.qDual);
    }

    public static DualQuat operator *(DualQuat dq1, DualQuat dq2) {
        return new DualQuat(dq1.qReal*dq2.qReal, 
                            dq1.qDual*dq2.qReal + dq1.qReal*dq2.qDual);
    }

    public static DualQuat operator *(DualQuat dq1, float f1) {
        return new DualQuat(dq1.qReal * f1, dq1.qDual * f1);
    }

    public static DualQuat operator *(float f1, DualQuat dq1) {
        return new DualQuat(dq1.qReal * f1, dq1.qDual * f1);
    }
    #endregion

    public override string ToString(){
        return "real: ( " + qReal.coeff.x + ", " + qReal.coeff.y + ", " + qReal.coeff.z + ", " + qReal.coeff.w + ")\n" + 
               "dual: ( " + qDual.coeff.x + ", " + qDual.coeff.y + ", " + qDual.coeff.z + ", " + qDual.coeff.w + ")\n";
    }
}
