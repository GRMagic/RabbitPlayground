using RabbitMQ.Client;
using System;

namespace Send
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
            channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

            // Envio da mensagem
            Console.WriteLine("Digite uma mensagem para enviar");
            string message;
            do
            {
                message = Console.ReadLine();
                var body = System.Text.Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: null, body: body);
                Console.WriteLine(">> {0}", message);

            } while (!string.IsNullOrEmpty(message));
        }
    }
}
