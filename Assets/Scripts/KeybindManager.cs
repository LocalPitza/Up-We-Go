using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance { get; private set; }
    
    [System.Serializable]
    public class Keybind
    {
        public string actionName;
        public KeyCode primaryKey;
        public KeyCode alternateKey;
        
        public Keybind(string name, KeyCode primary, KeyCode alternate = KeyCode.None)
        {
            actionName = name;
            primaryKey = primary;
            alternateKey = alternate;
        }
    }
    
    [Header("Default Keybinds")]
    public List<Keybind> keybinds = new List<Keybind>();
    
    private Dictionary<string, Keybind> keybindDict = new Dictionary<string, Keybind>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeKeybinds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeKeybinds()
    {
        // Set default keybinds if none exist
        if (keybinds.Count == 0)
        {
            // Movement & Basic Actions
            keybinds.Add(new Keybind("Jump", KeyCode.Space));
            keybinds.Add(new Keybind("Crouch", KeyCode.LeftControl, KeyCode.C));
            keybinds.Add(new Keybind("Sprint", KeyCode.LeftShift));
            
            // Interaction
            keybinds.Add(new Keybind("Interact", KeyCode.E, KeyCode.F));
            keybinds.Add(new Keybind("Pickup", KeyCode.Mouse0)); // Left Mouse Button
            keybinds.Add(new Keybind("Throw", KeyCode.Mouse1)); // Right Mouse Button
            keybinds.Add(new Keybind("RotateObject", KeyCode.R));
            
            // UI & Menus
            keybinds.Add(new Keybind("Inventory", KeyCode.I, KeyCode.Tab));
            keybinds.Add(new Keybind("Pause", KeyCode.Escape, KeyCode.P));
        }
        
        // Build dictionary for fast lookup
        keybindDict.Clear();
        foreach (var keybind in keybinds)
        {
            keybindDict[keybind.actionName] = keybind;
        }
        
        // Load saved keybinds from PlayerPrefs
        LoadKeybinds();
    }

    // Check if a key is pressed for a specific action
    public bool GetKeyDown(string actionName)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            return Input.GetKeyDown(keybind.primaryKey) || 
                   (keybind.alternateKey != KeyCode.None && Input.GetKeyDown(keybind.alternateKey));
        }
        
        Debug.LogWarning($"Keybind '{actionName}' not found!");
        return false;
    }

    // Check if a key is being held for a specific action
    public bool GetKey(string actionName)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            return Input.GetKey(keybind.primaryKey) || 
                   (keybind.alternateKey != KeyCode.None && Input.GetKey(keybind.alternateKey));
        }
        
        Debug.LogWarning($"Keybind '{actionName}' not found!");
        return false;
    }

    // Check if a key was released for a specific action
    public bool GetKeyUp(string actionName)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            return Input.GetKeyUp(keybind.primaryKey) || 
                   (keybind.alternateKey != KeyCode.None && Input.GetKeyUp(keybind.alternateKey));
        }
        
        Debug.LogWarning($"Keybind '{actionName}' not found!");
        return false;
    }

    // Special method for mouse scroll delta (for adjusting hold distance)
    public float GetMouseScrollDelta()
    {
        return Input.mouseScrollDelta.y;
    }

    // Get the primary key for an action
    public KeyCode GetPrimaryKey(string actionName)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            return keybind.primaryKey;
        }
        return KeyCode.None;
    }

    // Get the alternate key for an action
    public KeyCode GetAlternateKey(string actionName)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            return keybind.alternateKey;
        }
        return KeyCode.None;
    }

    // Set a new primary key for an action
    public void SetPrimaryKey(string actionName, KeyCode newKey)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            keybind.primaryKey = newKey;
            SaveKeybinds();
        }
    }

    // Set a new alternate key for an action
    public void SetAlternateKey(string actionName, KeyCode newKey)
    {
        if (keybindDict.TryGetValue(actionName, out Keybind keybind))
        {
            keybind.alternateKey = newKey;
            SaveKeybinds();
        }
    }

    // Save keybinds to PlayerPrefs
    public void SaveKeybinds()
    {
        foreach (var keybind in keybinds)
        {
            PlayerPrefs.SetInt($"Keybind_{keybind.actionName}_Primary", (int)keybind.primaryKey);
            PlayerPrefs.SetInt($"Keybind_{keybind.actionName}_Alternate", (int)keybind.alternateKey);
        }
        PlayerPrefs.Save();
        Debug.Log("Keybinds saved!");
    }

    // Load keybinds from PlayerPrefs
    public void LoadKeybinds()
    {
        foreach (var keybind in keybinds)
        {
            if (PlayerPrefs.HasKey($"Keybind_{keybind.actionName}_Primary"))
            {
                keybind.primaryKey = (KeyCode)PlayerPrefs.GetInt($"Keybind_{keybind.actionName}_Primary");
            }
            if (PlayerPrefs.HasKey($"Keybind_{keybind.actionName}_Alternate"))
            {
                keybind.alternateKey = (KeyCode)PlayerPrefs.GetInt($"Keybind_{keybind.actionName}_Alternate");
            }
        }
        
        // Rebuild dictionary after loading
        keybindDict.Clear();
        foreach (var keybind in keybinds)
        {
            keybindDict[keybind.actionName] = keybind;
        }
        
        Debug.Log("Keybinds loaded!");
    }

    // Reset all keybinds to defaults
    public void ResetToDefaults()
    {
        keybinds.Clear();
        keybinds.Add(new Keybind("Jump", KeyCode.Space));
        keybinds.Add(new Keybind("Crouch", KeyCode.LeftControl, KeyCode.C));
        keybinds.Add(new Keybind("Sprint", KeyCode.LeftShift));
        keybinds.Add(new Keybind("Interact", KeyCode.E, KeyCode.F));
        keybinds.Add(new Keybind("Pickup", KeyCode.Mouse0));
        keybinds.Add(new Keybind("Throw", KeyCode.Mouse1));
        keybinds.Add(new Keybind("RotateObject", KeyCode.R));
        keybinds.Add(new Keybind("Inventory", KeyCode.I, KeyCode.Tab));
        keybinds.Add(new Keybind("Pause", KeyCode.Escape, KeyCode.P));
        
        keybindDict.Clear();
        foreach (var keybind in keybinds)
        {
            keybindDict[keybind.actionName] = keybind;
        }
        
        SaveKeybinds();
        Debug.Log("Keybinds reset to defaults!");
    }

    // Get all keybinds for UI display
    public List<Keybind> GetAllKeybinds()
    {
        return keybinds;
    }
    
    // Get a user-friendly name for display
    public string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "Left Mouse";
            case KeyCode.Mouse1: return "Right Mouse";
            case KeyCode.Mouse2: return "Middle Mouse";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift";
            case KeyCode.LeftAlt: return "Left Alt";
            case KeyCode.RightAlt: return "Right Alt";
            case KeyCode.None: return "Not Set";
            default: return key.ToString();
        }
    }
}