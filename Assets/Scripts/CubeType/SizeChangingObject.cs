using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeChangingObject : MonoBehaviour
{
    [Header("Size Change Settings")]
    [SerializeField] private float minScale = 0.5f; // Minimum scale multiplier
    [SerializeField] private float maxScale = 3f; // Maximum scale multiplier
    [SerializeField] private float scaleTransitionSpeed = 5f; // How fast the size changes
    [SerializeField] private float velocityThreshold = 0.5f; // Minimum velocity to consider "thrown"
    
    [Header("Physics Handling")]
    [SerializeField] private float pushForce = 10f; // Force to push away objects when growing
    [SerializeField] private LayerMask affectedLayers; // Layers that can be pushed
    
    [Header("Audio")]
    [SerializeField] private AudioClip growSound;
    [SerializeField] private AudioClip shrinkSound;
    [SerializeField] private AudioSource audioSource;
    
    private Rigidbody rb;
    private Collider objectCollider;
    private ObjectInteraction playerInteraction;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Vector3 previousScale;
    private bool wasThrown = false;
    private bool isTransitioning = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();
        
        if (rb == null)
        {
            Debug.LogError("SizeChangingObject requires a Rigidbody component!");
            enabled = false;
            return;
        }
        
        if (objectCollider == null)
        {
            Debug.LogError("SizeChangingObject requires a Collider component!");
            enabled = false;
            return;
        }
        
        // Store the original scale
        originalScale = transform.localScale;
        targetScale = originalScale;
        previousScale = originalScale;
        
        // Find the player's ObjectInteraction script
        playerInteraction = FindObjectOfType<ObjectInteraction>();
        
        // Setup audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
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
        
        // Smoothly transition to target scale
        if (isTransitioning)
        {
            previousScale = transform.localScale;
            
            transform.localScale = Vector3.Lerp(
                transform.localScale, 
                targetScale, 
                scaleTransitionSpeed * Time.deltaTime
            );
            
            // Push away overlapping objects when growing
            if (transform.localScale.magnitude > previousScale.magnitude)
            {
                PushAwayOverlappingObjects();
            }
            
            // Check if we've reached the target scale
            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                isTransitioning = false;
            }
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
        
        // Generate a random scale multiplier
        float scaleMultiplier = Random.Range(minScale, maxScale);
        targetScale = originalScale * scaleMultiplier;
        isTransitioning = true;
        
        // Play appropriate sound based on whether we're growing or shrinking
        if (scaleMultiplier > 1f)
        {
            PlaySound(growSound);
            Debug.Log($"Object growing to {scaleMultiplier}x size");
        }
        else
        {
            PlaySound(shrinkSound);
            Debug.Log($"Object shrinking to {scaleMultiplier}x size");
        }
    }
    
    void OnPickedUp()
    {
        // Revert to original size
        targetScale = originalScale;
        isTransitioning = true;
        
        // Reset thrown state
        wasThrown = false;
        
        Debug.Log("Object reverting to original size");
    }
    
    void PushAwayOverlappingObjects()
    {
        // Get all colliders overlapping with this object
        Collider[] overlappingColliders = Physics.OverlapBox(
            transform.position,
            transform.localScale * 0.5f,
            transform.rotation,
            affectedLayers
        );
        
        foreach (Collider col in overlappingColliders)
        {
            // Skip self
            if (col == objectCollider) continue;
            
            Rigidbody otherRb = col.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                // Calculate push direction (away from center)
                Vector3 pushDirection = (col.transform.position - transform.position).normalized;
                
                // Handle edge case where objects are at exact same position
                if (pushDirection.magnitude < 0.1f)
                {
                    pushDirection = Random.onUnitSphere;
                }
                
                // Check if the other object is stuck to this one
                StickyObject stickyObj = col.GetComponent<StickyObject>();
                if (stickyObj != null)
                {
                    // Force unstick by simulating a pickup
                    // This works because StickyObject unsticks when picked up
                    // We'll need to manually unstick it
                    ForceUnstick(col.gameObject);
                }
                
                // Apply push force
                if (!otherRb.isKinematic)
                {
                    otherRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                }
            }
        }
    }
    
    void ForceUnstick(GameObject stickyObj)
    {
        StickyObject sticky = stickyObj.GetComponent<StickyObject>();
        if (sticky != null)
        {
            // Access the sticky object's rigidbody and force it to unstick
            Rigidbody stickyRb = stickyObj.GetComponent<Rigidbody>();
            if (stickyRb != null && stickyRb.isKinematic)
            {
                stickyRb.isKinematic = false;
                stickyRb.useGravity = true;
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Optional: Visual feedback in editor
    void OnDrawGizmos()
    {
        if (Application.isPlaying && wasThrown)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
    
    // Public method to get current scale ratio (useful for other scripts)
    public float GetCurrentScaleRatio()
    {
        return transform.localScale.magnitude / originalScale.magnitude;
    }
    
    // Public method to manually set a specific scale
    public void SetScale(float scaleMultiplier)
    {
        scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScale, maxScale);
        targetScale = originalScale * scaleMultiplier;
        isTransitioning = true;
    }
}

