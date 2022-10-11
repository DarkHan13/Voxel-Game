using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Threading;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttribute biome;
    
    public Transform player;
    public Vector3 spawnPosition;
    
    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    //private
    public Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    private ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;

    private Queue<ChunkCoord> chunksToCreate = new Queue<ChunkCoord>();
    private List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;
    
    private Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;
    public GameObject CreativeInventory;
    public GameObject CursorSlot;
    
    // Threading
    private Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();

    //Debug Screen
    public GameObject debugScreen;
    private void Start()
    {
        Random.InitState(seed);
        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight - 20,
            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

        ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
        ChunkUpdateThread.Start();
        
        debugScreen.SetActive(false);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        
        if (!playerChunkCoord.Equals(playerLastChunkCoord)) 
            CheckViewDistance();

        if (chunksToCreate.Count > 0) 
            CreateChunk();
        
        if (chunksToUpdate.Count > 0)
            UpdateChunks();
        
        if (chunksToDraw.Count > 0)
            lock (chunksToDraw)
            {
                if (chunksToDraw.Peek().isEditable) chunksToDraw.Dequeue().CreateMesh();
            } 
        
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
                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(newChunk, this);
                //activeChunks.Add(new ChunkCoord(x, z));
                chunksToCreate.Enqueue(newChunk);
                
            }
        }

        player.position = spawnPosition;
    }

    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate.Dequeue();
        activeChunks.Add(c);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].isEditable)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(index);
                updated = true;
            }
            else index++;
        }
    }


    void ThreadedUpdate()
    {
        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();
        }
    }

    private void OnDisable()
    {
        ChunkUpdateThread.Abort();
    }

    void ApplyModifications()
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                ChunkCoord c = GetChunkCoordFromVector3(v.position);

                if (chunks[c.x, c.z] == null)
                {
                    chunks[c.x, c.z] = new Chunk(c, this);
                    activeChunks.Add(c);
                }
            
                chunks[c.x, c.z].modifications.Enqueue(v);
            
                if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
                    chunksToUpdate.Add(chunks[c.x, c.z]);                
            }

        }

        applyingModifications = false;
        
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

        activeChunks.Clear();
        
        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (isChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunksToCreate.Enqueue(new ChunkCoord(x, z));
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
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
        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;

        return blockTypes[GetVoxel(pos)].isSolid;
    }
    
    public bool CheckIfVoxelTransparent (Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!isChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight) return false;
        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].renderNeighborFaces;

        return blockTypes[GetVoxel(pos)].renderNeighborFaces;
    }

    public bool inUI
    {
        get { return _inUI; }
        set
        {
            _inUI = value;
            if (inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                CreativeInventory.SetActive(true);
                CursorSlot.SetActive(true);
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                CreativeInventory.SetActive(false);
                CursorSlot.SetActive(false);
                Cursor.visible = false;
            }
        }
    }
    public byte GetVoxel(Vector3 pos)
    {
        int y = Mathf.FloorToInt(pos.y);
        /* IMMUTABLE PASS */
        
        // if outside world, return air block
        if (!isVoxelInWorld(pos)) return 0;
        
        // if bottom of chunk, return bedrock
        if (y == 0) return 1;

        /* BASIC TERRAIN PASS */   

        int terrainHeight = Mathf.FloorToInt( biome.terrainHeight * Noise.Get2DPerlin
            (new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
        byte voxelValue = 0;
        
        if (y == terrainHeight) voxelValue = 3;
        else if (y < terrainHeight && y > terrainHeight - 4) voxelValue = 5;
        else if (y > terrainHeight) voxelValue = 0;
        else voxelValue = 2;
        
        /* SECOND PASS */
        
        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (y > lode.minHeight && y < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.BlockID;

            }
        }
        
        /* TREE PASS */
        if (y == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treePlacementScale) >
                    biome.treePlacementThreshold)
                {
                    modifications.Enqueue(Structure_Tree.MakeTree(pos, biome.minTreeHeight, 
                        biome.maxTreeHeight, this));
                }
            }
        }
        
        return voxelValue;

    }
    
    // useless code
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
    public bool renderNeighborFaces;
    public Sprite icon;
    
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

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }
    
    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}
