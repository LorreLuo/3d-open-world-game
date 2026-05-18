using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; set; }

    // ---- Player Health ----
    public float currentHealth;
    public float maxHealth;

    // ---- Player Hydration ----
    public float currentHydrationPercent;
    public float maxHydrationPercent;

    public bool isHydrationActive;

    // ---- Player Calories ----
    public float currentCalories;
    public float maxCalories;

    float distanceTravelled = 0;
    Vector3 lastPosition;

    public GameObject playerBody;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        currentHealth = maxHealth;
        currentCalories = maxCalories;
        currentHydrationPercent = maxHydrationPercent;

        StartCoroutine(decreaseHydration());
    }

    IEnumerator decreaseHydration()
    {
        while(true)
        {
            currentHydrationPercent -= 1;
            yield return new WaitForSeconds(10);
        }
    }

    // Update is called once per frame
    void Update()
    {
        distanceTravelled += Vector3.Distance(lastPosition, playerBody.transform.position);
        lastPosition = playerBody.transform.position;

        if(distanceTravelled >= 10)
        {
            distanceTravelled = 0;
            currentCalories -= 1;
        }



        if(Input.GetKeyDown(KeyCode.H))
        {
            currentHealth -= 10;
        }
    }

    public void setHealth(float newHealth)
    {
        currentHealth = newHealth;
    }

    public void setHydration(float newHydration)
    {
        currentHydrationPercent = newHydration;
    }

    public void setCalories(float newCalories)
    {
        currentCalories = newCalories;
    }
}
