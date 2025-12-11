using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Float Settings")]
    [SerializeField] private float timeBeforeFloat = 2f; // Time after being thrown before gravity disables
    [SerializeField] private float velocityThreshold = 0.5f; // Minimum velocity to consider "thrown"
    
    private Rigidbody rb;
    private ObjectInteraction playerInteraction;
    private bool isFloating = false;
    private bool wasThrown = false;
    private Coroutine floatCoroutine;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("FloatingObject requires a Rigidbody component!");
            enabled = false;
            return;
        }
        
        // Find the player's ObjectInteraction script
        playerInteraction = FindObjectOfType<ObjectInteraction>();
    }
    
    void Update()
    {
        // Check if this object was just thrown
        if (!wasThrown && !IsBeingHeld() && rb.velocity.magnitude > velocityThreshold)
        {
            OnThrown();
        }
        
        // Check if object was picked up again
        if (wasThrown && IsBeingHeld())
        {
            OnPickedUp();
        }
    }
    
    bool IsBeingHeld()
    {
        if (playerInteraction == null) return false;
        return playerInteraction.GetHeldObject() == gameObject;
    }
    
    void OnThrown()
    {
        wasThrown = true;
        
        // Cancel any existing coroutine
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
        }
        
        // Start countdown to disable gravity
        floatCoroutine = StartCoroutine(DisableGravityAfterDelay());
    }
    
    void OnPickedUp()
    {
        // Re-enable gravity when picked up
        if (rb != null)
        {
            rb.useGravity = true;
        }
        
        // Cancel floating countdown if active
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }
        
        // Reset states
        wasThrown = false;
        isFloating = false;
    }
    
    IEnumerator DisableGravityAfterDelay()
    {
        yield return new WaitForSeconds(timeBeforeFloat);
        
        // Disable gravity and stop movement
        if (rb != null && !IsBeingHeld())
        {
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            isFloating = true;
        }
        
        floatCoroutine = null;
    }
    
    // Optional: Visual feedback
    void OnDrawGizmos()
    {
        if (isFloating)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
