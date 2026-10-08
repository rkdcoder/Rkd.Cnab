namespace Rkd.Cnab.Models.Config
{
    /// <summary>
    /// Tipo de registro de um layout (header, detalhe, trailer, segmento...).
    /// </summary>
    public class CnabObjeto
    {
        /// <summary>Nome do registro (chave em <c>CnabResponse.Data</c>).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Regras combinadas com AND para reconhecer a linha.</summary>
        public List<CnabIdentificador> Identificadores { get; set; } = new List<CnabIdentificador>();

        /// <summary>Campos extraídos da linha.</summary>
        public List<CnabAtributo> Atributos { get; set; } = new List<CnabAtributo>();
    }
}
