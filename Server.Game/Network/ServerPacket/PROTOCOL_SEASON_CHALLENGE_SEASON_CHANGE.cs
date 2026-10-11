using Server.Game.Data.Models;
using Server.Game.Network;

public class PROTOCOL_SEASON_CHALLENGE_SEASON_CHANGE : GameServerPacket
{
    private Account Player;

    public PROTOCOL_SEASON_CHALLENGE_SEASON_CHANGE(Account Player)
    {
        this.Player = Player;
    }

    public override void Write()
    {
        WriteH(8450);
        WriteH(0);
        WriteC(1);
        WriteC(0);
    }
}