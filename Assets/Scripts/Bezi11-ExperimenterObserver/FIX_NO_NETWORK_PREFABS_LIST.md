# 🔧 Fix: No Network Prefabs Lists in NetworkManager

## Your Situation

You don't have a "Network Prefabs Lists" array configured in your NetworkManager. This is normal - we just need to create and configure it manually.

## ✅ Complete Step-by-Step Fix (3 minutes)

### Step 1: Create the Network Prefabs List Asset

1. In the **Project window**, navigate to:
   ```
   Assets/Prefabs/Bezi11-ExperimenterObserver/
   ```

2. **Right-click** in the folder → Select:
   ```
   Create > Netcode > Network Prefabs List
   ```

3. **Name it:**
   ```
   Bezi11NetworkPrefabs
   ```

4. **Select the asset** you just created

5. In the **Inspector**, you'll see:
   - `Is Default` checkbox (leave unchecked)
   - `List` array (expand it)

6. Click the **+** button to add an element

7. **Drag** the `NetworkSessionManager.prefab` into the **Prefab** field
   - It's in the same folder: `/Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkSessionManager.prefab`

8. Make sure **Override** is set to `None`

9. **Save** (`Ctrl+S` or `Cmd+S`)

---

### Step 2: Add the List to NetworkManager

1. **Select** the `NetworkManager.prefab`:
   ```
   Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkManager.prefab
   ```
   (You probably already have this selected!)

2. In the **Inspector**, find the **NetworkManager** component

3. Expand the **Network Config** section

4. Find **Prefabs** section and expand it

5. You'll see **Network Prefabs Lists** array:
   - If it shows `Size: 0`, click on the number and change it to `1`
   - Or click the **+** button to add an element

6. **Drag** the `Bezi11NetworkPrefabs` asset into **Element 0**

7. **Save** the prefab (`Ctrl+S` or `Cmd+S`)

---

## ✅ Visual Guide

### What You're Creating:

```
📁 Bezi11-ExperimenterObserver/
  ├── NetworkManager.prefab
  ├── NetworkSessionManager.prefab
  └── Bezi11NetworkPrefabs.asset ← CREATE THIS
```

### Final Structure:

```
NetworkManager.prefab
└── NetworkManager (Component)
    └── Network Config
        └── Prefabs
            └── Network Prefabs Lists [Size: 1]
                └── Element 0: Bezi11NetworkPrefabs ← DRAG HERE
```

```
Bezi11NetworkPrefabs.asset
└── List [Size: 1]
    └── Element 0
        └── Prefab: NetworkSessionManager.prefab ← DRAG HERE
        └── Override: None
```

---

## 🎯 Verification Checklist

After completing both steps, verify:

- [ ] `Bezi11NetworkPrefabs.asset` exists in `/Assets/Prefabs/Bezi11-ExperimenterObserver/`
- [ ] When you select it, the **List** array has 1 element with `NetworkSessionManager.prefab`
- [ ] `NetworkManager.prefab` → **NetworkManager** component → **Network Config** → **Prefabs** → **Network Prefabs Lists** has 1 element
- [ ] That element references `Bezi11NetworkPrefabs`

---

## 📸 What It Should Look Like

### Bezi11NetworkPrefabs.asset Inspector:
```
Network Prefabs List (Script)
  Is Default: ☐ (unchecked)
  
  List
    Size: 1
    ▼ Element 0
      Override: None
      Prefab: NetworkSessionManager
      (other fields can be empty/None)
```

### NetworkManager.prefab Inspector:
```
Network Manager (Script)
  ▼ Network Config
    Protocol Version: 0
    Network Transport: UnityTransport
    Player Prefab: None
    
    ▼ Prefabs
      ▼ Network Prefabs Lists
        Size: 1
        Element 0: Bezi11NetworkPrefabs ← Should show here!
    
    Tick Rate: 30
    ...
```

---

## ❓ Troubleshooting

### "I don't see 'Create > Netcode' option"

**Solution:** Make sure Unity Netcode for GameObjects is installed:
- Window > Package Manager
- Search for "Netcode for GameObjects"
- It should show version 1.15.1 (installed)

### "I can't find the Network Prefabs Lists array"

**Solution:** 
1. Select NetworkManager.prefab
2. Look for the **NetworkManager** component (not Transform)
3. Scroll down to **Network Config**
4. Expand it
5. Look for **Prefabs** section (NOT "Prefab" singular)
6. **Network Prefabs Lists** is inside the **Prefabs** section

### "The array size is 0 and I can't change it"

**Solution:**
1. Click on the "0" number next to "Size"
2. Type "1" and press Enter
3. Or click the tiny "+" icon at the bottom of the array section

---

## 🚀 After Completing This

Once NetworkSessionManager is properly added to the Network Prefabs List:

✅ **Your system will be able to:**
- Spawn NetworkSessionManager on connected clients
- Sync participant ID, scene name, and game mode
- Display real-time session metadata on observer PC
- Properly network all session data

✅ **Next steps:**
1. Add PlayerCameraStreamer to rig prefabs
2. Create NetworkBootstrap in 0-StartMenu scene
3. Create ExperimenterClientScene
4. Test the connection!

---

## 💡 Why This Step Is Critical

Unity Netcode requires all networked prefabs to be registered in the NetworkManager's prefabs list **before** the network starts. Without this:

- NetworkSessionManager won't spawn ❌
- Session data won't sync ❌
- Observer will see "N/A" everywhere ❌
- Console will show errors ❌

With proper configuration:

- Everything spawns correctly ✅
- Real-time data sync works ✅
- Observer sees live updates ✅
- No errors ✅

---

**This is the most important manual step - take your time and follow the guide carefully!**
