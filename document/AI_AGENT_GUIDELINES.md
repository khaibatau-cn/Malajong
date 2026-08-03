# 🤖 MALAJONG — AI Agent Engineering & Contribution Guidelines

> **Audience:** AI Coding Assistants (Antigravity, Claude, Gemini, GPT-4, Codex, etc.)  
> **Workspace Context:** Unity 6 (6000.0.10f1+) 2D URP Roguelike Deckbuilder  
> **Repository:** `Malajong` (Branch: `ui/test1` / `main`)  

---

## 1. Golden Rules of the Codebase

When modifying or expanding **Malajong**, every AI Agent **MUST** adhere to the following 5 golden rules:

### 1.1 Preserve ScriptableObject Separation (No Hardcoded Gameplay Data)
* All tile properties, spirit definitions, and blind parameters live as **ScriptableObjects** under `Assets/ScriptableObjects/`.
* **DO NOT** hardcode tile lists, spirit costs, or blind quotas inside `MonoBehaviour` scripts.
* When adding new game content, provide both the C# logic class and an automated editor generator in `TileDataGenerator.cs` or `SceneSetupTool.cs`.

### 1.2 The LayoutGroup Anti-Jitter Rule (CRITICAL)
* **NEVER** call `transform.SetAsLastSibling()` inside `TileUI.cs` during `OnPointerEnter`, `OnPointerExit`, or `Update()`.
* **Why:** Calling `SetAsLastSibling()` while childed to a `HorizontalLayoutGroup` forces immediate layout recalculation, invalidating mouse hover state and causing continuous high-frequency visual jitter.
* **Solution:** Tile hover and selection lifting must be purely visual transformations via `transform.localPosition = Vector3.Lerp(...)` or by moving an inner `CardVisual` container.

### 1.3 TextMeshPro Font Asset Protocol
* The project uses the **`m5x7`** pixel font. TextMeshPro components (`TextMeshProUGUI`) require a `TMP_FontAsset`, not a raw Unity `Font`.
* Font assets live in `Assets/Fonts/m5x7.ttf` and `Assets/Fonts/m5x7_FontAsset.asset`.
* Always load/assign font via `SceneSetupTool.GetOrCreatePixelFont()` when creating UI elements via script.
* **Point Size Rule:** Because `m5x7` is a pixel font, standard 14–16px sizes appear microscopic on a 1080p canvas. Use **24px–32px** for standard body/buttons and **40px–54px** for numbers and headers. Do **not** use shrinking tags like `<size=60%>`.

### 1.4 Unity 6 Input System Compatibility
* Malajong supports the **New Input System** (`com.unity.inputsystem`).
* When creating or configuring an `EventSystem`, always check for `UnityEngine.InputSystem.UI.InputSystemUIInputModule` and ensure `StandaloneInputModule` is replaced if the New Input System is active.

### 1.5 Preserve File Structure Integrity
* Do not scatter new assets into the root `Assets/` or `Assets/Sprites/` directory.
* Follow the established directory layout:
  - `Assets/Fonts/`: Font TTFs and `TMP_FontAsset` objects.
  - `Assets/Sprites/Tilesets/Blueeyedrat/`: Primary tile sprite sheets and sliced textures.
  - `Assets/Sprites/RawSource/`: Source `.aseprite` and raw assets.
  - `Assets/Script/Core/`, `Artifacts/`, `Roguelike/`, `UI/`, `Editor/`.

---

## 2. Standard Recipes for Common AI Tasks

### 🍳 Recipe 1: Adding a New Spirit (Artifact / Joker)

1. **Create the Concrete Spirit Class** in `Assets/Script/Artifacts/`:
   ```csharp
   using UnityEngine;

   public class ExampleSpirit : Spirit
   {
       public override void OnComboPlayed(Combo combo, ref int chips, ref float mult, SuitAffinity affinity, GameManager run)
       {
           if (combo is Pong)
           {
               chips += 30; // Adds +30 Chips on any Pong
           }
       }
   }
   ```
2. **Register in `TileDataGenerator.cs`**:
   Add the new item to `GenerateDefaultSpirits()` with name, description, cost, and rarity.
3. **Run One-Click Scene Setup**:
   Call `SceneSetupTool.SetupPlayableScene()` to ensure it automatically populates in the Shop catalog.

---

### 🍳 Recipe 2: Adding a New Yaku / Combo Validator

1. **Implement `Combo` Subclass** in `Assets/Script/Core/Combo.cs`:
   ```csharp
   public class TripleChow : Combo
   {
       public override int BaseChips => 45;
       public override float BaseMult => 3.5f;
       public override float AffinityBonus => 0.3f;

       public TripleChow(List<Tile> tiles) : base("Triple Chow", tiles) { }

       public override bool IsValid()
       {
           // Validation logic here
           return Tiles != null && Tiles.Count == 9;
       }
   }
   ```
2. **Register in `ScoreEngine.FindPlayableCombos()`**:
   Add detection logic so the UI's `PlayableCombosText` highlights it when tiles in the hand form this combo.

---

### 🍳 Recipe 3: Updating the UI Layout or Adding a New HUD Element

1. **Modify `SceneSetupTool.cs`**:
   - Add the element creation inside `SceneSetupTool.SetupPlayableScene()`.
   - Set anchors (`anchorMin`, `anchorMax`), pivot, and size deltas.
   - Use `CreateText(...)` or `CreateButton(...)` to ensure the pixel font and button styling are automatically bound.
2. **Wire to `UIManager.cs`**:
   - Add the `[SerializeField] public TextMeshProUGUI MyNewText;` field on `UIManager`.
   - Assign the reference inside `SceneSetupTool.cs` (`uiManager.MyNewText = myNewText;`).
   - Update its state dynamically inside `UIManager.UpdateHUD()`.

---

## 3. Automation Checklist for AI Submissions

Before finishing any user request, verify:
- [ ] Code compiles cleanly with zero C# warnings in Unity 6.
- [ ] Any new sprite or font asset paths match the clean folder structure.
- [ ] Sliced sprites and `TMP_FontAsset` references are valid.
- [ ] `git status` shows clean, tracked changes without leftover `.tmp` or untracked trash files.
- [ ] Changes are committed with standard conventional commit messages (`feat:`, `fix:`, `refactor:`, `style:`).
