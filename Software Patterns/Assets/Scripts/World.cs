using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttribute biome;
    
    public Transform player;
    public Vector3 spawnPosition;
    
    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    private ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;

    private Queue<ChunkCoord> chunksToCreate = new Queue<ChunkCoord>();
    private bool _isCreatingChunks;
    
    
    //Debug Screen
    public GameObject debugScreen;
    private void Start()
    {
        Random.InitState(seed);
        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight - 20,
            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
        
        debugScreen.SetActive(false);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0 && !_isCreatingChunks)
            StartCoroutine("CreateChunks");
        
        if (Input.GetKeyDown(KeyCode.Tab))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    void GenerateWorld()
    {
        int startLimit = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks;
        int finishLimit = (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks;
        for (int x = startLimit; x < finishLimit; x++)
        {
            for (int z = startLimit; z < finishLimit; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        player.position = spawnPosition;
    }

    IEnumerator CreateChunks()
    {
        _isCreatingChunks = true;

        while (chunksToCreate.Count > 0)
        {
            ChunkCoord currentChunkCoord = chunksToCreate.Dequeue();
            chunks[currentChunkCoord.x, currentChunkCoord.z].Init();
            yield return null;
        }
        
        _isCreatingChunks = false;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];
    }
    
    private void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;
        
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (isChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunksToCreate.Enqueue(new ChunkCoord(x, z));
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
                
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].isActive = false;
        }
    }
    
    public bool CheckForVoxel (Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!isChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight) return false;
        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;

        return blockTypes[GetVoxel(pos)].isSolid;
    }

    public byte GetVoxel(Vector3 pos)
    {
        int y = Mathf.FloorToInt(pos.y);
        /* IMMUTABLE PASS */
        
        // if outside world, return air block
        if (!isVoxelInWorld(pos)) return 0;
        
        // if bottom of chunk, return bedrock
        if (y == 0) return 1;

        // BASIC TERRAIN PASS   

        int terrainHeight = Mathf.FloorToInt( biome.terrainHeight * Noise.Get2DPerlin
            (new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
        byte voxelValue = 0;
        
        if (y == terrainHeight) voxelValue = 3;
        else if (y < terrainHeight && y > terrainHeight - 4) voxelValue = 5;
        else if (y > terrainHeight) voxelValue = 0;
        else voxelValue = 2;
        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (y > lode.minHeight && y < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.BlockID;
                    
            }
        }
        return voxelValue;

    }
    /*void CreateNewChunk(int x, int z)
    {
        chunks[x, z] = new Chunk(new ChuckCoord(x, z), this);
        activeChunks.Add(new ChuckCoord(x, z));
    }*/

    bool isChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 &&
            coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1) return true;
        return false;
    }

    bool isVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
            pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels )
            return true;
        return false;
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture values")] 
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
        }
        return -1;
    }

}
