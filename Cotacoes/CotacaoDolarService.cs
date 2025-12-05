using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cotacoes
{
    public class CotacaoDolarService
    {
        /// <summary>
        /// Obtém a cotação do dólar para uma data específica a partir da string de cotações do BNDES
        /// </summary>
        /// <param name="conteudoTxt">Conteúdo obtido de https://www.bndes.gov.br/Moedas/um604.txt</param>
        /// <param name="dataString">Data no formato DD/MM/YYYY para buscar a cotação</param>
        /// <returns>Valor da cotação como decimal</returns>
        /// <exception cref="System.ArgumentException">Quando os parâmetros são inválidos</exception>
        /// <exception cref="System.FormatException">Quando a data não está no formato correto</exception>
        /// <exception cref="System.InvalidOperationException">Quando a cotação não é encontrada para a data</exception>
        public static decimal ObterCotacaoDolarPorData(string conteudoTxt, string dataString)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(conteudoTxt))
            {
                throw new System.ArgumentException("O conteúdo do arquivo TXT não pode ser nulo ou vazio", nameof(conteudoTxt));
            }

            if (string.IsNullOrWhiteSpace(dataString))
            {
                throw new System.ArgumentException("A data não pode ser nula ou vazia", nameof(dataString));
            }

            // Tentar parse da data
            System.DateTime dataBusca;
            try
            {
                dataBusca = System.DateTime.ParseExact(dataString.Trim(), "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (System.FormatException ex)
            {
                throw new System.FormatException(
                    $"A data '{dataString}' não está no formato DD/MM/YYYY válido. Exemplo: 01/12/2025", ex);
            }

            // Processar o conteúdo do arquivo
            var cotações = ProcessarConteudoCotacoes(conteudoTxt);

            // Buscar a cotação para a data especificada
            if (cotações.TryGetValue(dataBusca, out decimal valorCotacao))
            {
                return valorCotacao;
            }
            else
            {
                // Tentar encontrar datas próximas (útil para fins de semana e feriados)
                var dataAnterior = EncontrarCotacaoMaisProxima(cotações, dataBusca);

                if (dataAnterior.HasValue)
                {
                    return cotações[dataAnterior.Value];
                }

                // Se não encontrou nem data próxima
                throw new System.InvalidOperationException(
                    $"Cotação do dólar não encontrada para a data {dataString}. " +
                    $"Datas disponíveis: {string.Join(", ", cotações.Keys.Take(5).Select(d => d.ToString("dd/MM/yyyy")))}" +
                    (cotações.Count > 5 ? "..." : ""));
            }
        }

        /// <summary>
        /// Processa o conteúdo do arquivo TXT e retorna um dicionário com datas e valores
        /// </summary>
        private static Dictionary<System.DateTime, decimal> ProcessarConteudoCotacoes(string conteudoTxt)
        {
            var cotações = new Dictionary<System.DateTime, decimal>();

            // Dividir por linhas
            string[] linhas = conteudoTxt.Split(new[] { "\r\n", "\r", "\n" },
                System.StringSplitOptions.RemoveEmptyEntries);

            // Validar que temos pelo menos a linha de cabeçalho
            if (linhas.Length < 2)
            {
                throw new System.ArgumentException(
                    "O conteúdo do arquivo está incompleto. Esperado pelo menos linha de cabeçalho e uma linha de dados.");
            }

            // Validar cabeçalho
            string cabecalhoEsperado = "   data   ;     valor";
            if (!linhas[0].Trim().Equals(cabecalhoEsperado, System.StringComparison.OrdinalIgnoreCase))
            {
                // Tentar ser flexível com o cabeçalho
                if (!linhas[0].Contains("data") || !linhas[0].Contains("valor"))
                {
                    throw new System.ArgumentException(
                        "Cabeçalho do arquivo não está no formato esperado. Esperado: '   data   ;     valor'");
                }
            }

            // Processar cada linha de dados
            for (int i = 1; i < linhas.Length; i++)
            {
                string linha = linhas[i].Trim();

                // Ignorar linhas vazias
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                // Processar a linha
                var (data, valor) = ProcessarLinhaCotacao(linha, i + 1);

                if (data.HasValue && valor.HasValue)
                {
                    // Evitar duplicatas - usar a primeira ocorrência
                    if (!cotações.ContainsKey(data.Value))
                    {
                        cotações.Add(data.Value, valor.Value);
                    }
                }
            }

            if (cotações.Count == 0)
            {
                throw new System.ArgumentException("Nenhuma cotação válida foi encontrada no arquivo.");
            }

            return cotações;
        }

        /// <summary>
        /// Processa uma linha individual de cotação
        /// </summary>
        private static (System.DateTime? Data, decimal? Valor) ProcessarLinhaCotacao(string linha, int numeroLinha)
        {
            try
            {
                // Dividir pelo ponto e vírgula
                string[] partes = linha.Split(new string[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries);

                if (partes.Length != 2)
                {
                    // Tentar regex para ser mais flexível
                    var match = System.Text.RegularExpressions.Regex.Match(linha,
                        @"(\d{2}/\d{2}/\d{4})\s*;\s*([\d,]+)");

                    if (match.Success)
                    {
                        partes = new[] { match.Groups[1].Value, match.Groups[2].Value };
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Linha {numeroLinha}: Formato inválido - '{linha}'");
                        return (null, null);
                    }
                }

                // Limpar os valores
                string dataString = partes[0].Trim();
                string valorString = partes[1].Trim();

                // Parse da data
                if (!System.DateTime.TryParseExact(dataString, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out System.DateTime data))
                {
                    System.Diagnostics.Debug.WriteLine($"Linha {numeroLinha}: Data inválida - '{dataString}'");
                    return (null, null);
                }

                // Parse do valor - substituir vírgula por ponto para parse decimal
                // Remover possíveis pontos de milhar
                valorString = valorString.Replace(".", "").Replace(",", ".");

                if (!decimal.TryParse(valorString,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal valor))
                {
                    System.Diagnostics.Debug.WriteLine($"Linha {numeroLinha}: Valor inválido - '{partes[1]}'");
                    return (null, null);
                }

                return (data, valor);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar linha {numeroLinha} ('{linha}'): {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Encontra a cotação mais próxima (data anterior) quando não há cotação exata
        /// </summary>
        private static System.DateTime? EncontrarCotacaoMaisProxima(
            Dictionary<System.DateTime, decimal> cotações,
            System.DateTime dataBusca)
        {
            // Ordenar datas disponíveis
            var datasOrdenadas = cotações.Keys.OrderByDescending(d => d).ToList();

            // Encontrar a data mais próxima anterior
            foreach (var data in datasOrdenadas)
            {
                if (data <= dataBusca)
                {
                    return data;
                }
            }

            // Se não encontrou data anterior, retorna a mais próxima no geral
            if (datasOrdenadas.Any())
            {
                // Encontrar a data mais próxima em valor absoluto
                return datasOrdenadas.OrderBy(d => Math.Abs((d - dataBusca).TotalDays)).First();
            }

            return null;
        }

        /// <summary>
        /// Versão assíncrona que faz a requisição diretamente ao site do BNDES
        /// </summary>
        public static async System.Threading.Tasks.Task<decimal> ObterCotacaoDolarPorDataOnlineAsync(
            string dataString,
            System.Threading.CancellationToken cancellationToken = default)
        {
            string url = "https://www.bndes.gov.br/Moedas/um604.txt";

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                try
                {
                    // Configurar timeout
                    httpClient.Timeout = System.TimeSpan.FromSeconds(30);

                    // Fazer a requisição
                    string conteudo = await httpClient.GetStringAsync(url);

                    // Usar o método principal para processar
                    return ObterCotacaoDolarPorData(conteudo, dataString);
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    throw new System.Net.Http.HttpRequestException(
                        $"Erro ao acessar o site do BNDES ({url}): {ex.Message}", ex);
                }
                catch (System.Threading.Tasks.TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new System.OperationCanceledException("Operação cancelada pelo usuário", cancellationToken);
                }
            }
        }
    }

}