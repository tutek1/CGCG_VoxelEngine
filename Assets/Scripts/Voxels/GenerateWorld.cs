using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWorld : MonoBehaviour
{
    [SerializeField] private int _diameterOfChunks = 1;
    [SerializeField] private int _voxelsInChunk = 16;
    [SerializeField] private float _voxelScale = 1;
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
    
    //private Dictionary<Vector2, Chunk> _chunks = new Dictionary<Vector2, Chunk>();
    
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
        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // Start tracking time to test performance
        double startTime = Time.realtimeSinceStartup;
        
        // Create chunks in the -diameter to +diameter square radius
        for (int x = -_diameterOfChunks; x < _diameterOfChunks; x++)
        {
            for (int z = -_diameterOfChunks; z < _diameterOfChunks; z++)
            {
                // Set name, position, scale and parent
                Chunk chunk = new GameObject("Chunk[" + x +"," + z + "]").AddComponent<Chunk>();
                Vector3 chunkPosition = new Vector3(x * _voxelsInChunk * _voxelScale, 0, z * _voxelsInChunk * _voxelScale);
                chunk.transform.position = chunkPosition;
                chunk.transform.localScale = Vector3.one * _voxelScale;
                chunk.transform.SetParent(transform);

                // Initialize the chunks and create the meshes
                chunk.Init(_voxelMaterial);
                //chunk.GenerateVoxels(_useNoise, _seed, _octaves, _frequency, _amplitude, _terrainHeight, _voxelsInChunk);
                chunk.GenerateVoxels(_useNoise, _seed, 1, _frequency, _amplitude, _terrainHeight, _voxelsInChunk);
                chunk.GenerateMesh();
            }
        }

        Debug.Log("Generation took: " + (Time.realtimeSinceStartup - startTime) + " seconds.");
    }
}
