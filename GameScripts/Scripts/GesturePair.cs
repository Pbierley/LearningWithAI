using System;
using UnityEngine;

[Serializable]
public struct GesturePair : IEquatable<GesturePair>
{
    [Tooltip("Gesture detected on the left hand")]
    public GestureLabel Left;

    [Tooltip("Gesture detected on the right hand")]
    public GestureLabel Right;

    public GesturePair(GestureLabel left, GestureLabel right)
    {
        this.Left = left;
        this.Right = right;
    }

    public override string ToString()
    {
        return $"(Left:{Left},Right:{Right})";
    }

    public bool Equals(GesturePair other) => Left == other.Left && Right == other.Right;

    public override bool Equals(object obj) => obj is GesturePair other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Left, Right);

    public static bool operator ==(GesturePair a, GesturePair b) => a.Equals(b);
    public static bool operator !=(GesturePair a, GesturePair b) => !a.Equals(b);
}