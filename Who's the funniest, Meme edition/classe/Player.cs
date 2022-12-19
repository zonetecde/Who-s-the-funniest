using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Who_s_the_funniest.classe
{
    public class Player
    {
        public Player(string username, string id)
        {
            Username = username;
            Id = id;
        }

        public string Username { get; }
        public string Id { get; }
    }
}
