namespace Rkd.Cnab.Models.Runtime
{
    /// <summary>
    /// Linha do arquivo que não pôde ser convertida.
    /// </summary>
    public class CnabLinhaErro
    {
        /// <summary>Descrição objetiva do problema.</summary>
        public string Motivo { get; set; } = string.Empty;

        /// <summary>Conteúdo original da linha.</summary>
        public string Conteudo { get; set; } = string.Empty;
    }
}
