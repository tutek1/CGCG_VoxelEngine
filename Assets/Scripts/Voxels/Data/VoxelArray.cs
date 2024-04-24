using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// Taken from https://github.com/pixelreyn/VoxelProjectSeries

[System.Serializable]
public class VoxelArray
{
    private bool initialized = false;

    [SerializeField]
    [HideInInspector]
    public Voxel[] array;

    [SerializeField]
    [HideInInspector]
    public Vector2Int size
    {
        get { return _size; }
        private set { _size = value; }
    }

    private Vector2Int _size;
    private int _count = 0;
    private int _capacity = 0;
    
    public VoxelArray(int sizeX, int sizeY)
    {
        _size = new Vector2Int(sizeX, sizeY);
        _capacity = size.x * size.y * size.x;
        array = new Voxel[_capacity];
        initialized = true;
    }

    int IndexFromCoord(Vector3 idx)
    {
        return Mathf.RoundToInt(idx.x) + (Mathf.RoundToInt(idx.y) * size.x) + (Mathf.RoundToInt(idx.z) * size.x * size.y);
    }

    int IndexFromCoord(float x, float y, float z)
    {
        return Mathf.RoundToInt(x) + (Mathf.RoundToInt(y) * size.x) + (Mathf.RoundToInt(z) * size.x * size.y);
    }


    public void Clear()
    {
        if (!initialized)
            return;
        _count = 0;
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.x; z++)
                    array[x + (y * size.x) + (z * size.x * size.y)] = new Voxel();
    }

    public int Capacity
    {
        get { return _capacity; }
    }

    public int Count
    {
        get { return _count; }
    }

    public Voxel[] GetData
    {
        get
        {
            return array;
        }
    }

    public bool IsVoxelValid(Vector3 coord)
    {
        return this[coord].ID != 0;
    }

    public bool IsVoxelValid(float x, float y, float z)
    {
        return this[x,y,z].ID != 0;
    }

    public Voxel this[Vector3 coord]
    {
        get
        {
            if (coord.x < 0 || coord.x > size.x ||
            coord.y < 0 || coord.y > size.y ||
            coord.z < 0 || coord.z > size.x)
            {
                return new Voxel();
            }
            int idx = IndexFromCoord(coord);
            if (idx >= _capacity) return new Voxel();
            return array[idx];
        }
        set
        {
            if (coord.x < 0 || coord.x >= size.x ||
            coord.y < 0 || coord.y >= size.y ||
            coord.z < 0 || coord.z >= size.x)
            {
                return;
            }

            if (IsVoxelValid(coord))
            {
                if (value.ID == 0) _count -= 1;
            }
            else if (value.ID != 0)
            {
                _count += 1;
            }
            int idx = IndexFromCoord(coord);
            if (idx >= _capacity) return;

            array[idx] = value;
        }
    }

    public Voxel this[float x, float y, float z]
    {
        get
        {
            if (x < 0 || x > size.x ||
            y < 0 || y > size.y ||
            z < 0 || z > size.x)
            {
                return new Voxel();
            }
            int idx = IndexFromCoord(x,y,z);
            if (idx >= _capacity) return new Voxel();
            return array[idx];
        }
        set
        {
            if (x < 0 || x >= size.x ||
            y < 0 || y >= size.y ||
            z < 0 || z >= size.x)
            {
                return;
            }
            if (IsVoxelValid(x, y, z))
            {
                if (value.ID == 0) _count -= 1;
            }
            else if (value.ID != 0)
            {
                _count += 1;
            }
            int idx = IndexFromCoord(x, y, z);
            if (idx >= _capacity) return;

            array[idx] = value;
        }
    }

}
