using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SendCallback
{

    partial class Mensageiro : IDisposable
    {
        private IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _consumerResponse;
        private EventingBasicConsumer _consumer;

        private readonly string _id;
        protected List<string> filas = new List<string>();
        protected Dictionary<string, Chamada> chamadas = new Dictionary<string, Chamada>();
        protected Dictionary<string, Delegate> Tratadores = new Dictionary<string, Delegate>();

        public Mensageiro(IConnectionFactory connectionFactory)
        {
            // Conexão
            _connectionFactory = connectionFactory;
            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();

            _id = Guid.NewGuid().ToString();
            _consumerResponse = new EventingBasicConsumer(_model);
            _consumerResponse.Received += RespostaRecebida;
            _model.QueueDeclare(queue: _id, durable: false, exclusive: true, autoDelete: true, arguments: null);
            _model.BasicConsume(queue: _id, autoAck: true, consumer: _consumerResponse);

            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += ChamadaRecebida;
        }

        public void Dispose()
        {
            _model?.Dispose();
            _connection?.Dispose();
        }

        public async Task<TResponse> EnviarAsync<TResponse>(IRequest<TResponse> request, bool fireAndForget = false) where TResponse : class
        {
            var fila = GerarNomeFila(request.GetType());
            CriarFila(fila);

            var chamada = new Chamada(request);
            if(!fireAndForget)
                chamadas[chamada.Id] = chamada;

            var properties = CriarPropriedades(chamada, fireAndForget);

            _model.BasicPublish(exchange: "", routingKey: fila, basicProperties: properties, body: chamada.ConstruirCorpo());

            if (!fireAndForget)
            {
                await chamada.Aguardar();

                chamadas.Remove(chamada.Id);


                if (chamada.Sucesso)
                {
                    return chamada.ConstruirResposta<TResponse>();
                }
                else 
                {
                    var msg = chamada.ConstruirResposta<string>();
                    throw new MensageiroRemoteException(msg);
                }
                
            }

            return null;
        }

        public async Task EnviarAsync(IBaseRequest request, bool fireAndForget = false)
        {
            var fila = GerarNomeFila(request.GetType());
            CriarFila(fila);

            var chamada = new Chamada(request);
            if (!fireAndForget)
                chamadas[chamada.Id] = chamada;

            var properties = CriarPropriedades(chamada, fireAndForget);

            _model.BasicPublish(exchange: "", routingKey: fila, basicProperties: properties, body: chamada.ConstruirCorpo());
            if (!fireAndForget)
            {
                await chamada.Aguardar();

                chamadas.Remove(chamada.Id);

                if (!chamada.Sucesso)
                {
                    var msg = chamada.ConstruirResposta<string>();
                    throw new MensageiroRemoteException(msg);
                }
            }
        }

        protected void CriarFila(string fila)
        {
            if (!filas.Contains(fila))
            {
                _model.QueueDeclare(queue: fila, durable: false, exclusive: false, autoDelete: false, arguments: null);
                filas.Add(fila);
            }
        }

        protected IBasicProperties CriarPropriedades(Chamada chamada, bool fireAndForget)
        {
            var properties = _model.CreateBasicProperties();
            properties.ContentType = "application/json";
            if (!fireAndForget)
            {
                properties.CorrelationId = chamada.Id;
                properties.ReplyTo = _id;
            }
            return properties;
        }

        private void RespostaRecebida(object sender, BasicDeliverEventArgs e)
        {
            var id = e.BasicProperties.CorrelationId;
            if (!string.IsNullOrEmpty(id))
            {
                var chamada = chamadas[id];
                chamada.Sucesso = (bool)e.BasicProperties.Headers["sucesso"];
                chamada.Resposta = e.Body.ToArray();
                chamada.Concluir();
            }
        }

        protected string GerarNomeFila(Type tipo) => tipo.Name.ToLower();

        public Mensageiro Tratar<T>(Action<T> tratamento)
        {
            var fila = GerarNomeFila(typeof(T));
            CriarFila(fila);
            Tratadores[fila] = tratamento;
            _model.BasicConsume(queue: fila, autoAck: false, consumer: _consumer);
            return this;
        }

        public Mensageiro Tratar<TRequest, TReturn>(Func<TRequest, TReturn> tratamento)
        {
            var fila = GerarNomeFila(typeof(TRequest));
            CriarFila(fila);
            Tratadores[fila] = tratamento;
            _model.BasicConsume(queue: fila, autoAck: false, consumer: _consumer);
            return this;
        }

        private void ChamadaRecebida(object sender, BasicDeliverEventArgs e)
        {
            Delegate tratador;
            object parametro;
            try
            {
                var fila = e.RoutingKey;
                tratador = Tratadores[fila];

                var json = System.Text.Encoding.UTF8.GetString(e.Body.ToArray());
                parametro = System.Text.Json.JsonSerializer.Deserialize(json, tratador.Method.GetParameters()[0].ParameterType);
            }
            catch
            {
                _model.BasicNack(e.DeliveryTag, false, true);
                return;
            }
            _model.BasicAck(e.DeliveryTag, false);
            object retorno;
            bool sucesso = true;
            try
            {
                retorno = tratador.DynamicInvoke(parametro);
            }
            catch(Exception exception)
            {
                retorno = exception.InnerException.Message;
                sucesso = false;
            }
            if (!string.IsNullOrEmpty(e.BasicProperties.ReplyTo))
            {
                
                var jsonResposta = System.Text.Json.JsonSerializer.Serialize(retorno);
                var bytesResposta = System.Text.Encoding.UTF8.GetBytes(jsonResposta);

                var properties = _model.CreateBasicProperties();
                properties.CorrelationId = e.BasicProperties.CorrelationId;
                properties.Headers = new Dictionary<string, object>();
                properties.Headers.Add("sucesso", sucesso);
                _model.BasicPublish(exchange: "", routingKey: e.BasicProperties.ReplyTo, basicProperties: properties, body: bytesResposta);

            }
        }
    }
}
