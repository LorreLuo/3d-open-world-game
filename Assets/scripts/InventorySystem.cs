using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

 
public class InventorySystem : MonoBehaviour
{
    
    public GameObject ItemInfoUi;
    public static InventorySystem Instance { get; set; }
 
    public GameObject inventoryScreenUI;

    public List<GameObject> slotList = new List<GameObject>();

    public List<string> itemList = new List<string>();

    private GameObject itemToAdd;

    private GameObject whatSlotToEquip;

    //public bool isFull;

    public bool isOpen;
 
    //
    public GameObject pickupAlert;
    public TMP_Text pickupName;
    public Image pickupImage;
    [SerializeField] private float pickupAlertDuration = 2f;
    private float _pickupAlertHideAt = -1f;
 
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
 
 
    void Start()
    {
        isOpen = false;
        //isFull = false; 

        PopulateSlotList();
        
        Cursor.visible = false;
    }
 
    private void PopulateSlotList()
    {
        foreach(Transform child in inventoryScreenUI.transform)
        {
            if(child.CompareTag("Slot"))
            {
                slotList.Add(child.gameObject);
            }
        }
    }
 
    void Update()
    {
        if (pickupAlert != null && pickupAlert.activeSelf && _pickupAlertHideAt > 0f && Time.unscaledTime >= _pickupAlertHideAt)
        {
            pickupAlert.SetActive(false);
            _pickupAlertHideAt = -1f;
        }
 
        if (Input.GetKeyDown(KeyCode.I) && !isOpen)
        {
 
		    Debug.Log("i is pressed");
            inventoryScreenUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            isOpen = true;

            //显示系统鼠标光标
            Cursor.visible = true;
            //禁用场景交互射线与提示 UI
            SelectionManager.Instance.DisableSelection();
            SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;
 
        }
        else if (Input.GetKeyDown(KeyCode.I) && isOpen)
        {
            inventoryScreenUI.SetActive(false);

            if(!CraftingSystem.Instance.isOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;

                //隐藏系统鼠标光标
                Cursor.visible = false;
                //启用场景交互射线与提示 UI
                SelectionManager.Instance.EnableSelection();
                SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
                
            }
            
            isOpen = false;
        }
    }

    public void AddToInventory(string itemName)
    {
        if(CheckIfFull())
        {
            Debug.Log("Inventory is full!");
        }
        else
        {
            whatSlotToEquip = FindNextEmptySlot();
            GameObject itemPrefab = Resources.Load<GameObject>(itemName);
            if (itemPrefab == null)
            {
                Debug.LogError($"Item prefab '{itemName}' was not found in a Resources folder.");
                return;
            }

            itemToAdd = Instantiate(itemPrefab, whatSlotToEquip.transform);
            itemToAdd.transform.localPosition = Vector3.zero;
            itemToAdd.transform.localScale = Vector3.one;

            TriggerPickupAlert(itemName,itemToAdd.GetComponent<Image>().sprite);
            
            ReCalculateList();
            CraftingSystem.Instance.RefreshNeededItems();
        }
    }

    void TriggerPickupAlert(string itemName,Sprite itemSprite)
    {
        pickupAlert.SetActive(true);
        pickupName.text = itemName;
        pickupImage.sprite = itemSprite;
        _pickupAlertHideAt = Time.unscaledTime + pickupAlertDuration;
    }

    private GameObject FindNextEmptySlot()
    {
        foreach(GameObject slot in slotList)
        {
            if(slot.transform.childCount == 0)
            {
                return slot;
            }
        }

        return new GameObject();
    }

    public bool CheckIfFull()
    {
        int counter = 0;

        foreach(GameObject slot in slotList)
        {
            if(slot.transform.childCount > 0)
            {
                counter += 1;
            }  
        }

        if(counter == 18)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    

    public void RemoveFromInventory(string nameToRemove,int amountToRemove)
    {
        int counter = amountToRemove;

        for(var i = slotList.Count - 1; i >= 0; i--)
        {
            if(slotList[i].transform.childCount > 0)
            {
                if(slotList[i].transform.GetChild(0).name == nameToRemove + "(Clone)" && counter != 0)
                {
                    DestroyImmediate(slotList[i].transform.GetChild(0).gameObject);

                    counter -= 1;
                }
            }
        }
        
        ReCalculateList();
        CraftingSystem.Instance.RefreshNeededItems();
    }

    public void ReCalculateList()
    {
        itemList.Clear();

        foreach(GameObject slot in slotList)
        {
            if(slot.transform.childCount > 0)
            {
                string name = slot.transform.GetChild(0).name;
                string str2 = "(Clone)";
                string result = name.Replace(str2,"");

                itemList.Add(result);
            }
        }
    }

    
    
}
