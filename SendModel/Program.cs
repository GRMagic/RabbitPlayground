using RabbitMQ.Client;
using System;

namespace SendModel
{
    class Program
    {
        static void Main(string[] args)
        {
            // Conexão
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Declaração da fila
            channel.QueueDeclare(queue: "modelo", durable: false, exclusive: false, autoDelete: false, arguments: null);

            // Envio da mensagem
            Console.WriteLine("Enviando o modelo");
            var model = new Modelo {
                Idade = 30,
                Nome = "Gustavo"
            };

            var message = System.Text.Json.JsonSerializer.Serialize(model);
            var body = System.Text.Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "", routingKey: "modelo", basicProperties: null, body: body);
            Console.WriteLine(">> {0}", message);
        }
    }
}
