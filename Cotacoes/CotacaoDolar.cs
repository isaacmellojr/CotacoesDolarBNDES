using System;
using System.Globalization;

namespace Cotacoes
{
    public class CotacaoDolar
    {
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }

        public CotacaoDolar(DateTime data, decimal valor)
        {
            Data = data;
            Valor = valor;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0:N6}", Valor);
        }
    }
}
