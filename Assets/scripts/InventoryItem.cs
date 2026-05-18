using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
 
public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // --- Is this item trashable --- //
    public bool isTrashable;
 
    // --- Item Info UI --- //
    private GameObject itemInfoUI;
 
    private TMP_Text itemInfoUI_itemName;
    private TMP_Text itemInfoUI_itemDescription;
    private TMP_Text itemInfoUI_itemFunctionality;
 
    public string thisName, thisDescription, thisFunctionality;
 
    // --- Consumption --- //
    private GameObject itemPendingConsumption;
    public bool isConsumable;
 
    public float healthEffect;
    public float caloriesEffect;
    public float hydrationEffect;

    // --- Equipment --- //
    public bool isEquippable;
    private GameObject itemPendingEquipment;
    public bool isInsideQuickSlot;
 
    public bool isSelected;
 
    private void Start()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("InventorySystem.Instance is null. Make sure an InventorySystem exists in the scene.");
            return;
        }

        itemInfoUI = InventorySystem.Instance.ItemInfoUi;
        if (itemInfoUI == null)
        {
            Debug.LogError("InventorySystem.ItemInfoUi is not assigned in the Inspector.");
            return;
        }

        Transform itemNameTransform = itemInfoUI.transform.Find("itemName");
        Transform itemDescriptionTransform = itemInfoUI.transform.Find("itemDescription");
        Transform itemFunctionalityTransform = itemInfoUI.transform.Find("itemFunctionality");

        if (itemNameTransform == null || itemDescriptionTransform == null || itemFunctionalityTransform == null)
        {
            Debug.LogError("ItemInfoUi is missing one of these children: itemName, itemDescription, itemFunctionality.");
            return;
        }

        itemInfoUI_itemName = itemNameTransform.GetComponent<TMP_Text>();
        itemInfoUI_itemDescription = itemDescriptionTransform.GetComponent<TMP_Text>();
        itemInfoUI_itemFunctionality = itemFunctionalityTransform.GetComponent<TMP_Text>();
    }

    void Update()
    {
        if(isSelected)
        {
            gameObject.GetComponent<DragDrop>().enabled = false;
        }
        else
        {
            gameObject.GetComponent<DragDrop>().enabled = true;
        }
    }
 
    // Triggered when the mouse enters into the area of the item that has this script.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInfoUI == null || itemInfoUI_itemName == null || itemInfoUI_itemDescription == null || itemInfoUI_itemFunctionality == null)
        {
            return;
        }

        itemInfoUI.SetActive(true);
        itemInfoUI_itemName.text = thisName;
        itemInfoUI_itemDescription.text = thisDescription;
        itemInfoUI_itemFunctionality.text = thisFunctionality;
    }
 
    // Triggered when the mouse exits the area of the item that has this script.
    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemInfoUI == null)
        {
            return;
        }

        itemInfoUI.SetActive(false);
    }
 
    // Triggered when the mouse is clicked over the item that has this script.
    public void OnPointerDown(PointerEventData eventData)
    {
        //Right Mouse Button Click on
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isConsumable)
            {
                // Setting this specific gameobject to be the item we want to destroy later
                itemPendingConsumption = gameObject;
                consumingFunction(healthEffect, caloriesEffect, hydrationEffect);
            }

            // Right Mouse Button Click on Equippable Item
            if (isEquippable && isInsideQuickSlot == false && EquipSystem.Instance.CheckIfFull() == false)
            {
                EquipSystem.Instance.AddToQuickSlots(gameObject);
                isInsideQuickSlot = true;
            }
        }
    }
 
    // Triggered when the mouse button is released over the item that has this script.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isConsumable && itemPendingConsumption == gameObject)
            {
                DestroyImmediate(gameObject);
                InventorySystem.Instance.ReCalculateList();
                CraftingSystem.Instance.RefreshNeededItems();
            }
        }
    }
 
    private void consumingFunction(float healthEffect, float caloriesEffect, float hydrationEffect)
    {
        itemInfoUI.SetActive(false);
 
        healthEffectCalculation(healthEffect);
 
        caloriesEffectCalculation(caloriesEffect);
 
        hydrationEffectCalculation(hydrationEffect);
 
    }
 
 
    private static void healthEffectCalculation(float healthEffect)
    {
        // --- Health --- //
 
        float healthBeforeConsumption = PlayerState.Instance.currentHealth;
        float maxHealth = PlayerState.Instance.maxHealth;
 
        if (healthEffect != 0)
        {
            if ((healthBeforeConsumption + healthEffect) > maxHealth)
            {
                PlayerState.Instance.currentHealth = maxHealth;
            }
            else
            {
                PlayerState.Instance.currentHealth = healthBeforeConsumption + healthEffect;
            }
        }
    }
 
 
    private static void caloriesEffectCalculation(float caloriesEffect)
    {
        // --- Calories --- //
 
        float caloriesBeforeConsumption = PlayerState.Instance.currentCalories;
        float maxCalories = PlayerState.Instance.maxCalories;
 
        if (caloriesEffect != 0)
        {
            if ((caloriesBeforeConsumption + caloriesEffect) > maxCalories)
            {
                PlayerState.Instance.currentCalories = maxCalories;
            }
            else
            {
                PlayerState.Instance.currentCalories = caloriesBeforeConsumption + caloriesEffect;
            }
        }
    }
 
 
    private static void hydrationEffectCalculation(float hydrationEffect)
    {
        // --- Hydration --- //
 
        float hydrationBeforeConsumption = PlayerState.Instance.currentHydrationPercent;
        float maxHydration = PlayerState.Instance.maxHydrationPercent;
 
        if (hydrationEffect != 0)
        {
            if ((hydrationBeforeConsumption + hydrationEffect) > maxHydration)
            {
                PlayerState.Instance.currentHydrationPercent = maxHydration;
            }
            else
            {
                PlayerState.Instance.currentHydrationPercent = hydrationBeforeConsumption + hydrationEffect;
            }
        }
    }
 
 
}