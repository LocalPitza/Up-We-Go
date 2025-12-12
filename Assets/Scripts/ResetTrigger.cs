using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetTrigger : MonoBehaviour
{
    [Header("Layer Detection")]
    [Tooltip("Objects on these layers will be reset when they fall through (e.g., 'Interactable' and 'Puzzle Object')")]
    public LayerMask resetLayers;
    
    [Header("Reset Settings")]
    [Tooltip("Delay before resetting object (gives time for fall animation)")]
    public float resetDelay = 0.1f;
    
    [Tooltip("Should we log reset events to console?")]
    public bool debugMode = false;
    
    // Global registry to store initial transforms of all resetable objects
    private static Dictionary<GameObject, TransformData> globalTransforms = new Dictionary<GameObject, TransformData>();
    
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
        // Ensure this has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("ResetTrigger requires a Collider component! Adding BoxCollider...");
            col = gameObject.AddComponent<BoxCollider>();
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning("Collider on ResetTrigger should be set to 'Is Trigger'. Setting it now...");
            col.isTrigger = true;
        }
        
        // Register all objects with the specified layers at scene start
        RegisterAllResetableObjects();
    }

    void RegisterAllResetableObjects()
    {
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        int registeredCount = 0;
        foreach (GameObject obj in allObjects)
        {
            // Check if object is on one of the reset layers
            if (IsInLayerMask(obj.layer, resetLayers))
            {
                // Only register if not already registered
                if (!globalTransforms.ContainsKey(obj))
                {
                    globalTransforms[obj] = new TransformData(obj.transform);
                    registeredCount++;
                    
                    if (debugMode)
                    {
                        Debug.Log($"Registered {obj.name} for reset");
                    }
                }
            }
        }
        
        Debug.Log($"ResetTrigger: Registered {registeredCount} new objects for reset (Total: {globalTransforms.Count})");
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object is on one of the reset layers
        if (IsInLayerMask(other.gameObject.layer, resetLayers))
        {
            if (debugMode)
            {
                Debug.Log($"{other.gameObject.name} fell off the map! Resetting...");
            }
            
            // Register the object if it's not already registered
            if (!globalTransforms.ContainsKey(other.gameObject))
            {
                globalTransforms[other.gameObject] = new TransformData(other.transform);
                Debug.LogWarning($"{other.gameObject.name} was not registered! Saving current position as default.");
            }
            
            // Start reset coroutine
            StartCoroutine(ResetObjectWithDelay(other.gameObject));
        }
    }

    IEnumerator ResetObjectWithDelay(GameObject obj)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(resetDelay);
        
        // Make sure object still exists
        if (obj != null && globalTransforms.ContainsKey(obj))
        {
            TransformData data = globalTransforms[obj];
            
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
            
            if (debugMode)
            {
                Debug.Log($"{obj.name} has been reset to starting position");
            }
        }
    }

    // Helper method to check if a layer is in a LayerMask
    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    // Public method to manually register a new object at runtime
    public static void RegisterObject(GameObject obj)
    {
        if (obj != null && !globalTransforms.ContainsKey(obj))
        {
            globalTransforms[obj] = new TransformData(obj.transform);
            Debug.Log($"Manually registered {obj.name} for reset");
        }
    }

    // Public method to update an object's stored position (useful after puzzle completion)
    public static void UpdateStoredPosition(GameObject obj)
    {
        if (obj != null && globalTransforms.ContainsKey(obj))
        {
            globalTransforms[obj] = new TransformData(obj.transform);
            Debug.Log($"Updated stored position for {obj.name}");
        }
    }

    // Public method to unregister an object (if you don't want it to reset anymore)
    public static void UnregisterObject(GameObject obj)
    {
        if (obj != null && globalTransforms.ContainsKey(obj))
        {
            globalTransforms.Remove(obj);
            Debug.Log($"Unregistered {obj.name} from reset system");
        }
    }

    // Visualize the trigger in the editor
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
