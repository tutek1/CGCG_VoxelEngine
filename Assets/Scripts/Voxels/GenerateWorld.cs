using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWorld : MonoBehaviour
{
    private const uint DEFAULT_SIZE = 16;

    [SerializeField] private int _diameterOfChunks = 1;
    [SerializeField] private int _voxelsInChunk = 16;
    [SerializeField] private Material _voxelMaterial;

    [Space]
    [Header("Procedural Generation")]
    [SerializeField] private bool _useNoise = true;
    [SerializeField] private int _seed = 13;
    //[SerializeField] private int _octaves = 3;
    [SerializeField] private float _frequency = 20.0f;
    [SerializeField] private float _amplitude = 0.5f;
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
        
        float voxelScale = (float)DEFAULT_SIZE / _voxelsInChunk;

        // Start tracking time to test performance
        double startTime = Time.realtimeSinceStartup;
        
        // Create chunks in the -diameter to +diameter square radius
        for (int x = -_diameterOfChunks; x < _diameterOfChunks; x++)
        {
            for (int z = -_diameterOfChunks; z < _diameterOfChunks; z++)
            {
                // Set name, position, scale and parent
                Chunk chunk = new GameObject("Chunk[" + x +"," + z + "]").AddComponent<Chunk>();
                Vector3 chunkPosition = new Vector3(x * _voxelsInChunk * voxelScale, 0, z * _voxelsInChunk * voxelScale);
                chunk.transform.position = chunkPosition;
                chunk.transform.SetParent(transform);

                // Initialize the chunks and create the meshes
                chunk.Init(this, _voxelMaterial);
                //chunk.GenerateVoxels(_useNoise, _seed, _octaves, _frequency, _amplitude, _terrainHeight, _voxelsInChunk);
                chunk.GenerateVoxels(_useNoise, _seed, 1, _frequency, _amplitude, _terrainHeight, _voxelsInChunk);

                _chunks.Add(chunkPosition, chunk);
            }
        }

        foreach (Chunk savedChunk in _chunks.Values)
        {
            savedChunk.GenerateMesh();
        }

        Debug.Log("Generation took: " + (Time.realtimeSinceStartup - startTime) + " seconds.");
    }

    public bool IsVoxelInChunkPresentAndNotTop(Chunk callingChunk, Vector3 chunkPos, Vector3 voxelPos)
    {
        if (!_chunks.ContainsKey(chunkPos)) return false;
        
        Chunk targetChunk = _chunks[chunkPos];
        // TODO will be fixed by using octatree
        if (targetChunk._voxelsInChunk > callingChunk._voxelsInChunk) return false;

        float scaleCoef = (float)targetChunk._voxelsInChunk/callingChunk._voxelsInChunk;
        voxelPos.x = (int)(voxelPos.x * scaleCoef);
        voxelPos.y = (int)(voxelPos.y * scaleCoef);
        voxelPos.z = (int)(voxelPos.z * scaleCoef);

        return targetChunk.IsVoxelPresent(voxelPos) && targetChunk.IsVoxelPresent(voxelPos + Vector3.up);
    }
}
