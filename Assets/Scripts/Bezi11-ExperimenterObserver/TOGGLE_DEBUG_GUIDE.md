# 🔧 Connection Mode Toggle - Debug Guide

## What Was Fixed

1. ✅ **Toggle reference re-linked** - Ensured it's properly assigned
2. ✅ **Better error logging** - Now shows ERROR if toggle is NULL
3. ✅ **"Creating code..." message** - Shows while waiting for relay code
4. ✅ **Better UI feedback** - Shows error if relay fails

---

## Critical: Check Console on Start

**When you enter RoomScene, immediately check Console:**

### ✅ Good (Toggle Working):
```
[ConnectionCodeGenerator] Start() called
[ConnectionCodeGenerator] Set panel inactive
[ConnectionCodeGenerator] Toggle registered, starting in LAN mode
```

**If you see this:** Toggle is properly linked ✓

### ❌ Bad (Toggle NOT Working):
```
[ConnectionCodeGenerator] Start() called
[ConnectionCodeGenerator] connectionModeToggle is NULL! Toggle will not work!
[ConnectionCodeGenerator] Please assign the toggle reference in the Inspector!
```

**If you see this:** Toggle reference is missing!

**Fix:**
1. Open RoomScene
2. Select `/ConnectionCodeCanvas` GameObject
3. In Inspector, find `ConnectionCodeGenerator` component
4. Look for `Connection Mode Toggle` field
5. If empty, drag `/ConnectionCodeCanvas/ConnectionPanel/ConnectionModeToggle` into it
6. Save scene

---

## Testing: Step-by-Step

### Step 1: Start Game

1. **Start from 0-StartMenu**
2. **Navigate to RoomScene**
3. **IMMEDIATELY check Console**

**Expected:**
```
[ConnectionCodeGenerator] Start() called
[ConnectionCodeGenerator] Toggle registered, starting in LAN mode
```

**If you see "Toggle is NULL":**
- STOP! Toggle reference is missing
- See fix above
- Don't continue until fixed

---

### Step 2: Check Initial State

**Look at ConnectionCodeCanvas panel (top-right):**

```
┌──────────────────────────────────┐
│ Experimenter Connection          │
│                                  │
│ ☐ Use Relay (Internet Mode)     │← Should be UNCHECKED
│                                  │
│ Mode:                            │
│ LAN (Direct)                     │← Should say LAN
│                                  │
│ Connection Address:              │
│ 192.168.X.X:7777                │← Should show IP
└──────────────────────────────────┘
```

**Verify:**
- [ ] Toggle is unchecked
- [ ] Mode says "LAN (Direct)"
- [ ] Connection shows IP address

---

### Step 3: Click Toggle (Switch to Relay)

1. **Click the checkbox** to check it

2. **IMMEDIATELY watch Console:**

**Expected logs (in order):**
```
[ConnectionCodeGenerator] Mode toggled to: Relay
[ConnectionCodeGenerator] Already hosting, restarting to apply new mode...
[ConnectionCodeGenerator] Shutting down current host...
[ConnectionCodeGenerator] Restarting host with new mode...
[RelayConnectionManager] Connection mode set to: Relay
```

**If you DON'T see "Mode toggled to: Relay":**
- ❌ Toggle listener not working!
- ❌ onValueChanged not firing
- Check that toggle reference is assigned
- Check Console for errors

---

### Step 4: Watch "Creating code..." Message

**While relay is setting up, the panel should show:**

```
┌──────────────────────────────────┐
│ Experimenter Connection          │
│                                  │
│ ☑ Use Relay (Internet Mode)     │← CHECKED
│                                  │
│ Mode:                            │
│ Internet (Relay)                 │← Changed to Internet
│                                  │
│ Connection Address:              │
│ Creating code...                 │← SHOWS LOADING MESSAGE
└──────────────────────────────────┘
```

**Expected Console (continuing):**
```
[ConnectionCodeGenerator] Showing 'Creating code...' message
[RelayConnectionManager] Relay allocation created
[RelayConnectionManager] Join Code: ABCD-1234
[ConnectionCodeGenerator] Relay join code received: ABCD-1234
```

---

### Step 5: Verify Final State

**After a few seconds, panel should update to:**

```
┌──────────────────────────────────┐
│ Experimenter Connection          │
│                                  │
│ ☑ Use Relay (Internet Mode)     │← Still checked
│                                  │
│ Mode:                            │
│ Internet (Relay)                 │← Internet mode
│                                  │
│ Connection Address:              │
│ ABCD-1234                        │← JOIN CODE!
└──────────────────────────────────┘
```

**Expected Console (final):**
```
[ConnectionCodeGenerator] StartHost() returned: True
[ConnectionCodeGenerator] OnServerStarted called!
[ConnectionCodeGenerator] Using Relay mode
[ConnectionCodeGenerator] Connection Info: ABCD-1234
[ConnectionCodeGenerator] Mode: Internet (Relay)
[ConnectionCodeGenerator] Set connectionInfoText
[ConnectionCodeGenerator] Set connectionModeText
[ConnectionCodeGenerator] Activated connectionPanel
```

---

## Troubleshooting

### Issue 1: Toggle Click Does Nothing

**Symptom:** Click toggle, nothing happens, no console logs

**Cause:** Toggle reference not assigned

**Check Console for:**
```
[ConnectionCodeGenerator] connectionModeToggle is NULL!
```

**Fix:**
1. Open RoomScene
2. Select ConnectionCodeCanvas GameObject
3. In Inspector → ConnectionCodeGenerator component
4. Assign toggle reference (see above)

---

### Issue 2: "Mode toggled" but Nothing Changes

**Symptom:** See "Mode toggled to: Relay" but UI doesn't update

**Check Console for:**
- Do you see "Shutting down current host..."?
- Do you see "Restarting host with new mode..."?
- Do you see "Relay allocation created"?
- Do you see "Join Code: ..."?

**If missing "Shutting down":**
- Check `isHosting` flag might be false
- Host might not be running
- Check NetworkManager.Singleton exists

**If missing "Relay allocation":**
- Unity Services not set up
- No internet connection
- Relay service not enabled
- See RELAY_SETUP.md

---

### Issue 3: "Failed to get relay join code"

**Symptom:** 
```
[ConnectionCodeGenerator] Failed to get relay join code!
[ConnectionCodeGenerator] Falling back to LAN mode...
```

**Causes:**
1. **Unity Services not set up**
   - Not signed in
   - Project not linked
   - Relay not enabled

2. **No internet connection**
   - Can't reach Unity Services
   - Firewall blocking

3. **Unity Services error**
   - Service down
   - Account issue
   - Rate limited

**Fix:**
- See RELAY_SETUP.md for full setup
- Check internet connection
- Check Unity Services status

**Result of Fallback:**
- Toggle auto-unchecks
- Switches back to LAN mode
- Shows IP address instead

---

### Issue 4: "Creating code..." Stuck Forever

**Symptom:** Message shows "Creating code..." but never changes

**Check Console for errors:**
```
[RelayConnectionManager] Failed to start relay host: [error]
```

**Possible Causes:**
- Relay allocation failed
- Task hung/timed out
- Exception thrown

**Fix:**
- Click toggle again to try again
- Check full Console for error details
- Restart Unity if stuck

---

### Issue 5: Toggle Exists But Wrong GameObject

**Symptom:** Toggle in scene but reference still NULL

**Cause:** Multiple toggles or wrong toggle selected

**Verify:**
1. RoomScene should have ONLY ONE toggle:
   `/ConnectionCodeCanvas/ConnectionPanel/ConnectionModeToggle`

2. Check path is exactly:
   - Parent: `/ConnectionCodeCanvas/ConnectionPanel`
   - Name: `ConnectionModeToggle`
   - Has Toggle component

3. Assign the CORRECT toggle

---

## Complete Expected Console Output

**Full successful toggle to Relay:**

```
[ConnectionCodeGenerator] Mode toggled to: Relay
[ConnectionCodeGenerator] Already hosting, restarting to apply new mode...
[ConnectionCodeGenerator] Shutting down current host...
[ConnectionCodeGenerator] Restarting host with new mode...
[RelayConnectionManager] Connection mode set to: Relay
[ConnectionCodeGenerator] Showing 'Creating code...' message
[RelayConnectionManager] Relay allocation created
[RelayConnectionManager] Join Code: ABCD-1234
[ConnectionCodeGenerator] Relay join code received: ABCD-1234
[ConnectionCodeGenerator] StartHost() returned: True
[ConnectionCodeGenerator] OnServerStarted called!
[ConnectionCodeGenerator] Using Relay mode
[ConnectionCodeGenerator] Connection Info: ABCD-1234
[ConnectionCodeGenerator] Mode: Internet (Relay)
[ConnectionCodeGenerator] Set connectionInfoText
[ConnectionCodeGenerator] Set connectionModeText
[ConnectionCodeGenerator] Activated connectionPanel
[ConnectionCodeGenerator] Hosting - Mode: Internet (Relay), Connection: ABCD-1234
[ParticipantSession] [Network] Started hosting - Mode: Internet (Relay), Connection: ABCD-1234
```

**Key Checkpoints:**
1. ✅ "Mode toggled to: Relay"
2. ✅ "Showing 'Creating code...'"
3. ✅ "Join Code: ABCD-1234"
4. ✅ "Relay join code received"
5. ✅ "StartHost() returned: True"
6. ✅ "OnServerStarted called!"
7. ✅ "Connection Info: ABCD-1234"

**All 7 checkpoints = SUCCESS!** ✅

---

## Quick Verification Checklist

Before testing toggle:

- [ ] Started from 0-StartMenu (not direct RoomScene)
- [ ] Navigated to RoomScene through game flow
- [ ] Console shows "Toggle registered, starting in LAN mode"
- [ ] NO "Toggle is NULL" error
- [ ] Panel visible in top-right corner
- [ ] Toggle visible and unchecked

When clicking toggle:

- [ ] Console shows "Mode toggled to: Relay"
- [ ] UI shows "Creating code..."
- [ ] Console shows "Join Code: ABCD-1234"
- [ ] UI shows join code (not "Creating code...")
- [ ] Toggle stays checked
- [ ] Mode shows "Internet (Relay)"

If ALL checked: ✅ **Working perfectly!**

If ANY unchecked: ❌ **See troubleshooting above**

---

## Manual Fix: Assign Toggle Reference

If toggle reference is NULL:

### Step 1: Open RoomScene

1. `File > Open Scene`
2. Navigate to `/Assets/Scenes/RoomScene.unity`
3. Open it

### Step 2: Select ConnectionCodeCanvas

1. In Hierarchy, find `/ConnectionCodeCanvas`
2. Click to select it

### Step 3: Check Inspector

1. Look at Inspector panel
2. Find `ConnectionCodeGenerator` component
3. Look for `Connection Mode Toggle` field

### Step 4: Assign Toggle

**If field is empty:**

1. In Hierarchy, navigate to:
   `/ConnectionCodeCanvas/ConnectionPanel/ConnectionModeToggle`
2. Drag this GameObject into the `Connection Mode Toggle` field

**Or:**

1. Click the circle icon next to the field
2. In the popup, search for "ConnectionModeToggle"
3. Select it

### Step 5: Save

1. `File > Save` or Ctrl+S
2. Done!

---

## Summary

**Fixed:**
- ✅ Toggle reference re-linked
- ✅ Better error detection
- ✅ "Creating code..." loading message
- ✅ Error feedback if relay fails

**To Test:**
1. Check Console on start for "Toggle registered"
2. If you see "Toggle is NULL", assign reference
3. Click toggle
4. Watch Console for "Mode toggled to: Relay"
5. See "Creating code..." then join code

**Expected Result:**
- Toggle click → "Creating code..." → Join code appears ✓

**If not working:**
- Check Console for specific error
- See troubleshooting section above
- Verify toggle reference is assigned

---

**Try it now and watch the Console carefully!** 🔍
