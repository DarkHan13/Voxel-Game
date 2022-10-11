using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;
    
    private GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    private MeshCollider _meshCollider;
    
    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    private List<int> transparentTriangles = new List<int>();
    private Material[] _materials = new Material[2];

    public Vector3 position;

    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
    
    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private bool isVoxelMapPopulated = false;
    
    World world;

    private bool _isActive;

    public Chunk(ChunkCoord coord, World _world)
    {
        this.coord = coord;
        world = _world;
        isActive = true;
        
    }

    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        _meshCollider = chunkObject.AddComponent<MeshCollider>();


        _materials[0] = world.material;
        _materials[1] = world.transparentMaterial;
        meshRenderer.materials = _materials;
        
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f,
            this.coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk (" + coord.x + ", " + coord.z + ")";

        position = chunkObject.transform.position;
        
        PopulateVoxelMap();

        // На всякий случай
        //_meshCollider.sharedMesh = meshFilter.mesh;
    }


    public void UpdateChunk()
    {

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = v.id;
        }
        
        ClearMeshData();
        
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        UpdateMeshData(new Vector3(x, y, z));
                }    
            }
        }

        lock (world.chunksToDraw)
        {
            world.chunksToDraw.Enqueue(this);
        }
        
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        //_meshCollider.sharedMesh = null;
    }

    public bool isActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            if (chunkObject != null) chunkObject.SetActive(value);
        }
    }
    

    public bool isEditable
    {
        get
        {
            if (!isVoxelMapPopulated) return false;
            return true;
        }
    }
    
    bool isVoxelInChunk(int x, int y, int z)
    {
        return !(x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 ||
                y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1);
    }
    
    public void EditVoxel(Vector3 pos, byte newId) {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(chunkObject.transform.position.x);
        z -= Mathf.FloorToInt(chunkObject.transform.position.z);

        voxelMap[x, y, z] = newId;
        
        // Update surrounding chunks
        UpdateSurroundingVoxels(x, y, z);
        
        UpdateChunk();
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.FaceChecks[p];

            if (!isVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.GetChunkFromVector3(currentVoxel + position).UpdateChunk();
            }
        }
    }
    
    bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!isVoxelInChunk(x, y, z))
            return world.CheckIfVoxelTransparent(pos + position);
        return world.blockTypes[voxelMap[x, y, z]].renderNeighborFaces;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(position.x);
        z -= Mathf.FloorToInt(position.z);

        return voxelMap[x, y, z];
    }
    
    void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }    
            }
        }
        UpdateChunk();
        isVoxelMapPopulated = true;
    }
    
    // Add voxel Data to Chunk
    void UpdateMeshData(Vector3 pos)
    {
        
        byte blockId = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blockTypes[blockId].renderNeighborFaces;
        
        for (int p = 0; p < 6; p++)
        {
            if (CheckVoxel(pos + VoxelData.FaceChecks[p]))
            {
                for (int i = 0; i < 4; i++)
                {
                    vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris [p, i]]);
                }
                AddTexture(world.blockTypes[blockId].GetTextureID(p));
                
                
                if (!isTransparent)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }
                vertexIndex += 4;
            }
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        
        mesh.uv = uvs.ToArray();
        
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        
        _meshCollider.sharedMesh = meshFilter.mesh;
    }

    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizeBlockTextureSize;
        y *= VoxelData.NormalizeBlockTextureSize;

        y = 1f - y - VoxelData.NormalizeBlockTextureSize;
        
        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizeBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizeBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizeBlockTextureSize, y + VoxelData.NormalizeBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }
    public ChunkCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xBlock = Mathf.FloorToInt(pos.x);
        int zBlock = Mathf.FloorToInt(pos.z);

        x = xBlock / VoxelData.ChunkWidth;
        z = zBlock / VoxelData.ChunkWidth;
    }

    public bool Equals(ChunkCoord other)
    {
        if (other == null) return false;
        if (other.x == x && other.z == z) return true;
        return false;
    }
}
