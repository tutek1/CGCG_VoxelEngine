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
    private Vector2 _chunkIdx;


    //Debug public to be private
    public bool _useNoise;
    public int _seed;
    public int _octaves;
    public float _frequencyDiff;
    public float _amplitudeDiff;
    public float _scale;
    public int _terrainHeight;
    public float _chunkSize;

    private GenerateWorld _generateWorld;
    private IndexedArray<Voxel> _voxels;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _mesh;
    private ComputeShader _computeShaderMeshGen;

    // DEBUG button for reload of chunk
    public bool reload;

    private bool _isGenerationRunning = false;
    private bool _regenerateMeshNextFrame = false;

    // DEBUG
    private void FixedUpdate()
    {
        if (reload)
        {
            reload = false;
            Init(_generateWorld, GetComponent<MeshRenderer>().material, _computeShaderMeshGen);
            GenerateVoxels(_useNoise, _seed, _octaves, _frequencyDiff, _amplitudeDiff, _scale, _terrainHeight, _chunkSize);
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
        _chunkIdx = new Vector2((int)(transform.position.x / _chunkSize), (int)(transform.position.z / _chunkSize));
    }

    public void GenerateVoxels(bool useNoise, int seed, int octaves, float frequencyDiff,
                               float amplitudeDiff, float scale,
                               int terrainHeight, float chunkSize)
    {
        _useNoise = useNoise;
        _seed = seed;
        _octaves = octaves;
        _frequencyDiff = frequencyDiff;
        _amplitudeDiff = amplitudeDiff;
        _scale = scale;
        _terrainHeight = terrainHeight;
        _chunkSize = chunkSize;

        float voxelScale = (float)_chunkSize / GenerateWorld.VOXELS_IN_CHUNK;

        _voxels = new IndexedArray<Voxel>(GenerateWorld.VOXELS_IN_CHUNK, GenerateWorld.MAX_HEIGHT);

        // Setup seed
        Random.InitState(seed);
        float randomOffset = Random.value * 100000f;

        // Cache position
        Vector3 pos = transform.position;


        // Generate all voxels and add them to a chunk
        for (int voxelX = 0; voxelX < GenerateWorld.VOXELS_IN_CHUNK; voxelX++)
        {
            for (int voxelZ = 0; voxelZ < GenerateWorld.VOXELS_IN_CHUNK; voxelZ++)
            {
                // Terrain gen but not really working good
                float height;
                float noise = 0;
                float maxPossibleNoise = 0;

                Vector3 voxelRealPostion = pos + new Vector3(voxelX, 0, voxelZ) * voxelScale;
                if (_useNoise) {
                    float frequency = 1;
                    float amplitude = 1;
                    for (int octave = 0; octave < _octaves; octave++)
                    {
                        float x = voxelRealPostion.x / scale * frequency + randomOffset;
                        float z = voxelRealPostion.z / scale * frequency + randomOffset;

                        float perlin = Mathf.Abs(Mathf.PerlinNoise(x, z) * 2 - 1);
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


                int voxelY = 0;
                float currentRealHeight = 0;
                do
                {
                    // Generate a noise color for each voxel for now
                    Color color;
                    //color = new Color(Random.value, Random.value, Random.value);
                    color = new Color(noise, noise, noise);
                    
                    _voxels[new Vector3(voxelX, voxelY, voxelZ)] 
                        = new Voxel() {position = new Vector3(voxelX, voxelY, voxelZ), color=color, ID = 1};
                    voxelY++;
                    currentRealHeight += voxelScale;
                } while (currentRealHeight < height);
            }                    
        }
    }

    public void GenerateMesh()
    {
        _isGenerationRunning = true;
        float startTime = Time.realtimeSinceStartup;

        // Create buffers
        float voxelScale =  (float)_chunkSize / GenerateWorld.VOXELS_IN_CHUNK;
        int intsize = sizeof(int);
        int vector2size = sizeof(float) * 2;
        int vector3size = sizeof(float) * 3;
        int vector4size = sizeof(float) * 4;
        ComputeBuffer voxelsBuffer = new ComputeBuffer(_voxels.Count, vector3size + vector4size + intsize);
        voxelsBuffer.SetData(_voxels.GetData);

        int numVerts = _voxels.Capacity * 24;
        Vector3[] vertices = new Vector3[numVerts];
        GraphicsBuffer verticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector3size);
        Color[] colors = new Color[numVerts];
        GraphicsBuffer colorsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector4size);
        Vector2[] uvs = new Vector2[numVerts];
        GraphicsBuffer uvsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector2size);
        Vector3[] normals = new Vector3[numVerts];
        GraphicsBuffer normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numVerts, vector3size);
        int[] faces = new int[_voxels.Capacity * 36];
        GraphicsBuffer facesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, faces.Length, intsize);
        int[] counters = new int[2];
        GraphicsBuffer counterBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, intsize);
        counterBuffer.SetData(counters);

        //Debug.Log("After buffer creation " + (Time.realtimeSinceStartup - startTime));
        //startTime = Time.realtimeSinceStartup;

        // Set buffers
        int shaderID = _computeShaderMeshGen.FindKernel("CSMain");
        _computeShaderMeshGen.SetBuffer(shaderID, "voxels", voxelsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "vertices", verticesBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "colors" , colorsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "uvs", uvsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "normals", normalsBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "faces", facesBuffer);
        _computeShaderMeshGen.SetBuffer(shaderID, "counters", counterBuffer);

        _computeShaderMeshGen.SetVector("size", (Vector2)_voxels.size);
        _computeShaderMeshGen.SetFloat("voxel_scale", voxelScale);
        _computeShaderMeshGen.SetInt("voxel_count", _voxels.Capacity);

        //Debug.Log("After set " + (Time.realtimeSinceStartup - startTime));
        //startTime = Time.realtimeSinceStartup;

        // Dispatch compute shader
        _computeShaderMeshGen.GetKernelThreadGroupSizes(shaderID, out uint groupSizeX, out _, out _);
        int dispatchSize = Mathf.CeilToInt((float)_voxels.Capacity / groupSizeX);
        _computeShaderMeshGen.Dispatch(shaderID, dispatchSize, 1, 1);

        //Debug.Log("After dispatch " + (Time.realtimeSinceStartup - startTime));
        //startTime = Time.realtimeSinceStartup;

        AsyncGPUReadback.Request(verticesBuffer, request =>
        {

            // Get data from shader
            verticesBuffer.GetData(vertices);
            colorsBuffer.GetData(colors);
            uvsBuffer.GetData(uvs);
            normalsBuffer.GetData(normals);
            facesBuffer.GetData(faces);
            counterBuffer.GetData(counters);

            // Get only valid faces
            //faces = faces.Where(val => val != 0).ToArray();

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
            _isGenerationRunning = false;

            //Debug.Log("After mesh update " + (Time.realtimeSinceStartup - startTime));
            //startTime = Time.realtimeSinceStartup;

            // Release the allocated data
            voxelsBuffer.Dispose();
            voxelsBuffer.Release();
            verticesBuffer.Dispose();
            verticesBuffer.Release();
            colorsBuffer.Dispose();
            colorsBuffer.Release();
            uvsBuffer.Dispose();
            uvsBuffer.Release();
            normalsBuffer.Dispose();
            normalsBuffer.Release();
            facesBuffer.Dispose();
            facesBuffer.Release();
        });
    }

    public void DestroyVoxel(Vector3 pos, int area)
    { 
        pos -= transform.position;
        float voxelX = (int)((pos.x / _chunkSize) * GenerateWorld.VOXELS_IN_CHUNK);
        float voxelY = (int)((pos.y / _chunkSize) * GenerateWorld.VOXELS_IN_CHUNK);
        float voxelZ = (int)((pos.z / _chunkSize) * GenerateWorld.VOXELS_IN_CHUNK);

        for (float x = -area; x <= area; x++)
        {
            for (float y = -area; y <= area; y ++)
            {
                for (float z = -area; z <= area; z ++)
                {
                    Vector3 offset = new Vector3(x,y,z);
                    if (offset.magnitude > area + 0.5f) continue;

                    Vector3 newPos = new Vector3(voxelX + x, voxelY + y, voxelZ + z);
                    if (newPos.x < 0)
                    {
                        newPos.x += GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.left * _chunkSize);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.x >= GenerateWorld.VOXELS_IN_CHUNK)
                    {
                        newPos.x -= GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.right * _chunkSize);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.y < 0 || newPos.y >= GenerateWorld.MAX_HEIGHT)
                    {
                        continue;
                    }
                    if (newPos.z < 0)
                    {
                        newPos.z += GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.back * _chunkSize);
                        if (otherChunk != null)
                        {
                            otherChunk.DestroyVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.z >= GenerateWorld.VOXELS_IN_CHUNK)
                    {
                        newPos.z -= GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.forward * _chunkSize);
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

    public void CreateVoxel(Vector3 pos, int area)
    { 
        pos -= transform.position;
        float voxelX = (int)((pos.x / _chunkSize) * GenerateWorld.VOXELS_IN_CHUNK);
        float voxelY = (int)((pos.y / _chunkSize) * GenerateWorld.VOXELS_IN_CHUNK);
        float voxelZ = (int)((pos.z / _chunkSize) * GenerateWorld.VOXELS_IN_CHUNK);

        for (float x = -area; x <= area; x++)
        {
            for (float y = -area; y <= area; y ++)
            {
                for (float z = -area; z <= area; z ++)
                {
                    Vector3 offset = new Vector3(x,y,z);
                    if (offset.magnitude > area + 0.5f) continue;

                    Vector3 newPos = new Vector3(voxelX + x, voxelY + y, voxelZ + z);
                    if (newPos.x < 0)
                    {
                        newPos.x += GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.left * _chunkSize);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.x >= GenerateWorld.VOXELS_IN_CHUNK)
                    {
                        newPos.x -= GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.right * _chunkSize);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.y < 0 || newPos.y >= GenerateWorld.MAX_HEIGHT)
                    {
                        continue;
                    }
                    if (newPos.z < 0)
                    {
                        newPos.z += GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.back * _chunkSize);
                        if (otherChunk != null)
                        {
                            otherChunk.CreateVoxelFromOtherChunk(newPos);
                        }
                        continue;
                    }
                    if (newPos.z >= GenerateWorld.VOXELS_IN_CHUNK)
                    {
                        newPos.z -= GenerateWorld.VOXELS_IN_CHUNK;
                        Chunk otherChunk = _generateWorld.GetChunk(transform.position + Vector3.forward * _chunkSize);
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
}
