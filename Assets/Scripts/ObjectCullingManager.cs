using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCullingManager : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float cullingDistance = 30f;
    [SerializeField] private float horizontalBuffer = 0.3f; // Extra area beyond left/right screen edges
    [SerializeField] private float verticalBuffer = 0.3f; // Extra area beyond top/bottom screen edges
    [SerializeField] private LayerMask interactableLayer; // Objects on this layer will be culled
    [SerializeField] private LayerMask puzzleLayer; // Objects on this layer will NEVER be culled
    [SerializeField] private Camera playerCamera;
    
    [Header("Integration")]
    [SerializeField] private ObjectInteraction objectInteraction; // Reference to pickup script
    
    [Header("Performance Settings")]
    [SerializeField] private int maxObjectsPerFrame = 15;
    
    [Header("What to Disable")]
    [SerializeField] private bool disableRenderers = true;
    [SerializeField] private bool disableColliders = true;
    [SerializeField] private bool disableRigidbodies = false;
    
    private Dictionary<GameObject, ObjectData> trackedObjects = new Dictionary<GameObject, ObjectData>();
    private List<GameObject> objectList = new List<GameObject>();
    private float updateTimer;
    private int currentIndex = 0;
    
    private class ObjectData
    {
        public Renderer[] renderers;
        public Collider[] colliders;
        public Rigidbody rigidbody;
        public bool isActive = true;
        public bool isPuzzleObject = false;
        
        public ObjectData(GameObject obj, bool isPuzzle)
        {
            renderers = obj.GetComponentsInChildren<Renderer>();
            colliders = obj.GetComponentsInChildren<Collider>();
            rigidbody = obj.GetComponent<Rigidbody>();
            isPuzzleObject = isPuzzle;
        }
    }
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        // Auto-find ObjectInteraction if not assigned
        if (objectInteraction == null)
            objectInteraction = GetComponent<ObjectInteraction>();
        
        ScanForObjects();
    }
    
    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            ProcessObjectBatch();
        }
    }
    
    [ContextMenu("Scan For Objects")]
    public void ScanForObjects()
    {
        trackedObjects.Clear();
        objectList.Clear();
        
        // Find all objects on interactable layer
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if object is on interactable layer
            if (((1 << obj.layer) & interactableLayer) != 0)
            {
                // Check if it's a puzzle object
                bool isPuzzle = ((1 << obj.layer) & puzzleLayer) != 0;
                
                // Only track if it has a rigidbody (interactable)
                if (obj.GetComponent<Rigidbody>() != null)
                {
                    ObjectData data = new ObjectData(obj, isPuzzle);
                    trackedObjects[obj] = data;
                    objectList.Add(obj);
                }
            }
        }
        
        Debug.Log($"Tracking {trackedObjects.Count} objects ({objectList.Count} total)");
    }
    
    void ProcessObjectBatch()
    {
        if (objectList.Count == 0) return;
        
        int processed = 0;
        int startIndex = currentIndex;
        
        while (processed < maxObjectsPerFrame && objectList.Count > 0)
        {
            if (currentIndex >= objectList.Count)
                currentIndex = 0;
            
            GameObject obj = objectList[currentIndex];
            
            // Remove null objects
            if (obj == null)
            {
                trackedObjects.Remove(obj);
                objectList.RemoveAt(currentIndex);
                if (objectList.Count == 0) break;
                continue;
            }
            
            ProcessObject(obj);
            
            currentIndex++;
            processed++;
            
            // Prevent infinite loop
            if (currentIndex == startIndex && processed > 0)
                break;
        }
    }
    
    void ProcessObject(GameObject obj)
    {
        if (!trackedObjects.ContainsKey(obj))
            return;
        
        ObjectData data = trackedObjects[obj];
        
        // NEVER cull held objects
        if (objectInteraction != null && objectInteraction.GetHeldObject() == obj)
        {
            if (!data.isActive)
            {
                SetObjectActive(data, true);
                data.isActive = true;
            }
            return;
        }
        
        // Skip puzzle objects - they're always active
        if (data.isPuzzleObject)
        {
            if (!data.isActive)
            {
                SetObjectActive(data, true);
                data.isActive = true;
            }
            return;
        }
        
        float distance = Vector3.Distance(transform.position, obj.transform.position);
        bool isInView = IsObjectInView(obj.transform.position);
        
        // Object should be active if it's within distance AND in view
        bool shouldBeActive = distance <= cullingDistance && isInView;
        
        // Update state if changed
        if (data.isActive != shouldBeActive)
        {
            SetObjectActive(data, shouldBeActive);
            data.isActive = shouldBeActive;
        }
    }
    
    void SetObjectActive(ObjectData data, bool active)
    {
        if (disableRenderers && data.renderers != null)
        {
            foreach (Renderer r in data.renderers)
            {
                if (r != null)
                    r.enabled = active;
            }
        }
        
        if (disableColliders && data.colliders != null)
        {
            foreach (Collider c in data.colliders)
            {
                if (c != null)
                    c.enabled = active;
            }
        }
        
        if (disableRigidbodies && data.rigidbody != null)
        {
            data.rigidbody.isKinematic = !active;
            
            if (!active)
            {
                data.rigidbody.velocity = Vector3.zero;
                data.rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
    
    bool IsObjectInView(Vector3 worldPosition)
    {
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(worldPosition);
        
        // Apply separate horizontal and vertical buffers
        float minX = 0f - horizontalBuffer;
        float maxX = 1f + horizontalBuffer;
        float minY = 0f - verticalBuffer;
        float maxY = 1f + verticalBuffer;
        
        // Check if object is in front of camera and within buffered viewport bounds
        return viewportPoint.z > 0 && 
               viewportPoint.x >= minX && viewportPoint.x <= maxX &&
               viewportPoint.y >= minY && viewportPoint.y <= maxY;
    }
    
    // Call this if you spawn new objects at runtime
    public void RefreshObjects()
    {
        ScanForObjects();
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, cullingDistance);
        
        // Draw lines to tracked objects
        if (Application.isPlaying && trackedObjects != null)
        {
            foreach (var kvp in trackedObjects)
            {
                if (kvp.Key != null)
                {
                    Gizmos.color = kvp.Value.isPuzzleObject ? Color.yellow : Color.green;
                    Gizmos.DrawLine(transform.position, kvp.Key.transform.position);
                }
            }
        }
    }
}