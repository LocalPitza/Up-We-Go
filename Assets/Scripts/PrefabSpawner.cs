using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PrefabEntry
{
    public string name;
    public GameObject prefab;
}

public class PrefabSpawner : MonoBehaviour
{
    public static PrefabSpawner Instance { get; private set; }
    
    [Header("Prefab Registry")]
    public List<PrefabEntry> prefabs = new List<PrefabEntry>();
    
    [Header("Spawn Settings")]
    public float spawnDistance = 3f;
    public Transform playerTransform;
    public Transform cameraTransform;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Auto-find player and camera if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public GameObject SpawnPrefab(string prefabName)
    {
        // Find the prefab by name (case insensitive)
        PrefabEntry entry = prefabs.Find(p => p.name.ToLower() == prefabName.ToLower());
        
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"Prefab '{prefabName}' not found in registry!");
            return null;
        }
        
        // Calculate spawn position in front of camera
        Vector3 spawnPosition = GetSpawnPosition();
        
        // Spawn the prefab
        GameObject spawnedObject = Instantiate(entry.prefab, spawnPosition, Quaternion.identity);
        
        // Face the same direction as the camera (optional)
        if (cameraTransform != null)
        {
            spawnedObject.transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        
        Debug.Log($"Spawned '{prefabName}' at {spawnPosition}");
        return spawnedObject;
    }

    public GameObject SpawnPrefabAtPosition(string prefabName, Vector3 position)
    {
        PrefabEntry entry = prefabs.Find(p => p.name.ToLower() == prefabName.ToLower());
        
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"Prefab '{prefabName}' not found in registry!");
            return null;
        }
        
        GameObject spawnedObject = Instantiate(entry.prefab, position, Quaternion.identity);
        Debug.Log($"Spawned '{prefabName}' at {position}");
        return spawnedObject;
    }

    private Vector3 GetSpawnPosition()
    {
        if (cameraTransform != null)
        {
            // Spawn in front of camera
            return cameraTransform.position + cameraTransform.forward * spawnDistance;
        }
        else if (playerTransform != null)
        {
            // Fallback to player forward
            return playerTransform.position + playerTransform.forward * spawnDistance;
        }
        else
        {
            // Last resort
            return Vector3.zero;
        }
    }

    public List<string> GetAvailablePrefabs()
    {
        List<string> names = new List<string>();
        foreach (PrefabEntry entry in prefabs)
        {
            if (entry.prefab != null)
            {
                names.Add(entry.name);
            }
        }
        return names;
    }

    public bool HasPrefab(string prefabName)
    {
        return prefabs.Exists(p => p.name.ToLower() == prefabName.ToLower() && p.prefab != null);
    }
}
