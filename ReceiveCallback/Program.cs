using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace ReceiveCallback
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
            //channel.QueueDeclare(queue: "modelo", durable: false, exclusive: false, autoDelete: false, arguments: null);

            // Consumidor
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                var modelo = System.Text.Json.JsonSerializer.Deserialize<Modelo>(message);
                Console.WriteLine($"<< {modelo.Nome} tem {modelo.Idade} anos.");

                modelo.Idade++;

                Console.WriteLine($">> {modelo.Nome} tem {modelo.Idade} anos.");
                message = System.Text.Json.JsonSerializer.Serialize(modelo);
                body = System.Text.Encoding.UTF8.GetBytes(message);

                if (!string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
                {
                    var properties = channel.CreateBasicProperties();
                    properties.CorrelationId = ea.BasicProperties.CorrelationId;
                    channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo, basicProperties: properties, body: body);
                }
                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume(queue: "modelo", autoAck: false, consumer: consumer);

            Console.ReadLine();
        }
    }
}
