using Microsoft.Extensions.Configuration;
using Rkd.Cnab.Models.Config;
using Rkd.Cnab.Models.Runtime;

namespace Rkd.Cnab
{
    public sealed class CnabConverter
    {
        private readonly CnabConfigRoot _config;

        public CnabConverter(IConfiguration configuration)
        {
            _config = configuration
                .GetSection("CnabConfiguration")
                .Get<CnabConfigRoot>()
                ?? throw new InvalidOperationException(
                    "Seção 'CnabConfiguration' não encontrada ou inválida.");
        }

        public CnabResponse Convert(string conteudoArquivo, string nomeLayout)
        {
            var response = new CnabResponse();

            if (string.IsNullOrWhiteSpace(conteudoArquivo))
                return response.Fail("Conteúdo do arquivo vazio.");

            var layout = _config.Layouts
                .FirstOrDefault(l =>
                    l.Nome.Equals(nomeLayout, StringComparison.OrdinalIgnoreCase));

            if (layout == null)
                return response.Fail($"Layout '{nomeLayout}' não encontrado.");

            response.LayoutUtilizado = layout.Nome;

            foreach (var obj in layout.Objetos)
                response.Data[obj.Nome] = new List<Dictionary<string, string>>();

            var linhas = conteudoArquivo
                .Split(new[] { "\r\n", "\n", "\r" },
                       StringSplitOptions.RemoveEmptyEntries);

            response.TotalLinhas = linhas.Length;

            foreach (var linha in linhas)
            {
                if (linha.Length != layout.TamanhoLinha)
                {
                    response.AddErro(
                        linha,
                        $"Tamanho inválido. Esperado: {layout.TamanhoLinha}, Encontrado: {linha.Length}");
                    continue;
                }

                var objeto = layout.Objetos
                    .FirstOrDefault(o =>
                        MatchIdentificadores(linha, o.Identificadores));

                if (objeto == null)
                {
                    response.AddErro(linha, "Linha não reconhecida pelo layout.");
                    continue;
                }

                var campos = ExtrairCampos(linha, objeto.Atributos);
                response.Data[objeto.Nome].Add(campos);
            }

            response.Finalizar();
            return response;
        }

        private static bool MatchIdentificadores(
            string linha,
            List<CnabIdentificador> identificadores)
        {
            return identificadores.All(id =>
            {
                int idx = id.Posicao - 1;
                return idx >= 0 &&
                       idx + id.Valor.Length <= linha.Length &&
                       linha.Substring(idx, id.Valor.Length) == id.Valor;
            });
        }

        private static Dictionary<string, string> ExtrairCampos(
            string linha,
            List<CnabAtributo> atributos)
        {
            var resultado = new Dictionary<string, string>();

            foreach (var atr in atributos)
            {
                int inicio = atr.De - 1;
                int tamanho = atr.Ate - atr.De + 1;

                if (inicio < 0 || inicio + tamanho > linha.Length)
                {
                    resultado[atr.Nome] = string.Empty;
                    continue;
                }

                resultado[atr.Nome] =
                    linha.Substring(inicio, tamanho).Trim();
            }

            return resultado;
        }
    }
}
