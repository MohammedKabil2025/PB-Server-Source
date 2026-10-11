using Plugin.Core.Network;

namespace Server.Game.Network.ServerPacket
{
    public class PROTOCOL_AUTH_FRIEND_INSERT_ACK : GameServerPacket
    {
        private readonly uint Error;
        public PROTOCOL_AUTH_FRIEND_INSERT_ACK(uint Error)
        {
            this.Error = Error;
        }
        public override void Write()
        {
            WriteH(795);
            WriteD(Error);
            /*
             * 0x80001038 STR_TBL_GUI_BASE_FAIL_REQUEST_FRIEND_BY_LIMIT
             * 0x80001038 STR_TBL_GUI_BASE_NO_MORE_GET_FRIEND_STATE
             * 0x80001041 STR_TBL_GUI_BASE_ALREADY_REGIST_FRIEND_LIST
             * 
             */
        }
    }
}