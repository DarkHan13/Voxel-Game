using System;
using UnityEngine;
using Random = UnityEngine.Random;


public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highLight;
    [NonSerialized] public byte slotIndex = 0;

    private void Start()
    {

        byte blockId = 1;
        foreach (UIItemSlot s in slots)
        {
            ItemStack stack = new ItemStack(blockId, Random.Range(1, 65));
            ItemSlot slot = new ItemSlot(s, stack);
            blockId++;
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0) slotIndex++;
            else slotIndex--;
            if (slotIndex < 0) slotIndex = 8;
            else if (slotIndex > 8) slotIndex = 0;

            highLight.position = slots[slotIndex].slotIcon.transform.position;
        }

        for (byte i = 0; i <= 8; i++)
        {
            if (Input.GetKeyDown((KeyCode)(49 + i)))
            {
                slotIndex = i;
                
                highLight.position = slots[slotIndex].slotIcon.transform.position;
                break;
            }
        }
    }
}
