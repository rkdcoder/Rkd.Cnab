namespace Rkd.Cnab.Models.Config
{
    public class CnabObjeto
    {
        public string Nome { get; set; }
        public List<CnabIdentificador> Identificadores { get; set; } = new List<CnabIdentificador>();
        public List<CnabAtributo> Atributos { get; set; } = new List<CnabAtributo>();
    }
}
