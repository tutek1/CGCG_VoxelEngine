using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private const uint DEFAULT_SIZE = 16;

    //Debug
    public bool _useNoise;
    public int _seed;
    public int _octaves;
    public float _frequency;
    public float _amplitude;
    public int _terrainHeight;
    public int _voxelsInChunk;

    private Dictionary<Vector3, Voxel> voxels = new Dictionary<Vector3, Voxel>();

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _mesh;

    // DEBUG
    public Vector3 voxelToAddPos;
    public bool reload;

    // DEBUG
    private void Update()
    {
        if (reload)
        {
            reload = false;
            voxels.Clear();
            Init(GetComponent<MeshRenderer>().material);
            GenerateVoxels(_useNoise, _seed, _octaves, _frequency, _amplitude, _terrainHeight, _voxelsInChunk);
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
        _useNoise = useNoise;
        _seed = seed;
        _octaves = octaves;
        _frequency = frequency;
        _amplitude = amplitude;
        _terrainHeight = terrainHeight;
        _voxelsInChunk = voxelsInChunk;

        float voxelScale =  (float)DEFAULT_SIZE / _voxelsInChunk;
        
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
                float height;
                float noise = 0;

                Vector3 voxelRealPostion = pos + new Vector3(voxelX, 0, voxelZ) * voxelScale;
                if (_useNoise) {
                    float tempFrequency = _frequency;
                    float tempAmplitude = _amplitude;
                    for (int octave = 0; octave < _octaves; octave++)
                    {
                        noise += (Mathf.PerlinNoise(voxelRealPostion.x / tempFrequency + randomOffset,
                                                     voxelRealPostion.z / tempFrequency + randomOffset) * 2 - 1) * tempAmplitude;
                        tempFrequency *= _frequency;
                        tempAmplitude *= _amplitude;
                    }
                    noise = Mathf.Abs(noise);
                    height = noise * _terrainHeight;
                }
                else {
                    height = Random.value * _terrainHeight;
                }

                if (height < 1) height = 1;

                int voxelY = 0;
                float currentRealHeight = 0;
                while (currentRealHeight < height)
                {
                    // Generate a noise color for each voxel for now
                    Color color;
                    color = new Color(noise, noise, noise);
                    
                    voxels[new Vector3(voxelX, voxelY++, voxelZ)] = new Voxel() {color=color};
                    currentRealHeight += voxelScale;
                }
            }                    
        }
    }

    public void GenerateMesh()
    {
        _mesh.Clear();

        float voxelScale =  (float)DEFAULT_SIZE / _voxelsInChunk;

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
                    faceVertices[j] = (VOXEL_VERTS[VOXEL_VERT_IDXS[i, j]] + voxelPos) * voxelScale;
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
