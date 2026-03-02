---
marp: true
title: Spell Arena Campaign PPP
paginate: true
footer: © 2026
style: |
  section {
    font-size: 28px;
    padding: 48px 56px;
  }
  h1 { font-size: 52px; }
  h2 { font-size: 34px; }
  h3 {
    font-size: 26px;
    margin: 0.2em 0;
  }
  ul {
    margin: 0.2em 0 0 1em;
    padding-left: 0.8em;
  }
  li {
    margin: 0.15em 0;
    line-height: 1.2;
  }
---

# Spell Arena
## Campaign Mode PPP

---

## Project Goal

Expand the Hand Gesture Wizard game from a single-scenario demo into a structured campaign with progression, map-based encounters, and clearer player goals.

---

## Why This Matters

- Adds long-term progression and replay value.
- Improves player learning through staged complexity.
- Supports measurable development and capstone validation.

---

## Implemented Progress (Weeks 4-7)

- Campaign flow designed and decomposed into milestones.
- World map system implemented.
- Navigation between locations implemented.
- Location-based opponent fights implemented.
- Main menu reworked to include `Multiplayer` and `Campaign`.
- Collaboration planning started with DFX students for character/model support.

---

## My Learning With AI
### Supervised Learning for Custom Gesture Classification

- Train a gesture classifier on labeled hand-pose/trajectory examples.
- Improve recognition accuracy for custom spell gestures over time.
- Use validation metrics to tune model performance before deployment.

---

## My Learning With AI
### Reinforcement Learning for "Smart" Enemy Reactions

- Train enemy behavior through reward-based combat outcomes.
- Encourage adaptive reactions to player spell/gesture patterns.
- Use reward tuning to balance challenge without making AI unfair.

---

## Feature 1: Campaign Progression System

### Status
In progress

### Requirements
- R1.1: Track player campaign progress persistently across sessions.
- R1.2: Define discrete campaign stages (nodes or levels).
- R1.3: Completion of a stage unlocks the next stage.
- R1.4: Store progress data using Unity save/persistent storage.

---

## Feature 2: Spell Progression and Unlocking

### Status
Planned

### Requirements
- R2.1: Initialize new campaigns with a restricted spell set.
- R2.2: Unlock spells based on campaign milestones.
- R2.3: Allow equipping only unlocked spells.
- R2.4: Keep unlock conditions data-driven.

---

## Feature 3: World Map Navigation


### Requirements
- R3.1: Present a navigable campaign map UI.
- R3.2: Map nodes correspond to encounters/events.
- R3.3: Locked nodes are visually distinct from unlocked nodes.
- R3.4: Selecting an unlocked node transitions into encounter.

---

## Feature 4: AI Opponent Scaling


### Requirements
- R4.1: Support multiple AI difficulty profiles.
- R4.2: Increase AI difficulty with campaign progression.
- R4.3: Keep AI behavior parameters configurable without code changes.

---

## Feature 5: Tutorial Integration


### Requirements
- R5.1: Tutorial is the first required campaign stage.
- R5.2: Completing tutorial unlocks first combat node.
- R5.3: Record tutorial completion in campaign progress data.

---

## Feature 6: Metrics and Validation Support

### Status
In progress

### Requirements
- R6.1: Expose counts of implemented features and requirements.
- R6.2: Clearly identify unimplemented features.
- R6.3: Make completion data verifiable in docs/logs.

---

## Completed Requirements (As of Week 7)

- R1.2 completed: Discrete campaign stages/nodes are defined through campaign flow design.
- R3.1 completed: Navigable campaign map UI implemented.
- R3.2 completed: Map locations are tied to encounter/opponent fights.
- R3.4 completed: Selecting locations navigates into associated fights.

---

## Current Implementation Summary

- Implemented now: campaign flow design, world map navigation core, location-based encounters, updated main menu flow.
- In progress: campaign stage persistence, tutorial-to-campaign integration, progress metrics/reporting.
- Planned next: spell unlock progression and AI scaling profiles.

---

## Retrospective

### What Went Well
- Implementing the map went well.
- Map navigation and location encounter setup were completed successfully.

### What Did Not Go Well
- Scheduling did not go well.
- Time coordination made it harder to keep work pacing consistent.

---

## Burndown Snapshot

- Total features identified: 6
- Lines of code ~ 342
- Fully completed features: 1/6 (`F3`) = 16.7%
- Features in progress: 2/6  = 33.3%
- Features not started/planned: 3/6  = 50.0%

---

## Burndown Snapshot (Requirements)

- Total requirements identified: 21
- Completed requirements: 4/21 = 19.0%
- Remaining requirements: 17/21 = 81.0%
