---
marp: true
theme: default
paginate: true
---

# Reinforcement Learning for "Smart" Enemy Reactions
### Learning with AI — Wizards Game Project

---

## What Is Reinforcement Learning?

Reinforcement learning (RL) is where an agent learns by **trial and error** — taking actions, receiving rewards or penalties, and over time developing a policy that maximizes its reward.

| RL Concept | This Project |
|---|---|
| **Agent** | The enemy wizard (CPU) |
| **Environment** | The match — player health, mana, spell queue |
| **Actions** | Attack, block, dodge, counter, heal |
| **Reward signal** | Surviving longer, dealing damage |
| **Policy** | The probability weights and thresholds we tune |

The enemy doesn't just pick randomly — it **observes** what the player does and **responds** based on what it has learned is effective.

---

## The Problem with Pure Random

The original enemy AI (`AISpellManager2`) selected a spell at random from the spellbook every few seconds:

```csharp
// AISpellManager2.cs — original baseline
private IEnumerator AILoop()
{
    while (isActive)
    {
        yield return new WaitForSeconds(pauseBetweenSpells);
        SpellBook.Entry entry = PickRandomEntry();
        if (!spellCaster.CanCast(entry.spell)) continue;
        yield return StartCoroutine(DisplayGestureSequence(entry.sequence));
        spellCaster.Cast(entry.spell);
    }
}
```

**Problems this caused:**
- Opponent healing at full health
- Shielding when no attack was incoming
- Never counter-attacking — the most interesting response
- Attacks continuing mid-shield sequence (doubling up)

---

## Designing the Observation Space

In RL, the agent needs to **observe** its environment before it can react. We defined two key observations:

**1. Player casts a damaging spell:**
```csharp
// ResponsiveAISpellManager.cs
private void OnAnySpellCast(SpellCaster caster, Spell spell)
{
    if (!caster.CompareTag("Player")) return;
    if (spell.damage <= 0) return;       // ignore non-damaging spells
    if (_healSpellNames.Contains(spell.name)) return; // ignore heals
    // → trigger block or counter decision
}
```

**2. Enemy takes damage:**
```csharp
private void OnSelfDamaged(SpellCaster caster)
{
    float healthPercent = caster.currentHealth / caster.maxHealth;
    if (healthPercent >= healHealthThreshold) return; // only react when low
    // → trigger heal decision
}
```

These two event hooks are the enemy's entire "sensor array."

---

## Designing the Action Space

With observations defined, we defined what actions the enemy can take in response:

| Trigger | Possible Actions |
|---|---|
| Player casts a damaging spell | **Block** (shield) or **Dodge** or **Counter-attack** |
| Enemy health drops below threshold | **Heal** (weighted spell selection) |
| No reaction pending | **Random attack** from spell pool |

Each action is mutually exclusive — the state machine prevents the enemy from, say, shielding and attacking simultaneously using flags:

```csharp
private bool _isReacting = false;
private bool _isHealing = false;
private bool _attackSequenceRunning = false;
```

The AI loop waits on all three before queuing the next attack:
```csharp
yield return new WaitUntil(() => !_isReacting && !_isHealing);
```

---

## The Policy — Probability as Behavior

Rather than a fully trained neural network, our "policy" is a set of **Inspector-tunable probability weights** — the parameters that define how the enemy behaves:

```csharp
[Range(0f, 1f)] private float blockChance = 0.4f;
[Range(0f, 1f)] private float counterChance = 0.3f;
[Range(0f, 1f)] private float healChance = 0.4f;
[Range(0f, 1f)] private float healHealthThreshold = 0.8f;
[Range(0f, 1f)] private float dodgeVsShieldRatio = 0f;
```

The decision rolls a single die against these thresholds:

```csharp
float roll = Random.value;
if (roll < blockChance)
    StartCoroutine(ReactWithBlock());       // 0.0 – 0.4
else if (roll < blockChance + counterChance)
    StartCoroutine(ReactWithCounter());     // 0.4 – 0.7
// else: no reaction, let the random attack loop continue
```

One roll. Mutually exclusive outcomes. No "stacking" of reactions.

---

## Timing as Strategy — Reaction Delays

A key insight from playtesting: **an enemy that reacts instantly feels cheap**. Real strategy involves reaction time.

Each action has its own delay, tunable independently:

```csharp
[SerializeField] private float blockReactionDelay  = 0.3f;
[SerializeField] private float dodgeReactionDelay  = 0.15f;
[SerializeField] private float counterReactionDelay = 0.8f;
[SerializeField] private float healReactionDelay    = 0.5f;
```

Dodge is faster than shield — it's physically simpler. Counter-attacking takes longer — the enemy needs time to "read" the incoming spell before committing to a response. These delays were arrived at through playtesting and tuning, not math.

---

## Interrupting Current Behavior

A major issue in early builds: the enemy would **keep attacking mid-sequence** even after deciding to block. The new attack and the block would both play simultaneously.

**Solution — `CancelCurrentAttack()` runs before every reactive response:**

```csharp
private void CancelCurrentAttack()
{
    if (_currentAttack != null)
    {
        StopCoroutine(_currentAttack);
        _currentAttack = null;
        _attackSequenceRunning = false;
    }
    gestureDisplay?.ClearUIBuffer();
}
```

This clears the gesture display and stops the queued spell before the reactive behavior starts — the enemy commits fully to its new decision, just like a real opponent would.

---

## The Heal System — Weighted Policy Selection

Healing isn't just "cast any heal" — the enemy has a **weighted preference** across four heal spell tiers:

```csharp
[System.Serializable]
public class HealOption
{
    public string spellName = "Heal lvl 1";
    [Range(0f, 1f)]
    public float weight = 0.25f;
}
```

The selector rolls against cumulative weights — higher-weight options are chosen more often:

```csharp
float roll = Random.value * _totalHealWeight;
float cumulative = 0f;
foreach (var option in healOptions)
{
    cumulative += option.weight;
    if (roll <= cumulative)
        return spellBook.GetEntryByName(option.spellName);
}
```

This lets designers configure a harder enemy that prefers stronger heals, or an easier one that wastes turns on weak heals.

---

## Preventing Degenerate Behavior

In RL terms, "degenerate behavior" is when the agent finds an unintended strategy that technically maximizes reward but breaks the experience. We had several:

**Problem 1:** Shield and Dodge spells appearing in the random attack pool → enemy shielding unprompted, looking confused.
**Fix:** `excludeFromRandom` list — Shield and Dodge are never picked by the attack loop.

**Problem 2:** Heal spells triggering the enemy's own block reaction.
**Fix:** `_healSpellNames` HashSet — heal spell casts are explicitly ignored in `OnAnySpellCast`.

**Problem 3:** Counter-attacking with negative mana (Air Combo costs were checked before the gesture sequence, but not after).
**Fix:** Double `CanCast()` check — once before displaying gestures, once before casting.

```csharp
if (!spellCaster.CanCast(counterEntry.spell)) yield break; // before gestures
yield return StartCoroutine(DisplayGestureSequence(...));
if (!spellCaster.CanCast(counterEntry.spell)) yield break; // after gestures
spellCaster.Cast(counterEntry.spell);
```

---

## Using Markdown Flows to Understand the System

Before writing a single line of the reactive AI, AI (Claude) was used to generate structured flow descriptions of the existing game logic — how spells were queued, when mana was deducted, how coroutines interacted.

Rather than reading raw code, having the logic described as a flow made it much easier to:
- Identify **where** in the execution a new reactive behavior should hook in
- Spot **ordering bugs** (e.g. mana deducted before or after animation?)
- Communicate intent clearly before implementing

**Key discovery from the flow analysis:**
Mana is not deducted when `Cast()` is called — it's deducted later inside `OnReleaseSpell()`, the animation event. This meant the AI's shield behavior needed to call `UseMana()` directly, bypassing the normal cast path entirely.

---

## Balancing Challenge, Fairness, and Unpredictability

AI helped reason through the difference between a **hard** enemy and a **fun** enemy:

| Hard enemy | Fun enemy |
|---|---|
| Reacts to everything instantly | Has believable reaction delays |
| Always blocks | Blocks *most* of the time — misses sometimes |
| Never wastes mana | Occasionally makes suboptimal choices |
| Predictable once learned | Mix of reactive and random keeps players guessing |

The tunable parameters in the Inspector (`blockChance`, `counterChance`, `dodgeVsShieldRatio`, `healHealthThreshold`) are the direct levers for this balance. Different scenes can use different preset values to create progression — early enemies are more random, later enemies react more precisely.

---

## Iterating on the Policy

Each tuning cycle followed the same loop:

```
Playtest → observe behavior → identify problem → adjust parameter → repeat
```

**Examples of iterations:**

- `blockChance = 0.8` → enemy blocked too often, felt unfair → lowered to `0.4`
- `counterReactionDelay = 0.3` → counter felt instant, unfair → raised to `0.8`
- `dodgeVsShieldRatio = 0.5` → too much dodging when mana was low → lowered to `0.0` (always shield)
- Heal weight `Heal lvl 4 = 0.7` → enemy always fully healed, too strong → balanced all weights to `0.25`

AI helped interpret what each change would likely produce before testing — shortening the feedback loop significantly. But **every decision was validated through play**, not through logs.

---

## The Architecture We Ended Up With

```
Player casts spell (SpellCaster.OnSpellCast — static event)
    └─ ResponsiveAISpellManager.OnAnySpellCast()
         ├─ roll < blockChance  → ReactWithBlock()  (delay 0.3s)
         │       └─ CancelCurrentAttack() → shield or dodge
         └─ roll < block + counter → ReactWithCounter() (delay 0.8s)
                 └─ CancelCurrentAttack() → gesture display → Cast()

Enemy takes damage (SpellCaster.OnDamageTaken — instance event)
    └─ ResponsiveAISpellManager.OnSelfDamaged()
         └─ health < threshold && roll < healChance
                 └─ ReactWithHeal() → PickWeightedHeal() → Cast()

No reaction pending (AILoop — coroutine)
    └─ WaitUntil(!_isReacting && !_isHealing && !shield.IsActive)
         └─ PickRandomEntry() → RunAttackSequence()
```

Two event hooks + one loop + three flags = a convincingly reactive opponent.

---

## What I Learned — Reinforcement Learning

> Reinforcement learning requires patience and careful design. The enemy doesn't just "get smarter" — you have to deliberately define what *smart means* through the actions available, the signals it observes, and the parameters that shape its decisions.
>
> The most useful thing AI did here wasn't write the code — it was help me **think clearly about the problem**. Asking "what should the enemy observe?" and "what counts as a good outcome?" before touching the code meant that when I did write it, the architecture made sense end-to-end.
>
> Every parameter was arrived at through play. No amount of reasoning about numbers replaced actually fighting the enemy and feeling whether it was fair.

---

*All gameplay systems were designed, implemented, and validated by me. AI tools were used as learning aids and development accelerators.*
