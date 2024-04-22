using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// Taken from https://github.com/pixelreyn/VoxelProjectSeries

[System.Serializable]
public class IndexedArray<T> where T : struct
{
    private bool initialized = false;

    [SerializeField]
    [HideInInspector]
    public T[] array;

    [SerializeField]
    [HideInInspector]
    public Vector2Int size
    {
        get { return _size; }
        private set { _size = value; }
    }

    private Vector2Int _size;
    
    public IndexedArray(int sizeX, int sizeY)
    {
        size = new Vector2Int(sizeX + 3, sizeY + 1);
        array = new T[Capacity];
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

        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.x; z++)
                    array[x + (y * size.x) + (z * size.x * size.y)] = default(T);
    }

    public int Capacity
    {
        get { return size.x * size.y * size.x; }
    }

    public int Count
    {
        get { return size.x * size.y * size.x; }
    }

    public T[] GetData
    {
        get
        {
            return array;
        }
    }

    public bool Contains(Vector3 coord)
    {
        return !EqualityComparer<T>.Default.Equals(this[coord], default(T));
    }

    public bool Contains(float x, float y, float z)
    {
        return !EqualityComparer<T>.Default.Equals(this[x,y,z], default(T));
    }

    public T this[Vector3 coord]
    {
        get
        {
            if (coord.x < 0 || coord.x > size.x ||
            coord.y < 0 || coord.y > size.y ||
            coord.z < 0 || coord.z > size.x)
            {
                return default(T);
            }
            return array[IndexFromCoord(coord)];
        }
        set
        {
            if (coord.x < 0 || coord.x >= size.x ||
            coord.y < 0 || coord.y >= size.y ||
            coord.z < 0 || coord.z >= size.x)
            {
                return;
            }
            array[IndexFromCoord(coord)] = value;
        }
    }

    public T this[float x, float y, float z]
    {
        get
        {
            if (x < 0 || x > size.x ||
            y < 0 || y > size.y ||
            z < 0 || z > size.x)
            {
                Debug.LogError($"Coordinates out of bounds! {x}, {y}, {z}");
                return default(T);
            }
            return array[IndexFromCoord(x, y, z)];
        }
        set
        {
            if (x < 0 || x >= size.x ||
            y < 0 || y >= size.y ||
            z < 0 || z >= size.x)
            {
                Debug.LogError($"Coordinates out of bounds! {x},{y},{z}");
                return;
            }
            array[IndexFromCoord(x, y, z)] = value;
        }
    }

}
