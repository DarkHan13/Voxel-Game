using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    private World _world;
    private Text _text;

    private float timer;
    private float frameRate;

    private int halfWorldSizeInVoxels;  
    void Start()
    {
        _world = GameObject.Find("World").GetComponent<World>();
        _text = GetComponent<Text>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
    }

    void Update()
    {
        string debugText = "Software patterns: Voxel game in Unity\n";
        debugText += "\n";
        debugText += "FPS: " + frameRate;
        debugText += "\n\n";

        debugText += "XYZ: " + Mathf.FloorToInt(_world.player.transform.position.x - halfWorldSizeInVoxels) + " / "
                     + Mathf.FloorToInt(_world.player.transform.position.y) + " / "
                     + Mathf.FloorToInt(_world.player.transform.position.z - halfWorldSizeInVoxels);
        
        _text.text = debugText;

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        } else
        {
            timer += Time.deltaTime;
        }
    }
}
