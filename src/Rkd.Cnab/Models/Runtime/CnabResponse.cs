namespace Rkd.Cnab.Models.Runtime
{
    public sealed class CnabResponse
    {
        public bool Success { get; private set; } = true;
        public bool CompletelyConverted { get; private set; }

        public string Message { get; private set; }

        public string LayoutUtilizado { get; internal set; }
        public int TotalLinhas { get; internal set; }
        public int TotalErros => Erros.Count;

        public Dictionary<string, List<Dictionary<string, string>>> Data { get; }
            = new();

        public List<CnabLinhaErro> Erros { get; } = new();

        public void AddErro(string linha, string motivo)
        {
            Erros.Add(new CnabLinhaErro
            {
                Motivo = motivo,
                Conteudo = linha
            });
        }

        public CnabResponse Fail(string message)
        {
            Success = false;
            CompletelyConverted = false;
            Message = message;
            return this;
        }

        public void Finalizar()
        {
            if (!Success)
                return;

            CompletelyConverted = Erros.Count == 0;
            Message = CompletelyConverted
                ? "Arquivo convertido com sucesso."
                : "Conversão concluída com inconsistências (verifique a lista de erros).";
        }
    }
}
