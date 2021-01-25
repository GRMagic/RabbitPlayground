namespace SendCallback
{
    class ComRetorno : IRequest<Retorno>
    {
        public int Idade { get; set; }
        public string Nome { get; set; }
    }

    class Retorno
    {
        public int Resposta { get; set; }
    }
}
