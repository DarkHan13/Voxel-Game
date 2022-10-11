using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_raycaster = null;
    private PointerEventData m_pointerEventData;
    [SerializeField] private EventSystem m_eventSystem = null;

    private World _world;
    private void Start()
    {
        _world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if (!_world.inUI) return;

        cursorSlot.transform.position = Input.mousePosition;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (CheckForSlot() != null) HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        if (clickedSlot == null) return;
        
        if (!clickedSlot.hasItem && !cursorSlot.hasItem) return;

        if (clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot._stack);
        }
        
        if (!cursorSlot.hasItem && clickedSlot.hasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
        }
        else if (cursorSlot.hasItem && clickedSlot.hasItem)
        {
            if (cursorSlot.itemSlot._stack.id != clickedSlot.itemSlot._stack.id)
            {
                ItemStack tmp = cursorItemSlot._stack;
                cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
                clickedSlot.itemSlot.InsertStack(tmp);
            }
        }
        else if (cursorSlot.hasItem && !clickedSlot.hasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll()); 
        }
    }
    private UIItemSlot CheckForSlot()
    {
        m_pointerEventData = new PointerEventData(m_eventSystem);
        m_pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_raycaster.Raycast(m_pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("UIItemSlot"))
            {
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
