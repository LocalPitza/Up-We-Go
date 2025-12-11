using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ObjectCollision : MonoBehaviour
{
    [Header("Collision Sound Settings")]
    [Tooltip("List of sounds to randomly play on collision")]
    public AudioClip[] collisionClips;
    
    [Tooltip("Minimum collision force required to play sound")]
    [Range(0f, 10f)]
    public float minimumForce = 0.5f;
    
    [Tooltip("Volume of the collision sound")]
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Tooltip("Random pitch variation (0 = no variation)")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.1f;
    
    [Header("Optional Settings")]
    [Tooltip("Cooldown time between sounds (prevents spam)")]
    [Range(0f, 1f)]
    public float soundCooldown = 0.1f;
    
    [Tooltip("Only play sound when colliding with specific tag (leave empty for all)")]
    public string collisionTag = "";
    
    private AudioSource audioSource;
    private float lastSoundTime;

    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if enough time has passed since last sound
        if (Time.time - lastSoundTime < soundCooldown)
            return;
        
        // Check if we should filter by tag
        if (!string.IsNullOrEmpty(collisionTag) && !collision.gameObject.CompareTag(collisionTag))
            return;
        
        // Check if collision force is strong enough
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minimumForce)
            return;
        
        // Play the sound if clips are assigned
        if (collisionClips != null && collisionClips.Length > 0)
        {
            PlayRandomCollisionSound();
            lastSoundTime = Time.time;
        }
        else
        {
            Debug.LogWarning("No collision sound clips assigned on " + gameObject.name);
        }
    }

    void PlayRandomCollisionSound()
    {
        // Pick a random clip from the list
        AudioClip randomClip = collisionClips[Random.Range(0, collisionClips.Length)];
        
        // Skip if the selected clip is null
        if (randomClip == null)
        {
            Debug.LogWarning("One of the collision clips is null on " + gameObject.name);
            return;
        }
        
        // Add random pitch variation for more natural sound
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.volume = volume;
        audioSource.PlayOneShot(randomClip);
    }
}
