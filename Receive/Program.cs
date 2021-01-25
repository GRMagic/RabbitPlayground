using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Receive
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

            // Consumidor
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine("<< {0}", message);
            };

            channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);

            Console.ReadLine();
        }
    }
}
