namespace DeathRoom.Domain;

public enum ProjectionCode {
    xy,
    xz,
    yz
}

public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3 projection(ProjectionCode code)
    {
        return code switch
        {
            ProjectionCode.xy => new Vector3(X, Y, 0),
            ProjectionCode.xz => new Vector3(X, 0, Z),
            ProjectionCode.yz => new Vector3(0, Y, Z),
            _ => this
        };
    }

    public static float operator !(Vector3 operand)
        => (float)Math.Sqrt(operand.X * operand.X + operand.Y * operand.Y + operand.Z * operand.Z);

    public static Vector3 operator +(Vector3 left, Vector3 right)
        => new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3 operator -(Vector3 left, Vector3 right)
        => new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static float operator *(Vector3 left, Vector3 right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z;
} 