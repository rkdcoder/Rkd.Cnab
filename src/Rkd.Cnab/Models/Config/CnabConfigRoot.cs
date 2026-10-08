namespace Rkd.Cnab.Models.Config
{
    /// <summary>
    /// Raiz da seção <c>CnabConfiguration</c> do appsettings.
    /// </summary>
    public class CnabConfigRoot
    {
        /// <summary>Layouts CNAB suportados pela aplicação.</summary>
        public List<CnabLayout> Layouts { get; set; } = new List<CnabLayout>();
    }
}
