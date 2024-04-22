using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private const uint DEFAULT_SIZE = 16;

    //Debug public to be pribate
    public bool _useNoise;
    public int _seed;
    public int _octaves;
    public float _frequency;
    public float _amplitude;
    public int _terrainHeight;
    public int _voxelsInChunk;

    private GenerateWorld _generateWorld;
    private IndexedArray<Voxel> _voxels;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _mesh;

    // DEBUG button for reload of chunk
    public bool reload;

    // DEBUG
    private void Update()
    {
        if (reload)
        {
            reload = false;
            Init(_generateWorld, GetComponent<MeshRenderer>().material);
            GenerateVoxels(_useNoise, _seed, _octaves, _frequency, _amplitude, _terrainHeight, _voxelsInChunk);
            GenerateMesh();
        }
    }

    public void Init(GenerateWorld generateWorld, Material voxelMaterial)
    {
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = _meshFilter.mesh;
        _meshRenderer.material = voxelMaterial;
        _generateWorld = generateWorld;
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

        if (_voxels == null) _voxels = new IndexedArray<Voxel>(_voxelsInChunk, GenerateWorld.MAX_HEIGHT);
        
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
                    // OCTAVES ARE CURRENTLY NOT WORKING
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


                int voxelY = 0;
                float currentRealHeight = 0;
                do
                {
                    // Generate a noise color for each voxel for now
                    Color color;
                    color = new Color(Random.value, Random.value, Random.value);
                    //color = new Color(noise, noise, noise);
                    
                    _voxels[new Vector3(voxelX, voxelY, voxelZ)] = new Voxel() {position = new Vector3(voxelX, voxelY, voxelZ), color=color};
                    voxelY++;
                    currentRealHeight += voxelScale;
                } while (currentRealHeight < height);
            }                    
        }

        // Set blocked tags for easier mesh generation
        /*foreach (Voxel voxel in voxels.Values.ToList())
        {
            Vector3 position = voxel.position;

            // Up
            if (!voxels.ContainsKey(position + Vector3.up)) continue;
            
            // Down
            if (position.y != 0 && !voxels.ContainsKey(position + Vector3.down)) continue;

            // Left
            if (position.x == 0)
            {
                Vector3 voxelPosInOtherChunk = position;
                voxelPosInOtherChunk.x = _voxelsInChunk - 1;
                // 16 meters per chunk
                if (!_generateWorld.IsVoxelInChunkPresent(this, transform.position + Vector3.left*16, voxelPosInOtherChunk))
                {
                    continue;
                }
            }
            else if (!voxels.ContainsKey(position + Vector3.left)) continue;

            // Right
            if (position.x == _voxelsInChunk - 1)
            {
                Vector3 voxelPosInOtherChunk = position;
                voxelPosInOtherChunk.x = 0;
                // 16 meters per chunk
                if (!_generateWorld.IsVoxelInChunkPresent(this, transform.position + Vector3.right*16, voxelPosInOtherChunk))
                {
                    continue;
                }
            }
            else if (!voxels.ContainsKey(position + Vector3.right)) continue;

            // Back
            if (position.z == 0)
            {
                Vector3 voxelPosInOtherChunk = position;
                voxelPosInOtherChunk.z = _voxelsInChunk - 1;
                // 16 meters per chunk
                if (!_generateWorld.IsVoxelInChunkPresent(this, transform.position + Vector3.back*16, voxelPosInOtherChunk))
                {
                    continue;
                }
            }
            else if (!voxels.ContainsKey(position + Vector3.back)) continue;

            // Forward
           if (position.x == _voxelsInChunk - 1)
            {
                Vector3 voxelPosInOtherChunk = position;
                voxelPosInOtherChunk.z = 0;
                // 16 meters per chunk
                if (!_generateWorld.IsVoxelInChunkPresent(this, transform.position + Vector3.forward*16, voxelPosInOtherChunk))
                {
                    continue;
                }
            }
            else if (!voxels.ContainsKey(position + Vector3.forward)) continue;


            Voxel voxelCopy = voxel;
            voxelCopy.isBlocked = true;
            voxels[position] = voxelCopy;
        }*/
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
        
        for (int x = 0; x < _voxels.size.x; x++)
        {
            for (int y = 0; y <  _voxels.size.y; y++)
            {
                for (int z = 0; z <  _voxels.size.x; z++)
                {
                    if (!_voxels.Contains(x, y, z)) continue;
                    
                    voxel = _voxels[x, y, z];
                    voxelPos = voxel.position;

                    if (voxel.isBlocked) continue;

                    //Iterate over each face direction
                    for (int i = 0; i < 6; i++)
                    {
                        // Don't draw faces that are hidden
                        Vector3 faceVoxelNeighborPos = voxelPos + DIRECTIONS[i];
                        if (this[faceVoxelNeighborPos] != null)
                            continue;

                        // Check for chunk edges (except for blocks on the top, which need to be have an extra face outwards for holeless LOD transitions)
                        if (_voxels.Contains(voxelPos + Vector3.up) &&
                        (voxelPos.x == 0 || voxelPos.x == _voxelsInChunk - 1 || voxelPos.z == 0 || voxelPos.z == _voxelsInChunk - 1))
                        {
                            Vector3 voxelPosInOtherChunk = voxelPos + DIRECTIONS[i];
                            if (voxelPosInOtherChunk.x == -1 || voxelPosInOtherChunk.x == _voxelsInChunk ||
                                voxelPosInOtherChunk.z == -1 || voxelPosInOtherChunk.z == _voxelsInChunk)
                            {
                                voxelPosInOtherChunk.x = voxelPosInOtherChunk.x < 0 ? _voxelsInChunk-1 : voxelPosInOtherChunk.x % _voxelsInChunk;
                                voxelPosInOtherChunk.z = voxelPosInOtherChunk.z < 0 ? _voxelsInChunk-1 : voxelPosInOtherChunk.z % _voxelsInChunk;
                                if (_generateWorld.IsVoxelInChunkPresentAndNotTop(this, transform.position + DIRECTIONS[i]*16, voxelPosInOtherChunk))
                                {
                                    continue;
                                }
                            }
                        }

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
            if (_voxels.Contains(index))
                return _voxels[index];
            else
                return null;
        }

        set
        {
            if (value == null) return;

            _voxels[index] = (Voxel)value;
        }
    }

    public bool IsVoxelPresent(Vector3 index)
    {
        return _voxels.Contains(index);
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
