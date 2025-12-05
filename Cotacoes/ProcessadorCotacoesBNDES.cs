using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cotacoes
{
    public class ProcessadorCotacoesBNDES
    {
        private const string URL_COTACOES = "https://www.bndes.gov.br/Moedas/um604.txt";
        private readonly HttpClient _httpClient;

        private List<CotacaoDolar> listaCotacoes;
        public bool CarregadoComSucesso { get; private set; }

        public ProcessadorCotacoesBNDES()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            listaCotacoes = new List<CotacaoDolar>();
            CarregadoComSucesso = false;

            CarregarCotacoesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public CotacaoDolar ObterCotacaoPorData(string dataString)
        {
            if (!CarregadoComSucesso || listaCotacoes.Count == 0)
                return null;

            if (!DateTime.TryParseExact(
                dataString?.Trim() ?? string.Empty,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dataBusca))
            {
                return null;
            }

            return listaCotacoes.FirstOrDefault(c => c.Data.Date == dataBusca.Date);
        }

        public bool ExisteCotacaoParaData(string dataString)
        {
            return ObterCotacaoPorData(dataString) != null;
        }

        private async Task CarregarCotacoesAsync()
        {
            try
            {
                string conteudo = await _httpClient.GetStringAsync(URL_COTACOES);
                listaCotacoes = ProcessarConteudo(conteudo);
                CarregadoComSucesso = true;
            }
            catch
            {
                CarregadoComSucesso = false;
            }
        }

        private List<CotacaoDolar> ProcessarConteudo(string conteudo)
        {
            var cotacoes = new List<CotacaoDolar>();

            if (string.IsNullOrWhiteSpace(conteudo))
                return cotacoes;

            string[] linhas = conteudo.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            if (linhas.Length < 2)
                return cotacoes;

            for (int i = 1; i < linhas.Length; i++)
            {
                string linha = linhas[i].Trim();
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                var cotacao = ProcessarLinhaCotacao(linha);
                if (cotacao != null)
                    cotacoes.Add(cotacao);
            }

            return cotacoes.OrderByDescending(c => c.Data).ToList();
        }

        private CotacaoDolar ProcessarLinhaCotacao(string linha)
        {
            try
            {
                int posicaoSeparador = linha.IndexOf(';');
                if (posicaoSeparador < 0)
                    return null;

                string parteData = linha.Substring(0, posicaoSeparador).Trim();
                string parteValor = linha.Substring(posicaoSeparador + 1).Trim();

                if (!DateTime.TryParseExact(
                    parteData,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime data))
                {
                    return null;
                }

                string valorFormatado = parteValor.Replace(".", "").Replace(",", ".");

                if (!decimal.TryParse(
                    valorFormatado,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal valor))
                {
                    return null;
                }

                return new CotacaoDolar(data, valor);
            }
            catch
            {
                return null;
            }
        }
    }
}
