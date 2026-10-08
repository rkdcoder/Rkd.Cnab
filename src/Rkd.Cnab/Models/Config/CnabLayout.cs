namespace Rkd.Cnab.Models.Config
{
    /// <summary>
    /// Layout CNAB completo (ex.: CNAB 240 ou 400) com seus tipos de registro.
    /// </summary>
    public class CnabLayout
    {
        /// <summary>Nome lógico do layout (usado em <c>CnabConverter.Convert</c>, sem diferenciar maiúsculas/minúsculas).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Tamanho fixo esperado para cada linha do arquivo.</summary>
        public int TamanhoLinha { get; set; }

        /// <summary>Tipos de registro (header, detalhe, trailer, segmentos...). O primeiro que casar vence.</summary>
        public List<CnabObjeto> Objetos { get; set; } = new List<CnabObjeto>();
    }
}
