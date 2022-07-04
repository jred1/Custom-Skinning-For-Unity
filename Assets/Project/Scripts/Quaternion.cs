using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct quat
{
    float4 coeff;

    //add constructor that converts matrix
    quat(float x, float y, float z, float w){
        coeff = new float4(x,y,z,w);
    }

    quat(float3 xyz, float w){
        coeff = new float4(xyz,w);
    }

    quat conjugate(){
        return new quat(-coeff.xyz, coeff.w);
    }
    
    float3 rotate(float3 v3){
        return v3 + math.cross(coeff.xyz*2.0f, math.cross(coeff.xyz,v3) + v3 * coeff.w);
    }

    float norm(){
        return math.sqrt(coeff.x * coeff.x +
                         coeff.y * coeff.y +
                         coeff.z * coeff.z +
                         coeff.w * coeff.w );
    }

    float normalize(){
        float n = norm();
        coeff /= n;
        return n;
    }

	float dot(quat q){
		return math.dot(coeff, q.coeff);
	}
    public static quat operator *(quat q1, quat q2)
    {
        return new quat(
        q1.coeff.w*q2.coeff.w - q1.coeff.x*q2.coeff.x - q1.coeff.y*q2.coeff.y - q1.coeff.z*q2.coeff.z,
        q1.coeff.w*q2.coeff.x + q1.coeff.x*q2.coeff.w + q1.coeff.y*q2.coeff.z - q1.coeff.z*q2.coeff.y,
        q1.coeff.w*q2.coeff.y + q1.coeff.y*q2.coeff.w + q1.coeff.z*q2.coeff.x - q1.coeff.x*q2.coeff.z,
        q1.coeff.w*q2.coeff.z + q1.coeff.z*q2.coeff.w + q1.coeff.x*q2.coeff.y - q1.coeff.y*q2.coeff.x);
    }

};
