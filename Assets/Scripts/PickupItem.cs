using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Item";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool rotateItem = true;
    [SerializeField] private float rotationSpeed = 50f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupVolume = 0.7f;

    void Update()
    {
        if (rotateItem)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    public void Interact(PlayerController player)
    {
        // Play pickup sound
        if (pickupSound != null)
        {
            // Create a temporary AudioSource at the pickup location
            GameObject soundObject = new GameObject("PickupSound");
            soundObject.transform.position = transform.position;
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = pickupSound;
            audioSource.volume = pickupVolume;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.Play();
            
            // Destroy the sound object after the clip finishes
            Destroy(soundObject, pickupSound.length);
        }
        
        player.AddItem(itemName);
        
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            // Optionally just disable the object
            gameObject.SetActive(false);
        }
    }
}