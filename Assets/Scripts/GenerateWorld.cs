using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWorld : MonoBehaviour
{
    [SerializeField] private int _diameterOfChunks = 1;
    [SerializeField] private int _chunkSizeInVoxels = 16;
    [SerializeField] private float _voxelScale = 1;
    [SerializeField] private Material _voxelMaterial;

    [Space]
    [Header("Procedural Generation")]
    [SerializeField] private bool _useNoise = true;
    [SerializeField] private int _seed = 13;
    [SerializeField] private int _octaves = 3;
    [SerializeField] private float _frequency = 20.0f;
    [SerializeField] private float _amplitude = 0.5f;
    [SerializeField] private int _terrainHeight = 10;
    
    [SerializeField] private bool _generate = false;
    
    //private HashMap<Vector2, Chunk> _chunks = new Dictionary<Vector2, Chunk>();
    
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
        // Setup seed
        Random.InitState(_seed);
        float randomOffset = Random.value * 100000f;

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
                Vector3 chunkPosition = new Vector3(x * _chunkSizeInVoxels * _voxelScale, 0, z * _chunkSizeInVoxels * _voxelScale);
                chunk.transform.position = chunkPosition;
                chunk.transform.localScale = Vector3.one * _voxelScale;
                chunk.transform.SetParent(transform);

                // Generate all voxels and add them to a chunk
                for (int voxelX = 0; voxelX < _chunkSizeInVoxels; voxelX++)
                {
                    for (int voxelZ = 0; voxelZ < _chunkSizeInVoxels; voxelZ++)
                    {
                        // Generate a random height (TODO use Perlin noise)
                        //float 


                        // Terrain gen but not really working good
                        float height = 1;

                        if (_useNoise) {
                            float frequency = _frequency;
                            float amplitude = _amplitude;
                            for (int octave = 0; octave < _octaves; octave++)
                            {
                                height += (Mathf.PerlinNoise((chunkPosition.x + voxelX) / frequency + randomOffset,
                                                            (chunkPosition.z + voxelZ) / frequency + randomOffset) * 2 - 1) * amplitude;
                                frequency *= _frequency;
                                amplitude *= _amplitude;
                            }
                            height *= _terrainHeight;
                        }
                        else {
                            height = Random.value * _terrainHeight;;
                        }

                        if (height < 1) height = 1;

                        for (int voxelY = 0; voxelY < height; voxelY++)
                        {
                            // Generate a random color for each voxel for now
                            //Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                            Color color;
                            if (_useNoise)
                            {
                                if (voxelY + 1 > height) color = new Color(0f, 0.2f, 0f);
                                else                      color = new Color(0.4f, 0.3f, 0f);
                            }
                            else
                            {
                                color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                            }
                            
                            chunk[new Vector3(voxelX, voxelY, voxelZ)] = new Voxel() {color=color};
                        }
                    }                    
                }

                // Initialize the chunks and create the meshes
                chunk.Init(_voxelMaterial);
                chunk.GenerateMesh();
            }
        }

        Debug.Log("Generation took: " + (Time.realtimeSinceStartup - startTime) + " seconds.");
    }
}
