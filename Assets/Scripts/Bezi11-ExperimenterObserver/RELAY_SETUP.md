# Internet Connectivity Setup Guide (Unity Relay)

## Overview

Your Experimenter Observer System now supports **two connection modes**:

### 🏠 LAN Mode (Default - No Additional Setup)
- **Use case:** Two PCs on the same local network
- **Requirements:** None (already working)
- **Connection:** Direct IP address (e.g., `192.168.1.100:7777`)

### 🌐 Internet Mode (Relay - Requires Setup)
- **Use case:** Two PCs on different networks (over the internet)
- **Requirements:** Unity Services packages + Unity account
- **Connection:** Join code (e.g., `ABC123`)

---

## Enable Internet Connectivity (Optional)

### Step 1: Install Unity Services Packages

Open Package Manager (`Window > Package Manager`) and install these packages:

1. **Unity Services Core**
   - Click `+` > `Add package by name...`
   - Enter: `com.unity.services.core`
   - Click `Add`

2. **Unity Authentication**
   - Click `+` > `Add package by name...`
   - Enter: `com.unity.services.authentication`
   - Click `Add`

3. **Unity Relay**
   - Click `+` > `Add package by name...`
   - Enter: `com.unity.services.relay`
   - Click `Add`

### Step 2: Link Unity Project to Cloud

1. Go to `Edit > Project Settings > Services`
2. Click **"Create Unity Project ID"** (or select existing one)
3. Sign in with your Unity account
4. Your project is now linked!

### Step 3: Enable Relay Service (Free)

1. Go to [Unity Dashboard](https://dashboard.unity3d.com/)
2. Select your project
3. Navigate to **Multiplayer > Relay**
4. Click **"Set up Relay"**
5. Choose **Free tier** (100 concurrent users - more than enough for research)

### Step 4: Create RelayConnectionManager Prefab

1. Create empty GameObject: `RelayConnectionManager`
2. Add component: `RelayConnectionManager`
3. Inspector settings:
   - ✅ **Use Relay For Internet Connection** (checked for internet, unchecked for LAN only)
4. Save as prefab: `/Assets/Prefabs/Bezi11-ExperimenterObserver/RelayConnectionManager.prefab`

### Step 5: Update NetworkBootstrap

1. Open `0-StartMenu` scene
2. Select the `NetworkBootstrap` GameObject
3. In Inspector:
   - Assign `RelayConnectionManager` prefab to the **Relay Manager Prefab** field
4. Save scene

---

## How To Use

### For LAN Connections (Same Network)

**Player PC:**
1. Run game normally
2. Connection panel shows: `192.168.1.100:7777`

**Experimenter PC:**
1. Open ExperimenterClientScene
2. **Uncheck** "Use Relay" toggle
3. Enter IP from player: `192.168.1.100:7777`
4. Click Connect

### For Internet Connections (Different Networks)

**Player PC:**
1. In RelayConnectionManager prefab, enable "Use Relay For Internet Connection"
2. Run game normally
3. Connection panel shows: `ABC123` (6-character join code)

**Experimenter PC:**
1. Open ExperimenterClientScene
2. **Check** "Use Relay" toggle
3. Enter join code: `ABC123`
4. Click Connect

---

## Architecture

### LAN Mode (Direct Connection)
```
Player PC ←---[Local Network]---→ Experimenter PC
   Host                             Client
```

### Internet Mode (Relay)
```
Player PC ←---[Unity Relay Servers]---→ Experimenter PC
   Host        (Free, Automatic)          Client
```

Unity Relay acts as a middleman:
- No port forwarding required
- No firewall configuration needed
- Works across NAT/firewalls
- Free up to 100 concurrent connections

---

## Benefits of Relay

✅ **No Network Configuration:** Works automatically across any network  
✅ **No Firewall Issues:** Unity handles NAT traversal  
✅ **Secure:** No exposing your local network  
✅ **Simple:** Just share a 6-character code  
✅ **Free:** Generous free tier for research use  

---

## Troubleshooting

### "Unity Services packages not installed" Warning

Install the three packages listed in Step 1.

### "Failed to initialize Unity Services"

1. Check internet connection
2. Verify project is linked in `Edit > Project Settings > Services`
3. Ensure Relay is enabled in Unity Dashboard

### "Failed to create relay allocation"

1. Verify Relay is enabled in Unity Dashboard
2. Check you haven't exceeded free tier limits (100 concurrent users)
3. Try signing out and back in: `AuthenticationService.Instance.SignOut()`

### Connection Fails in Relay Mode

1. Ensure both PCs have internet connection
2. Verify join code was entered correctly (case-sensitive)
3. Check Unity Console for detailed error messages
4. Try LAN mode first to verify basic system works

---

## Cost & Limits (Free Tier)

- **Concurrent Users:** 100
- **Bandwidth:** Unlimited for free tier
- **Data Transfer:** No limits for relay traffic
- **Perfect for:** Research labs, small studies

For larger studies, Unity offers paid tiers with higher limits.

---

## Technical Details

### How It Works

1. Player calls `StartHostWithRelay()` → Gets join code
2. Unity Relay allocates relay server
3. Player shares join code with experimenter
4. Experimenter calls `JoinWithRelay(code)` → Connects
5. All traffic routes through Unity's relay servers
6. Camera stream + session data transmitted normally

### Performance

- **Latency:** +20-50ms compared to direct LAN (usually imperceptible)
- **Bandwidth:** Same as LAN mode (JPEG compressed stream)
- **Quality:** No degradation in stream quality

---

## Disable Internet Mode (LAN Only)

If you only need LAN connectivity:

1. Don't install Unity Services packages
2. Don't assign RelayConnectionManager prefab in NetworkBootstrap
3. System will automatically use LAN-only mode

The relay code won't compile without packages installed, but everything else works perfectly!

---

## Next Steps

1. Install packages (Step 1-3) if you need internet connectivity
2. Create and configure RelayConnectionManager prefab (Step 4)
3. Update NetworkBootstrap (Step 5)
4. Test both LAN and Internet modes
5. Choose which mode to use for your research deployment

---

## Support Resources

- [Unity Relay Documentation](https://docs.unity.com/relay/)
- [Unity Netcode for GameObjects](https://docs-multiplayer.unity3d.com/)
- [Unity Dashboard](https://dashboard.unity3d.com/)
