using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyObject : MonoBehaviour
{
    [Header("Stick Settings")]
    [SerializeField] private float velocityThreshold = 1f; // Minimum velocity to stick on impact
    [SerializeField] private LayerMask stickableLayer; // Set to "Interaction" layer
    [SerializeField] private float stickDelay = 0.1f; // Small delay before sticking to avoid immediate stick
    
    [Header("Audio")]
    [SerializeField] private AudioClip stickSound;
    [SerializeField] private AudioSource audioSource;
    
    private Rigidbody rb;
    private ObjectInteraction playerInteraction;
    private bool isStuck = false;
    private bool canStick = false;
    private GameObject stuckToObject;
    private Vector3 localStickPosition;
    private Quaternion localStickRotation;
    private Rigidbody stuckToRigidbody;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("StickyObject requires a Rigidbody component!");
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
        // Check if object was just thrown/released
        if (!canStick && !IsBeingHeld() && rb.velocity.magnitude > velocityThreshold)
        {
            StartCoroutine(EnableStickingAfterDelay());
        }
        
        // Check if object was picked up again
        if (isStuck && IsBeingHeld())
        {
            Unstick();
        }
    }
    
    void FixedUpdate()
    {
        // Update position if stuck to a moving object
        if (isStuck && stuckToObject != null)
        {
            UpdateStuckPosition();
        }
    }
    
    bool IsBeingHeld()
    {
        if (playerInteraction == null) return false;
        return playerInteraction.GetHeldObject() == gameObject;
    }
    
    IEnumerator EnableStickingAfterDelay()
    {
        yield return new WaitForSeconds(stickDelay);
        canStick = true;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Only stick if we're moving fast enough and allowed to stick
        if (!canStick || isStuck || IsBeingHeld()) return;
        
        // Check if we hit an object on the stickable layer
        if (((1 << collision.gameObject.layer) & stickableLayer) != 0)
        {
            // Don't stick to yourself
            if (collision.gameObject == gameObject) return;
            
            StickToObject(collision.gameObject, collision.contacts[0].point);
        }
    }
    
    void StickToObject(GameObject target, Vector3 contactPoint)
    {
        isStuck = true;
        canStick = false;
        stuckToObject = target;
        stuckToRigidbody = target.GetComponent<Rigidbody>();
        
        // Calculate local position and rotation relative to target
        localStickPosition = target.transform.InverseTransformPoint(transform.position);
        localStickRotation = Quaternion.Inverse(target.transform.rotation) * transform.rotation;
        
        // Stop all physics BEFORE making kinematic
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Now make it kinematic
        rb.isKinematic = true;
        
        // Play stick sound
        PlaySound(stickSound);
        
        Debug.Log($"Stuck to {target.name}");
    }
    
    void UpdateStuckPosition()
    {
        // Update position and rotation to follow the stuck object
        // Use MovePosition and MoveRotation for kinematic rigidbodies
        Vector3 newPosition = stuckToObject.transform.TransformPoint(localStickPosition);
        Quaternion newRotation = stuckToObject.transform.rotation * localStickRotation;
        
        rb.MovePosition(newPosition);
        rb.MoveRotation(newRotation);
    }
    
    void Unstick()
    {
        if (!isStuck) return;
        
        isStuck = false;
        canStick = false;
        
        // Re-enable physics
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.drag = 0.5f; // Reset to reasonable default
        rb.angularDrag = 0.05f;
        
        // If we were stuck to a moving object, inherit its velocity
        if (stuckToRigidbody != null)
        {
            rb.velocity = stuckToRigidbody.velocity;
        }
        
        stuckToObject = null;
        stuckToRigidbody = null;
        
        Debug.Log("Unstuck from object");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Optional: Visual feedback
    void OnDrawGizmos()
    {
        if (isStuck && stuckToObject != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, stuckToObject.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}