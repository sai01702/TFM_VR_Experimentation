# Connection Panel Debug Guide

## What I Fixed

### Issue
The ConnectionCodeCanvas was disappearing when running the game.

### Root Cause
The script needed better handling for cases where:
1. NetworkManager might not exist when the script starts
2. Server might start before the script subscribes to the event
3. The panel state wasn't being logged for debugging

### Solution Applied

**Updated ConnectionCodeGenerator.cs:**

1. **Added OnEnable() callback** - Re-registers callbacks when scene loads
2. **Added immediate check** - If server is already running when script starts, immediately show panel
3. **Better callback management** - Prevents duplicate subscriptions
4. **Extensive logging** - Every step is now logged to Console

**Set Panel State:**
- ConnectionPanel is now inactive by default in scene
- Will activate when server starts (logged in Console)

---

## How to Test & Debug

### Step 1: Enter Play Mode

1. Start the game in Unity Editor
2. **Immediately check Console** for these messages:

```
[ConnectionCodeGenerator] OnServerStarted called!
[ConnectionCodeGenerator] Using LAN mode
[ConnectionCodeGenerator] Connection Info: 192.168.X.X:7777
[ConnectionCodeGenerator] Mode: LAN (Direct)
[ConnectionCodeGenerator] Set connectionInfoText
[ConnectionCodeGenerator] Set connectionModeText
[ConnectionCodeGenerator] Activated connectionPanel
[ConnectionCodeGenerator] Hosting - Mode: LAN (Direct), Connection: 192.168.X.X:7777
```

### Step 2: Navigate to RoomScene

Once you're in RoomScene, check:

1. **Console logs:**
   ```
   [NetworkBootstrap] Started hosting in RoomScene
   [ConnectionCodeGenerator] OnServerStarted called!
   ```

2. **Top-right corner:**
   - Dark panel should appear
   - Should show connection info

### Step 3: Check for Warnings

If you see these warnings, there's a problem:

```
❌ [ConnectionCodeGenerator] connectionInfoText is NULL!
❌ [ConnectionCodeGenerator] connectionModeText is NULL!
❌ [ConnectionCodeGenerator] connectionPanel is NULL!
```

**Solution:** The UI references weren't assigned. Check Inspector on ConnectionCodeCanvas.

---

## Expected Behavior

### Before RoomScene Loads:
- ❌ Panel NOT visible (inactive)
- ❌ No connection info shown

### When RoomScene Loads:
1. NetworkBootstrap starts hosting
2. OnServerStarted event fires
3. ConnectionCodeGenerator activates panel
4. ✅ Panel becomes visible
5. ✅ Shows connection info

### In Game View:
```
                    ┌──────────────────┐
                    │ Experimenter     │
                    │ Connection       │
                    │                  │
                    │ Mode:            │
                    │ LAN (Direct)     │
                    │                  │
                    │ Connection       │
                    │ Address:         │
                    │                  │
                    │ 192.168.1.100    │
                    │      :7777       │
                    └──────────────────┘
```

---

## Common Issues & Solutions

### Issue 1: Panel Never Appears

**Check Console for:**
```
[NetworkBootstrap] Started hosting in RoomScene
```

**If missing:**
- NetworkBootstrap might not be in 0-StartMenu scene
- NetworkManager prefab not assigned
- Room scene name wrong (should be "RoomScene")

**Solution:**
- Verify NetworkBootstrap exists in 0-StartMenu
- Check Inspector: Network Manager Prefab is assigned
- Room Scene Name = "RoomScene"

---

### Issue 2: Panel Appears Then Disappears

**Check Console for:**
```
[ConnectionCodeGenerator] Already hosting, skipping...
```

**This is normal** - means the callback was called twice.

**If panel disappears:**
- Another script might be deactivating it
- Scene reload might be happening

---

### Issue 3: NULL Reference Warnings

**Check Console for:**
```
[ConnectionCodeGenerator] connectionInfoText is NULL!
```

**Solution:**
1. Select ConnectionCodeCanvas in RoomScene hierarchy
2. Look at Inspector → ConnectionCodeGenerator component
3. Verify all fields are assigned:
   - Connection Info Text → ConnectionInfoText
   - Connection Mode Text → ConnectionModeText  
   - Connection Panel → ConnectionPanel

**To fix:**
- Drag references from hierarchy to Inspector
- Or delete and recreate using Setup Wizard

---

### Issue 4: "Waiting..." Text Stays

**If you see:**
- Panel visible ✓
- Text still shows "Waiting..." ❌

**Check Console for:**
```
[ConnectionCodeGenerator] Set connectionInfoText
```

**If missing:**
- connectionInfoText reference is null
- Text component doesn't exist

**Solution:**
- Verify ConnectionInfoText GameObject exists in hierarchy
- Has TextMeshProUGUI component
- Reference is assigned in Inspector

---

## Debug Checklist

When testing, verify all these log messages appear:

- [ ] `[NetworkBootstrap] NetworkManager created and persisted`
- [ ] `[NetworkBootstrap] Started hosting in RoomScene`
- [ ] `[ConnectionCodeGenerator] OnServerStarted called!`
- [ ] `[ConnectionCodeGenerator] Using LAN mode`
- [ ] `[ConnectionCodeGenerator] Connection Info: [IP]:[Port]`
- [ ] `[ConnectionCodeGenerator] Set connectionInfoText`
- [ ] `[ConnectionCodeGenerator] Set connectionModeText`
- [ ] `[ConnectionCodeGenerator] Activated connectionPanel`
- [ ] Panel visible in top-right corner

**If ALL appear:** ✅ System working correctly!
**If ANY missing:** ❌ Follow troubleshooting above

---

## Manual Override (For Testing)

If you want to see the panel immediately without hosting:

1. **In RoomScene hierarchy:**
   - Select `/ConnectionCodeCanvas/ConnectionPanel`
   - In Inspector, check the checkbox next to the name (activates it)

2. **Panel should appear immediately**
   - Shows default "Waiting..." text
   - Useful for checking layout/styling

3. **Uncheck when done** (so script can control it)

---

## Advanced Debugging

### Enable Detailed Netcode Logs:

1. Select NetworkManager prefab
2. Inspector → NetworkManager component
3. Set **Log Level** to `Developer` (more detailed)
4. Now Console shows all network events

### Check Network State:

Add this to see network status:
```csharp
Debug.Log($"Is Server: {NetworkManager.Singleton.IsServer}");
Debug.Log($"Is Host: {NetworkManager.Singleton.IsHost}");
Debug.Log($"Is Client: {NetworkManager.Singleton.IsClient}");
```

Should show when in RoomScene:
```
Is Server: True
Is Host: True
Is Client: False
```

---

## What to Report If Still Not Working

If the panel still doesn't appear after following this guide, report:

1. **All Console logs** from startup to RoomScene
2. **Screenshot** of ConnectionCodeCanvas Inspector
3. **Screenshot** of NetworkBootstrap Inspector  
4. **Network State** (Is Server/Host/Client values)
5. **Any warnings or errors** in Console

---

## Quick Fix Script (Emergency)

If nothing works, try this:

**Add to ConnectionCodeGenerator.cs Update():**
```csharp
void Update()
{
    // TEMP DEBUG - Remove after testing
    if (NetworkManager.Singleton != null && 
        NetworkManager.Singleton.IsServer && 
        connectionPanel != null && 
        !connectionPanel.activeSelf)
    {
        Debug.Log("FORCING PANEL ACTIVE");
        connectionPanel.SetActive(true);
        if (connectionInfoText != null)
            connectionInfoText.text = "FORCED VISIBLE";
    }
}
```

This will force the panel visible if hosting. Use only for debugging!

---

## Success Criteria

You'll know it's working when:

✅ Panel appears automatically when entering RoomScene
✅ Shows your local IP address and port  
✅ Console shows all expected log messages
✅ Panel stays visible while in RoomScene
✅ Can share the displayed address with experimenter

**The system is ready when all above are true!**
