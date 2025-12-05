using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cotacoes
{
    // Classe para representar uma cotação individual
    public class CotacaoDolar
    {
        public System.DateTime Data { get; set; }
        public decimal Valor { get; set; }
        public decimal FatorMultiplicador => Valor * 10000m;

        public CotacaoDolar(System.DateTime data, decimal valor)
        {
            Data = data;
            Valor = valor;
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:N6}", Valor);
        }
    }
}
