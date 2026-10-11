namespace Plugin.Core.Models
{
    public class FriendModel
    {
        public long ObjectId { get; set; }
        public long OwnerId { get; set; }
        public long PlayerId { get; set; }
        public int State { get; set; }
        public bool Removed { get; set; }
        public PlayerInfo Info { get; set; }
        public FriendModel(long PlayerId)
        {
            this.PlayerId = PlayerId;
            Info = new PlayerInfo(PlayerId);
        }
        public FriendModel(long PlayerId, int Rank, int NickColor, string Nickname, bool IsOnline, AccountStatus Status)
        {
            this.PlayerId = PlayerId;
            SetModel(PlayerId, Rank, NickColor, Nickname, IsOnline, Status);
        }
        public void SetModel(long PlayerId, int Rank, int NickColor, string Nickname, bool IsOnline, AccountStatus Status)
        {
            Info = new PlayerInfo(PlayerId, Rank, NickColor, Nickname, IsOnline, Status);
        }
    }
}