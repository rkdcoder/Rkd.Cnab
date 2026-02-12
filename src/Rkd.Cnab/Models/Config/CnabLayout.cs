namespace Rkd.Cnab.Models.Config
{
    public class CnabLayout
    {
        public string Nome { get; set; }
        public int TamanhoLinha { get; set; }
        public List<CnabObjeto> Objetos { get; set; } = new List<CnabObjeto>();
    }
}
