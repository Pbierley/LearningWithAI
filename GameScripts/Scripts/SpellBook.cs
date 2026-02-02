using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spellbook", menuName = "Spells/SpellBook")]
public class SpellBook : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Sequence of left/right gestures needed to cast this spell")]
        public List<GesturePair> sequence;
        [Tooltip("The spell to cast if the sequence matches")]
        public Spell spell;
    }

    // Makes this in the editor
    public List<Entry> entries = new();

    public bool TryGetSpell(List<GesturePair> buffer, out Spell spell)
    {
        var current = new GestureSequence(buffer);
        foreach (var entry in entries)
        {
            var seq = new GestureSequence(entry.sequence);

            if (seq.Equals(current))
            {
                spell = entry.spell;
                return true;
            }
        }

        spell = null;
        return false;
    }

    private void OnValidate()
    {
        var seen = new HashSet<GestureSequence>();
        for (int i = 0; i < entries.Count; i++)
        {
            var seq = new GestureSequence(entries[i].sequence);
            if (!seen.Add(seq))
            {
                Debug.LogError(
                    $"Duplicate gesture sequence found in SpellBook '{name}' at entry {i}. " +
                    $"Gesture sequence: {string.Join(", ", entries[i].sequence)}"
                );
            }
        }
    }

    public List<Spell> GetAllSpells()
    {
        List<Spell> spells = new();
        foreach (var entry in entries)
        {
            if (entry.spell != null)
                spells.Add(entry.spell);
        }
        return spells;
    }

    public Spell GetSpellByName(string name)
    {
        foreach (var entry in entries)
        {
            if (entry.spell.name == name)
                return entry.spell;
        }
        return null;
    }

    public List<Entry> GetEntries()
    {
        return entries;
    }

    public Entry GetEntryByName(string name)
    {
        foreach (var entry in entries)
        {
            if (entry.spell.name == name)
                return entry;
        }
        return null;
    }

}
