using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Who_s_the_funniest.classe.msg
{
    /// <summary>
    /// Mesage content pour informé que x à rejoint la partie "id"
    /// </summary>
    public class Join
    {
        public Join(string username, string id, int partyId)
        {
            Username = username;
            Id = id;
            PartyId = partyId;
        }

        public string Username { get; set; }
        public string Id { get; set; }
        public int PartyId { get; set; }
    }
}
