using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float airAcceleration = 2f;
    [SerializeField] private float friction = 6f;
    [SerializeField] private float airControl = 0.3f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = 20f;
    
    [Header("Crouch Settings")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    
    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip crouchSound;
    [SerializeField] private AudioClip uncrouchSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private float footstepInterval = 0.5f;
    [SerializeField] private float crouchFootstepInterval = 0.7f;
    [SerializeField] private float footstepVolume = 0.5f;
    [SerializeField] private float jumpVolume = 0.6f;
    [SerializeField] private float landVolume = 0.8f;
    
    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobSmoothing = 8f;
    
    // Components
    private CharacterController controller;
    
    // Movement state
    private Vector3 velocity;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isCrouching;
    private float currentHeight;
    private bool wasCrouching;
    private bool wasGrounded;
    
    // Look
    private float verticalRotation = 0f;
    
    // Audio
    private float footstepTimer = 0f;
    private int currentFootstepIndex = 0;
    
    // Head bob
    private float bobTimer = 0f;
    private Vector3 cameraStartPos;
    
    // Inventory (simple string list)
    private List<string> inventory = new List<string>();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentHeight = standHeight;
        wasCrouching = false;
        wasGrounded = true;
        
        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.spatialBlend = 0f; // 2D sound for player
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Setup camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        // Store initial camera position for head bob
        cameraStartPos = cameraTransform.localPosition;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCrouch();
        HandleMovement();
        HandleHeadBob();
        HandleInteraction();
        
        // Debug: Show inventory on I key
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowInventory();
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleCrouch()
    {
        bool crouchInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        
        if (crouchInput)
        {
            if (!isCrouching)
            {
                // Just started crouching
                PlayCrouchSound();
            }
            isCrouching = true;
        }
        else if (isCrouching)
        {
            // Check if there's room to stand up
            if (CanStandUp())
            {
                isCrouching = false;
                PlayUncrouchSound();
            }
        }
        
        wasCrouching = isCrouching;
        
        // Smoothly transition height
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.height = currentHeight;
        
        // Update camera start position for head bob based on crouch state
        cameraStartPos.y = currentHeight * 0.9f - standHeight * 0.5f;
    }

    bool CanStandUp()
    {
        // Raycast upward to check for obstacles
        float checkDistance = standHeight - crouchHeight + 0.2f;
        Vector3 start = transform.position + Vector3.up * crouchHeight;
        
        return !Physics.Raycast(start, Vector3.up, checkDistance);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        
        // Detect landing
        if (isGrounded && !wasGrounded)
        {
            PlayLandSound();
        }
        
        wasGrounded = isGrounded;
        
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Calculate wish direction
        Vector3 inputDirection = transform.right * horizontal + transform.forward * vertical;
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);
        
        // Get current speed
        float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
        Vector3 wishDirection = inputDirection * currentSpeed;
        
        if (isGrounded)
        {
            // Ground movement with acceleration and friction
            GroundMove(wishDirection);
            
            // Jump
            if (Input.GetButtonDown("Jump") && !isCrouching)
            {
                velocity.y = jumpForce;
                PlayJumpSound();
            }
            else if (velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }
        }
        else
        {
            // Air movement with limited control
            AirMove(wishDirection);
            
            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;
        }
        
        // Move the character
        controller.Move(velocity * Time.deltaTime);
        
        // Handle footstep sounds
        HandleFootsteps();
    }

    void GroundMove(Vector3 wishDirection)
    {
        // Apply friction
        float currentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        
        if (currentSpeed > 0.1f)
        {
            float drop = currentSpeed * friction * Time.deltaTime;
            float newSpeed = Mathf.Max(currentSpeed - drop, 0f);
            if (currentSpeed > 0)
            {
                velocity *= newSpeed / currentSpeed;
            }
        }
        
        // Accelerate
        Accelerate(wishDirection, acceleration);
    }

    void AirMove(Vector3 wishDirection)
    {
        // Limited air control
        Accelerate(wishDirection, airAcceleration * airControl);
    }

    void Accelerate(Vector3 wishDirection, float accel)
    {
        float wishSpeed = wishDirection.magnitude;
        wishDirection.Normalize();
        
        float currentSpeed = Vector3.Dot(velocity, wishDirection);
        float addSpeed = wishSpeed - currentSpeed;
        
        if (addSpeed <= 0) return;
        
        float accelSpeed = accel * wishSpeed * Time.deltaTime;
        accelSpeed = Mathf.Min(accelSpeed, addSpeed);
        
        velocity += wishDirection * accelSpeed;
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(this);
                }
            }
        }
    }

    void HandleFootsteps()
    {
        if (!isGrounded) return;
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        
        // Check if player is moving
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        
        if (horizontalSpeed > 0.1f)
        {
            // Get the appropriate interval based on crouch state
            float interval = isCrouching ? crouchFootstepInterval : footstepInterval;
            
            footstepTimer += Time.deltaTime;
            
            if (footstepTimer >= interval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void HandleHeadBob()
    {
        if (!enableHeadBob || !isGrounded)
        {
            // Smoothly return to start position when not moving or in air
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, 
                cameraStartPos, 
                Time.deltaTime * bobSmoothing
            );
            return;
        }
        
        // Check if player is moving
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        
        if (horizontalSpeed > 0.1f)
        {
            // Increment bob timer based on speed
            bobTimer += Time.deltaTime * bobFrequency;
            
            // Calculate bob offset
            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmplitude;
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmplitude * 0.5f; // Subtle horizontal sway
            
            // Reduce bob amplitude when crouching
            float crouchMultiplier = isCrouching ? 0.5f : 1f;
            bobOffsetY *= crouchMultiplier;
            bobOffsetX *= crouchMultiplier;
            
            // Apply bob to camera
            Vector3 targetPos = cameraStartPos + new Vector3(bobOffsetX, bobOffsetY, 0f);
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, 
                targetPos, 
                Time.deltaTime * bobSmoothing
            );
        }
        else
        {
            // Smoothly return to start position when not moving
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, 
                cameraStartPos, 
                Time.deltaTime * bobSmoothing
            );
            bobTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;
        
        // Play the current footstep sound in sequence
        AudioClip clip = footstepSounds[currentFootstepIndex];
        audioSource.PlayOneShot(clip, footstepVolume);
        
        // Move to next sound in the array
        currentFootstepIndex = (currentFootstepIndex + 1) % footstepSounds.Length;
    }

    void PlayCrouchSound()
    {
        if (crouchSound != null)
        {
            audioSource.PlayOneShot(crouchSound, 0.6f);
        }
    }

    void PlayUncrouchSound()
    {
        if (uncrouchSound != null)
        {
            audioSource.PlayOneShot(uncrouchSound, 0.6f);
        }
    }

    void PlayJumpSound()
    {
        if (jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound, jumpVolume);
        }
    }

    void PlayLandSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        
        // Use a random footstep sound for landing (keep it varied)
        int index = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[index];
        audioSource.PlayOneShot(clip, landVolume);
    }

    // Inventory methods
    public void AddItem(string itemName)
    {
        inventory.Add(itemName);
        Debug.Log($"Picked up: {itemName}");
    }

    public bool HasItem(string itemName)
    {
        return inventory.Contains(itemName);
    }

    public bool RemoveItem(string itemName)
    {
        return inventory.Remove(itemName);
    }

    public void ShowInventory()
    {
        Debug.Log("=== INVENTORY ===");
        if (inventory.Count == 0)
        {
            Debug.Log("Empty");
        }
        else
        {
            foreach (string item in inventory)
            {
                Debug.Log($"- {item}");
            }
        }
    }
}