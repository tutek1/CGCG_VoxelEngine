using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWorld : MonoBehaviour
{
    public const int MAX_HEIGHT = 256;

    public const int CHUNK_SIZE = 16;

    [Header("Chunks")]
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

    [Header("Realtime LODing")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private int _viewDistance = 8;
    [SerializeField] private int _maxDetailDistance = 5;
    [SerializeField] private int _minDetail = 4;
    [SerializeField] [Range(1, 12)] private int _LODingOffset = 1;

    
    private Dictionary<Vector3, Chunk> _chunks = new Dictionary<Vector3, Chunk>();
    
    void Start()
    {
        InvokeRepeating(nameof(CheckChunksToRemove), 0.5f, 1.0f);
        InvokeRepeating(nameof(CheckChunksToCreate), 0.0f, 1.0f);
    }

    // DEBUG
    void FixedUpdate()
    {
        if (_generate){
            _generate = false;
            ReGenerate();
        }
    }

    private void ReGenerate()
    {
        // Delete all existing chunks if some exist
        _chunks.Clear();
        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void GenerateChunk(int x, int z)
    {
        // Set name, position, scale and parent
        Chunk chunk = new GameObject("Chunk[" + x +"," + z + "]").AddComponent<Chunk>();
        Vector3 chunkPosition = new Vector3(x * CHUNK_SIZE, 0, z * CHUNK_SIZE);
        chunk.transform.position = chunkPosition;
        chunk.transform.SetParent(transform);

        // Initialize the chunks and create the meshes
        chunk.Init(this, _voxelMaterial, _computerShader);
        chunk.GenerateVoxels(_useNoise, _seed, _octaves, _frequencyDiff, _amplitudeDiff, _scale, _terrainHeight, _voxelsInChunk);
        chunk.GenerateMesh();
        _chunks.Add(chunkPosition, chunk);
    }

    private void CheckChunksToRemove()
    {
        List<Vector3> keysToDelete = new List<Vector3>();
        Vector3 playerPos = _playerTransform.position;
        playerPos.y = 0;

        foreach (var chunkPos in _chunks.Keys)
        {

            if (Vector3.Distance(chunkPos, playerPos) > (_viewDistance + _LODingOffset) * CHUNK_SIZE)
            {
                keysToDelete.Add(chunkPos);
            }
        }

        foreach (Vector3 chunkPos in keysToDelete)
        {
            Chunk chunk = _chunks[chunkPos];
            Destroy(chunk.gameObject);
            _chunks.Remove(chunkPos);
        }
    }

    private void CheckChunksToCreate()
    {
        Vector3 playerPos = _playerTransform.position;
        playerPos.y = 0;
        int xMid = Mathf.RoundToInt(playerPos.x / CHUNK_SIZE);
        int zMid = Mathf.RoundToInt(playerPos.z / CHUNK_SIZE);

        int xStart = (xMid - _viewDistance + _LODingOffset/2);
        int xEnd = (xMid + _viewDistance - _LODingOffset/2);

        int zStart = (zMid - _viewDistance + _LODingOffset/2);
        int zEnd = (zMid + _viewDistance - _LODingOffset/2);
        
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int z = zStart; z <= zEnd; z++)
            {
                Vector3 chunkPos = new Vector3(x, 0, z) * CHUNK_SIZE;
                if ((chunkPos - playerPos).magnitude > (_viewDistance - _LODingOffset) * CHUNK_SIZE) continue;
                if (_chunks.ContainsKey(chunkPos)) continue;
                GenerateChunk(x, z);
            }
        }
    }

    public Chunk GetChunk(Vector3 pos)
    {
        return _chunks.ContainsKey(pos) ? _chunks[pos] : null;
    }
}
