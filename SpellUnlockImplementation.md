---
marp: true
theme: default
paginate: true
---

# Spell Unlock System — Implementation Plan
### Unlocking Spells by Defeating Enemies at Map Locations

---

## Overview

When a player defeats an enemy at a map location, a new spell is unlocked and saved. The spellbook UI dynamically shows only the spells the player has unlocked so far. This builds on the existing `MapProgressStorage`, `SpellBook`, and `Spell` systems already in place.

---

## How It Fits Into Existing Code

| Existing System | Role in This Feature |
|---|---|
| `MapProgressStorage` | Already saves completed location IDs — extend it to also save unlocked spell names |
| `Spell` (ScriptableObject) | Each spell already has a `spellName` — use this as the unlock key |
| `SpellBook` | Already holds all entries — filter it to only show unlocked spells |
| `MatchManager` | Detects when the enemy dies — this is where you trigger the unlock |
| `MapLocation` | Already knows its `locationId` — add a field for which spell it awards |

---

## Step 1 — Add a Spell Reward to Each Map Location

In [MapLocation.cs](Assets/Scripts/MapLocation.cs), add one new field:

```csharp
[Header("Spell Reward")]
public Spell spellReward; // drag the Spell ScriptableObject here in Inspector
```

In the Unity Inspector, for each location on the map, drag the corresponding `Spell` ScriptableObject into the **Spell Reward** slot.

---

## Step 2 — Extend MapProgressStorage to Save Unlocked Spells

In [MapProgressStorage.cs](Assets/Scripts/MapProgressStorage.cs), extend `MapProgressData` to also store unlocked spell names:

```csharp
[Serializable]
public class MapProgressData
{
    public List<string> completedLocationIds = new List<string>();
    public List<string> unlockedSpellNames = new List<string>(); // NEW
}
```

Add a new static method to `MapProgressStorage`:

```csharp
public static void UnlockSpell(string spellName)
{
    if (string.IsNullOrWhiteSpace(spellName)) return;

    MapProgressData data = Load();

    if (!data.unlockedSpellNames.Contains(spellName))
    {
        data.unlockedSpellNames.Add(spellName);
        Save(data);
    }
}

public static List<string> GetUnlockedSpells()
{
    return Load().unlockedSpellNames;
}
```

---

## Step 3 — Trigger the Unlock When the Enemy Dies

In [MatchManager.cs](Assets/Scripts/MatchManager.cs), find where the enemy death is handled. Add a reference to the current map location and call the unlock there:

```csharp
[Header("Spell Unlock")]
[SerializeField] private MapLocation currentMapLocation; // assign in Inspector

// Inside the method that handles enemy defeat (OnDeath callback or similar):
private void HandleEnemyDeath(SpellCaster enemy)
{
    // Existing win logic...

    // Unlock the spell reward for this location
    if (currentMapLocation != null && currentMapLocation.spellReward != null)
    {
        MapProgressStorage.UnlockSpell(currentMapLocation.spellReward.spellName);
        MapProgressStorage.MarkCompleted(currentMapLocation.locationId);
    }
}
```

In the Unity Inspector, assign the **Map Location** ScriptableObject that matches the current scene to the `currentMapLocation` field on the MatchManager.

---

## Step 4 — Filter the SpellBook to Only Show Unlocked Spells

Add a new method to [SpellBook.cs](Assets/Scripts/SpellBook.cs):

```csharp
public List<Entry> GetUnlockedEntries()
{
    List<string> unlocked = MapProgressStorage.GetUnlockedSpells();
    List<Entry> result = new();

    foreach (var entry in entries)
    {
        if (entry.spell != null && unlocked.Contains(entry.spell.spellName))
            result.Add(entry);
    }

    return result;
}
```

---

## Step 5 — Update the Spellbook UI to Reflect Unlocked Spells

Wherever you populate the spellbook UI (a panel, scroll list, or grid of spell cards), replace the call to `GetEntries()` or `GetAllSpells()` with `GetUnlockedEntries()`:

```csharp
// Before:
var entries = spellbook.GetEntries();

// After:
var entries = spellbook.GetUnlockedEntries();
```

Then re-populate the UI from that filtered list. Each time the spellbook screen opens, call this again so it reflects the latest saved progress.

---

## Step 6 — Handle the Starting Spell (First Location)

The first location should be unlocked from the start so the player always has at least one spell. Two options:

**Option A** — Unlock it by default in `MapProgressStorage.Load()`:
```csharp
// Inside Load(), after deserializing:
if (data.unlockedSpellNames.Count == 0)
    data.unlockedSpellNames.Add("YourStartingSpellName");
```

**Option B** — In the SpellBook UI, always show the first spell regardless of unlock state.

---

```
Player defeats enemy
        |
        v
MatchManager.HandleEnemyDeath()
        |
        ├── MapProgressStorage.MarkCompleted(locationId)
        |       → saves to map_progress.json
        |
        └── MapProgressStorage.UnlockSpell(spellName)
                → appends to unlockedSpellNames in map_progress.json

Player opens Spellbook UI
        |
        v
SpellBook.GetUnlockedEntries()
        |
        v
MapProgressStorage.GetUnlockedSpells()
        |       → reads unlockedSpellNames from map_progress.json
        v
UI renders only entries whose spell.spellName is in the unlocked list
```

---

## Inspector Setup Per Scene

For each battle scene, set these fields in the Unity Inspector:

| Component | Field | Value |
|---|---|---|
| `MatchManager` | `currentMapLocation` | The MapLocation asset for this scene |
| `MapLocation` | `spellReward` | The Spell ScriptableObject awarded here |
| `MapLocation` | `locationId` | Unique string e.g. `"forest_1"` |
| `MapLocation` | `sceneToLoad` | Scene name e.g. `"ForestLevel"` |

---

## Example Location → Spell Mapping

| Location | locationId | Spell Awarded |
|---|---|---|
| Location 1 | `"village"` | Fireball |
| Location 2 | `"forest"` | Lightning Strike |
| Location 3 | `"ruins"` | Ice Shard |
| Location 4 | `"castle"` | Holy Sword |

---

## Files to Modify

| File | Change |
|---|---|
| [MapLocation.cs](Assets/Scripts/MapLocation.cs) | Add `public Spell spellReward` field |
| [MapProgressStorage.cs](Assets/Scripts/MapProgressStorage.cs) | Add `unlockedSpellNames` to data, add `UnlockSpell()` and `GetUnlockedSpells()` |
| [SpellBook.cs](Assets/Scripts/SpellBook.cs) | Add `GetUnlockedEntries()` method |
| [MatchManager.cs](Assets/Scripts/MatchManager.cs) | Call `UnlockSpell()` on enemy defeat |
| Spellbook UI script | Use `GetUnlockedEntries()` instead of `GetEntries()` |
