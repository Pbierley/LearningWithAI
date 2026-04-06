---
marp: true
theme: default
paginate: true
---

# Map User Flow
### How Players Progress Through Locations

---

## Overview

The map uses a **linear unlock system** — locations are completed in order, and each completed location unlocks the next one. The player cannot skip ahead or access locked locations.

---

## Location States

| State | Color | Behavior |
|---|---|---|
| **Locked** | Gray | Button is disabled, cannot be clicked |
| **Unlocked** | Yellow | Button is active, player can enter |
| **Completed** | Green | Previously finished, can be replayed |

---

## User Flow Diagram

---

```
[ Open Map Screen ]
        |
        v
+-------------------+     +-------------------+     +-------------------+
|   Location 1      |     |   Location 2      |     |   Location 3      |
|   UNLOCKED ✓      | --> |   LOCKED          |     |   LOCKED          |
|   (Yellow)        |     |   (Gray)          |     |   (Gray)          |
+-------------------+     +-------------------+     +-------------------+

        |
        | Player clicks Location 1
        v

[ Scene Loads for Location 1 ]
        |
        | Player completes the level
        v

[ MapProgressStorage.MarkCompleted("location_1") ]
[ Saves to: map_progress.json ]
        |
        v

[ Player returns to Map Screen ]
        |
        v

+-------------------+     +-------------------+     +-------------------+
|   Location 1      |     |   Location 2      |     |   Location 3      |
|   COMPLETED ✓     | --> |   UNLOCKED ✓      |     |   LOCKED          |
|   (Green)         |     |   (Yellow)        |     |   (Gray)          |
+-------------------+     +-------------------+     +-------------------+

        |
        | Player clicks Location 2
        v

[ Scene Loads for Location 2 ]
        |
        | Player completes the level
        v

[ MapProgressStorage.MarkCompleted("location_2") ]
        |
        v

[ Player returns to Map Screen ]
        |
        v

+-------------------+     +-------------------+     +-------------------+
|   Location 1      |     |   Location 2      |     |   Location 3      |
|   COMPLETED ✓     | --> |   COMPLETED ✓     | --> |   UNLOCKED ✓      |
|   (Green)         |     |   (Green)         |     |   (Yellow)        |
+-------------------+     +-------------------+     +-------------------+

        |
        | Pattern continues for all locations...
        v

[ All Locations Completed ]
```

---

## Unlock Rules

```
For each location at index N:

  IF location N is in completedLocationIds
      → State = COMPLETED (green)

  ELSE IF N is the first location (index 0)
      → State = UNLOCKED (yellow)

  ELSE IF location N-1 is COMPLETED
      → State = UNLOCKED (yellow)

  ELSE
      → State = LOCKED (gray)
```

---

## Progress Persistence

- Progress is saved to `map_progress.json` on the player's device
- A list of completed `locationId` strings is stored
- Progress persists between sessions — closing and reopening the game keeps all completed locations
- Progress can be fully reset by deleting the save file (`MapProgressStorage.ResetAll()`)

---

## How a Location Knows Where to Go

Each `MapLocation` component on the map has two fields set in the Unity Inspector:

| Field | Example | Purpose |
|---|---|---|
| `locationId` | `"forest_1"` | Unique ID used to track completion |
| `sceneToLoad` | `"ForestLevel"` | The Unity scene loaded when clicked |

When the player clicks an unlocked location, the game immediately loads the assigned scene.

---

## Re-entering Completed Locations

Players **can** click on completed (green) locations to replay them. Replaying does not affect progress — the location stays marked as completed regardless of outcome.

---

## Summary Flow

```
Map Screen
    → Click Unlocked Location
        → Scene Loads
            → Complete Level
                → MarkCompleted() called
                    → Return to Map
                        → Next Location Unlocks
                            → Repeat
```
