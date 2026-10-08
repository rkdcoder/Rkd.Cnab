namespace Rkd.Cnab.Models.Runtime
{
    /// <summary>
    /// Resultado da conversão de um arquivo CNAB.
    /// </summary>
    public sealed class CnabResponse
    {
        /// <summary><c>false</c> quando houve erro estrutural (conteúdo vazio, layout inexistente).</summary>
        public bool Success { get; private set; } = true;

        /// <summary><c>true</c> quando todas as linhas foram reconhecidas e convertidas.</summary>
        public bool CompletelyConverted { get; private set; }

        /// <summary>Mensagem resumida do processamento.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>Nome do layout efetivamente utilizado.</summary>
        public string LayoutUtilizado { get; internal set; } = string.Empty;

        /// <summary>Total de linhas não vazias lidas do arquivo.</summary>
        public int TotalLinhas { get; internal set; }

        /// <summary>Total de linhas com erro.</summary>
        public int TotalErros => Erros.Count;

        /// <summary>Registros convertidos, agrupados pelo nome do objeto do layout.</summary>
        public Dictionary<string, List<Dictionary<string, string>>> Data { get; }
            = new();

        /// <summary>Linhas que falharam, com o motivo.</summary>
        public List<CnabLinhaErro> Erros { get; } = new();

        /// <summary>Registra uma linha com erro.</summary>
        public void AddErro(string linha, string motivo)
        {
            Erros.Add(new CnabLinhaErro
            {
                Motivo = motivo,
                Conteudo = linha
            });
        }

        /// <summary>Marca a resposta como falha estrutural.</summary>
        public CnabResponse Fail(string message)
        {
            Success = false;
            CompletelyConverted = false;
            Message = message;
            return this;
        }

        /// <summary>Consolida flags e mensagem ao final do processamento.</summary>
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
