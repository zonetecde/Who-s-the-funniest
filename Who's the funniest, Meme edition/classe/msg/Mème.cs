using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Who_s_the_funniest.classe.msg
{
    public class Mème
    {
        public Mème(string nom, string url, List<ZoneTexte> textes, string fromId, string fromUsername)
        {
            Nom = nom;
            Url = url;
            Textes = textes;
            FromId = fromId;
            FromUsername = fromUsername;
        }

        public string Nom { get; set; }
        public string Url { get; set; }
        public List<ZoneTexte> Textes { get; set; }
        public string FromId { get; set; }
        public string FromUsername { get; set; }

        public List<int> Notes = new List<int>();
    }
}
