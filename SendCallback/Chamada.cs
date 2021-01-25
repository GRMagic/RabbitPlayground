using System;
using System.Threading;
using System.Threading.Tasks;

namespace SendCallback
{
    partial class Mensageiro
    {
        protected class Chamada
        {
            public string Id { get; private set; } = Guid.NewGuid().ToString();
            protected CancellationTokenSource CancellationTokenSource { get; private set; } = new CancellationTokenSource();
            public byte[] Resposta { get; set; }
            public bool Sucesso { get; set; }
            private object Requisicao;

            public Chamada(object request)
            {
                Requisicao = request;
            }

            public async Task Aguardar()
            {
                try { 
                    await Task.Delay(-1, CancellationTokenSource.Token); 
                } catch { }
            }

            public void Concluir()
            {
                CancellationTokenSource.Cancel();
            }

            public byte[] ConstruirCorpo()
            {
                var json = System.Text.Json.JsonSerializer.Serialize(Requisicao);
                return System.Text.Encoding.UTF8.GetBytes(json);
            }

            public T ConstruirResposta<T>()
            {
                var json = System.Text.Encoding.UTF8.GetString(Resposta);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
        }
    }
}
