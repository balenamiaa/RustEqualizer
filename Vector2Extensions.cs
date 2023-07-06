namespace RustEQ;

using System.Numerics;

public static class Vector2Extensions
{
    public static Vector2 DivideByScalar(this Vector2 vector, float scalar)
    {
        return new Vector2(vector.X / scalar, vector.Y / scalar);
    }
}
