# ⚠️ CRITICAL: Complete Step 2 - NetworkManager Configuration

## Problem Detected

Your **NetworkManager prefab is missing the NetworkSessionManager** in its Network Prefabs list. This will cause runtime errors when the system tries to spawn networked objects.

## ✅ How to Fix (2 minutes)

### Method 1: Using Unity Inspector (Recommended)

1. **Select the NetworkManager prefab:**
   - Go to `/Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkManager.prefab`
   - Click on it (you already have it selected!)

2. **Open the Network Prefabs List:**
   - In the Inspector, find the **NetworkManager** component
   - Scroll down to **Network Config** section
   - Find **Network Prefabs List** → click the dropdown arrow

3. **Add NetworkSessionManager:**
   - You'll see it references `/Assets/DefaultNetworkPrefabs.asset`
   - Click on this asset to open it
   - In the asset Inspector, find the **Prefab List** array
   - Click the **+** button to add a new element
   - Drag `/Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkSessionManager.prefab` into the new slot

4. **Save:**
   - Press `Ctrl+S` or `Cmd+S` to save
   - Done!

### Method 2: Create New Network Prefabs List Asset (Alternative)

If the `DefaultNetworkPrefabs.asset` doesn't exist or you want a clean setup:

1. **Create new NetworkPrefabsList asset:**
   - In Unity, go to `/Assets/Prefabs/Bezi11-ExperimenterObserver/`
   - Right-click > `Create > Netcode > Network Prefabs List`
   - Name it: `Bezi11NetworkPrefabs`

2. **Add NetworkSessionManager to the list:**
   - Select the new `Bezi11NetworkPrefabs` asset
   - In Inspector, expand **Prefab List**
   - Click **+** to add element
   - Drag `NetworkSessionManager.prefab` into the slot

3. **Assign to NetworkManager:**
   - Select `NetworkManager.prefab`
   - In Inspector, find **NetworkManager** component
   - Under **Network Config** > **Network Prefabs List**
   - Click the circular icon next to **Network Prefabs Lists**
   - In the popup, click **+** to add an element
   - Drag the `Bezi11NetworkPrefabs` asset into this slot

4. **Save** the prefab

---

## ✅ Verification

After completing the fix, verify:

1. Select `NetworkManager.prefab`
2. In Inspector, expand **Network Config** > **Network Prefabs List**
3. You should see **at least one** Network Prefabs List asset
4. Inside that asset, the **Prefab List** should contain `NetworkSessionManager.prefab`

---

## Why This is Important

Without NetworkSessionManager in the Network Prefabs list:
- ❌ NetworkSessionManager won't spawn on clients
- ❌ Session data won't sync (Participant ID, Scene, Mode)
- ❌ Observer UI will show "N/A" for all metadata
- ❌ Runtime errors when trying to sync data

With it properly configured:
- ✅ NetworkSessionManager spawns automatically
- ✅ Session data syncs in real-time
- ✅ Observer sees live updates
- ✅ No runtime errors

---

## Other Missing Folders (Optional)

You may also want to create these folders for organization:

### Create Scenes Folder (Optional):
- Right-click in `/Assets/Scenes/`
- `Create > Folder`
- Name: `Bezi11-ExperimenterObserver`
- This is where you'll create the ExperimenterClientScene

### Create UI Folder (Optional):
- Right-click in `/Assets/UI/`
- `Create > Folder`  
- Name: `Bezi11-ExperimenterObserver`
- Store UI prefabs here

---

## Quick Visual Guide

```
NetworkManager.prefab
└── NetworkManager (component)
    └── Network Config
        └── Network Prefabs List
            └── Network Prefabs Lists [Array]
                └── Element 0: [DefaultNetworkPrefabs or Bezi11NetworkPrefabs]
                    └── Prefab List [Array]
                        └── Element 0: ✅ NetworkSessionManager.prefab ← ADD THIS!
```

---

## Next Steps After This Fix

Once NetworkSessionManager is added to the network prefabs list:

1. ✅ Proceed to Step 3: Add PlayerCameraStreamer to rig prefabs
2. ✅ Step 4: Create NetworkBootstrap in 0-StartMenu scene
3. ✅ Step 5: Create connection UI in RoomScene
4. ✅ Step 6: Create ExperimenterClientScene

---

## Need Help?

If you can't find `DefaultNetworkPrefabs.asset`:
- Use **Method 2** above to create a new NetworkPrefabsList
- This is cleaner and keeps Bezi11 components isolated

The NetworkManager is already selected in your Unity Editor - just follow the steps above! 🚀
