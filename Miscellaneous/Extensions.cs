using Godot;
using System;

// Simple class that adds a few versatile and useful operations
public static class Extensions
{
    // Shorthand for enums. It can handle enums with up to 8 values when using bytes, which is the number of states the player has
    public static bool MatchesEnum<T>(this T state1, T state2) where T : IComparable, IFormattable, IConvertible
    {
        return (Convert.ToByte(state1) & Convert.ToByte(state2)) > 0; 
    }

    // Sets a float between between two optional bounds    
    public static float Clamp(this float a, float? min = null, float? max = null)
    {
        if (min != null)
        {
            if (a < (float)min)
            {
                return (float)min;
            }
        }

        if (max != null)
        {
            if (a > (float)max)
            {
                return (float)max;
            }
        }

        return a;
    }

    // Linearly interpolates a float based on a weight
    public static float LinearInterpolate(this float from, float to, float weight)
    {
        return from + (weight * (to - from));
    }

    // Convenient access to calculate angle between two vectors
    public static float AngleFrom(this Vector2 vec, Vector2 refVec)
    {
        float refAngle = refVec.Angle();
        return vec.Angle() - refAngle;
    }
}