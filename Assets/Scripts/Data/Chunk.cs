using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private int _voxelsInChunk;

    private Dictionary<Vector3, Voxel> voxels = new Dictionary<Vector3, Voxel>();

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _mesh;

    // DEBUG
    public Vector3 voxelToAddPos;
    public bool add;

    // DEBUG
    private void Update()
    {
        if (add)
        {
            add = false;
            Init(GetComponent<MeshRenderer>().material);
            GenerateMesh();
        }
    }

    public void Init(Material voxelMaterial)
    {
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = _meshFilter.mesh;
        _meshRenderer.material = voxelMaterial;
    }

    public void GenerateVoxels(bool useNoise, int seed, int octaves, float frequency,
                               float amplitude, int terrainHeight, int voxelsInChunk)
    {
        _voxelsInChunk = voxelsInChunk;
        
        // Setup seed
        Random.InitState(seed);
        float randomOffset = Random.value * 100000f;

        // Cache position
        Vector3 pos = transform.position;

        // Generate all voxels and add them to a chunk
        for (int voxelX = 0; voxelX < _voxelsInChunk; voxelX++)
        {
            for (int voxelZ = 0; voxelZ < _voxelsInChunk; voxelZ++)
            {
                // Terrain gen but not really working good
                float height = 1;

                if (useNoise) {
                    float tempFrequency = frequency;
                    float tempAmplitude = amplitude;
                    for (int octave = 0; octave < octaves; octave++)
                    {
                        height += (Mathf.PerlinNoise((pos.x + voxelX) / tempFrequency + randomOffset,
                                                    (pos.z + voxelZ) / tempFrequency + randomOffset) * 2 - 1) * tempAmplitude;
                        tempFrequency *= frequency;
                        tempAmplitude *= amplitude;
                    }
                    height *= terrainHeight;
                }
                else {
                    height = Random.value * terrainHeight;
                }

                if (height < 1) height = 1;

                for (int voxelY = 0; voxelY < height; voxelY++)
                {
                    // Generate a random color for each voxel for now
                    //Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                    Color color;
                    if (useNoise)
                    {
                        if (voxelY + 1 > height) color = new Color(0f, 0.2f, 0f);
                        else                      color = new Color(0.4f, 0.3f, 0f);
                    }
                    else
                    {
                        color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                    }
                    
                    voxels[new Vector3(voxelX, voxelY, voxelZ)] = new Voxel() {color=color};
                }
            }                    
        }
    }

    public void GenerateMesh()
    {
        _mesh.Clear();

        Vector3 voxelPos;
        Voxel voxel;

        int counter = 0;
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> faces = new List<int>();

        Vector3[] faceVertices = new Vector3[4];
        Vector2[] faceUVs = new Vector2[4];
        
        foreach (KeyValuePair<Vector3, Voxel> posVoxelPair in voxels)
        {
            voxelPos = posVoxelPair.Key;
            voxel = posVoxelPair.Value;

            //Iterate over each face direction
            for (int i = 0; i < 6; i++)
            {
                // Don't draw faces that are hidden
                if (this[voxelPos + DIRECTIONS[i]] != null)
                    continue;


                //Collect the appropriate vertices from the default vertices and add the block position
                for (int j = 0; j < 4; j++)
                {
                    faceVertices[j] = VOXEL_VERTS[VOXEL_VERT_IDXS[i, j]] + voxelPos;
                    faceUVs[j] = VOXEL_UVS[j];
                }

                for (int j = 0; j < 6; j++)
                {
                    vertices.Add(faceVertices[VOXEL_TRIS[i, j]]);
                    colors.Add(voxel.color);
                    uvs.Add(faceUVs[VOXEL_TRIS[i, j]]);
                    faces.Add(counter);
                    counter++;
                }
            }
        }

        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.SetVertices(vertices);
        _mesh.SetColors(colors);
        _mesh.SetTriangles(faces, 0, false);
        _mesh.SetUVs(0, uvs);
        _mesh.Optimize();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _mesh.UploadMeshData(false);
        _meshFilter.mesh = _mesh;
        if (vertices.Count > 3){
            _meshCollider.sharedMesh = _mesh;
        }
    }

    public Voxel? this[Vector3 index]
        {
            get
            {
                if (voxels.ContainsKey(index))
                    return voxels[index];
                else
                    return null;
            }

            set
            {
                if (value == null) return;
                
                // Unity has a max number of faces 65 534 -> 5 460 voxels max
                /*if (voxels.Count > 5460) 
                {
                    Debug.LogError("Too many voxels in one chunk!");
                    return;
                }*/

                if (voxels.ContainsKey(index))
                    voxels[index] = (Voxel)value;
                else
                {
                    voxels.Add(index, (Voxel)value);
                }
            }
        }

    static readonly Vector3[] DIRECTIONS = new Vector3[6]
        {
            new Vector3(0,0,-1),//back
            new Vector3(0,0,1),//front
            new Vector3(-1,0,0),//left
            new Vector3(1,0,0),//right
            new Vector3(0,-1,0),//bottom
            new Vector3(0,1,0)//top
        };
    
    static readonly Vector3[] VOXEL_VERTS = new Vector3[8]
    {
        new Vector3(0,0,0),//0
        new Vector3(1,0,0),//1
        new Vector3(0,1,0),//2
        new Vector3(1,1,0),//3

        new Vector3(0,0,1),//4
        new Vector3(1,0,1),//5
        new Vector3(0,1,1),//6
        new Vector3(1,1,1),//7
    };

    static readonly int[,] VOXEL_VERT_IDXS = new int[6, 4]
    {
        {0,1,2,3},
        {4,5,6,7},
        {4,0,6,2},
        {5,1,7,3},
        {0,1,4,5},
        {2,3,6,7},
    };

    static readonly Vector2[] VOXEL_UVS = new Vector2[4]
    {
        new Vector2(0,0),
        new Vector2(0,1),
        new Vector2(1,0),
        new Vector2(1,1)
    };

    static readonly int[,] VOXEL_TRIS = new int[6, 6]
    {
        {0,2,3,0,3,1},
        {0,1,2,1,3,2},
        {0,2,3,0,3,1},
        {0,1,2,1,3,2},
        {0,1,2,1,3,2},
        {0,2,3,0,3,1},
    };
}
