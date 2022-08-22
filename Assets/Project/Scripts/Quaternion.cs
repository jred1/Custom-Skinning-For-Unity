using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[System.Serializable]
public struct Quat
{
    [field: SerializeField]
    public float4 coeff { get; private set; }

    #region Constructors
    public Quat(Matrix4x4 t)
    {
        float trace = 1.0f + t.m00 + t.m11 + t.m22;

        float s, x, y, z, w;

        if (trace > 0.000001f){
            s = math.sqrt(trace) * 2.0f;

            x = (t.m12 - t.m21) / s;
            y = (t.m20 - t.m02) / s;
            z = (t.m01 - t.m10) / s;
            w = 0.25f * s;
        }
        else if (t.m00 > t.m11 && t.m00 > t.m22){
            s  = math.sqrt(1.0f + t.m00 - t.m11 - t.m22) * 2.0f;

            x = 0.25f * s;
            y = (t.m01 + t.m10) / s;
            z = (t.m20 + t.m02) / s;
            w = (t.m12 - t.m21) / s;
        }
        else if (t.m11 > t.m22){
            s  = math.sqrt(1.0f + t.m11 - t.m00 - t.m22) * 2.0f;

            x = (t.m01 + t.m10) / s;
            y = 0.25f * s;
            z = (t.m12 + t.m21) / s;
            w = (t.m20 - t.m02) / s;
        }
        else {
            s  = math.sqrt(1.0f + t.m22 - t.m00 - t.m11) * 2.0f;

            x = (t.m20 + t.m02) / s;
            y = (t.m12 + t.m21) / s;
            z = 0.25f * s;
            w = (t.m01 - t.m10) / s;
        }

        coeff = new float4(-x,-y,-z,w);
    }

    public Quat(float x, float y, float z, float w){
        coeff = new float4(x,y,z,w);
    }

    public Quat(float3 xyz, float w){
        coeff = new float4(xyz,w);
    }
    #endregion

    #region Methods
    public Quat Conjugate(){
        return new Quat(-coeff.xyz, coeff.w);
    }

    public float3 Rotate(float3 v3){
        return v3 + math.cross(coeff.xyz*2.0f, math.cross(coeff.xyz,v3) + v3 * coeff.w);
    }

    public float Norm(){
        return math.sqrt(coeff.x * coeff.x +
                         coeff.y * coeff.y +
                         coeff.z * coeff.z +
                         coeff.w * coeff.w );
    }

    public float Normalize(){
        float n = Norm();
        coeff /= n;
        return n;
    }

    public float Dot(Quat q){
        return math.dot(coeff, q.coeff);
    }
    #endregion

    #region Operators
    public static Quat operator *(Quat q1, Quat q2)
    {
        //when q = (v,w), (w1*v2+w2*v1+(v1xv2),w1*w2-v1*v2)
        return new Quat(
        q1.coeff.w*q2.coeff.x + q1.coeff.x*q2.coeff.w + q1.coeff.y*q2.coeff.z - q1.coeff.z*q2.coeff.y,
        q1.coeff.w*q2.coeff.y + q1.coeff.y*q2.coeff.w + q1.coeff.z*q2.coeff.x - q1.coeff.x*q2.coeff.z,
        q1.coeff.w*q2.coeff.z + q1.coeff.z*q2.coeff.w + q1.coeff.x*q2.coeff.y - q1.coeff.y*q2.coeff.x,
        q1.coeff.w*q2.coeff.w - q1.coeff.x*q2.coeff.x - q1.coeff.y*q2.coeff.y - q1.coeff.z*q2.coeff.z);
    }
    public static Quat operator *(Quat q1, float f1)
    {
        return new Quat(
        q1.coeff.x * f1,
        q1.coeff.y * f1,
        q1.coeff.z * f1,
        q1.coeff.w * f1);
    }
    public static Quat operator *(float f1,Quat q1)
    {
        return new Quat(
        q1.coeff.x * f1,
        q1.coeff.y * f1,
        q1.coeff.z * f1,
        q1.coeff.w * f1);
    }
    public static Quat operator /(Quat q1, float f1)
    {
        return new Quat(
        q1.coeff.x / f1,
        q1.coeff.y / f1,
        q1.coeff.z / f1,
        q1.coeff.w / f1);
    }
    public static Quat operator +(Quat q1, Quat q2)
    {
        return new Quat(
        q1.coeff.x + q2.coeff.x,
        q1.coeff.y + q2.coeff.y,
        q1.coeff.z + q2.coeff.z,
        q1.coeff.w + q2.coeff.w);
    }
    #endregion
};
