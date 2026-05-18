using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class CraftingSystem : MonoBehaviour
{
    public GameObject craftingScreenUI;
    public GameObject toolsScreenUI;
    public List<string>inventoryItemList = new List<string>();

    //tool Button
    Button toolsBTN;
    //Craft Button
    Button craftAxeBTN;

    //Required Items Text
    TMP_Text AxeReq1,AxeReq2;

    public bool isOpen;

    //道具蓝图
    public Blueprint AxeBLP = new Blueprint("Axe",2,"Stone",3,"Stick",3);


    public static CraftingSystem Instance { get; set; }

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

        toolsBTN = craftingScreenUI.transform.Find("ToolsButton").GetComponent<Button>();
        toolsBTN.onClick.AddListener(delegate { OpenToolsCategory(); });

        //Axe
        AxeReq1 = toolsScreenUI.transform.Find("Axe").transform.Find("req1").GetComponent<TMP_Text>();
        AxeReq2 = toolsScreenUI.transform.Find("Axe").transform.Find("req2").GetComponent<TMP_Text>();

        craftAxeBTN = toolsScreenUI.transform.Find("Axe").transform.Find("Button").GetComponent<Button>();
        craftAxeBTN.onClick.AddListener(delegate { CraftAnyItem(AxeBLP); });
    }

    void OpenToolsCategory()
    {
        craftingScreenUI.SetActive(false);
        toolsScreenUI.SetActive(true);
    }

    void CraftAnyItem(Blueprint blueprintToCraft)
    {
        //把item放入到物品栏中
        InventorySystem.Instance.AddToInventory(blueprintToCraft.itemName);

        if(blueprintToCraft.numOfRequirements == 1)
        {
            //从物品栏中移除资源
            InventorySystem.Instance.RemoveFromInventory(blueprintToCraft.Req1,blueprintToCraft.Req1amount);
        }
        else if(blueprintToCraft.numOfRequirements == 2)
        {
            InventorySystem.Instance.RemoveFromInventory(blueprintToCraft.Req1,blueprintToCraft.Req1amount);
            InventorySystem.Instance.RemoveFromInventory(blueprintToCraft.Req2,blueprintToCraft.Req2amount);
        }

        StartCoroutine(calculate());

        //RefreshNeededItems();
    }

    public IEnumerator calculate()
    {
        //yield return new WaitForSeconds(1f);

        //InventorySystem.Instance.ReCalculateList();

        yield return 0;
        InventorySystem.Instance.ReCalculateList();
        RefreshNeededItems();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.C) && !isOpen)
        {
            craftingScreenUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            //禁用场景交互射线与提示 UI
            SelectionManager.Instance.DisableSelection();
            SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;

            isOpen = true;
 
        }
        else if (Input.GetKeyDown(KeyCode.C) && isOpen)
        {
            craftingScreenUI.SetActive(false);
            toolsScreenUI.SetActive(false);

            if(!InventorySystem.Instance.isOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                //启用场景交互射线与提示 UI
                SelectionManager.Instance.EnableSelection();
                SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
            }
            
            isOpen = false;
        }
    }

    public void RefreshNeededItems()
    {
        if (InventorySystem.Instance == null) return;

        int stone_count = 0;
        int stick_count = 0;
        
        inventoryItemList = InventorySystem.Instance.itemList;

        foreach(string item in inventoryItemList)
        {
            switch (item)
            {
                case "Stone":
                    stone_count += 1;
                    break;
                case "Stick":
                    stick_count += 1;
                    break;
            }
        }

        //----AXE----//
        if (AxeReq1 != null)
            AxeReq1.text = "3 Stone [" + stone_count + "]";
        if (AxeReq2 != null)
            AxeReq2.text = "3 Stick [" + stick_count + "]";

        if (craftAxeBTN != null)
            craftAxeBTN.gameObject.SetActive(stone_count >= 3 && stick_count >= 3);
    }

    



}
