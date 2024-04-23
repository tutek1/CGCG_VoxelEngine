using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Voxel
{
    public int ID;
    public Vector3 position;
    public Vector4 color;
}
