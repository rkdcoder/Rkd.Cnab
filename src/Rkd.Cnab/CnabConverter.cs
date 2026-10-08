using Microsoft.Extensions.Configuration;
using Rkd.Cnab.Models.Config;
using Rkd.Cnab.Models.Runtime;

namespace Rkd.Cnab
{
    /// <summary>
    /// Converte arquivos CNAB (240/400) em um resultado estruturado, com base em
    /// layouts definidos na seção <c>CnabConfiguration</c> da configuração.
    /// </summary>
    /// <remarks>
    /// A instância é imutável após a criação e segura para uso concorrente
    /// (pode ser registrada como singleton).
    /// </remarks>
    public sealed class CnabConverter
    {
        private const string SecaoConfiguracao = "CnabConfiguration";

        private readonly CnabConfigRoot _config;

        /// <summary>
        /// Cria o conversor lendo e validando a seção <c>CnabConfiguration</c>.
        /// </summary>
        /// <param name="configuration">Configuração da aplicação hospedeira.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> é nulo.</exception>
        /// <exception cref="InvalidOperationException">
        /// A seção não existe ou o layout é estruturalmente inválido (fail-fast).
        /// </exception>
        public CnabConverter(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _config = configuration
                .GetSection(SecaoConfiguracao)
                .Get<CnabConfigRoot>()
                ?? throw new InvalidOperationException(
                    $"Seção '{SecaoConfiguracao}' não encontrada ou inválida.");

            ValidarConfiguracao(_config);
        }

        /// <summary>
        /// Converte o conteúdo de um arquivo CNAB usando o layout informado.
        /// </summary>
        /// <param name="conteudoArquivo">Conteúdo textual completo do arquivo.</param>
        /// <param name="nomeLayout">Nome do layout configurado (sem diferenciar maiúsculas/minúsculas).</param>
        /// <returns>
        /// Um <see cref="CnabResponse"/>. <see cref="CnabResponse.Success"/> é <c>false</c> em erros
        /// estruturais; linhas inválidas não interrompem o processamento e ficam em
        /// <see cref="CnabResponse.Erros"/>.
        /// </returns>
        public CnabResponse Convert(string conteudoArquivo, string nomeLayout)
        {
            var response = new CnabResponse();

            if (string.IsNullOrWhiteSpace(conteudoArquivo))
                return response.Fail("Conteúdo do arquivo vazio.");

            var layout = _config.Layouts
                .FirstOrDefault(l =>
                    string.Equals(l.Nome, nomeLayout, StringComparison.OrdinalIgnoreCase));

            if (layout == null)
                return response.Fail($"Layout '{nomeLayout}' não encontrado.");

            response.LayoutUtilizado = layout.Nome;

            foreach (var obj in layout.Objetos)
                response.Data[obj.Nome] = new List<Dictionary<string, string>>();

            // Arquivos salvos como "UTF-8 com BOM" trariam um ﻿ no início da 1ª linha,
            // quebrando a validação de tamanho e as posições.
            var linhas = conteudoArquivo
                .TrimStart('﻿')
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
            foreach (var id in identificadores)
            {
                int idx = id.Posicao - 1;

                if (idx < 0 || idx + id.Valor.Length > linha.Length)
                    return false;

                if (string.CompareOrdinal(linha, idx, id.Valor, 0, id.Valor.Length) != 0)
                    return false;
            }

            return true;
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

                if (inicio < 0 || tamanho <= 0 || inicio + tamanho > linha.Length)
                {
                    resultado[atr.Nome] = string.Empty;
                    continue;
                }

                resultado[atr.Nome] =
                    linha.Substring(inicio, tamanho).Trim();
            }

            return resultado;
        }

        private static void ValidarConfiguracao(CnabConfigRoot config)
        {
            if (config.Layouts.Count == 0)
                throw ConfigInvalida("nenhum layout configurado em 'Layouts'.");

            var nomesLayouts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < config.Layouts.Count; i++)
            {
                var layout = config.Layouts[i];
                var ctxLayout = $"Layouts[{i}]";

                if (string.IsNullOrWhiteSpace(layout.Nome))
                    throw ConfigInvalida($"{ctxLayout}: 'Nome' é obrigatório.");

                ctxLayout = $"Layout '{layout.Nome}'";

                if (!nomesLayouts.Add(layout.Nome))
                    throw ConfigInvalida($"{ctxLayout}: nome de layout duplicado.");

                if (layout.TamanhoLinha <= 0)
                    throw ConfigInvalida($"{ctxLayout}: 'TamanhoLinha' deve ser maior que zero.");

                if (layout.Objetos.Count == 0)
                    throw ConfigInvalida($"{ctxLayout}: nenhum objeto configurado em 'Objetos'.");

                var nomesObjetos = new HashSet<string>(StringComparer.Ordinal);

                foreach (var obj in layout.Objetos)
                {
                    if (string.IsNullOrWhiteSpace(obj.Nome))
                        throw ConfigInvalida($"{ctxLayout}: todo objeto precisa de 'Nome'.");

                    var ctxObjeto = $"{ctxLayout}, objeto '{obj.Nome}'";

                    if (!nomesObjetos.Add(obj.Nome))
                        throw ConfigInvalida($"{ctxObjeto}: nome de objeto duplicado.");

                    ValidarIdentificadores(obj, layout.TamanhoLinha, ctxObjeto);
                    ValidarAtributos(obj, layout.TamanhoLinha, ctxObjeto);
                }
            }
        }

        private static void ValidarIdentificadores(CnabObjeto obj, int tamanhoLinha, string ctx)
        {
            // Sem identificadores o objeto casaria com qualquer linha.
            if (obj.Identificadores.Count == 0)
                throw ConfigInvalida($"{ctx}: informe ao menos um item em 'Identificadores'.");

            foreach (var id in obj.Identificadores)
            {
                if (string.IsNullOrEmpty(id.Valor))
                    throw ConfigInvalida($"{ctx}: identificador na posição {id.Posicao} sem 'Valor'.");

                if (id.Posicao < 1 || id.Posicao + id.Valor.Length - 1 > tamanhoLinha)
                    throw ConfigInvalida(
                        $"{ctx}: identificador '{id.Valor}' na posição {id.Posicao} excede o 'TamanhoLinha' ({tamanhoLinha}).");
            }
        }

        private static void ValidarAtributos(CnabObjeto obj, int tamanhoLinha, string ctx)
        {
            var nomes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var atr in obj.Atributos)
            {
                if (string.IsNullOrWhiteSpace(atr.Nome))
                    throw ConfigInvalida($"{ctx}: todo atributo precisa de 'Nome'.");

                if (!nomes.Add(atr.Nome))
                    throw ConfigInvalida($"{ctx}: atributo '{atr.Nome}' duplicado.");

                if (atr.De < 1 || atr.Ate < atr.De || atr.Ate > tamanhoLinha)
                    throw ConfigInvalida(
                        $"{ctx}: atributo '{atr.Nome}' com intervalo inválido (De={atr.De}, Ate={atr.Ate}, TamanhoLinha={tamanhoLinha}).");
            }
        }

        private static InvalidOperationException ConfigInvalida(string detalhe) =>
            new($"Configuração '{SecaoConfiguracao}' inválida: {detalhe}");
    }
}
