using System;
using EzLib.Services;

namespace EzLib.Services
{
    public class HiNetSmsSender
    {
        private readonly Hiair2Net _hiair;
        private readonly string _serverIp;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public HiNetSmsSender(string serverIp, int port, string username, string password)
        {
            _hiair = new Hiair2Net();
            _serverIp = serverIp;
            _port = port;
            _username = username;
            _password = password;
        }

        public int Connect()
        {
            return _hiair.StartCon(_serverIp, _port, _username, _password);
        }

        public string GetMessage()
        {
            return _hiair.Get_Message();
        }

        public int SendMessage(string phoneNumber, string message)
        {
            return _hiair.SendMsg(phoneNumber, message);
        }

        public void Disconnect()
        {
            _hiair.EndCon();
        }
    }
}