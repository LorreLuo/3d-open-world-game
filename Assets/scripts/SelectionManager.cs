using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; set; }

    public bool onTarget;

    public GameObject selectedObject;

    public GameObject interaction_Info_UI;
    Text interaction_text;

    public Image centerDotImage;
    public Image handIcon;

    public bool handIsVisable;

    private void Start()
    {
        onTarget = false;

        interaction_text = interaction_Info_UI.GetComponent<Text>();
    }

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

    void Update()
    {
        // 只要背包或合成界面打开，就屏蔽场景交互射线与提示 UI
        bool inventoryOpen = InventorySystem.Instance != null && InventorySystem.Instance.isOpen;
        bool craftingOpen = CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen;
        if (inventoryOpen || craftingOpen)
        {
            // 重置当前可交互目标，避免 UI 打开时仍可拾取场景物体
            onTarget = false;
            selectedObject = null;
            interaction_Info_UI.SetActive(false);
            centerDotImage.gameObject.SetActive(true);
            handIcon.gameObject.SetActive(false);               
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            var selectionTransform = hit.transform;

            InteractableObject interactable = selectionTransform.GetComponent<InteractableObject>();

            if (interactable && interactable.playerInRange)
            {
                onTarget = true;
                selectedObject = interactable.gameObject;
                interaction_text.text = interactable.GetItemName();
                interaction_Info_UI.SetActive(true);

                if(interactable.CompareTag("pickable"))
                {
                    centerDotImage.gameObject.SetActive(false);
                    handIcon.gameObject.SetActive(true);

                    handIsVisable = true;
                }
                else
                {
                    centerDotImage.gameObject.SetActive(true);
                    handIcon.gameObject.SetActive(false);
                    
                    handIsVisable = false;
                }

            }
            else
            {
                onTarget = false;

                interaction_Info_UI.SetActive(false);

                centerDotImage.gameObject.SetActive(true);
                handIcon.gameObject.SetActive(false);

                handIsVisable = false;
            }
        }
        else
        {
            onTarget = false;

            interaction_Info_UI.SetActive(false);

            centerDotImage.gameObject.SetActive(true);
            handIcon.gameObject.SetActive(false);

            handIsVisable = false;
        }
    }

    public void DisableSelection()
    {
        handIcon.enabled = false;
        centerDotImage.enabled = false;
        interaction_Info_UI.SetActive(false);

        selectedObject = null;
    }

    public void EnableSelection()
    {
        handIcon.enabled = true;
        centerDotImage.enabled = true;
        interaction_Info_UI.SetActive(true);
    }
}