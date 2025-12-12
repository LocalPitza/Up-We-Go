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
    [SerializeField] private float moveForce = 1000f;
    [SerializeField] private float maxVelocity = 15f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float dampingFactor = 5f;
    [SerializeField] private float verticalMultiplier = 1.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("References")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask playerLayer;
    
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private float originalDrag;
    private float originalAngularDrag;
    private bool isHoldingObject;
    private int originalLayer;
    
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
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
            
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
        // Pick up or drop object using KeybindManager
        bool pickupPressed = false;
        bool throwPressed = false;
        bool rotateHeld = false;
        float scrollDelta = 0f;
        
        if (KeybindManager.Instance != null)
        {
            pickupPressed = KeybindManager.Instance.GetKeyDown("Pickup");
            throwPressed = KeybindManager.Instance.GetKeyDown("Throw");
            rotateHeld = KeybindManager.Instance.GetKey("RotateObject");
            scrollDelta = KeybindManager.Instance.GetMouseScrollDelta();
        }
        else
        {
            // Fallback to direct input
            pickupPressed = Input.GetMouseButtonDown(0);
            throwPressed = Input.GetMouseButtonDown(1);
            rotateHeld = Input.GetKey(KeyCode.R);
            scrollDelta = Input.mouseScrollDelta.y;
        }
        
        // Pick up or drop
        if (pickupPressed)
        {
            if (!isHoldingObject)
                TryPickupObject();
            else
                DropObject();
        }
        
        // Throw object
        if (throwPressed && isHoldingObject)
        {
            ThrowObject();
        }
        
        // Adjust hold distance with mouse wheel
        if (isHoldingObject && scrollDelta != 0)
        {
            holdDistance = Mathf.Clamp(holdDistance + scrollDelta * 0.5f, 1f, 5f);
            holdPoint.localPosition = Vector3.forward * holdDistance;
        }
        
        // Rotate object
        if (isHoldingObject && rotateHeld)
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
        
        originalDrag = rb.drag;
        originalAngularDrag = rb.angularDrag;
        originalLayer = obj.layer;
        
        rb.drag = dampingFactor;
        rb.angularDrag = dampingFactor;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.None;
        
        obj.layer = LayerMask.NameToLayer("Ignore Raycast");
        
        PlaySound(pickupSound);
    }
    
    void MoveHeldObject()
    {
        Vector3 targetPos = holdPoint.position;
        Vector3 direction = targetPos - heldRigidbody.position;
        float distance = direction.magnitude;
        
        if (distance > 0.01f)
        {
            Vector3 force = direction.normalized * moveForce * distance;
            force.y *= verticalMultiplier;
            
            heldRigidbody.AddForce(force, ForceMode.Force);
            
            if (heldRigidbody.velocity.magnitude > maxVelocity)
            {
                heldRigidbody.velocity = heldRigidbody.velocity.normalized * maxVelocity;
            }
        }
        else
        {
            heldRigidbody.velocity *= 0.9f;
        }
        
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
            heldRigidbody.drag = originalDrag;
            heldRigidbody.angularDrag = originalAngularDrag;
            heldRigidbody.useGravity = true;
            heldObject.layer = originalLayer;
        }
        
        PlaySound(dropSound);
        
        heldObject = null;
        heldRigidbody = null;
        isHoldingObject = false;
    }
    
    void ThrowObject()
    {
        if (heldRigidbody != null)
        {
            heldRigidbody.drag = originalDrag;
            heldRigidbody.angularDrag = originalAngularDrag;
            heldRigidbody.useGravity = true;
            
            Vector3 throwDirection = playerCamera.transform.forward;
            heldRigidbody.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            
            heldObject.layer = originalLayer;
        }
        
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