using System;
using UnityEngine;
using UnityEngine.UI;
public class UIItemSlot : MonoBehaviour
{
    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public Text slotAmount;

    private World _world;

    private void Awake()
    {
        _world = GameObject.Find("World").GetComponent<World>();
    }

    public bool hasItem
    {
        get
        {
            if (itemSlot == null) return false;
            return itemSlot.HasItem;
        }
    }

    public void Link(ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void UnLink()
    {
        itemSlot.UnLinkkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (itemSlot != null && itemSlot.HasItem)
        {
            slotIcon.sprite = _world.blockTypes[itemSlot._stack.id].icon;
            slotAmount.text = itemSlot._stack.amount.ToString();
            slotIcon.enabled = true;
            slotAmount.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy()
    {
        if (itemSlot != null) itemSlot.UnLinkkUISlot();
    }
}

public class ItemSlot
{
    public ItemStack _stack;
    private UIItemSlot _uiItemSlot = null;
    public bool isCreative = false;


    public ItemSlot(UIItemSlot uiItemSlot)
    {
        _uiItemSlot = uiItemSlot;
        uiItemSlot.Link(this);
    }
    
    public ItemSlot(UIItemSlot uiItemSlot, ItemStack stack)
    {
        _stack = stack;
        _uiItemSlot = uiItemSlot;
        Debug.Log(this);
        _uiItemSlot.Link(this);
    }

    public void LinkUISlot(UIItemSlot uiItemSlot)
    {
        _uiItemSlot = uiItemSlot;
    }

    public void UnLinkkUISlot()
    {
        _uiItemSlot = null;
    }

    public void EmptySlot()
    {
        _stack = null;
        if (_uiItemSlot != null) _uiItemSlot.UpdateSlot();
    }

    public int Take(int amt)
    {
        int cur;
        if (amt >= _stack.amount)
        {
            cur = _stack.amount;
            EmptySlot();
            return cur;
        } else
        {
            _stack.amount -= amt;
            cur = amt;
        }
        _uiItemSlot.UpdateSlot();
        return cur;

    }

    public ItemStack TakeAll()
    {
        ItemStack handOver = new ItemStack(_stack.id, _stack.amount);
        EmptySlot();
        _uiItemSlot.UpdateSlot();
        return handOver;
    }

    public void InsertStack(ItemStack stack)
    {
        _stack = stack;
        _uiItemSlot.UpdateSlot();
    }
    public bool HasItem
    {
        get
        {
            if (_stack != null) return true;
            return false;
        }
    }
}
