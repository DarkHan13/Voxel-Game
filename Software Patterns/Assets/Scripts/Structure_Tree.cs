using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure_Tree 
{
    public static Queue<VoxelMod> MakeTree(Vector3 pos, int minTrunkHeight, int maxTrunkHeight, World world)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 250f, 3f));

        if (height < minTrunkHeight) height = minTrunkHeight;
        for (int i = 1; i < height; i++)
            queue.Enqueue(new VoxelMod(new Vector3(pos.x, pos.y + i, pos.z), 6));

        for (int y = height; y > height - 3; y--)
        {
            for (int x = y - height; x <= height - y; x++)
            {
                for (int z = y - height; z <= height - y; z++)
                {
                    if (x != 0 || z != 0 || y == height)
                        queue.Enqueue(new VoxelMod(
                            new Vector3(pos.x + x, pos.y + y, pos.z + z), 11));
                }
            }
        }

        return queue;
    }
}
