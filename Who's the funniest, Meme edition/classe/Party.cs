using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Who_s_the_funniest.classe
{
    public class Party
    {
        public Party(string nom, byte nbreJoueurMax, string langue, List<Player> players, int id)
        {
            Nom = nom;
            NbreJoueurMax = nbreJoueurMax;
            Langue = langue;

            Players = players;
            Id = id;
        }

        public string Nom { get; set; }
        public byte NbreJoueurMax { get; set; }
        public string Langue { get; set; }
        public List<Player> Players { get; set; }
        public int Id { get; }
    }
}
