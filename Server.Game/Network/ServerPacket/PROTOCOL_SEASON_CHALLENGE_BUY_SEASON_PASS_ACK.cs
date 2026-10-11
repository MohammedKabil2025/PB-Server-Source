//namespace Server.Game.Network.ServerPacket
//{
//    public class PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK : GameServerPacket
//    {
//        private readonly uint Error;
//        public PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(uint Error)
//        {
//            this.Error = Error;
//        }
//        public override void Write()
//        {
//            WriteH(8454);
//            WriteH(0);
//            WriteD(Error);
//        }
//    }
//}

using Server.Game.Network;

public class PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK : GameServerPacket
{
    private readonly uint Error;
    private readonly int PassType;
    private readonly int Price;
    private readonly int Currency;
    private readonly int RemainDays;

    public PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
        uint error, int passType, int price, int currency, int remainDays)
    {
        Error = error;
        PassType = passType;
        Price = price;
        Currency = currency;
        RemainDays = remainDays;
    }

    public override void Write()
    {
        WriteH(8454);
        WriteH(0);
        WriteD(Error);
        WriteD(PassType);
        WriteD(Price);
        WriteD(Currency);
        WriteD(RemainDays);
    }
}
