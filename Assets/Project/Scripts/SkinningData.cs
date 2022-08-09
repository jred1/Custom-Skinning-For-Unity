using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinningData : ScriptableObject
{
    public Matrix4x4[] bindMatrices;
    public DualQuat[] bindDQs;

}
