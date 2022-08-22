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

        float S, X, Y, Z, W;

        if (trace > 0.000001f){
            S = math.sqrt(trace) * 2.0f;

            X = (t.m12 - t.m21) / S;
            Y = (t.m20 - t.m02) / S;
            Z = (t.m01 - t.m10) / S;
            W = 0.25f * S;
        }
        else if (t.m00 > t.m11 && t.m00 > t.m22){
            S  = math.sqrt(1.0f + t.m00 - t.m11 - t.m22) * 2.0f;

            X = 0.25f * S;
            Y = (t.m01 + t.m10) / S;
            Z = (t.m20 + t.m02) / S;
            W = (t.m12 - t.m21) / S;
        }
        else if (t.m11 > t.m22){
            S  = math.sqrt(1.0f + t.m11 - t.m00 - t.m22) * 2.0f;

            X = (t.m01 + t.m10) / S;
            Y = 0.25f * S;
            Z = (t.m12 + t.m21) / S;
            W = (t.m20 - t.m02) / S;
        }
        else {
            S  = math.sqrt(1.0f + t.m22 - t.m00 - t.m11) * 2.0f;

            X = (t.m20 + t.m02) / S;
            Y = (t.m12 + t.m21) / S;
            Z = 0.25f * S;
            W = (t.m01 - t.m10) / S;
        }

        coeff = new float4(-X,-Y,-Z,W);
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
