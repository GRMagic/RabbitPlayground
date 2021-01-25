using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace SendCallback
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var mensageiro = new Mensageiro(factory);

            mensageiro.Tratar<ComRetorno,Retorno>(Teste);
            mensageiro.Tratar<SemRetorno>(TesteVoid);

            var resultado = await mensageiro.EnviarAsync(new ComRetorno { Idade = 30, Nome = "Gustavo" }, true);

            //Console.WriteLine(resultado.Resposta);
            await mensageiro.EnviarAsync(new SemRetorno { Idade = 30, Nome = "Gustavo" }, true);

            Console.ReadLine();
        }

        static Retorno Teste(ComRetorno modelo)
        {
            Console.WriteLine($"ComRetorno {modelo.Nome} -> {modelo.Idade}");
            //throw new Exception("Teste de erro");
            return new Retorno { Resposta = 123 };
        }

        static void TesteVoid(SemRetorno modelo)
        {
            //throw new Exception("Outro teste de erro");
            Console.WriteLine($"SemRetorno {modelo.Nome} -> {modelo.Idade}");
        }
    }
}
