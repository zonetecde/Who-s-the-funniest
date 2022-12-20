using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Who_s_the_funniest.classe.msg
{
    /// <summary>
    /// Zone de texte d'un mème
    /// </summary>
    public class ZoneTexte
    {
        public ZoneTexte(double canvasLeft, double canvasTop, double width, double height, string texte, Brush foreground, Brush background)
        {
            CanvasLeft = canvasLeft;
            CanvasTop = canvasTop;
            Width = width;
            Height = height;
            Texte = texte;
            Foreground = foreground;
            Background = background;
        }

        public double CanvasLeft { get; set; }
        public double CanvasTop { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Texte { get; set; }
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
    }
}
