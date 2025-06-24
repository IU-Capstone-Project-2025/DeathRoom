using MessagePack;

namespace DeathRoom.Common.dto
{
	public enum ProjectionCode {
		xy,
		xz,
		yz
	}

    [MessagePackObject]
    public struct Vector3
    {
        [Key(0)]
        public float X { get; set; }
        [Key(1)]
        public float Y { get; set; }
        [Key(2)]
        public float Z { get; set; }

		public Vector3(float x, float y, float z) {
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		public Vector3 projection(ProjectionCode code) {
			switch (code) {
				case ProjectionCode.xy:
					return new Vector3(this.X, this.Y, 0);

				case ProjectionCode.xz:
					return new Vector3(this.X, 0, this.Z);

				case ProjectionCode.yz:
					return new Vector3(0, this.Y, this.Z);
			}
			return this;
		}

		public static float operator!(Vector3 operand)
			=> (float)Math.Sqrt(operand.X*operand.X + operand.Y*operand.Y + operand.Z*operand.Z);

		public static Vector3 operator+(Vector3 left, Vector3 right)
			=> new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

		public static Vector3 operator-(Vector3 left, Vector3 right)
			=> new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

		public static float operator*(Vector3 left, Vector3 right)
			=> left.X*right.X + left.Y*right.Y + left.Z*right.Z;
    }
} 
