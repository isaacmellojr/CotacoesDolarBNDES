using System;
using System.Globalization;
using System.Threading;

namespace Cotacoes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TESTE COTAÇÃO DÓLAR BNDES ===\n");


            
            var processador = new ProcessadorCotacoesBNDES();

            while (true)
            {
                if (processador.CarregadoComSucesso)
                {
                    Console.Write("Informe a data para receber a cotação: ");
                    string data = Console.ReadLine();

                    DateTime datac = new DateTime();

                    if (DateTime.TryParseExact(data, "dd/MM/yyyy"
                        , CultureInfo.InvariantCulture
                        , DateTimeStyles.None
                        , out datac))
                    {
                        if (processador.ExisteCotacaoParaData(datac.ToString("dd/MM/yyyy")))
                        {
                            var cotacaoObj = processador.ObterCotacaoPorData(datac.ToString("dd/MM/yyyy"));
                            Console.WriteLine(cotacaoObj.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Não existe cotação para essa data");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Data inválida");
                    }
                }
                else {
                    Console.WriteLine("Aguardando carregamento");
                    Thread.Sleep(5000);
                }
                
            }

            Console.WriteLine("\n=== FIM DO TESTE ===");
            Console.ReadKey();
        }
    }
}
