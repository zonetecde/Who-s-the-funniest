using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Who_s_the_funniest.classe
{
    public class WhosTheFunniestMessage
    {
        public WhosTheFunniestMessage(MessageType msgType, string content)
        {
            MsgType = msgType;
            Content = content;
        }

        public MessageType MsgType { get; }
        public string Content { get; }
    }

    public enum MessageType
    {
        NEW_PARTY,
        JOIN_PARTY,
        SEND_ME_GAMES,
        HERE_IS_THE_GAMES,
        GAME_LEFT,
        GAME_START
    }
}
