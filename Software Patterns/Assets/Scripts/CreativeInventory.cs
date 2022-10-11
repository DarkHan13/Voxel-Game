using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreativeInventory : MonoBehaviour
{
    public GameObject slotPrefab;
    private World _world;

    private List<ItemSlot> slots = new List<ItemSlot>();

    void Start()
    {
        _world = GameObject.Find("World").GetComponent<World>();

        for (int i = 1; i < _world.blockTypes.Length; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, transform);
            ItemStack stack = new ItemStack((byte)i, 64);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack);
            slot.isCreative = true;
        }
    }

}
