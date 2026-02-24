# ✅ Experimenter Mode Button - ADDED!

## What Was Created

I've successfully added an "Experimenter Mode" button to the **0-StartMenu** scene that instantly switches to the ExperimenterClientScene with a single mouse click!

---

## 📍 Button Location

**Scene:** `/Assets/Scenes/0-StartMenu.unity`
**Position:** Bottom right corner (20px from edges)

```
┌──────────────────────────────────────────────┐
│                                              │
│         Start Menu UI                        │
│                                              │
│                                              │
│                                              │
│                                              │
│                            [Experimenter Mode]│
└──────────────────────────────────────────────┘
```

---

## 🎨 Button Design

**Visual Properties:**
- **Size:** 200×50 pixels
- **Color:** Blue (RGB: 0.2, 0.4, 0.8)
- **Text:** "Experimenter Mode" (Bold, 18pt, White)
- **Position:** Anchored to bottom-right corner

**Interaction:**
- **Normal:** Blue
- **Hover:** Lighter blue (RGB: 0.3, 0.5, 1)
- **Pressed:** Darker blue (RGB: 0.15, 0.3, 0.6)
- **Mouse Click:** Switches to ExperimenterClientScene

---

## 🔧 What Was Created

### 1. SceneSwitcher Script
**File:** `/Assets/Scripts/Bezi11-ExperimenterObserver/SceneSwitcher.cs`

**Purpose:** Simple utility script to load the ExperimenterClientScene

**Key Features:**
- Configurable target scene name
- Button click listener
- Debug logging
- Proper cleanup on destroy

**Code Overview:**
```csharp
public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "ExperimenterClientScene";
    [SerializeField] private Button switchButton;

    public void SwitchToExperimenterScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
```

### 2. UI Button in 0-StartMenu
**Hierarchy:**
```
0-StartMenu
└── SessionRoot
    └── Canvas
        └── ExperimenterModeButton ← NEW!
            └── Text
```

**Components:**
- RectTransform (anchored to bottom-right)
- Image (blue background)
- Button (with color transitions)
- TextMeshProUGUI ("Experimenter Mode")

### 3. SceneSwitcher GameObject
**Hierarchy:**
```
0-StartMenu
└── SceneSwitcher ← NEW!
    └── SceneSwitcher script
```

**Configuration:**
- ✅ Target Scene: "ExperimenterClientScene"
- ✅ Switch Button: Reference to ExperimenterModeButton

---

## ✅ All References Linked

**SceneSwitcher Component:**
- ✅ `targetSceneName` → "ExperimenterClientScene"
- ✅ `switchButton` → ExperimenterModeButton (Button)

**Button OnClick Event:**
- Automatically wired in SceneSwitcher.Start()
- Calls `SwitchToExperimenterScene()` on click

---

## 🚀 How It Works

### User Flow:

1. **Game starts** → 0-StartMenu scene loads
2. **User sees button** → Bottom right corner: "Experimenter Mode"
3. **User clicks button** → SceneSwitcher.SwitchToExperimenterScene() called
4. **Scene loads** → ExperimenterClientScene opens immediately
5. **Experimenter can connect** → To participant's session

### Technical Flow:

```
User Click
  ↓
Button.onClick event
  ↓
SceneSwitcher.SwitchToExperimenterScene()
  ↓
SceneManager.LoadScene("ExperimenterClientScene")
  ↓
ExperimenterClientScene loads
  ↓
Ready to connect to participant!
```

---

## 🎯 Use Cases

### For Experimenters:
1. Launch the game
2. Click "Experimenter Mode" (no participant ID needed)
3. Enter Play Mode immediately
4. Connect to participant's session

### For Development:
- Quick access to ExperimenterClientScene
- No need to manually open scene in Unity
- Can test from play mode directly

### For Production:
- Separate experimenter PC can use this button
- No need for separate builds
- One executable for both roles

---

## ⚠️ IMPORTANT: Add Scene to Build Settings

For the button to work, **ExperimenterClientScene MUST be in Build Settings**:

### How to Add:

1. **Open Build Settings:**
   - `File > Build Settings` (Ctrl+Shift+B)

2. **Add Scene:**
   - Click "Add Open Scenes"
   - Or drag `/Assets/Scenes/ExperimenterClientScene.unity` into the list

3. **Verify:**
   - ExperimenterClientScene should appear in "Scenes In Build"
   - It will have a build index (e.g., index 2)

### Current Expected Build Order:
```
0: 0-StartMenu ✓
1: RoomScene ✓
2: ExperimenterClientScene ← ADD THIS!
... (other scenes)
```

**Without this step, clicking the button will fail with:**
```
Scene 'ExperimenterClientScene' couldn't be loaded because it has not been added to the build settings
```

---

## 🧪 How to Test

### Test 1: Button Appearance

1. **Open 0-StartMenu scene**
2. **Check Scene view:**
   - Button visible at bottom right? ✓
   - Text reads "Experimenter Mode"? ✓
   - Blue color? ✓

### Test 2: Button Interaction (Edit Mode)

1. **In Scene view:**
   - Hover over button → Should highlight (lighter blue)
   - Click won't work in Edit mode (expected)

### Test 3: Full Flow (Play Mode)

1. **Add ExperimenterClientScene to Build Settings** (see above)
2. **Open 0-StartMenu scene**
3. **Enter Play Mode**
4. **Look at bottom right corner:**
   - Button should be visible
5. **Hover mouse over button:**
   - Should highlight (lighter blue)
6. **Click button:**
   - Console shows: `[SceneSwitcher] Loading scene: ExperimenterClientScene`
   - Scene switches immediately
   - ExperimenterClientScene loads
   - Connection UI appears

### Test 4: Verify Scene Loads

After clicking button:
- ✅ ExperimenterClientScene loads
- ✅ Canvas visible with 3 panels
- ✅ Connection input ready
- ✅ Session info shows N/A
- ✅ Can enter connection info and connect

---

## 🎨 Visual Design Details

### Button Positioning:
```csharp
Anchor: Bottom-Right (1, 0)
Position: (-20, 20) from corner
Size: 200×50 pixels
```

### Why Bottom Right?
- ✅ Out of the way of main menu UI
- ✅ Easy to find for experimenters
- ✅ Consistent with common UI patterns
- ✅ Accessible with mouse click
- ✅ Won't interfere with VR/Desktop selection

### Color Scheme:
| State | Color | RGB |
|-------|-------|-----|
| Normal | Blue | 0.2, 0.4, 0.8 |
| Hover | Light Blue | 0.3, 0.5, 1.0 |
| Pressed | Dark Blue | 0.15, 0.3, 0.6 |
| Disabled | Gray | 0.5, 0.5, 0.5 |

---

## 📋 Scene Hierarchy Update

**Before:**
```
0-StartMenu
├── Main Camera
├── Directional Light
├── SessionRoot
│   └── Canvas
│       ├── UI elements...
├── EventSystem
├── ParticipantSession
└── NetworkBootstrap
```

**After:**
```
0-StartMenu
├── Main Camera
├── Directional Light
├── SessionRoot
│   └── Canvas
│       ├── UI elements...
│       └── ExperimenterModeButton ← NEW!
│           └── Text
├── EventSystem
├── ParticipantSession
├── NetworkBootstrap
└── SceneSwitcher ← NEW!
```

---

## 🔄 Integration with Existing Systems

### Does NOT Interfere With:
- ✅ Participant ID entry
- ✅ Save button
- ✅ Normal game flow
- ✅ VR/Desktop mode selection
- ✅ NetworkBootstrap
- ✅ ParticipantSession

### Works Alongside:
- ✅ Normal start menu flow
- ✅ Network hosting
- ✅ Scene transitions
- ✅ All existing UI

### Experimenter-Specific:
- 🔵 Only for experimenters who want to observe
- 🔵 Bypasses participant ID requirement
- 🔵 Direct path to observer mode
- 🔵 No impact on participant experience

---

## 💡 Alternative Access Methods

### Method 1: Button in Start Menu (Current)
- ✅ **Implemented**
- User clicks "Experimenter Mode" button
- Fastest method

### Method 2: Keyboard Shortcut (Optional)
- Could add hotkey (e.g., F10)
- Modify SceneSwitcher to listen for key press
- Good for development

### Method 3: Scene Menu (Manual)
- File > Open Scene > ExperimenterClientScene
- Slowest but always works
- No build settings required

**Recommendation:** Use the button (Method 1) - it's instant and user-friendly!

---

## 🎓 For Developers

### To Change Button Position:
Modify in SceneSwitcher setup or Unity Inspector:
```csharp
anchoredPosition: { x: -20, y: 20 }  // Distance from bottom-right corner
```

### To Change Button Text:
Find `ExperimenterModeButton/Text` in hierarchy, change `text` property.

### To Change Target Scene:
Modify `SceneSwitcher` component → `Target Scene Name` field.

### To Add Keyboard Shortcut:
Update `SceneSwitcher.cs`:
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.F10))
    {
        SwitchToExperimenterScene();
    }
}
```

---

## ✅ Checklist

Setup:
- [x] SceneSwitcher script created
- [x] Button added to 0-StartMenu
- [x] Button positioned bottom right
- [x] Text set to "Experimenter Mode"
- [x] SceneSwitcher GameObject created
- [x] All references linked
- [ ] **ExperimenterClientScene added to Build Settings** ← YOU MUST DO THIS!

Testing:
- [ ] Open 0-StartMenu in Unity
- [ ] See button at bottom right
- [ ] Add ExperimenterClientScene to Build Settings
- [ ] Enter Play Mode
- [ ] Click button
- [ ] Verify scene loads
- [ ] Test connection to participant

---

## 📊 Summary

**Created:**
- ✅ SceneSwitcher.cs script
- ✅ ExperimenterModeButton UI (200×50, blue, bottom-right)
- ✅ SceneSwitcher GameObject with script
- ✅ All references linked automatically

**Features:**
- ✅ One-click access to ExperimenterClientScene
- ✅ Mouse-clickable button
- ✅ Bottom-right corner placement
- ✅ Professional blue styling
- ✅ Hover/press feedback
- ✅ No interference with existing UI

**Next Step:**
- ⚠️ **Add ExperimenterClientScene to Build Settings!**

---

## 🎯 Result

**Experimenters can now:**
1. Start the game
2. Click "Experimenter Mode" at bottom right
3. Instantly open ExperimenterClientScene
4. Connect to participant's session

**No more manual scene switching! Just one click!** 🎉

---

## 🐛 Troubleshooting

### Button not visible in Play Mode?
- Check SessionRoot/Canvas is active
- Verify button is child of Canvas
- Check RectTransform anchoring

### Button click does nothing?
- **Most likely:** ExperimenterClientScene not in Build Settings
- Check Console for error messages
- Verify SceneSwitcher has button reference

### Scene not found error?
```
Scene 'ExperimenterClientScene' couldn't be loaded...
```
**Fix:** Add ExperimenterClientScene to Build Settings (File > Build Settings > Add Open Scenes)

### Button in wrong position?
- Check RectTransform:
  - Anchor: (1, 0) = bottom right
  - Position: (-20, 20) = 20px from edges
  - Pivot: (1, 0) = bottom right

---

**The button is ready to use! Just add ExperimenterClientScene to Build Settings and test!** 🚀
