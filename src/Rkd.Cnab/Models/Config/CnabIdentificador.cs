namespace Rkd.Cnab.Models.Config
{
    /// <summary>
    /// Regra de identificação de um registro: o texto <see cref="Valor"/> deve
    /// estar presente a partir da <see cref="Posicao"/> da linha.
    /// </summary>
    public class CnabIdentificador
    {
        /// <summary>Posição inicial (base 1) onde o valor é procurado.</summary>
        public int Posicao { get; set; }

        /// <summary>Texto esperado a partir da posição (comparação ordinal, diferencia maiúsculas/minúsculas).</summary>
        public string Valor { get; set; } = string.Empty;
    }
}
