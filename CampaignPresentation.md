---
marp: true
theme: default
paginate: true
---

# Wizards Game — Campaign Mode
### Final Presentation · Philip Bierley

---

## What I Built

A full single-player **campaign mode** layered on top of an existing gesture-based wizards game.

The campaign adds:
- A **node-based world map** that gates progression
- **Spell unlocking** — players earn new spells by winning matches
- **Tutorial scenes** that teach each new spell before combat
- A **responsive AI opponent** that reacts to the player's actions
- **Persistent progress** saved across sessions

Six interconnected features, built incrementally over 15 weeks.

---

## Feature 1 — Spell Progression & Unlocking
#### Weeks 1–3

---

### The Core Problem

In a standalone match, every spell is available from the start. For a campaign, spells need to be **earned** — and the game needs to enforce that restriction at runtime everywhere spells are used: the SpellBook, the HUD, the pause menu.

---

### How Spells Are Locked

The `SpellBook` ScriptableObject stores all spells. Every time it's asked for valid entries, it checks campaign state:

```csharp
// SpellBook.cs
private static bool IsEntryUnlocked(Entry entry)
{
    if (MatchManager.Instance == null || !MatchManager.Instance.IsCampaignMatch)
        return true; // non-campaign: all spells available

    return entry != null &&
           entry.spell != null &&
           MapProgressStorage.IsSpellUnlocked(entry.spell.spellName);
}
```

One check. Every system that reads the SpellBook — gesture matching, the pause menu spellbook, the AI attack pool — automatically respects it with no extra code per system.

---

### Unlocking After Victory

When the player wins a campaign match, `MatchManager` saves their progress:

```csharp
// MatchManager.cs
private void SaveCampaignProgressIfNeeded()
{
    var locationId = MapSessionContext.ActiveLocationId;
    var spellReward = MapSessionContext.ActiveRewardSpellName;

    MapProgressStorage.MarkCompleted(locationId);

    if (!string.IsNullOrEmpty(spellReward))
        MapProgressStorage.UnlockSpell(spellReward);
}
```

Each map node has a `spellReward` field. Win the fight, earn the spell.

---

### Persistence — JSON on Disk

All progress is stored to a JSON file at `Application.persistentDataPath`:

```json
{
  "completedLocationIds": ["Pixelburg", "MysticTemple"],
  "unlockedSpellNames": ["Purple Ball", "Shield", "Heal lvl 1", "Holy Sword"]
}
```

Three spells are unlocked by default at the start of every new campaign: **Purple Ball**, **Shield**, and **Heal lvl 1** — enough to fight the first opponent.

Progress persists between sessions. `MapProgressStorage.ResetAll()` wipes it for a fresh run.

---

## Feature 2 — Learning-Oriented Spells
#### Weeks 4–5

---

### Teaching Before Testing

Simply unlocking a spell and dropping the player into combat isn't enough. Players need to learn the gesture sequence before they're expected to use it under pressure.

Each new spell has a dedicated **unlock scene** — a non-combat learning environment where the player practices the gesture against a crystal target before the spell is added to their arsenal.

---

### Restricting to One Spell

The `TutorialManager` tells `SpellManager` which spell is allowed for each step:

```csharp
// TutorialManager.cs — StartStep()
if (step.spellHint != null)
{
    spellManager.SetAllowedSpell(step.spellHint);
    pinnedSpellsPanel.SetTutorialOverride(step.spellHint.name);
}
```

Inside `SpellManager`, any gesture that doesn't match the taught spell fizzles:

```csharp
if (_allowedSpell != null && spell != _allowedSpell)
    playerCaster.PlayFizzle();   // wrong gesture — visual feedback only
else
    playerCaster.Cast(spell);    // correct — cast it
```

The player can only succeed by learning the right gesture. No shortcuts.

---

### Gesture Display in Learning Scenes

The `PinnedSpellsPanel` — the same HUD used in combat — is repurposed for learning. When a `spellHint` is set, it overrides the player's saved pinned spells and shows only the gesture icons for the spell being taught:

```csharp
// PinnedSpellsPanel.cs
public void SetTutorialOverride(string spellName)
{
    _tutorialOverride = new List<string> { spellName };
    RefreshDisplay();
}
```

The player has the gesture sequence in front of them at all times. When the tutorial ends, `ClearTutorialOverride()` restores their real pinned spells automatically.

---

### Crystal Target — Non-Combat Learning

Unlock scenes use a `TutorialCrystal` instead of a live opponent. The crystal is a simple `IDamageable` that calls `TutorialManager.CompleteCurrentStep()` when destroyed — no health bar, no AI, no pressure.

```csharp
// TutorialCrystal.cs
public void TakeDamage(float dmg, Transform source)
{
    TutorialManager.Instance?.CompleteCurrentStep();
    Destroy(gameObject);
}
```

The player can focus entirely on the gesture. Once they land the spell, the crystal shatters and the tutorial advances.

---

## Feature 3 — Campaign Map & World
#### Weeks 6–8

---

### Node-Based World Map

The campaign map is a **visual graph of locations** — each node represents a fight or tutorial scene. Nodes have three states driven by `MapProgressStorage`:

| State | Visual | Interaction |
|---|---|---|
| **Locked** | Grey, no outline | Not clickable |
| **Unlocked** | Normal, purple outline | Clickable — loads scene |
| **Completed** | Normal, gold outline | Still accessible for replay |

`MapProgressManager` reads stored progress at scene load and applies the correct state to every node.

---

### Progression Logic

The unlock rule is sequential — you must complete each location before the next opens:

```csharp
// MapProgressManager.cs
for (int i = 0; i < locations.Count; i++)
{
    if (i == 0)
    {
        locations[i].SetState(MapLocationState.Unlocked); // first always open
        continue;
    }

    bool previousCompleted = MapProgressStorage.IsCompleted(locations[i - 1].locationId);
    locations[i].SetState(previousCompleted
        ? MapLocationState.Unlocked
        : MapLocationState.Locked);
}
```

Completed locations stay accessible — players can replay fights or re-practice spells at any time.

---

### Session Context — Bridging Map to Match

When a player clicks a map node, `MapSessionContext` stores the session data so the match scene knows what it's loading for:

```csharp
// MapLocation.cs — on click
MapSessionContext.SetActive(locationId, sceneName, spellReward.spellName);
SceneManager.LoadScene(sceneName);
```

Inside the match scene, `MatchManager` reads this context to know which location to mark complete on victory, and which spell to unlock:

```
Map scene → click node → SetActive() → load match scene
    → win → SaveCampaignProgressIfNeeded() → return to Map
```

The map and match scenes share no direct references — `MapSessionContext` is the bridge.

---

## Feature 4 — Opponent Variety & Scaling
#### Weeks 9–10

---

### From Random to Reactive

The original AI (`AISpellManager2`) picked a random spell every few seconds. Every enemy felt identical.

`ResponsiveAISpellManager` replaces this with a **profile-driven reactive system** — the same component configured differently per opponent creates meaningfully different fights.

---

### Inspector-Tunable Profiles

Every aspect of enemy behavior is exposed as a slider:

```csharp
[Range(0f, 1f)] float blockChance       = 0.4f;  // reacts to player attacks
[Range(0f, 1f)] float counterChance     = 0.3f;  // counter-attacks instead
[Range(0f, 1f)] float dodgeVsShieldRatio = 0f;   // 0 = always shield, 1 = always dodge
[Range(0f, 1f)] float healChance        = 0.4f;  // heals when low
[Range(0f, 1f)] float healHealthThreshold = 0.8f; // what "low" means

float blockReactionDelay   = 0.3f;   // seconds before shielding
float dodgeReactionDelay   = 0.15f;  // seconds before dodging  
float counterReactionDelay = 0.8f;   // seconds before counter-attacking
float pauseBetweenSpells   = 2.5f;   // attack pace
```

An early-campaign opponent: high `pauseBetweenSpells`, low `counterChance`. A late-campaign boss: tight reaction delays, balanced block/counter split, aggressive heal threshold.

---

### Scaling Without New Code

Different difficulty profiles emerge purely from parameter changes — no new scripts, no branching logic:

| Opponent | Block | Counter | Heal Threshold | Attack Pace |
|---|---|---|---|---|
| Beginner | 20% | 10% | 50% HP | 3.5s |
| Intermediate | 40% | 30% | 70% HP | 2.5s |
| Boss | 55% | 35% | 85% HP | 1.8s |

The `excludeFromRandom` list prevents Shield and Dodge spells from appearing in random attacks, keeping behavior intentional.

---

## Feature 5 — Tutorial Integration
#### Weeks 11–12

---

### TutorialManager — Step-Based Flow

Every tutorial scene is driven by `TutorialManager` — a singleton that manages an ordered array of `TutorialStep` objects. Each step can:

- Activate or deactivate GameObjects (spawn crystals, enable enemies)
- Display a prompt to the player
- Damage the player on start (for heal tutorials)
- Show a specific spell's gestures in the HUD
- Restrict casting to that spell
- Auto-complete after a delay (for intro/congrats screens)
- Fire UnityEvents on start and completion

All configured in the Inspector. No code changes needed to author a new tutorial.

---

### Seamless Scene Transitions

When the last tutorial step completes, `TutorialManager` loads the next scene automatically:

```csharp
// TutorialManager.cs
private void CompleteTutorial()
{
    pinnedSpellsPanel?.ClearTutorialOverride();
    spellManager?.ClearAllowedSpell();
    OnTutorialComplete?.Invoke();

    if (!string.IsNullOrEmpty(sceneOnComplete))
        SceneManager.LoadScene(sceneOnComplete);
}
```

The `sceneOnComplete` field is set per scene — a practice scene loads the map, a combat tutorial loads directly into the fight. The player moves through the campaign without manual navigation.

---

### Enemy Death as Tutorial Completion

In scenes with an opponent, `TutorialEnemyDeathTrigger` watches the enemy's `SpellCaster.OnDeath` event and advances the tutorial when the enemy is defeated:

```csharp
// TutorialEnemyDeathTrigger.cs
private void OnEnemyDied(SpellCaster caster)
{
    TutorialManager.Instance?.CompleteCurrentStep();
}
```

One small component. Drag the enemy's SpellCaster into the field in the Inspector. The tutorial system handles everything else — scene load, progress save, spell unlock.

---

### Skill Reinforcement Loop

The full flow from learning to combat:

```
Unlock Scene (tutorial)
    └─ Gesture restriction: only the new spell fires
    └─ HUD shows gesture icons for that spell
    └─ Destroy crystal → advance step → load Map

Map Scene
    └─ New node unlocked
    └─ Player selects match

Match Scene
    └─ New spell now available in SpellBook
    └─ Player uses it against a live opponent
    └─ Win → save progress → return to Map
```

---

## Feature 6 — Advanced AI Combat
#### Weeks 13–15

---

### Event-Driven Reactions

The AI subscribes to two events that fire during gameplay:

```csharp
// Start()
SpellCaster.OnSpellCast   += OnAnySpellCast;   // static — fires for any caster
spellCaster.OnDamageTaken += OnSelfDamaged;    // instance — fires only for this AI
```

Every time the player casts a damaging spell, `OnAnySpellCast` fires. Every time the AI takes a hit, `OnSelfDamaged` fires. The AI never polls — it reacts to events.

---

### The Decision Roll

When the player attacks, a single dice roll determines the AI's response:

```csharp
float roll = Random.value;

if (roll < blockChance)
{
    _isReacting = true;             // lock out other reactions immediately
    StartCoroutine(ReactWithBlock());
}
else if (roll < blockChance + counterChance)
    StartCoroutine(ReactWithCounter());
// else: absorb the hit, keep attacking
```

One roll. Three mutually exclusive outcomes. The AI can't simultaneously block and counter — just like a real opponent.

---

### Interrupting Committed Actions

When a reaction fires, the AI's current attack is cancelled before it starts:

```csharp
private void CancelCurrentAttack()
{
    if (_currentAttack != null)
    {
        StopCoroutine(_currentAttack);
        _currentAttack = null;
        _attackSequenceRunning = false;
    }
    gestureDisplay?.ClearUIBuffer();  // clears gesture display mid-sequence
}
```

This solved the "doubling up" bug — without cancellation, the AI would show a block gesture while simultaneously completing an attack animation. Now it commits fully to its reactive decision.

---

### Health-Based Healing

The heal system activates when the AI takes damage below a health threshold:

```csharp
private void OnSelfDamaged(SpellCaster caster)
{
    float healthPercent = caster.currentHealth / caster.maxHealth;
    if (healthPercent >= healHealthThreshold) return;
    if (Random.value < healChance)
    {
        _isHealing = true;
        StartCoroutine(ReactWithHeal());
    }
}
```

The heal spell chosen is **weighted** — designers can make a harder enemy prefer stronger heals:

```csharp
// HealOption: spellName + weight
// Heal lvl 1: 0.1 | Heal lvl 2: 0.2 | Heal lvl 3: 0.3 | Heal lvl 4: 0.4
```

---

### Mana as a Real Constraint

The AI's shield doesn't go through the normal `Cast()` → animation event → `OnReleaseSpell()` path — shields activate instantly. This meant mana was never deducted.

Fix: the AI calls `UseMana()` directly before activating the shield, after verifying it can afford it:

```csharp
if (shieldEntry?.spell != null && !spellCaster.CanCast(shieldEntry.spell))
{
    _isReacting = false;
    yield break;  // can't afford to shield — absorb the hit
}

spellCaster.UseMana(shieldEntry.spell.manaCost);
aiShield.ActivateShield();
```

Counter-attacks check mana **twice** — before displaying the gesture sequence and after — preventing the AI from committing to a cast it can no longer afford mid-animation.

---

### The Full Reactive Architecture

```
Player casts spell → OnAnySpellCast()
    ├─ roll < blockChance  → ReactWithBlock()
    │       └─ delay → CancelCurrentAttack() → shield/dodge → _isReacting = false
    └─ roll < block+counter → ReactWithCounter()
            └─ delay → CancelCurrentAttack() → gesture display → Cast()

Enemy takes damage → OnSelfDamaged()
    └─ health < threshold → ReactWithHeal()
            └─ delay → CancelCurrentAttack() → PickWeightedHeal() → Cast()

Idle → AILoop()
    └─ WaitUntil(!_isReacting && !_isHealing && !shield.IsActive)
            └─ PickRandomEntry() → RunAttackSequence()
```

Two event hooks + one idle loop + three boolean flags = a convincingly reactive opponent.

---

## What I Delivered

| Feature | Status | Key Systems |
|---|---|---|
| Spell Progression & Unlocking | ✅ | `MapProgressStorage`, `SpellBook.IsEntryUnlocked` |
| Learning-Oriented Spells | ✅ | `TutorialManager`, `SetAllowedSpell`, gesture HUD override |
| Campaign Map & World | ✅ | `MapLocation`, `MapProgressManager`, `MapSessionContext` |
| Opponent Variety & Scaling | ✅ | `ResponsiveAISpellManager`, per-profile Inspector tuning |
| Tutorial Integration | ✅ | `TutorialStep`, `TutorialEnemyDeathTrigger`, scene transitions |
| Advanced AI Combat | ✅ | Reactive block/counter/heal, mana constraints, state machine |

---

## Key Technical Takeaways

1. **Single-source locking** — one `IsEntryUnlocked` check in `SpellBook` enforces spell restrictions everywhere automatically
2. **Event-driven AI** — subscribing to `OnSpellCast` and `OnDamageTaken` instead of polling keeps the AI lightweight and responsive
3. **Profiles over code** — every difficulty difference is a parameter change, not a new script
4. **Flag-based state machine** — three booleans coordinating coroutines replaced complex nested logic
5. **Context objects as bridges** — `MapSessionContext` and `SpellPracticeSessionContext` decoupled scenes that need to share state

---

*All systems designed, implemented, and playtested by Philip Bierley.*
*AI tools (Claude, ChatGPT) used as development and learning accelerators.*

---
