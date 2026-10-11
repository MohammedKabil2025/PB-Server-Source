using Fleck;

namespace Server.Game.RconConnect
{
    public abstract class RconReceive
    {
        private string[] _parts;
        private int _offset = 2;
        private IWebSocketConnection _client;

        public void Init(string[] Parts)
        {
            _parts = Parts;
        }

        public void SetClientConnection(IWebSocketConnection client)
        {
            _client = client;
        }

        protected void SendMessageToClient(string message)
        {
            _client?.Send(message);
        }

        protected void SendError(string error)
        {
            SendMessageToClient("ERROR|" + error);
        }

        protected void SendSuccess(string message)
        {
            SendMessageToClient("SUCCESS|" + message);
        }


        public string PopString()
        {
            string Next = _parts[_offset];
            _offset++;
            return Next;
        }

        public int PopInt()
        {
            int Next = int.Parse(_parts[_offset]);
            _offset++;
            return Next;
        }

        public long PopLong()
        {
            long Next = long.Parse(_parts[_offset]);
            _offset++;
            return Next;
        }

        public abstract void Run();
    }
}
