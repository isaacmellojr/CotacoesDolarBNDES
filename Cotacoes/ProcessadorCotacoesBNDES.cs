using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cotacoes
{


    public class ProcessadorCotacoesBNDES
    {
        private const string URL_COTACOES = "https://www.bndes.gov.br/Moedas/um604.txt";
        private readonly HttpClient _httpClient;

        public List<CotacaoDolar> listaCotacoes { get; private set; }
        public DateTime DataUltimaAtualizacao { get; private set; }
        public bool CarregadoComSucesso { get; private set; }
        public string MensagemErro { get; private set; }

        /// <summary>
        /// Construtor que inicializa o HttpClient e carrega as cotações automaticamente
        /// </summary>
        public ProcessadorCotacoesBNDES()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            listaCotacoes = new List<CotacaoDolar>();
            CarregadoComSucesso = false;
            MensagemErro = string.Empty;

            // Carrega as cotações de forma síncrona no construtor
            CarregarCotacoesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Construtor que permite injetar um HttpClient customizado
        /// </summary>
        public ProcessadorCotacoesBNDES(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            listaCotacoes = new List<CotacaoDolar>();
            CarregadoComSucesso = false;
            MensagemErro = string.Empty;

            // Carrega as cotações de forma síncrona no construtor
            CarregarCotacoesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Obtém a cotação para uma data específica
        /// </summary>
        /// <param name="dataString">Data no formato DD/MM/YYYY</param>
        /// <returns>Cotação correspondente ou null se não encontrada</returns>
        public CotacaoDolar ObterCotacaoPorData(string dataString)
        {
            // Se não carregou com sucesso, retorna null
            if (!CarregadoComSucesso || listaCotacoes.Count == 0)
                return null;

            // Tenta converter a string para DateTime
            if (!DateTime.TryParseExact(
                dataString?.Trim() ?? string.Empty,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dataBusca))
            {
                return null;
            }

            // Busca exata pela data
            var cotacaoExata = listaCotacoes.FirstOrDefault(c => c.Data.Date == dataBusca.Date);
            
            return cotacaoExata;

        }

        /// <summary>
        /// Força a recarga das cotações do site do BNDES
        /// </summary>
        public async Task<bool> RecarregarCotacoesAsync()
        {
            try
            {
                await CarregarCotacoesAsync();
                return CarregadoComSucesso;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtém a cotação mais recente disponível
        /// </summary>
        public CotacaoDolar ObterCotacaoMaisRecente()
        {
            if (!CarregadoComSucesso || listaCotacoes.Count == 0)
                return null;

            return listaCotacoes.OrderByDescending(c => c.Data).FirstOrDefault();
        }

        /// <summary>
        /// Verifica se existe cotação para uma data específica
        /// </summary>
        public bool ExisteCotacaoParaData(string dataString)
        {
            return ObterCotacaoPorData(dataString) != null;
        }

        /// <summary>
        /// Obtém o intervalo de datas disponíveis
        /// </summary>
        public (DateTime? DataInicial, DateTime? DataFinal) ObterIntervaloDatas()
        {
            if (!CarregadoComSucesso || listaCotacoes.Count == 0)
                return (null, null);

            return (listaCotacoes.Min(c => c.Data), listaCotacoes.Max(c => c.Data));
        }

        /// <summary>
        /// Método privado para carregar as cotações
        /// </summary>
        private async Task CarregarCotacoesAsync()
        {
            try
            {
                // Faz a requisição HTTP para obter o conteúdo
                string conteudo = await _httpClient.GetStringAsync(URL_COTACOES);

                // Processa o conteúdo
                var novasCotacoes = ProcessarConteudo(conteudo);

                // Atualiza as propriedades
                listaCotacoes = novasCotacoes;
                DataUltimaAtualizacao = DateTime.Now;
                CarregadoComSucesso = true;
                MensagemErro = string.Empty;
            }
            catch (HttpRequestException ex)
            {
                MensagemErro = $"Erro na requisição HTTP: {ex.Message}";
                CarregadoComSucesso = false;
            }
            catch (TaskCanceledException)
            {
                MensagemErro = "Timeout na requisição ao BNDES";
                CarregadoComSucesso = false;
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao processar cotações: {ex.Message}";
                CarregadoComSucesso = false;
            }
        }

        /// <summary>
        /// Processa o conteúdo do arquivo TXT do BNDES
        /// </summary>
        private List<CotacaoDolar> ProcessarConteudo(string conteudo)
        {
            var cotacoes = new List<CotacaoDolar>();

            if (string.IsNullOrWhiteSpace(conteudo))
                return cotacoes;

            // Divide o conteúdo em linhas
            string[] linhas = conteudo.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            // Valida se tem pelo menos uma linha de dados além do cabeçalho
            if (linhas.Length < 2)
                return cotacoes;

            // Processa cada linha (ignora a primeira que é o cabeçalho)
            for (int i = 1; i < linhas.Length; i++)
            {
                string linha = linhas[i].Trim();

                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                // Processa a linha
                var cotacao = ProcessarLinhaCotacao(linha);
                if (cotacao != null)
                {
                    cotacoes.Add(cotacao);
                }
            }

            // Ordena por data (mais recente primeiro)
            return cotacoes.OrderByDescending(c => c.Data).ToList();
        }

        /// <summary>
        /// Processa uma linha individual do arquivo
        /// </summary>
        private CotacaoDolar ProcessarLinhaCotacao(string linha)
        {
            try
            {
                // Procura pelo separador ';'
                int posicaoSeparador = linha.IndexOf(';');
                if (posicaoSeparador < 0)
                    return null;

                // Extrai as partes
                string parteData = linha.Substring(0, posicaoSeparador).Trim();
                string parteValor = linha.Substring(posicaoSeparador + 1).Trim();

                // Tenta converter a data
                if (!DateTime.TryParseExact(
                    parteData,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime data))
                {
                    return null;
                }

                // Prepara o valor para conversão
                // Remove pontos de milhar e substitui vírgula por ponto
                string valorFormatado = parteValor
                    .Replace(".", "")
                    .Replace(",", ".");

                // Tenta converter o valor
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
