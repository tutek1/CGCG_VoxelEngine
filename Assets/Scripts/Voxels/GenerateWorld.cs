using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWorld : MonoBehaviour
{
    public const int MAX_HEIGHT = 100;

    public const int VOXELS_IN_CHUNK = 4;

    [Header("Chunks")]
    [SerializeField] private int _diameterOfChunks = 1;
    [SerializeField] private float _chunkSize = 16;
    [SerializeField] private Material _voxelMaterial;
    [SerializeField] private ComputeShader _computerShader;

    [Space]
    [Header("Procedural Generation")]
    [SerializeField] private bool _useNoise = true;
    [SerializeField] private int _seed = 13;
    [SerializeField] private int _octaves = 5;
    [SerializeField] private float _frequencyDiff = 2.0f;
    [SerializeField] private float _amplitudeDiff = 0.5f;
    [SerializeField] private float _scale = 5f;

    [SerializeField] private int _terrainHeight = 10;
    
    [SerializeField] private bool _generate = false;
    
    private Dictionary<Vector3, Chunk> _chunks = new Dictionary<Vector3, Chunk>();
    
    // DEBUG
    void Update()
    {
        if (_generate){
            _generate = false;
            Generate();
        }
    }

    private void Generate()
    {
        // Delete all existing chunks if some exist
        _chunks.Clear();
        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        
        float voxelScale = (float)_chunkSize / VOXELS_IN_CHUNK;

        // Start tracking time to test performance
        double startTime = Time.realtimeSinceStartup;
        
        // Create chunks in the -diameter to +diameter square radius
        for (int x = -_diameterOfChunks; x < _diameterOfChunks; x++)
        {
            for (int z = -_diameterOfChunks; z < _diameterOfChunks; z++)
            {
                // Set name, position, scale and parent
                Chunk chunk = new GameObject("Chunk[" + x +"," + z + "]").AddComponent<Chunk>();
                Vector3 chunkPosition = new Vector3(x * _chunkSize, 0, z * _chunkSize);
                chunk.transform.position = chunkPosition;
                chunk.transform.SetParent(transform);

                // Initialize the chunks and create the meshes
                chunk.Init(this, _voxelMaterial, _computerShader);
                chunk.GenerateVoxels(_useNoise, _seed, _octaves, _frequencyDiff, _amplitudeDiff, _scale, _terrainHeight, _chunkSize);

                _chunks.Add(chunkPosition, chunk);
            }
        }

        Debug.Log("Voxel generation took: " + (Time.realtimeSinceStartup - startTime) + " seconds.");
        startTime = Time.realtimeSinceStartup;

        foreach (Chunk savedChunk in _chunks.Values)
        {
            savedChunk.GenerateMesh();
        }

        Debug.Log("Generation took without async: " + (Time.realtimeSinceStartup - startTime) + " seconds.");
    }

    public Chunk GetChunk(Vector3 pos)
    {
        return _chunks.ContainsKey(pos) ? _chunks[pos] : null;
    }
}
