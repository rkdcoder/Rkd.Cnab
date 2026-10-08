namespace Rkd.Cnab.Models.Config
{
    /// <summary>
    /// Campo posicional de um registro CNAB (posições base 1, inclusivas).
    /// </summary>
    public class CnabAtributo
    {
        /// <summary>Nome do campo no resultado (chave do dicionário).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Posição inicial (base 1).</summary>
        public int De { get; set; }

        /// <summary>Posição final (base 1, inclusiva).</summary>
        public int Ate { get; set; }
    }
}
