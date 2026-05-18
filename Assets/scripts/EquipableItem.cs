using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EquipableItem : MonoBehaviour
{

    public Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0) && 
        InventorySystem.Instance.isOpen == false && 
        CraftingSystem.Instance.isOpen == false &&
        SelectionManager.Instance.handIsVisable == false
        )
        {
            animator.SetTrigger("hit");
        }
    }
}
