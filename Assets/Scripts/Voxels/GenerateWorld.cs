using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWorld : MonoBehaviour
{
    public const int MAX_HEIGHT = 128;


    [Header("Chunks")]
    [SerializeField] private int _chunkSize = 32;

    [SerializeField] private int _voxelsInChunk = 32;
    [SerializeField] private Material _voxelMaterial;
    [SerializeField] private ComputeShader _computerShader;

    [Space]
    [Header("Procedural Generation")]
    [SerializeField] private bool _useNoise = true;
    [SerializeField] private int _seed = 13;
    [SerializeField] private int _octaves = 4;
    [SerializeField] private float _frequencyDiff = 2.0f;
    [SerializeField] private float _amplitudeDiff = 0.4f;
    [SerializeField] private float _scale = 200f;
    [SerializeField] private int _terrainHeight = 40;
    [SerializeField] private bool _generate = false;

    [Header("Performance and Realtime LODing")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private int _viewDistance = 8;
    [SerializeField] private Chunk.LODs _maxDetail = Chunk.LODs.Original;
    [SerializeField] private Chunk.LODs _minDetail = Chunk.LODs.Eighth;
    [SerializeField] [Range(-4, 20)] private int _LODingStart = 8;
    [SerializeField] [Range(0, 20)] private int _deleteOffset = 4;
    [SerializeField] [Range(1, 40)] private int _numChunksToCheck = 10;

    
    private Dictionary<Vector3, Chunk> _chunks = new Dictionary<Vector3, Chunk>();

    
    void Start()
    {
        GenerateAllChunks();
    }

    // DEBUG
    void FixedUpdate()
    {
        if (_generate){
            _generate = false;
            ReGenerate();
        }

    }

    private void Update()
    {
        CheckAChunkToCreate();
    }

    private void ReGenerate()
    {
        // Delete all existing chunks if some exist
        _chunks.Clear();
        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        GenerateAllChunks();
    }

    private void GenerateChunk(int x, int z, Chunk.LODs detail)
    {
        // Set name, position, scale and parent
        Chunk chunk = new GameObject("Chunk[" + x +"," + z + "]").AddComponent<Chunk>();
        Vector3 chunkPosition = new Vector3(x * _chunkSize, 0, z * _chunkSize);
        chunk.transform.position = chunkPosition;
        chunk.transform.SetParent(transform);

        // Initialize the chunks and create the meshes
        chunk.Init(this, _voxelMaterial, _computerShader, _playerTransform);
        chunk.GenerateVoxels(_useNoise, _seed, _octaves, _frequencyDiff, _amplitudeDiff, _scale, _terrainHeight, _voxelsInChunk, _chunkSize);
        chunk.DefferedGenerateMesh();
        _chunks.Add(chunkPosition, chunk);
    }

    private int currentX = int.MaxValue;
    private int currentZ = int.MaxValue;
    private void CheckAChunkToCreate()
    {
        Vector3 playerPos = _playerTransform.position;
        playerPos.y = 0;
        int xMid = Mathf.RoundToInt(playerPos.x / _chunkSize);
        int zMid = Mathf.RoundToInt(playerPos.z / _chunkSize);

        int xStart = xMid - _viewDistance;
        int xEnd = xMid + _viewDistance;

        int zStart = zMid - _viewDistance;
        int zEnd = zMid + _viewDistance;

        if (currentX == int.MaxValue) currentX = xStart;
        if (currentZ == int.MaxValue) currentZ = zStart;


        for (int updateIdx = 0; updateIdx < _numChunksToCheck; updateIdx++)
        {
            currentX++;
            if (currentX > xEnd)
            {
                currentX = xStart;
                currentZ++;
                if (currentZ > zEnd)
                {
                    currentZ = zStart;
                }

            }
            Vector3 chunkPos = new Vector3(currentX, 0, currentZ) * _chunkSize;
            if ((chunkPos - playerPos).magnitude > _viewDistance * _chunkSize) continue;
            if (_chunks.ContainsKey(chunkPos)) continue;

            GenerateChunk(currentX, currentZ, _minDetail);
        }
        
    }

    private void GenerateAllChunks()
    {
        // Generate all chunks
        Vector3 playerPos = _playerTransform.position;
        playerPos.y = 0;
        int xMid = Mathf.RoundToInt(playerPos.x / _chunkSize);
        int zMid = Mathf.RoundToInt(playerPos.z / _chunkSize);

        int xStart = xMid - _viewDistance;
        int xEnd = xMid + _viewDistance;

        int zStart = zMid - _viewDistance;
        int zEnd = zMid + _viewDistance;

        for (int x = xStart; x <= xEnd; x++)
        {
            for (int z = zStart; z <= zEnd; z++)
            {
                Vector3 chunkPos = new Vector3(x, 0, z) * _chunkSize;
                if ((chunkPos - playerPos).magnitude > _viewDistance * _chunkSize) continue;
                if (_chunks.ContainsKey(chunkPos)) continue;
                GenerateChunk(x, z, _minDetail);
            }
        }
    }

    public Chunk GetChunk(Vector3 pos)
    {
        return _chunks.ContainsKey(pos) ? _chunks[pos] : null;
    }

    public void RemoveChunk(Vector3 pos)
    {
        pos.y = 0;
        _chunks.Remove(pos);
    }

    public Chunk.LODs GetMinDetail()
    {
        return _minDetail;
    }

    public Chunk.LODs GetMaxDetail()
    {
        return _maxDetail;
    }

    public int GetLODingStart()
    {
        return _LODingStart;
    }

    public int GetViewDistance()
    {
        return _viewDistance;
    }

    public int GetDeleteOffset()
    {
        return _deleteOffset;
    }

}
