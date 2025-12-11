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
    private CharacterController PlayerController;
    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private bool wasKinematic;
    private Vector3 noclipVelocity;
    
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

    void FindPlayerComponents()
    {
        // Look for CharacterController on this object or children
        playerController = GetComponentInChildren<PlayerController>();
        CharacterController charController = GetComponentInChildren<CharacterController>();
        playerRigidbody = GetComponentInChildren<Rigidbody>();
        playerCollider = GetComponentInChildren<Collider>();
        
        // If not found, try to find the player by tag
        if (charController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
                charController = player.GetComponent<CharacterController>();
                playerRigidbody = player.GetComponent<Rigidbody>();
                playerCollider = player.GetComponent<Collider>();
            }
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

        // Toggle noclip with N key when console is open
        if (showConsole && Input.GetKeyDown(KeyCode.N))
        {
            ToggleNoclip();
        }
        
        // Execute command with Enter
        if (showConsole && Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteCommand(commandInput);
            commandInput = "";
            focusInput = true;
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
                
            case "help":
                logs.Add("<color=green>Available Commands:</color>");
                logs.Add("  tp main - Teleport to main spawn");
                logs.Add("  tp debug - Teleport to debug spawn");
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

    void ToggleNoclip()
    {
        noclipEnabled = !noclipEnabled;
        
        if (noclipEnabled)
        {
            // Disable player controller
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Disable physics
            CharacterController charController = GetComponent<CharacterController>();
            if (charController == null && playerController != null)
            {
                charController = playerController.GetComponent<CharacterController>();
            }
            
            if (charController != null)
                charController.enabled = false;
                
            if (playerRigidbody != null)
            {
                wasKinematic = playerRigidbody.isKinematic;
                playerRigidbody.isKinematic = true;
            }
            
            if (playerCollider != null)
                playerCollider.enabled = false;
                
            logs.Add("<color=green>[NOCLIP] Enabled - WASD to move, Space/Ctrl for up/down, Shift for speed boost</color>");
        }
        else
        {
            // Re-enable player controller only if console is closed
            if (playerController != null && !showConsole)
            {
                playerController.enabled = true;
            }
            
            // Re-enable physics
            CharacterController charController = GetComponent<CharacterController>();
            if (charController == null && playerController != null)
            {
                charController = playerController.GetComponent<CharacterController>();
            }
            
            if (charController != null)
                charController.enabled = true;
                
            if (playerRigidbody != null)
                playerRigidbody.isKinematic = wasKinematic;
                
            if (playerCollider != null)
                playerCollider.enabled = true;
                
            logs.Add("<color=yellow>[NOCLIP] Disabled</color>");
        }
    }

    void HandleNoclipMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastFlySpeed : flySpeed;
        
        Vector3 move = Vector3.zero;
        
        // Forward/backward
        if (Input.GetKey(KeyCode.W)) move += Camera.main.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= Camera.main.transform.forward;
        
        // Left/right
        if (Input.GetKey(KeyCode.A)) move -= Camera.main.transform.right;
        if (Input.GetKey(KeyCode.D)) move += Camera.main.transform.right;
        
        // Up/down
        if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;
        
        transform.position += move.normalized * speed * Time.deltaTime;
    }

    void OnGUI()
    {
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

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}