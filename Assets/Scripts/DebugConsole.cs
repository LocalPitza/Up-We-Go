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
    
    // Noclip settings
    private float flySpeed = 10f;
    private float fastFlySpeed = 25f;
    private CharacterController playerController;
    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private bool wasKinematic;
    private Vector3 noclipVelocity;
    
    // GUI settings
    private Rect consoleRect = new Rect(10, 10, Screen.width - 20, 300);
    private GUIStyle consoleStyle;
    private GUIStyle logStyle;

    void Awake()
    {
        Application.logMessageReceived += HandleLog;
        
        // Try to find player components
        FindPlayerComponents();
    }

    void FindPlayerComponents()
    {
        // Look for CharacterController on this object or children
        playerController = GetComponentInChildren<CharacterController>();
        playerRigidbody = GetComponentInChildren<Rigidbody>();
        playerCollider = GetComponentInChildren<Collider>();
        
        // If not found, try to find the player by tag
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<CharacterController>();
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
        }

        // Toggle noclip with N key when console is open
        if (showConsole && Input.GetKeyDown(KeyCode.N))
        {
            ToggleNoclip();
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

    void ToggleNoclip()
    {
        noclipEnabled = !noclipEnabled;
        
        if (noclipEnabled)
        {
            // Disable physics
            if (playerController != null)
                playerController.enabled = false;
                
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
            // Re-enable physics
            if (playerController != null)
                playerController.enabled = true;
                
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
        }

        // Draw console background
        GUI.Box(consoleRect, "", consoleStyle);

        GUILayout.BeginArea(new Rect(consoleRect.x + 5, consoleRect.y + 5, consoleRect.width - 10, consoleRect.height - 10));
        
        // Title and controls
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>Debug Console</b> - Press ` to close | N to toggle Noclip", logStyle);
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
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(consoleRect.height - 80));
        
        foreach (string log in logs)
        {
            GUILayout.Label(log, logStyle);
        }
        
        GUILayout.EndScrollView();
        
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