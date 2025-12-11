using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float holdDistance = 2f;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private LayerMask pickupLayer;
    
    [Header("Physics Settings")]
    [SerializeField] private float moveForce = 1000f; // Force to pull object to hold point
    [SerializeField] private float maxVelocity = 15f; // Max speed object can move
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float dampingFactor = 5f;
    [SerializeField] private float verticalMultiplier = 1.5f; // Extra boost for vertical movement
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("References")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask playerLayer; // Set to player's layer
    
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private float originalDrag;
    private float originalAngularDrag;
    private bool isHoldingObject;
    private int originalLayer;
    
    // Public method to check what object is being held
    public GameObject GetHeldObject()
    {
        return heldObject;
    }
    
    public bool IsHoldingObject()
    {
        return isHoldingObject;
    }
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        // Setup audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
            
        // Create hold point if not assigned
        if (holdPoint == null)
        {
            GameObject hp = new GameObject("HoldPoint");
            holdPoint = hp.transform;
            holdPoint.parent = playerCamera.transform;
            holdPoint.localPosition = Vector3.forward * holdDistance;
        }
    }
    
    void Update()
    {
        // Pick up or drop object
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            if (!isHoldingObject)
                TryPickupObject();
            else
                DropObject();
        }
        
        // Throw object
        if (Input.GetMouseButtonDown(1) && isHoldingObject) // Right click
        {
            ThrowObject();
        }
        
        // Adjust hold distance with mouse wheel
        if (isHoldingObject && Input.mouseScrollDelta.y != 0)
        {
            holdDistance = Mathf.Clamp(holdDistance + Input.mouseScrollDelta.y * 0.5f, 1f, 5f);
            holdPoint.localPosition = Vector3.forward * holdDistance;
        }
        
        // Rotate object with R key
        if (isHoldingObject && Input.GetKey(KeyCode.R))
        {
            heldObject.transform.Rotate(playerCamera.transform.up, 100f * Time.deltaTime, Space.World);
        }
    }
    
    void FixedUpdate()
    {
        if (isHoldingObject && heldRigidbody != null)
        {
            MoveHeldObject();
        }
    }
    
    void TryPickupObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, pickupRange, pickupLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            
            if (rb != null && !rb.isKinematic)
            {
                PickupObject(hit.collider.gameObject, rb);
            }
        }
    }
    
    void PickupObject(GameObject obj, Rigidbody rb)
    {
        heldObject = obj;
        heldRigidbody = rb;
        isHoldingObject = true;
        
        // Store original physics properties
        originalDrag = rb.drag;
        originalAngularDrag = rb.angularDrag;
        originalLayer = obj.layer;
        
        // Modify physics for holding
        rb.drag = dampingFactor;
        rb.angularDrag = dampingFactor;
        rb.useGravity = false;
        
        // Remove velocity constraints to allow full 3D movement
        rb.constraints = RigidbodyConstraints.None;
        
        // Disable collision between object and player by changing layer temporarily
        obj.layer = LayerMask.NameToLayer("Ignore Raycast");
        
        // Play pickup sound
        PlaySound(pickupSound);
    }
    
    void MoveHeldObject()
    {
        // Calculate target position
        Vector3 targetPos = holdPoint.position;
        
        // Calculate direction and distance to target
        Vector3 direction = targetPos - heldRigidbody.position;
        float distance = direction.magnitude;
        
        // Apply force proportional to distance
        if (distance > 0.01f)
        {
            // Boost vertical component for better up/down movement
            Vector3 force = direction.normalized * moveForce * distance;
            force.y *= verticalMultiplier; // Extra vertical force
            
            heldRigidbody.AddForce(force, ForceMode.Force);
            
            // Clamp velocity to prevent object from flying away
            if (heldRigidbody.velocity.magnitude > maxVelocity)
            {
                heldRigidbody.velocity = heldRigidbody.velocity.normalized * maxVelocity;
            }
        }
        else
        {
            // When very close to target, reduce velocity to prevent jitter
            heldRigidbody.velocity *= 0.9f;
        }
        
        // Smooth rotation towards camera forward
        Quaternion targetRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        heldRigidbody.MoveRotation(Quaternion.Slerp(
            heldRigidbody.rotation, 
            targetRotation, 
            rotationSpeed * Time.fixedDeltaTime
        ));
    }
    
    void DropObject()
    {
        if (heldRigidbody != null)
        {
            // Restore original physics properties
            heldRigidbody.drag = originalDrag;
            heldRigidbody.angularDrag = originalAngularDrag;
            heldRigidbody.useGravity = true;
            
            // Restore original layer
            heldObject.layer = originalLayer;
        }
        
        // Play drop sound
        PlaySound(dropSound);
        
        heldObject = null;
        heldRigidbody = null;
        isHoldingObject = false;
    }
    
    void ThrowObject()
    {
        if (heldRigidbody != null)
        {
            // Restore physics properties
            heldRigidbody.drag = originalDrag;
            heldRigidbody.angularDrag = originalAngularDrag;
            heldRigidbody.useGravity = true;
            
            // Apply throw force
            Vector3 throwDirection = playerCamera.transform.forward;
            heldRigidbody.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            
            // Restore original layer
            heldObject.layer = originalLayer;
        }
        
        // Play throw sound
        PlaySound(throwSound);
        
        heldObject = null;
        heldRigidbody = null;
        isHoldingObject = false;
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Optional: Draw gizmos to show pickup range
    void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * pickupRange);
        
        if (holdPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(holdPoint.position, 0.2f);
        }
    }
}