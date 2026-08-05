using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    None,
    Equipment,
    Consumable,
    Material,
}
public abstract class ItemData : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite itemIcon;
    public int itemMaxStack;
    public int itemPrice;
    public ItemType itemType;
}
