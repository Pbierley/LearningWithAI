---
marp: true
theme: default
paginate: true
---

# Supervised Learning for Custom Gesture Classification
### Learning with AI — Wizards Game Project

---

## What Is Supervised Learning?

Supervised learning is a type of machine learning where a model is trained on **labeled examples** so it can predict the correct label on new input.

In this project, that means:

| Input | Label |
|---|---|
| A snapshot of both hands | `"Open_Palm"` + `"Peace"` |
| A different hand configuration | `"Pointing_Up"` + `"Thumb_Up"` |
| Hands in casting position | `"ILoveYou"` + `"Closed_Fist"` |

The model learns: *"when hands look like this, the gesture is X."*

---

## The Problem We Were Solving

The game requires players to cast spells by making **real hand gestures** in front of a webcam in real time.

This means the system needs to:
1. See both hands simultaneously
2. Classify each hand's pose into a known gesture label
3. Match the pair of gesture labels to a spell in the player's spellbook
4. Cast the spell with low enough latency that gameplay feels responsive

**No gesture recognition = no game.**

---

## Why MediaPipe?

Last semester we used AI to brainstorm solutions for gesture detection. The key question was:

> *Do we train our own classifier from scratch, or build on an existing model?*

AI helped us weigh the options:

| Approach | Effort | Risk |
|---|---|---|
| Train from scratch | Very high | High — needs thousands of labeled hand images |
| Fine-tune an existing model | Medium | Medium |
| **Use MediaPipe's pre-trained model** | **Low** | **Low — production-tested by Google** |

We landed on **MediaPipe Hands** — a pre-trained supervised model that outputs 21 hand landmarks per hand, already classified into gesture names.

---

## How the Labeled Data Flows In

MediaPipe runs on a worker thread and calls back into our `SpellManager` with raw string labels:

```csharp
// SpellManager.cs — called by MediaPipe on a worker thread
public void OnGestureRecognized(string leftStr, string rightStr)
{
    var left  = GestureMapper.ToEnum(leftStr);
    var right = GestureMapper.ToEnum(rightStr);
    gestureQueue.Enqueue((left, right));
}
```

`GestureMapper.ToEnum()` converts MediaPipe's raw string output (e.g. `"Open_Palm"`) into a typed `GestureLabel` enum — our internal representation of each gesture class.

The queue safely hands data from the MediaPipe worker thread to Unity's main thread each `Update()`.

---

## From Labels to Spells

Once we have two classified gesture labels, we need to match them to a spell. This is done through the `SpellBook` — a ScriptableObject that stores every spell alongside its required gesture **sequence**.

```
Player makes gestures → GesturePair buffer builds up → SpellBook.TryGetSpell()
```

Each entry in the spellbook looks like:

```
Spell:    "Holy Sword"
Sequence: [ (ILoveYou, Closed_Fist), (Open_Palm, Pointing_Up) ]
```

The player must perform the pairs in order. When the buffer matches a sequence exactly, the spell is cast.

---

## The Gesture Buffer

A single gesture pair rarely means "cast now." Players need to build up a **sequence** of pairs to unlock more powerful spells.

```csharp
// When a stable new gesture is detected, it's added to the buffer
gestureBuffer.Add(new GesturePair(left, right));
gestureUIBuffer.AddToUIBuffer(left, right);  // shows in bottom-left HUD

// Then immediately checked against the spellbook
if (spellbook.TryGetSpell(gestureBuffer, out var spell))
{
    playerCaster.Cast(spell);
    gestureBuffer.Clear();
}
```

The bottom-left HUD shows the player their current gesture buffer in real time using gesture icons — giving visual feedback on what they've input so far.

---

## Improving Classification Accuracy: Stable Frames

One of the earliest problems: **false positives from transitional hand positions**.

As a player moves their hands between gestures, MediaPipe briefly outputs garbage labels — partial poses that don't represent any real gesture. If we acted on every frame, spells would fire constantly.

**Solution — require a gesture to be held stable for a minimum duration:**

```csharp
[SerializeField] private float requiredStableTime = 0.1f;

// Only commit a gesture if it's been held unchanged for requiredStableTime
if (Time.time - stableStartTime >= requiredStableTime
    && (left != stableLeft || right != stableRight))
{
    // commit to buffer
}
```

This one parameter — tuned with AI help — dramatically reduced misfires.

---

## Improving Accuracy Across Different Users

MediaPipe's pre-trained model was built on general hand data, but we encountered real-world gaps:

**Problems identified during testing:**
- Users with smaller hands had reduced detection range
- Darker skin tones were detected less reliably
- Poor or uneven lighting caused significant frame drops in detection

**Solutions — with AI assistance:**

- Implemented a **"missed frame" tolerance** — the classifier accepts a run of non-detected frames before resetting, rather than failing immediately
- Added guidance prompts to players about lighting setup
- Evaluated a white ring border around the game UI (minimal effect on detection, hurt visual design — not shipped)

---

## Teaching the Gesture System in Tutorials

This semester we extended the system with a full tutorial pipeline for **teaching players new spells gesture-by-gesture**.

When a tutorial step is active, `TutorialManager` calls into `SpellManager`:

```csharp
// TutorialManager.cs — StartStep()
if (step.spellHint != null)
{
    spellManager.SetAllowedSpell(step.spellHint);   // restrict to one spell
    pinnedSpellsPanel.SetTutorialOverride(step.spellHint.name); // show its gestures
}
```

The player can only cast the spell being taught — any other gesture sequence **fizzles**. The pinned spells HUD shows only that spell's gesture icons. Once they cast it successfully, the tutorial advances.

---

## The Tutorial Gesture Restriction

Inside `SpellManager`, the restriction is applied at cast time:

```csharp
if (spellbook.TryGetSpell(gestureBuffer, out var spell))
{
    if (_allowedSpell != null && spell != _allowedSpell)
        playerCaster.PlayFizzle();   // wrong spell — visual feedback, no cast
    else
        playerCaster.Cast(spell);    // correct spell — cast it
}
```

This keeps the learning loop tight:
- Players see exactly which gestures to make (HUD)
- Other gestures are blocked (fizzle feedback)
- Success is unambiguous (spell fires, tutorial advances)

---

## Wiring Gesture Output Into the Unity Game Loop

The pipeline from hand to spell in-game looks like this:

```
MediaPipe (worker thread)
    └─ OnGestureRecognized(leftStr, rightStr)
         └─ GestureMapper.ToEnum() → GestureLabel
              └─ gestureQueue.Enqueue()

Unity Update() (main thread)
    └─ gestureQueue.TryDequeue()
         └─ HandleGesture(left, right)
              └─ stability check → gestureBuffer.Add()
                   └─ SpellBook.TryGetSpell()
                        └─ SpellCaster.Cast(spell)
                             └─ Animator trigger → OnReleaseSpell()
                                  └─ spell.behavior.Cast() → VFX + damage
```

Each layer was built and debugged with AI help — identifying where timing issues, thread-safety problems, and frame-rate dependencies were hiding.

---

## Integrating the Spellbook as a Supervised Dataset

The `SpellBook` ScriptableObject is effectively our **training label set** — it defines every valid gesture sequence and maps it to a game action.

Adding a new spell means:
1. Choose a gesture sequence that MediaPipe can reliably classify
2. Create a `Spell` ScriptableObject with damage, VFX, and behavior
3. Add it to the SpellBook with its sequence
4. Optionally add it to a tutorial step's `spellHint` field so it gets taught

No retraining. No new ML pipeline. MediaPipe handles classification; the SpellBook handles mapping.

---

## Sourcing Assets and Scene Performance

As the game grew, importing high-quality environment assets impacted performance.

AI (Claude) helped by:
- Explaining how **polygon count affects render performance** and where the bottlenecks appear
- Walking through Unity's **Stats panel and Profiler** to identify the heaviest meshes
- Explaining Unity's **LOD (Level of Detail)** system — swapping in lower-poly versions of meshes at distance

These optimizations restored smooth framerate without changing how the scenes looked up close — essential for a gesture-based game where any frame drop can misclassify a gesture.

---

## What I Learned — Supervised Learning

> Supervised learning is only as good as the data behind it — and context works the same way with AI.
>
> The more precisely I described what I was trying to do, what had already failed, and what the code currently looked like, the more accurate and useful the AI's responses were. Vague questions got vague answers. Specific questions — with real variable names, real error messages, and real game constraints — got solutions I could use immediately.
>
> The gesture pipeline taught me that a pre-trained model is a starting point, not a finish line. The real work was understanding its outputs deeply enough to build a reliable game system on top of them.

---

*All gameplay systems were designed, implemented, and validated by me. AI tools were used as learning aids and development accelerators.*
