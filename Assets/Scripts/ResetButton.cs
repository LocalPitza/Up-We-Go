using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetButton : MonoBehaviour
{
    [Header("Objects to Reset")]
    [Tooltip("Add all objects you want to reset when this cube is activated")]
    public List<GameObject> objectsToReset = new List<GameObject>();
    
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance the player can interact from")]
    public float interactionDistance = 3f;
    
    [Tooltip("Layer mask for raycast (optional - leave as Nothing to hit all)")]
    public LayerMask interactionLayer;
    
    [Header("Visual Feedback")]
    [Tooltip("Material to use when player is looking at the cube")]
    public Material highlightMaterial;
    
    [Tooltip("Original material of the cube")]
    public Material normalMaterial;
    
    [Header("Audio (Optional)")]
    public AudioClip resetSound;
    public AudioClip buttonPressSound;
    private AudioSource audioSource;
    
    // Store initial transforms of objects
    private Dictionary<GameObject, TransformData> initialTransforms = new Dictionary<GameObject, TransformData>();
    
    private Renderer cubeRenderer;
    private bool isLookingAt = false;
    
    [System.Serializable]
    private class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        
        public TransformData(Transform t)
        {
            position = t.position;
            rotation = t.rotation;
            scale = t.localScale;
        }
    }

    void Start()
    {
        // Get renderer component
        cubeRenderer = GetComponent<Renderer>();
        
        // Store normal material if not set
        if (normalMaterial == null && cubeRenderer != null)
        {
            normalMaterial = cubeRenderer.material;
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && resetSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Store initial positions of all objects
        SaveInitialTransforms();
    }

    void Update()
    {
        CheckPlayerLookingAt();
        
        // Check for interact key press while looking at the cube
        if (isLookingAt && KeybindManager.Instance != null)
        {
            if (KeybindManager.Instance.GetKeyDown("Interact"))
            {
                PlayButtonPressSound();
                ResetAllObjects();
            }
        }
    }

    void CheckPlayerLookingAt()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            isLookingAt = false;
            UpdateVisuals();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Perform raycast
        bool hitSomething = false;
        if (interactionLayer.value != 0)
        {
            hitSomething = Physics.Raycast(ray, out hit, interactionDistance, interactionLayer);
        }
        else
        {
            hitSomething = Physics.Raycast(ray, out hit, interactionDistance);
        }

        // Check if we hit this cube
        if (hitSomething && hit.collider.gameObject == gameObject)
        {
            isLookingAt = true;
        }
        else
        {
            isLookingAt = false;
        }
        
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (cubeRenderer == null) return;
        
        // Change material based on whether player is looking at it
        if (isLookingAt && highlightMaterial != null)
        {
            cubeRenderer.material = highlightMaterial;
        }
        else if (normalMaterial != null)
        {
            cubeRenderer.material = normalMaterial;
        }
    }

    void SaveInitialTransforms()
    {
        initialTransforms.Clear();
        
        foreach (GameObject obj in objectsToReset)
        {
            if (obj != null)
            {
                initialTransforms[obj] = new TransformData(obj.transform);
            }
        }
        
        Debug.Log($"Saved initial transforms for {initialTransforms.Count} objects");
    }

    public void ResetAllObjects()
    {
        int resetCount = 0;
        
        foreach (GameObject obj in objectsToReset)
        {
            if (obj != null && initialTransforms.ContainsKey(obj))
            {
                TransformData data = initialTransforms[obj];
                
                // Reset transform
                obj.transform.position = data.position;
                obj.transform.rotation = data.rotation;
                obj.transform.localScale = data.scale;
                
                // Reset physics if the object has a Rigidbody
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                resetCount++;
            }
        }
        
        Debug.Log($"Reset {resetCount} objects to their starting positions");
        
        // Play reset completion sound effect
        if (audioSource != null && resetSound != null)
        {
            audioSource.PlayOneShot(resetSound);
        }
    }

    void PlayButtonPressSound()
    {
        // Play button press sound effect
        if (audioSource != null && buttonPressSound != null)
        {
            audioSource.PlayOneShot(buttonPressSound);
        }
    }

    // Public method to add objects at runtime
    public void AddObjectToReset(GameObject obj)
    {
        if (obj != null && !objectsToReset.Contains(obj))
        {
            objectsToReset.Add(obj);
            initialTransforms[obj] = new TransformData(obj.transform);
        }
    }

    // Public method to remove objects from the list
    public void RemoveObjectFromReset(GameObject obj)
    {
        if (objectsToReset.Contains(obj))
        {
            objectsToReset.Remove(obj);
            initialTransforms.Remove(obj);
        }
    }

    // Public method to refresh saved positions (useful if you move objects in the editor)
    public void RefreshInitialPositions()
    {
        SaveInitialTransforms();
    }

    // Visualize interaction distance in editor
    void OnDrawGizmosSelected()
    {
        if (Camera.main != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw interaction range
            Gizmos.color = Color.green;
            Vector3 cameraPos = Camera.main.transform.position;
            Vector3 direction = (transform.position - cameraPos).normalized;
            Gizmos.DrawLine(cameraPos, cameraPos + direction * interactionDistance);
        }
    }
}