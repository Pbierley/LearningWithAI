
using System;
using System.Collections.Generic;

[Serializable]
public struct GestureSequence : IEquatable<GestureSequence>
{
    public List<GesturePair> Steps;

    public GestureSequence(List<GesturePair> steps)
    {
        this.Steps = new List<GesturePair>(steps);
    }

    public override string ToString() => string.Join(" -> ", Steps);

    public bool Equals(GestureSequence other)
    {
        if (Steps.Count != other.Steps.Count) return false;
        for (int i = 0; i < Steps.Count; i++)
        {
            if (Steps[i] != other.Steps[i]) return false;
        }
        return true;
    }

    public override bool Equals(object obj) => obj is GestureSequence other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var step in Steps)
            hash = hash * 31 + step.GetHashCode();
        return hash;
    }
  
    
}