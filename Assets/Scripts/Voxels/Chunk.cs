using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    //Debug public to be private
    public bool _useNoise;
    public int _seed;
    public int _octaves;
    public float _frequencyDiff;
    public float _amplitudeDiff;
    public float _scale;
    public int _terrainHeight;
    
    private float _voxelScale;

    private GenerateWorld _generateWorld;
    private VoxelArray _voxels;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _mesh;
    private ComputeShader _computeShaderMeshGen;
    public Vector3Int _chunkBounds;


    // DEBUG button for reload of chunk
    public bool reload;

    private bool _isGenerationRunning = false;
    private bool _regenerateMeshNextFrame = false;

    private ComputeBuffer _voxelsBuffer;
    private GraphicsBuffer _verticesBuffer;
    private GraphicsBuffer _colorsBuffer;
    private GraphicsBuffer _uvsBuffer;
    private GraphicsBuffer _normalsBuffer;
    private GraphicsBuffer _facesBuffer;
    private ComputeBuffer _counterBuffer;


    // DEBUG
    private void FixedUpdate()
    {
        if (reload)
        {
            reload = false;
            Init(_generateWorld, GetComponent<MeshRenderer>().material, _computeShaderMeshGen);
            GenerateVoxels(_useNoise, _seed, _octaves, _frequencyDiff, _amplitudeDiff, _scale, _terrainHeight, _chunkBounds.x);
            GenerateMesh();
        }

        if (_regenerateMeshNextFrame && !_isGenerationRunning)
        {
            _regenerateMeshNextFrame = false;
            GenerateMesh();
        }
    }

    public void Init(GenerateWorld generateWorld, Material voxelMaterial, ComputeShader meshGenShader)
    {
        _computeShaderMeshGen = meshGenShader;
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = _meshFilter.mesh;
        _meshRenderer.material = voxelMaterial;
        _generateWorld = generateWorld;
    }

    public void GenerateVoxels(bool useNoise, int seed, int octaves, float frequencyDiff,
                               float amplitudeDiff, float scale,
                               int terrainHeight, int voxelsInChunk)
    {
        _useNoise = useNoise;
        _seed = seed;
        _octaves = octaves;
        _frequencyDiff = frequencyDiff;
        _amplitudeDiff = amplitudeDiff;
        _scale = scale;
        _terrainHeight = terrainHeight;

        _voxelScale = (float)GenerateWorld.CHUNK_SIZE / voxelsInChunk;
        _chunkBounds = new Vector3Int(voxelsInChunk, Mathf.CeilToInt(GenerateWorld.MAX_HEIGHT / _voxelScale), voxelsInChunk);
        _voxels = new VoxelArray(_chunkBounds.x, _chunkBounds.y);

        // Setup seed
        Random.InitState(seed);
        float randomOffset = Random.value * 59645.78456f;

        // Cache position
        Vector3 chunkPos = transform.position;


        // Generate all voxels and add them to a chunk
        for (int voxelX = 0; voxelX < _chunkBounds.x; voxelX++)
        {
            for (int voxelZ = 0; voxelZ < _chunkBounds.x; voxelZ++)
            {
                // Terrain gen but not really working good
                float height;
                float noise = 0;
                float maxPossibleNoise = 0;

                Vector3 voxelRealPostion = chunkPos + new Vector3(voxelX, 0, voxelZ) * _voxelScale;
                if (_useNoise) {
                    float frequency = 1;
                    float amplitude = 1;
                    for (int octave = 0; octave < _octaves; octave++)
                    {
                        float x = voxelRealPostion.x / scale * frequency + randomOffset;
                        float z = voxelRealPostion.z / scale * frequency + randomOffset;

                        float perlin = Mathf.Abs(Mathf.PerlinNoise(x, z) * 2 - 1);
                        perlin *= perlin;
                        maxPossibleNoise += 1 * amplitude;
                        noise += perlin * amplitude;
                        frequency *= _frequencyDiff;
                        amplitude *= _amplitudeDiff;
                    }

                    noise = Mathf.InverseLerp(0, maxPossibleNoise, noise);
                    height = noise * _terrainHeight;
                }
                else {
                    height = Random.value * _terrainHeight;
                }

                if (height < 2*_voxelScale) height = _voxelScale * 2;


                int voxelY = 0;
                float currentRealHeight = 0;
                do
                {
                    float noiseColorOffset = Mathf.PerlinNoise(voxelRealPostion.x / 100 + 1002f, voxelRealPostion.z / 100 + 2093f) * 0.6f;

                    // Generate a noise color for each voxel for now
                    Color color;

                    // Bedrock
                    if (voxelY == 0)
                    {
                        color = new Color(0.1f, 0.1f, 0.1f) - new Color(noiseColorOffset, noiseColorOffset, noiseColorOffset);
                    }
                    // Top grass block
                    else if (currentRealHeight + _voxelScale >= height)
                    {
                        color = new Color(0.1f, 0.8f, 0.1f) - new Color(noiseColorOffset, noiseColorOffset, noiseColorOffset);
                    }
                    // Dirt below
                    else if (currentRealHeight + _voxelScale * (3 + Random.value*2) >= height)
                    {
                        color = new Color(0.61f, 0.30f, 0.08f) - new Color(noiseColorOffset, noiseColorOffset, noiseColorOffset);
                    }
                    // Stone
                    else
                    {
                        color = new Color(0.4f, 0.4f, 0.4f) - new Color(noiseColorOffset, noiseColorOffset, noiseColorOffset);
                    }
                    
                    //color = new Color(Random.value, Random.value, Random.value);
                    //color = new Color(noise, noise, noise);
                    
                    _voxels[new Vector3(voxelX, voxelY, voxelZ)] 
                        = new Voxel() {position = new Vector3(voxelX, voxelY, voxelZ), color=color, ID = 1};
                    voxelY++;
                    currentRealHeight += _voxelScale;
                } while (currentRealHeight < height);
            }                    
        }
    }

    public void GenerateMesh()
    {
        _isGenerationRunning = true;
        float startTime = Time.realtimeSinceStartup;

        // Create buffers
        int intsize = sizeof(int);
        int vector2size = sizeof(float) * 2;
        int vector3size = sizeof(float) * 3;
        int vector4size = sizeof(float) * 4;
        _voxelsBuffer = new ComputeBuffer(_voxels.Capacity, vector3size + vector4size + intsize);
        _voxelsBuffer.SetData(_voxels.GetData);

        // Initialiaze buffers
        int numVerts = _voxels.Count * 24;
        _verticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector3size);
        _colorsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector4size);
        _uvsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector2size);
        _normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector3size);
        _facesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxels.Count * 36, intsize);
        int[] counters = new int[2];
        _counterBuffer = new ComputeBuffer(2, intsize);
        _counterBuffer.SetData(counters);

        // Set buffers
        int shaderID = _computeShaderMeshGen.FindKernel("CSMain");
        _computeShaderMeshGen.SetBuffer(shaderID, "voxels", _voxelsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "vertices", _verticesBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "colors" , _colorsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "uvs", _uvsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "normals", _normalsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "faces", _facesBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "counters", _counterBuffer);

        _computeShaderMeshGen.SetVector("voxels_size", (Vector2)_voxels.size);
        _computeShaderMeshGen.SetFloat("voxel_scale", _voxelScale);
        _computeShaderMeshGen.SetInt("voxel_count", _voxels.Capacity);

        // Dispatch compute shader
        _computeShaderMeshGen.GetKernelThreadGroupSizes(shaderID, out uint groupSizeX, out _, out _);
        int dispatchSize = Mathf.CeilToInt((float)_voxels.Capacity / groupSizeX);
        _computeShaderMeshGen.Dispatch(shaderID, dispatchSize, 1, 1);


        AsyncGPUReadback.Request(_verticesBuffer, request =>
        {
            // Chunk was deleted
            if (_meshFilter == null)
            {
                DisposeBuffers();
                return;
            }

            // Get data from shader
            _counterBuffer.GetData(counters);
            Vector3[] vertices = new Vector3[counters[0]];
            Color[] colors = new Color[counters[0]];
            Vector2[] uvs = new Vector2[counters[0]];
            Vector3[] normals = new Vector3[counters[0]];
            int[] faces = new int[counters[1]];
            _verticesBuffer.GetData(vertices);
            _colorsBuffer.GetData(colors);
            _uvsBuffer.GetData(uvs);
            _normalsBuffer.GetData(normals);
            _facesBuffer.GetData(faces);

            // Set all mesh data
            _mesh = new Mesh();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.SetVertices(vertices, 0, (int)counters[0]);
            _mesh.SetColors(colors, 0, (int)counters[0]);
            _mesh.SetUVs(0, uvs, 0, (int)counters[0]);
            _mesh.SetNormals(normals, 0, (int)counters[0]);
            _mesh.SetTriangles(faces, 0, (int)counters[1], 0, true);
            _mesh.Optimize();
            _mesh.RecalculateBounds();
            _mesh.UploadMeshData(true);
            _meshFilter.mesh = _mesh;
            _meshCollider.sharedMesh = _mesh;

            // Release the allocated data
            DisposeBuffers();
            _isGenerationRunning = false;
        });
    }

    private void DisposeBuffers()
    {
        _voxelsBuffer?.Dispose();
        _verticesBuffer?.Dispose();
        _colorsBuffer?.Dispose();
        _uvsBuffer?.Dispose();
        _normalsBuffer?.Dispose();
        _facesBuffer?.Dispose();
        _counterBuffer?.Dispose();
    }

    private void OnApplicationQuit()
    {
        DisposeBuffers();
    }

    public void DestroyVoxel(Vector3 pos, Vector3 normal, int area)
    { 
        pos -= transform.position;
        pos -= Vector3.one * _voxelScale * 0.5f;
        pos -= normal * _voxelScale * 0.5f;
        float voxelX = Mathf.RoundToInt((pos.x / GenerateWorld.CHUNK_SIZE) * _chunkBounds.x);
        float voxelY = Mathf.RoundToInt((pos.y / GenerateWorld.CHUNK_SIZE) * _chunkBounds.x);
        float voxelZ = Mathf.RoundToInt((pos.z / GenerateWorld.CHUNK_SIZE) * _chunkBounds.x);
        Vector3 voxelPos = new Vector3(voxelX, voxelY, voxelZ);

        for (float x = -area; x <= area; x++)
        {
            for (float y = -area; y <= area; y ++)
            {
                for (float z = -area; z <= area; z ++)
                {
                    Vector3 offset = new Vector3(x,y,z);
                    if (offset.magnitude > area + 0.25f) continue;

                    Vector3 newPos = voxelPos + offset;
                    if (newPos.x < 0)
                    {
                        newPos.x += _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.left * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.x >= _chunkBounds.x)
                    {
                        newPos.x -= _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.right * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.y < 0 || newPos.y >= _chunkBounds.y)
                    {
                        continue;
                    }
                    if (newPos.z < 0)
                    {
                        newPos.z += _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.back * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.z >= _chunkBounds.x)
                    {
                        newPos.z -= _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.forward * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }

                    Voxel voxel = _voxels[newPos];
                    if (voxel.ID == 0) continue;
                    voxel.ID = 0;
                    voxel.position = newPos;
                    _voxels[newPos] = voxel;
                    _regenerateMeshNextFrame = true;
                }
            }
        }
    }

    public void CreateVoxel(Vector3 pos, Vector3 normal, int area)
    { 
        pos -= transform.position;
        pos -= Vector3.one * _voxelScale * 0.5f;
        pos += normal * _voxelScale * 0.5f;
        float voxelX = Mathf.RoundToInt((pos.x / GenerateWorld.CHUNK_SIZE) * _chunkBounds.x);
        float voxelY = Mathf.RoundToInt((pos.y / GenerateWorld.CHUNK_SIZE) * _chunkBounds.x);
        float voxelZ = Mathf.RoundToInt((pos.z / GenerateWorld.CHUNK_SIZE) * _chunkBounds.x);
        Vector3 voxelPos = new Vector3(voxelX, voxelY, voxelZ);

        for (float x = -area; x <= area; x++)
        {
            for (float y = -area; y <= area; y ++)
            {
                for (float z = -area; z <= area; z ++)
                {
                    Vector3 offset = new Vector3(x,y,z);
                    if (offset.magnitude > area + 0.25f) continue;

                    Vector3 newPos = voxelPos + offset;
                    if (newPos.x < 0)
                    {
                        newPos.x += _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.left * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.x >= _chunkBounds.x)
                    {
                        newPos.x -= _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.right * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.y < 0 || newPos.y >= _chunkBounds.y)
                    {
                        continue;
                    }
                    if (newPos.z < 0)
                    {
                        newPos.z += _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.back * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.z >= _chunkBounds.x)
                    {
                        newPos.z -= _chunkBounds.x;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.forward * GenerateWorld.CHUNK_SIZE);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }

                    Voxel voxel = _voxels[newPos];
                    if (voxel.ID != 0) continue;
                    voxel.ID = 1;
                    voxel.position = newPos;
                    voxel.color = new Color(0f,0.8f,0f,0f);
                    _voxels[newPos] = voxel;
                    _regenerateMeshNextFrame = true;
                }
            }
        }
        
    }

    protected void DestroyVoxelFromOtherChunk(Vector3 voxelPos)
    {
        Voxel voxel = _voxels[voxelPos];
        if (voxel.ID == 0) return;
        voxel.ID = 0;
        _voxels[voxelPos] = voxel;
        DefferedGenerateMesh();
    }

    protected void CreateVoxelFromOtherChunk(Vector3 voxelPos)
    {
        Voxel voxel = _voxels[voxelPos];
        if (voxel.ID != 0) return;
        voxel.ID = 1;
        voxel.position = voxelPos;
        voxel.color = new Color(0f,0.8f,0f,0f);
        _voxels[voxelPos] = voxel;
        DefferedGenerateMesh();
    }

    public void DefferedGenerateMesh()
    {
        _regenerateMeshNextFrame = true;
    }

    // public Voxel this[Vector3 index]
    // {
    //     get
    //     {
    //         return _voxels[index];
    //     }

    //     set
    //     {
    //         _voxels[index] = (Voxel)value;
    //     }
    // }
}
