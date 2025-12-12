using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicatorObject : MonoBehaviour
{
    [Header("Duplication Settings")]
    [SerializeField] private LayerMask duplicatorLayer;
    [SerializeField] private float cloneLifetime = 5f; // How long clones exist
    [SerializeField] private int maxClones = 10; // Maximum clones that can exist
    [SerializeField] private float velocityThreshold = 1f; // Minimum velocity to trigger duplication
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = Vector3.up * 0.5f; // Offset from impact point
    [SerializeField] private bool inheritVelocity = true; // Should clones inherit velocity?
    [SerializeField] private float velocityMultiplier = 0.5f; // Multiplier for inherited velocity
    
    [Header("Clone Visual Settings")]
    [SerializeField] private Material cloneMaterial; // Optional: different material for clones
    [SerializeField] private Color cloneColor = new Color(1f, 1f, 1f, 0.7f); // Clone tint
    
    [Header("Audio")]
    [SerializeField] private AudioClip duplicateSound;
    [SerializeField] private AudioSource audioSource;
    
    private Rigidbody rb;
    private ObjectInteraction playerInteraction;
    private bool canDuplicate = true;
    
    // Object Pool
    private static Queue<GameObject> clonePool = new Queue<GameObject>();
    private static List<GameObject> activeClones = new List<GameObject>();
    private static int totalClones = 0;
    
    // Track if this is a clone
    private bool isClone = false;
    private Coroutine lifetimeCoroutine;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("DuplicatorObject requires a Rigidbody component!");
            enabled = false;
            return;
        }
        
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
        // Check if object was picked up (reset duplication cooldown)
        if (!canDuplicate && IsBeingHeld())
        {
            canDuplicate = true;
        }
    }
    
    bool IsBeingHeld()
    {
        if (playerInteraction == null) return false;
        return playerInteraction.GetHeldObject() == gameObject;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Don't duplicate if we're being held or already duplicated recently
        if (!canDuplicate || IsBeingHeld()) return;
        
        // Check velocity threshold
        if (rb.velocity.magnitude < velocityThreshold) return;
        
        // Check if we hit a duplicator surface
        if (((1 << collision.gameObject.layer) & duplicatorLayer) != 0)
        {
            CreateClone(collision.contacts[0].point, collision.contacts[0].normal);
            canDuplicate = false; // Prevent spam duplication
        }
    }
    
    void CreateClone(Vector3 impactPoint, Vector3 impactNormal)
    {
        // Check if we've reached max clones
        if (totalClones >= maxClones)
        {
            Debug.Log("Max clones reached. Cannot create more.");
            return;
        }
        
        GameObject clone = GetCloneFromPool();
        
        // Position the clone
        clone.transform.position = impactPoint + (impactNormal * spawnOffset.magnitude);
        clone.transform.rotation = transform.rotation;
        clone.transform.localScale = transform.localScale;
        
        // Setup clone properties
        Rigidbody cloneRb = clone.GetComponent<Rigidbody>();
        if (cloneRb != null)
        {
            // Inherit velocity if enabled
            if (inheritVelocity)
            {
                cloneRb.velocity = rb.velocity * velocityMultiplier;
                cloneRb.angularVelocity = rb.angularVelocity * velocityMultiplier;
            }
            else
            {
                cloneRb.velocity = Vector3.zero;
                cloneRb.angularVelocity = Vector3.zero;
            }
        }
        
        // Mark as clone and start lifetime countdown
        DuplicatorObject cloneScript = clone.GetComponent<DuplicatorObject>();
        if (cloneScript != null)
        {
            cloneScript.InitializeAsClone();
        }
        
        // Apply visual changes if specified
        ApplyCloneVisuals(clone);
        
        // Play sound
        PlaySound(duplicateSound);
        
        activeClones.Add(clone);
        totalClones++;
        
        Debug.Log($"Clone created! Active clones: {totalClones}/{maxClones}");
    }
    
    GameObject GetCloneFromPool()
    {
        GameObject clone;
        
        // Try to get from pool
        if (clonePool.Count > 0)
        {
            clone = clonePool.Dequeue();
            clone.SetActive(true);
        }
        else
        {
            // Create new clone
            clone = Instantiate(gameObject);
            clone.name = gameObject.name + " (Clone)";
        }
        
        return clone;
    }
    
    void InitializeAsClone()
    {
        isClone = true;
        canDuplicate = false; // Clones cannot duplicate
        
        // Start lifetime countdown
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        lifetimeCoroutine = StartCoroutine(CloneLifetimeCountdown());
    }
    
    IEnumerator CloneLifetimeCountdown()
    {
        yield return new WaitForSeconds(cloneLifetime);
        
        // Return to pool
        ReturnToPool();
    }
    
    void ReturnToPool()
    {
        if (!isClone) return;
        
        // Remove from active list
        activeClones.Remove(gameObject);
        totalClones--;
        
        // Reset state
        gameObject.SetActive(false);
        isClone = false;
        canDuplicate = true;
        
        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Add back to pool
        clonePool.Enqueue(gameObject);
        
        Debug.Log($"Clone returned to pool. Active clones: {totalClones}/{maxClones}");
    }
    
    void ApplyCloneVisuals(GameObject clone)
    {
        Renderer renderer = clone.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (cloneMaterial != null)
            {
                renderer.material = cloneMaterial;
            }
            else
            {
                // Create a new material instance and tint it
                Material mat = new Material(renderer.material);
                mat.color = cloneColor;
                renderer.material = mat;
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
    
    // Clean up when destroyed
    void OnDestroy()
    {
        if (isClone)
        {
            activeClones.Remove(gameObject);
            totalClones--;
        }
    }
    
    // Optional: Visual feedback
    void OnDrawGizmos()
    {
        if (isClone)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
    
    // Public methods for external control
    public bool IsClone()
    {
        return isClone;
    }
    
    public static int GetActiveCloneCount()
    {
        return totalClones;
    }
    
    public static void ClearAllClones()
    {
        foreach (GameObject clone in new List<GameObject>(activeClones))
        {
            DuplicatorObject script = clone.GetComponent<DuplicatorObject>();
            if (script != null)
            {
                script.ReturnToPool();
            }
        }
    }
}
