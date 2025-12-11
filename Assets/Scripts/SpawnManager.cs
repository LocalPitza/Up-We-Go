using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }
    
    [Header("Spawn Points")]
    public Transform mainSpawn;
    public Transform debugSpawn;
    
    [Header("Player Reference")]
    public GameObject player;
    
    private CharacterController playerController;

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
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
        }

        // Spawn player at main spawn
        SpawnAtMain();
    }

    public void SpawnAtMain()
    {
        if (mainSpawn != null && player != null)
        {
            TeleportPlayer(mainSpawn.position, mainSpawn.rotation);
            Debug.Log("Player spawned at main spawn point");
        }
        else
        {
            Debug.LogWarning("Main spawn or player not set!");
        }
    }

    public void SpawnAtDebug()
    {
        if (debugSpawn != null && player != null)
        {
            TeleportPlayer(debugSpawn.position, debugSpawn.rotation);
            Debug.Log("Player teleported to debug spawn point");
        }
        else
        {
            Debug.LogWarning("Debug spawn or player not set!");
        }
    }

    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (playerController != null)
        {
            // Disable controller, move, then re-enable
            playerController.enabled = false;
            player.transform.position = position;
            player.transform.rotation = rotation;
            playerController.enabled = true;
        }
        else
        {
            // Direct transform manipulation if no CharacterController
            player.transform.position = position;
            player.transform.rotation = rotation;
        }
    }

    public void TeleportToPosition(Vector3 position)
    {
        if (player != null)
        {
            TeleportPlayer(position, player.transform.rotation);
            Debug.Log($"Player teleported to {position}");
        }
    }
}
