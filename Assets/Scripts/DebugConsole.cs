using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugConsole : MonoBehaviour
{
    private bool showConsole = false;
    private bool noclipEnabled = false;
    private List<string> logs = new List<string>();
    private Vector2 scrollPos;
    private const int maxLogs = 100;
    
    // Player controller reference
    private PlayerController playerController;
    
    // Noclip settings
    private float flySpeed = 10f;
    private float fastFlySpeed = 25f;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private bool wasKinematic;
    private float mouseSensitivity = 2f;
    private float verticalRotation = 0f;
    private float maxLookAngle = 80f;
    private Transform cameraTransform;
    
    // GUI settings
    private Rect consoleRect = new Rect(10, 10, Screen.width - 20, 400);
    private GUIStyle consoleStyle;
    private GUIStyle logStyle;
    private GUIStyle inputStyle;
    
    // Command input
    private string commandInput = "";
    private bool focusInput = false;

    void Awake()
    {
        Application.logMessageReceived += HandleLog;
        
        // Try to find player components
        FindPlayerComponents();
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void FindPlayerComponents()
    {
        // Look for PlayerController on this object or children
        playerController = GetComponentInChildren<PlayerController>();
        characterController = GetComponentInChildren<CharacterController>();
        playerRigidbody = GetComponentInChildren<Rigidbody>();
        playerCollider = GetComponentInChildren<Collider>();
        
        // Get camera reference
        cameraTransform = Camera.main.transform;
        
        // If not found, try to find the player by tag
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
                characterController = player.GetComponent<CharacterController>();
                playerRigidbody = player.GetComponent<Rigidbody>();
                playerCollider = player.GetComponent<Collider>();
            }
        }
        
        // Store initial mouse sensitivity if player controller exists
        if (playerController != null)
        {
            // Try to match the player's mouse sensitivity
            mouseSensitivity = 2f; // Default value
        }
    }

    void Update()
    {
        // Toggle console with ` key
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            showConsole = !showConsole;
            
            if (showConsole)
            {
                // Show cursor and unlock
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                // Disable player controller
                if (playerController != null)
                {
                    playerController.enabled = false;
                }
                
                focusInput = true;
            }
            else
            {
                // Hide cursor and lock
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                
                // Re-enable player controller if not in noclip
                if (playerController != null && !noclipEnabled)
                {
                    playerController.enabled = true;
                }
            }
        }

        // Handle noclip movement
        if (noclipEnabled)
        {
            HandleNoclipMovement();
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string prefix = "";
        switch (type)
        {
            case LogType.Error:
                prefix = "<color=red>[ERROR]</color> ";
                break;
            case LogType.Warning:
                prefix = "<color=yellow>[WARNING]</color> ";
                break;
            case LogType.Exception:
                prefix = "<color=red>[EXCEPTION]</color> ";
                break;
            default:
                prefix = "[LOG] ";
                break;
        }

        logs.Add($"{prefix}{logString}");
        
        if (logs.Count > maxLogs)
        {
            logs.RemoveAt(0);
        }
    }

    void ExecuteCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) return;
        
        logs.Add($"<color=cyan>> {command}</color>");
        
        string[] parts = command.ToLower().Split(' ');
        
        switch (parts[0])
        {
            case "tp":
            case "-tp":
                if (parts.Length > 1)
                {
                    HandleTeleportCommand(parts[1]);
                }
                else
                {
                    logs.Add("<color=yellow>Usage: tp [main|debug]</color>");
                }
                break;
            
            case "spawn":
                if (parts.Length > 1)
                {
                    HandleSpawnCommand(parts[1]);
                }
                else
                {
                    logs.Add("<color=yellow>Usage: spawn [prefabname]</color>");
                    logs.Add("Type 'spawn list' to see available prefabs");
                }
                break;
                
            case "help":
                logs.Add("<color=green>Available Commands:</color>");
                logs.Add("  tp main - Teleport to main spawn");
                logs.Add("  tp debug - Teleport to debug spawn");
                logs.Add("  spawn [name] - Spawn a prefab in front of you");
                logs.Add("  spawn list - Show available prefabs");
                logs.Add("  noclip - Toggle noclip mode");
                logs.Add("  clear - Clear console");
                logs.Add("  help - Show this help");
                break;
                
            case "noclip":
                ToggleNoclip();
                break;
                
            case "clear":
                logs.Clear();
                break;
                
            default:
                logs.Add($"<color=red>Unknown command: {parts[0]}</color>");
                logs.Add("Type 'help' for available commands");
                break;
        }
    }

    void HandleTeleportCommand(string destination)
    {
        if (SpawnManager.Instance == null)
        {
            logs.Add("<color=red>SpawnManager not found in scene!</color>");
            return;
        }
        
        switch (destination)
        {
            case "main":
            case "spawn":
                SpawnManager.Instance.SpawnAtMain();
                logs.Add("<color=green>Teleported to main spawn</color>");
                break;
                
            case "debug":
                SpawnManager.Instance.SpawnAtDebug();
                logs.Add("<color=green>Teleported to debug spawn</color>");
                break;
                
            default:
                logs.Add($"<color=red>Unknown spawn point: {destination}</color>");
                logs.Add("Available: main, debug");
                break;
        }
    }

    void HandleSpawnCommand(string prefabName)
    {
        if (PrefabSpawner.Instance == null)
        {
            logs.Add("<color=red>PrefabSpawner not found in scene!</color>");
            logs.Add("Add PrefabSpawner component to scene to use spawn command");
            return;
        }
        
        // Check for special commands
        if (prefabName == "list")
        {
            List<string> availablePrefabs = PrefabSpawner.Instance.GetAvailablePrefabs();
            
            if (availablePrefabs.Count == 0)
            {
                logs.Add("<color=yellow>No prefabs registered in PrefabSpawner</color>");
            }
            else
            {
                logs.Add("<color=green>Available Prefabs:</color>");
                foreach (string name in availablePrefabs)
                {
                    logs.Add($"  - {name}");
                }
            }
            return;
        }
        
        // Try to spawn the prefab
        GameObject spawned = PrefabSpawner.Instance.SpawnPrefab(prefabName);
        
        if (spawned != null)
        {
            logs.Add($"<color=green>Spawned '{prefabName}' in front of you</color>");
        }
        else
        {
            logs.Add($"<color=red>Failed to spawn '{prefabName}'</color>");
            logs.Add("Type 'spawn list' to see available prefabs");
        }
    }

    void ToggleNoclip()
    {
        noclipEnabled = !noclipEnabled;
        
        if (noclipEnabled)
        {
            // Store current camera rotation
            if (cameraTransform != null)
            {
                verticalRotation = cameraTransform.localEulerAngles.x;
                if (verticalRotation > 180f) verticalRotation -= 360f;
            }
            
            // Lock cursor for noclip mouse look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Disable player controller
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Disable physics
            if (characterController != null)
                characterController.enabled = false;
                
            if (playerRigidbody != null)
            {
                wasKinematic = playerRigidbody.isKinematic;
                playerRigidbody.isKinematic = true;
            }
            
            if (playerCollider != null)
                playerCollider.enabled = false;
                
            logs.Add("<color=green>[NOCLIP] Enabled - WASD to move, Mouse to look, Space/Ctrl for up/down, Shift for speed boost</color>");
        }
        else
        {
            // Re-enable player controller only if console is closed
            if (playerController != null && !showConsole)
            {
                playerController.enabled = true;
            }
            
            // Re-enable physics
            if (characterController != null)
                characterController.enabled = true;
                
            if (playerRigidbody != null)
                playerRigidbody.isKinematic = wasKinematic;
                
            if (playerCollider != null)
                playerCollider.enabled = true;
            
            // If console is open, show cursor again
            if (showConsole)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
                
            logs.Add("<color=yellow>[NOCLIP] Disabled</color>");
        }
    }

    void HandleNoclipMovement()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Get the player transform
        Transform playerTransform = playerController != null ? playerController.transform : transform;
        
        // Rotate player horizontally
        playerTransform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
        
        // Movement
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastFlySpeed : flySpeed;
        
        Vector3 move = Vector3.zero;
        
        // Use camera forward direction for movement
        Transform movementReference = cameraTransform != null ? cameraTransform : playerTransform;
        
        // Forward/backward
        if (Input.GetKey(KeyCode.W)) move += movementReference.forward;
        if (Input.GetKey(KeyCode.S)) move -= movementReference.forward;
        
        // Left/right
        if (Input.GetKey(KeyCode.A)) move -= movementReference.right;
        if (Input.GetKey(KeyCode.D)) move += movementReference.right;
        
        // Up/down (world space)
        if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;
        
        // Move the player object
        playerTransform.position += move.normalized * speed * Time.deltaTime;
    }

    void OnGUI()
    {
        // Handle Enter key for command execution BEFORE TextField processes it
        if (showConsole && Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
            {
                ExecuteCommand(commandInput);
                commandInput = "";
                focusInput = true;
                Event.current.Use(); // Consume the event
            }
            else if (Event.current.keyCode == KeyCode.N && Event.current.control)
            {
                ToggleNoclip();
                Event.current.Use();
            }
        }
        
        if (!showConsole) return;

        // Initialize styles
        if (consoleStyle == null)
        {
            consoleStyle = new GUIStyle(GUI.skin.box);
            consoleStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));
            
            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.richText = true;
            logStyle.wordWrap = true;
            logStyle.fontSize = 12;
            
            inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = 14;
            inputStyle.padding = new RectOffset(5, 5, 5, 5);
        }

        // Draw console background
        GUI.Box(consoleRect, "", consoleStyle);

        GUILayout.BeginArea(new Rect(consoleRect.x + 5, consoleRect.y + 5, consoleRect.width - 10, consoleRect.height - 10));
        
        // Title and controls
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>Debug Console</b> - Press ` to close | Type 'help' for commands", logStyle);
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            logs.Clear();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Noclip status
        if (noclipEnabled)
        {
            GUILayout.Label("<color=green>NOCLIP ACTIVE - Speed: " + (Input.GetKey(KeyCode.LeftShift) ? fastFlySpeed : flySpeed) + "m/s</color>", logStyle);
        }
        
        GUILayout.Space(5);

        // Scrollable log area
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(consoleRect.height - 120));
        
        foreach (string log in logs)
        {
            GUILayout.Label(log, logStyle);
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.Space(5);
        
        // Command input
        GUILayout.BeginHorizontal();
        GUILayout.Label(">", logStyle, GUILayout.Width(15));
        GUI.SetNextControlName("CommandInput");
        commandInput = GUILayout.TextField(commandInput, inputStyle);
        GUILayout.EndHorizontal();
        
        // Auto-focus input
        if (focusInput)
        {
            GUI.FocusControl("CommandInput");
            focusInput = false;
        }
        
        GUILayout.EndArea();
    }

    private Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
            
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}