using DeathRoom.Common.Network;
using MessagePack;

[MessagePackObject]
public class PlayerAnimationPacket : IPacket
{
    [Key(0)]
    public int PlayerId { get; set; }

    [Key(1)]
    public long ClientTick { get; set; }

    [Key(2)]
    public Dictionary<string, bool> BoolParams { get; set; } = new();

    [Key(3)]
    public Dictionary<string, float> FloatParams { get; set; } = new();

    [Key(4)]
    public Dictionary<string, int> IntParams { get; set; } = new();

}