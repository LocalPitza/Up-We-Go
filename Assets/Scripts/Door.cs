using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour
{
    [Header("Door References")]
    [SerializeField] private GameObject leftDoor;
    [SerializeField] private GameObject rightDoor;

    [Header("Door Movement Settings")]
    [SerializeField] private Vector3 openOffset = new Vector3(-2f, 0f, 0f);
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDuration = 1f;
    [SerializeField] private Ease openEase = Ease.OutQuad;
    [SerializeField] private Ease closeEase = Ease.InQuad;

    [Header("Door Behavior")]
    [SerializeField] private bool startOpen = false; // If true, door starts in open position
    [SerializeField] private float autoCloseDelay = 2f;
    [SerializeField] private bool autoClose = true;
    [SerializeField] private bool toggleMode = false; // If true, trigger closes open doors
    [SerializeField] private bool oneTimeUse = false; // If true, door can only be triggered once
    [SerializeField] private float toggleCooldown = 1f; // Cooldown before door can be toggled again

    private Vector3 leftDoorClosedPos;
    private Vector3 rightDoorClosedPos;
    private bool isOpen = false;
    private bool hasBeenUsed = false;
    private int objectsInTrigger = 0;
    private float lastToggleTime = -999f;
    private Coroutine autoCloseCoroutine;

    void Start()
    {
        // Store initial positions
        if (leftDoor != null)
            leftDoorClosedPos = leftDoor.transform.localPosition;
        
        if (rightDoor != null)
            rightDoorClosedPos = rightDoor.transform.localPosition;

        // If door should start open, move doors to open position immediately
        if (startOpen)
        {
            isOpen = true;
            
            if (leftDoor != null)
                leftDoor.transform.localPosition = leftDoorClosedPos - openOffset;
            
            if (rightDoor != null)
                rightDoor.transform.localPosition = rightDoorClosedPos + openOffset;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering has a tag you want (e.g., "Player")
        // Remove this check if you want any collider to trigger the door
        if (other.CompareTag("Player") || true) // Change to your preferred tag
        {
            // Check if door is one-time use and has already been used
            if (oneTimeUse && hasBeenUsed)
                return;

            objectsInTrigger++;

            // Toggle mode: close if open, open if closed
            if (toggleMode)
            {
                // Check cooldown to prevent immediate re-triggering when passing through
                if (Time.time - lastToggleTime < toggleCooldown)
                    return;

                lastToggleTime = Time.time;

                if (isOpen)
                {
                    CloseDoor();
                }
                else
                {
                    OpenDoor();
                    if (oneTimeUse)
                        hasBeenUsed = true;
                }
            }
            else
            {
                // Normal mode: just open
                OpenDoor();
                if (oneTimeUse)
                    hasBeenUsed = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || true) // Change to your preferred tag
        {
            objectsInTrigger--;
            
            if (objectsInTrigger <= 0)
            {
                objectsInTrigger = 0;
                
                // Auto-close if enabled (works in both normal and toggle mode)
                if (autoClose)
                {
                    // Start auto-close timer
                    if (autoCloseCoroutine != null)
                        StopCoroutine(autoCloseCoroutine);
                    
                    autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
                }
            }
        }
    }

    private void OpenDoor()
    {
        if (isOpen) return;

        // Cancel auto-close if door is opening again
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        isOpen = true;

        // Animate left door
        if (leftDoor != null)
        {
            leftDoor.transform.DOLocalMove(leftDoorClosedPos - openOffset, openDuration)
                .SetEase(openEase);
        }

        // Animate right door
        if (rightDoor != null)
        {
            rightDoor.transform.DOLocalMove(rightDoorClosedPos + openOffset, openDuration)
                .SetEase(openEase);
        }
    }

    private void CloseDoor()
    {
        if (!isOpen) return;

        isOpen = false;

        // Animate left door back to closed position
        if (leftDoor != null)
        {
            leftDoor.transform.DOLocalMove(leftDoorClosedPos, closeDuration)
                .SetEase(closeEase);
        }

        // Animate right door back to closed position
        if (rightDoor != null)
        {
            rightDoor.transform.DOLocalMove(rightDoorClosedPos, closeDuration)
                .SetEase(closeEase);
        }
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        if (objectsInTrigger <= 0)
        {
            CloseDoor();
        }
        
        autoCloseCoroutine = null;
    }

    // Public methods for manual control
    public void ManualOpen()
    {
        OpenDoor();
    }

    public void ManualClose()
    {
        CloseDoor();
    }

    void OnDestroy()
    {
        // Clean up DOTween animations
        if (leftDoor != null)
            leftDoor.transform.DOKill();
        
        if (rightDoor != null)
            rightDoor.transform.DOKill();
    }
}