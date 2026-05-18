using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
public class EquipSystem : MonoBehaviour
{
    public static EquipSystem Instance { get; set; }
 
    // -- UI -- //
    public GameObject quickSlotsPanel;
 
    public List<GameObject> quickSlotsList = new List<GameObject>();
    //public List<string> itemList = new List<string>();

    public GameObject numbersHolder;

    public int selectedNumber = -1;
    //public GameObject selectedSlot;
    public GameObject selectedItem;
   
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
 
 
    private void Start()
    {
        PopulateSlotList();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectQuickSlot(1);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectQuickSlot(2);

        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectQuickSlot(3);

        }
        else if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectQuickSlot(4);

        }
        else if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectQuickSlot(5);

        }
        else if(Input.GetKeyDown(KeyCode.Alpha6))
        {
            SelectQuickSlot(6);

        }
        else if(Input.GetKeyDown(KeyCode.Alpha7))
        {
            SelectQuickSlot(7);

        }
      
    }

    //选择快捷栏
    public void SelectQuickSlot(int number)
    {
        if(CheckIfSlotIsFull(number) == true)
        {
            if(selectedNumber != number)
            {
                selectedNumber = number;

                //不选择之前选择过的物品
                if(selectedItem != null)
                {
                    selectedItem.GetComponent<InventoryItem>().isSelected = false;
                }
                //选择物品
                selectedItem = getSelectedItem(number);
                selectedItem.GetComponent<InventoryItem>().isSelected = true;

                //修改颜色
                SetAllNumberColors(Color.gray);
                SetNumberColor(number, Color.white);
            }
            else //我们要选择同一个物品
            {
                selectedNumber = -1;

                //不选择之前选择过的物品
                if(selectedItem != null)
                {
                    selectedItem.GetComponent<InventoryItem>().isSelected = false;
                    selectedItem = null;
                }

                //修改颜色
                SetAllNumberColors(Color.gray);
            }
            
        }
        
    }

    private void SetAllNumberColors(Color color)
    {
        foreach (Transform child in numbersHolder.transform)
        {
            TextMeshProUGUI tmp = child.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.color = color;
            else
                Debug.LogWarning($"[EquipSystem] numbersHolder child '{child.name}' has no TextMeshProUGUI in children.");
        }
    }

    private void SetNumberColor(int number, Color color)
    {
        Transform numberObj = numbersHolder.transform.Find("number" + number);
        if (numberObj == null)
        {
            Debug.LogWarning($"[EquipSystem] Could not find child 'number{number}' in numbersHolder. Children are: " +
                string.Join(", ", System.Linq.Enumerable.Select(
                    System.Linq.Enumerable.Range(0, numbersHolder.transform.childCount),
                    i => numbersHolder.transform.GetChild(i).name)));
            return;
        }
        TextMeshProUGUI tmp = numberObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.color = color;
    }

    GameObject getSelectedItem(int slotNumber)
    {
        return quickSlotsList[slotNumber-1].transform.GetChild(0).gameObject;
    }

    //检查快捷栏是否已满
    bool CheckIfSlotIsFull(int slotNumber)
    {
        if(quickSlotsList[slotNumber-1].transform.childCount > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
 
    // 填充快捷栏列表
    private void PopulateSlotList()
    {
        foreach (Transform child in quickSlotsPanel.transform)
        {
            if (child.CompareTag("QuickSlot"))
            {
                quickSlotsList.Add(child.gameObject);
            }
        }
    }
 
    public void AddToQuickSlots(GameObject itemToEquip)
    {
        // Find next free slot
        GameObject availableSlot = FindNextEmptySlot();
        // Set transform of our object
        itemToEquip.transform.SetParent(availableSlot.transform, false);
        // Getting clean name
        string cleanName = itemToEquip.name.Replace("(Clone)", "");
       
 
    }
 
 
    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return new GameObject();
    }
 
    public bool CheckIfFull()
    {
 
        int counter = 0;
 
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount > 0)
            {
                counter += 1;
            }
        }
 
        //快捷栏最多只能装备7个物品
        if (counter == 7)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}