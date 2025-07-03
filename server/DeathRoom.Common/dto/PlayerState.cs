using MessagePack;

namespace DeathRoom.Common.dto
{
    [MessagePackObject]
    public class PlayerState
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public required string Username { get; set; }
        [Key(2)]
        public Vector3Serializable Position { get; set; }
        [Key(3)]
        public Vector3Serializable Rotation { get; set; }
        [Key(4)]
        public int HealthPoint { get; set; }
        [Key(5)]
        public int MaxHealthPoint { get; set; } = 100;
		[Key(6)]
		public int ArmorPoint { get; set; }
		[Key(7)]
		public int MaxArmorPoint { get; set; } = 100;
		[Key(8)]
		public long ArmorExpirationTick { get; set; }
        [IgnoreMember]
        public Queue<PlayerSnapshot> Snapshots { get; } = new Queue<PlayerSnapshot>();

        public PlayerState Clone()
        {
            return new PlayerState
            {
                Id = this.Id,
                Username = this.Username,
                Position = new Vector3Serializable(this.Position.X, this.Position.Y, this.Position.Z),
                Rotation = new Vector3Serializable(this.Rotation.X, this.Rotation.Y, this.Rotation.Z),
                HealthPoint = this.HealthPoint
            };
        }

		public bool TakeDamage(int damage, long tick) {
			if (this.ArmorExpirationTick > tick) { this.ArmorPoint = 0; }
			if (this.ArmorPoint >= damage) {
				this.ArmorPoint -= damage;
				return false;
			} else if ( this.ArmorPoint > 0) {
				damage -= this.ArmorPoint;
				this.ArmorPoint = 0;
			}
			
			this.HealthPoint -= damage;
			if (this.HealthPoint <= 0) { 
				this.HealthPoint = 0; // Не даём здоровью стать отрицательным
				return true; 
			}
			return false;
		}

		public void ObtainArmor(long tick) {
			this.ArmorPoint = this.MaxArmorPoint;
			this.ArmorExpirationTick = tick;
		}
    }
} 
