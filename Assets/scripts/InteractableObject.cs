using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class InteractableObject : MonoBehaviour
{
    public bool playerInRange;

    public string ItemName;
 
    public string GetItemName()
    {
        return ItemName;
    }

    void Update()
    {
        // 当任意制作/背包 UI 打开时，禁止拾取输入
        bool inventoryOpen = InventorySystem.Instance != null && InventorySystem.Instance.isOpen;
        bool craftingOpen = CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen;
        if (inventoryOpen || craftingOpen)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Mouse0) && playerInRange && SelectionManager.Instance.onTarget && SelectionManager.Instance.selectedObject == gameObject)
        {
            if (!InventorySystem.Instance.CheckIfFull())
            {
                InventorySystem.Instance.AddToInventory(ItemName);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventory is full!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}