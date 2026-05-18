using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
 
 
public class TrashSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
 
    public GameObject trashAlertUI;
 
    private TMP_Text tmpTextToModify;
    private Text uiTextToModify;
 
    public Sprite trash_closed;
    public Sprite trash_opened;
 
    private Image imageComponent;
 
    Button YesBTN, NoBTN;
 
    GameObject draggedItem
    {
        get
        {
            return DragDrop.itemBeingDragged;
        }
    }
 
    GameObject itemToBeDeleted;
  
 
 
    public string itemName
    {
        get
        {
            if (itemToBeDeleted == null)
            {
                return "item";
            }

            string name = itemToBeDeleted.name;
            string toRemove = "(Clone)";
            string result = name.Replace(toRemove, "");
            return result;
        }
    }
 
 
 
    void Start()
    {
        Transform backgroundTransform = transform.Find("background");
        if (backgroundTransform != null)
        {
            imageComponent = backgroundTransform.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("[TrashSlot] Cannot find child object named 'background'.");
        }

        if (trashAlertUI == null)
        {
            Debug.LogError("[TrashSlot] trashAlertUI is not assigned in Inspector.");
            return;
        }
 
        // Prefer any TMP text inside the alert window. If not found, fallback to legacy Text.
        tmpTextToModify = trashAlertUI.GetComponentInChildren<TMP_Text>(true);
        if (tmpTextToModify == null)
        {
            uiTextToModify = trashAlertUI.GetComponentInChildren<Text>(true);
        }

        if (tmpTextToModify == null && uiTextToModify == null)
        {
            Debug.LogError("[TrashSlot] No TMP_Text/Text found under trashAlertUI.");
        }
 
        Transform yesTransform = trashAlertUI.transform.Find("yes");
        if (yesTransform != null)
        {
            YesBTN = yesTransform.GetComponent<Button>();
        }

        Transform noTransform = trashAlertUI.transform.Find("no");
        if (noTransform != null)
        {
            NoBTN = noTransform.GetComponent<Button>();
        }
 
        // Fallback: if names differ, use first two buttons under the alert window.
        if (YesBTN == null || NoBTN == null)
        {
            Button[] allButtons = trashAlertUI.GetComponentsInChildren<Button>(true);
            if (allButtons.Length >= 2)
            {
                if (YesBTN == null) YesBTN = allButtons[0];
                if (NoBTN == null) NoBTN = allButtons[1];
            }
        }

        if (YesBTN != null)
        {
            YesBTN.onClick.RemoveAllListeners();
            YesBTN.onClick.AddListener(delegate { DeleteItem(); });
        }
        else
        {
            Debug.LogError("[TrashSlot] Cannot find Yes button under trashAlertUI.");
        }

        if (NoBTN != null)
        {
            NoBTN.onClick.RemoveAllListeners();
            NoBTN.onClick.AddListener(delegate { CancelDeletion(); });
        }
        else
        {
            Debug.LogError("[TrashSlot] Cannot find No button under trashAlertUI.");
        }
 
    }
 
 
    public void OnDrop(PointerEventData eventData)
    {
        if (draggedItem == null)
        {
            return;
        }

        InventoryItem inventoryItem = draggedItem.GetComponent<InventoryItem>();
        if (inventoryItem != null && inventoryItem.isTrashable)
        {
            itemToBeDeleted = draggedItem.gameObject;
 
            StartCoroutine(notifyBeforeDeletion());
        }
        
    }
 
    IEnumerator notifyBeforeDeletion()
    {
        if (trashAlertUI == null)
        {
            yield break;
        }

        trashAlertUI.SetActive(true);
        string message = "Throw away this " + itemName + "?";

        if (tmpTextToModify != null)
        {
            tmpTextToModify.text = message;
        }
        else if (uiTextToModify != null)
        {
            uiTextToModify.text = message;
        }
        else
        {
            Debug.LogError("[TrashSlot] Text component not found under trashAlertUI. Add TMP_Text or Text.");
        }

        yield return new WaitForSeconds(1f);
    }
 
    private void CancelDeletion()
    {
        imageComponent.sprite = trash_closed;
        trashAlertUI.SetActive(false);
    }
 
    private void DeleteItem()
    {
        imageComponent.sprite = trash_closed;
        DestroyImmediate(itemToBeDeleted.gameObject);
        InventorySystem.Instance.ReCalculateList();
        CraftingSystem.Instance.RefreshNeededItems();
        trashAlertUI.SetActive(false);
    }
 
    public void OnPointerEnter(PointerEventData eventData)
    {
 
        if (draggedItem != null && draggedItem.GetComponent<InventoryItem>().isTrashable == true)
        {
            imageComponent.sprite = trash_opened;
        }
       
    }
 
    public void OnPointerExit(PointerEventData eventData)
    {
        if (draggedItem != null && draggedItem.GetComponent<InventoryItem>().isTrashable == true)
        {
            imageComponent.sprite = trash_closed;
        }
    }
 
}