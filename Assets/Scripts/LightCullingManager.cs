using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LightCullingManager : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private int maxActiveLights = 8;
    [SerializeField] private float cullDistance = 50f;
    [SerializeField] private float viewCullDistance = 30f;
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Priority Settings")]
    [SerializeField] private float closeLightDistance = 15f;
    [SerializeField] private float viewFrustumPadding = 1.2f;
    [SerializeField] private bool debugMode = false;
    
    [Header("View Buffer Settings")]
    [SerializeField] private float viewExitBuffer = 2f;
    [SerializeField] private float viewExitDelay = 1f;
    
    private Camera mainCamera;
    private List<ManagedLight> managedLights = new List<ManagedLight>();
    private float updateTimer;
    private Plane[] frustumPlanes;

    private class ManagedLight
    {
        public Light light;
        public float distanceToCamera;
        public bool isInView;
        public bool wasInView;
        public bool isClose;
        public float priority;
        public float timeExitedView;
        public bool isInViewBuffer;
        
        public ManagedLight(Light l)
        {
            light = l;
            wasInView = false;
            timeExitedView = -999f;
            isInViewBuffer = false;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found! Light culling disabled.");
            enabled = false;
            return;
        }
        
        RegisterAllLights();
        updateTimer = updateInterval;
    }

    void Update()
    {
        updateTimer -= Time.deltaTime;
        
        if (updateTimer <= 0f)
        {
            updateTimer = updateInterval;
            UpdateLightCulling();
        }
    }

    /// <summary>
    /// Registers all lights in the scene for culling management
    /// </summary>
    void RegisterAllLights()
    {
        Light[] allLights = FindObjectsOfType<Light>();
        
        foreach (Light light in allLights)
        {
            // Skip directional lights as they affect the entire scene
            if (light.type == LightType.Directional)
                continue;
                
            RegisterLight(light);
        }
        
        if (debugMode)
            Debug.Log($"Registered {managedLights.Count} lights for culling");
    }

    /// <summary>
    /// Registers a single light for culling management
    /// </summary>
    public void RegisterLight(Light light)
    {
        if (!managedLights.Any(ml => ml.light == light))
        {
            managedLights.Add(new ManagedLight(light));
        }
    }

    /// <summary>
    /// Unregisters a light from culling management
    /// </summary>
    public void UnregisterLight(Light light)
    {
        managedLights.RemoveAll(ml => ml.light == light);
    }

    /// <summary>
    /// Main culling logic - calculates priorities and enables/disables lights
    /// </summary>
    void UpdateLightCulling()
    {
        if (mainCamera == null || managedLights.Count == 0)
            return;

        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        // Update metrics for each light
        foreach (var ml in managedLights)
        {
            if (ml.light == null)
                continue;

            ml.distanceToCamera = Vector3.Distance(cameraPos, ml.light.transform.position);
            ml.isClose = ml.distanceToCamera <= closeLightDistance;
            ml.isInView = IsLightInView(ml.light, viewFrustumPadding);
            
            // Handle view buffer logic
            if (ml.isInView)
            {
                ml.wasInView = true;
                ml.isInViewBuffer = true;
                ml.timeExitedView = -999f;
            }
            else if (ml.wasInView)
            {
                // Just exited view - start buffer timer
                if (ml.timeExitedView < 0)
                {
                    ml.timeExitedView = Time.time;
                }
                
                // Check if still in buffer period
                float timeSinceExit = Time.time - ml.timeExitedView;
                if (timeSinceExit < viewExitDelay)
                {
                    // Still in buffer - check extended frustum
                    ml.isInViewBuffer = IsLightInView(ml.light, viewFrustumPadding + viewExitBuffer);
                }
                else
                {
                    // Buffer expired
                    ml.wasInView = false;
                    ml.isInViewBuffer = false;
                }
            }
            
            // Calculate priority score
            CalculatePriority(ml, cameraPos, cameraForward);
        }

        // Sort by priority (higher is better)
        managedLights.Sort((a, b) => b.priority.CompareTo(a.priority));

        // Enable/disable lights based on priority
        int activeLightCount = 0;
        
        foreach (var ml in managedLights)
        {
            if (ml.light == null)
                continue;

            bool shouldBeActive = false;

            // Always keep close lights active regardless of limit
            if (ml.isClose)
            {
                shouldBeActive = true;
            }
            // For other lights, respect the limit
            else if (activeLightCount < maxActiveLights)
            {
                // Check if within cull distance
                if (ml.distanceToCamera <= cullDistance)
                {
                    // If in view (or in view buffer), use view cull distance
                    if ((ml.isInView || ml.isInViewBuffer) && ml.distanceToCamera <= viewCullDistance)
                    {
                        shouldBeActive = true;
                    }
                    else if (!ml.isInView && !ml.isInViewBuffer && ml.distanceToCamera <= cullDistance)
                    {
                        shouldBeActive = true;
                    }
                }
            }

            // Only change state if needed to avoid unnecessary operations
            if (ml.light.enabled != shouldBeActive)
            {
                ml.light.enabled = shouldBeActive;
            }

            if (shouldBeActive)
                activeLightCount++;
        }

        if (debugMode)
            Debug.Log($"Active lights: {activeLightCount} / {managedLights.Count}");
    }

    /// <summary>
    /// Calculates priority score for a light based on multiple factors
    /// </summary>
    void CalculatePriority(ManagedLight ml, Vector3 cameraPos, Vector3 cameraForward)
    {
        float priority = 0f;

        // Distance factor (closer = higher priority)
        float distanceFactor = 1f - (ml.distanceToCamera / cullDistance);
        distanceFactor = Mathf.Clamp01(distanceFactor);
        priority += distanceFactor * 100f;

        // Close proximity bonus
        if (ml.isClose)
        {
            priority += 200f;
        }

        // View frustum bonus
        if (ml.isInView)
        {
            priority += 50f;
            
            // Additional bonus for being in center of view
            Vector3 toLight = (ml.light.transform.position - cameraPos).normalized;
            float viewAlignment = Vector3.Dot(cameraForward, toLight);
            priority += viewAlignment * 25f;
        }
        // Buffer bonus (less than in-view but still significant)
        else if (ml.isInViewBuffer)
        {
            priority += 30f;
            
            // Decay bonus based on how long it's been in buffer
            if (ml.timeExitedView > 0)
            {
                float bufferProgress = (Time.time - ml.timeExitedView) / viewExitDelay;
                priority += (1f - bufferProgress) * 20f;
            }
        }

        // Intensity factor
        priority += ml.light.intensity * 10f;

        ml.priority = priority;
    }

    /// <summary>
    /// Checks if a light is within the camera's view frustum
    /// </summary>
    bool IsLightInView(Light light, float padding)
    {
        // For point lights, check if sphere is in frustum
        if (light.type == LightType.Point)
        {
            float radius = light.range * padding;
            return GeometryUtility.TestPlanesAABB(frustumPlanes, 
                new Bounds(light.transform.position, Vector3.one * radius * 2f));
        }
        // For spot lights, check if cone intersects frustum
        else if (light.type == LightType.Spot)
        {
            float radius = light.range * Mathf.Tan(light.spotAngle * 0.5f * Mathf.Deg2Rad) * padding;
            return GeometryUtility.TestPlanesAABB(frustumPlanes,
                new Bounds(light.transform.position, Vector3.one * radius * 2f));
        }

        return true;
    }

    /// <summary>
    /// Force immediate update of light culling
    /// </summary>
    public void ForceUpdate()
    {
        UpdateLightCulling();
    }

    void OnDrawGizmos()
    {
        if (!debugMode || mainCamera == null)
            return;

        // Draw cull distance sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(mainCamera.transform.position, cullDistance);

        // Draw view cull distance sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(mainCamera.transform.position, viewCullDistance);

        // Draw close light distance sphere
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(mainCamera.transform.position, closeLightDistance);
    }
}
