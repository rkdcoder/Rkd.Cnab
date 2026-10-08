using Microsoft.Extensions.Configuration;

namespace Rkd.Cnab.Tests
{
    public sealed class CnabConverterTests
    {
        private readonly IConfiguration _configuration;

        public CnabConverterTests()
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(ConfigBase())
                .Build();
        }

        private static Dictionary<string, string?> ConfigBase()
        {
            return new Dictionary<string, string?>
            {
                ["CnabConfiguration:Layouts:0:Nome"] = "LAYOUT_TESTE_240",
                ["CnabConfiguration:Layouts:0:TamanhoLinha"] = "10",

                // Header
                ["CnabConfiguration:Layouts:0:Objetos:0:Nome"] = "header",
                ["CnabConfiguration:Layouts:0:Objetos:0:Identificadores:0:Posicao"] = "1",
                ["CnabConfiguration:Layouts:0:Objetos:0:Identificadores:0:Valor"] = "0",
                ["CnabConfiguration:Layouts:0:Objetos:0:Atributos:0:Nome"] = "tipo",
                ["CnabConfiguration:Layouts:0:Objetos:0:Atributos:0:De"] = "1",
                ["CnabConfiguration:Layouts:0:Objetos:0:Atributos:0:Ate"] = "1",
                ["CnabConfiguration:Layouts:0:Objetos:0:Atributos:1:Nome"] = "valor",
                ["CnabConfiguration:Layouts:0:Objetos:0:Atributos:1:De"] = "2",
                ["CnabConfiguration:Layouts:0:Objetos:0:Atributos:1:Ate"] = "10",

                // Detalhe
                ["CnabConfiguration:Layouts:0:Objetos:1:Nome"] = "detalhe",
                ["CnabConfiguration:Layouts:0:Objetos:1:Identificadores:0:Posicao"] = "1",
                ["CnabConfiguration:Layouts:0:Objetos:1:Identificadores:0:Valor"] = "3",
                ["CnabConfiguration:Layouts:0:Objetos:1:Identificadores:1:Posicao"] = "2",
                ["CnabConfiguration:Layouts:0:Objetos:1:Identificadores:1:Valor"] = "E",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:0:Nome"] = "tipo",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:0:De"] = "1",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:0:Ate"] = "1",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:1:Nome"] = "segmento",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:1:De"] = "2",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:1:Ate"] = "2",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:2:Nome"] = "conteudo",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:2:De"] = "3",
                ["CnabConfiguration:Layouts:0:Objetos:1:Atributos:2:Ate"] = "10"
            };
        }

        [Fact]
        public void Convert_DeveFalhar_QuandoConteudoVazio()
        {
            var converter = new CnabConverter(_configuration);

            var result = converter.Convert("", "LAYOUT_TESTE_240");

            Assert.False(result.Success);
            Assert.False(result.CompletelyConverted);
        }

        [Fact]
        public void Convert_DeveConverterHeaderComSucesso()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "0ABCDEFGH ";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.True(result.CompletelyConverted);
            Assert.Single(result.Data["header"]);
            Assert.Equal("0", result.Data["header"][0]["tipo"]);
            Assert.Equal("ABCDEFGH", result.Data["header"][0]["valor"]);
        }

        [Fact]
        public void Convert_DeveIdentificarSegmentoComposto()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "3ETESTE123";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.Single(result.Data["detalhe"]);
            Assert.Equal("3", result.Data["detalhe"][0]["tipo"]);
            Assert.Equal("E", result.Data["detalhe"][0]["segmento"]);
            Assert.Equal("TESTE123", result.Data["detalhe"][0]["conteudo"]);
        }

        [Fact]
        public void Convert_DeveRegistrarErro_QuandoTamanhoLinhaInvalido()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "0ABC";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.False(result.CompletelyConverted);
            Assert.Single(result.Erros);
        }

        [Fact]
        public void Convert_DeveRegistrarErro_QuandoIdentificadorNaoReconhecido()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "9XXXXXXXXX";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.False(result.CompletelyConverted);
            Assert.Single(result.Erros);
            Assert.Equal("Linha não reconhecida pelo layout.", result.Erros[0].Motivo);
            Assert.Equal(cnab, result.Erros[0].Conteudo);
        }

        [Fact]
        public void Convert_DeveFalhar_QuandoLayoutNaoExiste()
        {
            var converter = new CnabConverter(_configuration);

            var result = converter.Convert("0ABCDEFGH ", "INEXISTENTE");

            Assert.False(result.Success);
            Assert.False(result.CompletelyConverted);
            Assert.Contains("INEXISTENTE", result.Message);
        }

        [Fact]
        public void Convert_DeveFalhar_QuandoNomeLayoutNulo()
        {
            var converter = new CnabConverter(_configuration);

            var result = converter.Convert("0ABCDEFGH ", null!);

            Assert.False(result.Success);
        }

        [Fact]
        public void Convert_DeveIgnorarMaiusculasMinusculas_NoNomeDoLayout()
        {
            var converter = new CnabConverter(_configuration);

            var result = converter.Convert("0ABCDEFGH ", "layout_teste_240");

            Assert.True(result.Success);
            Assert.Equal("LAYOUT_TESTE_240", result.LayoutUtilizado);
        }

        [Fact]
        public void Convert_DeveProcessarVariasLinhas_ComQuebrasDeLinhaMistas()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "0ABCDEFGH \r\n3ETESTE123\n\n3EOUTRO123\r";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.True(result.CompletelyConverted);
            Assert.Equal(3, result.TotalLinhas);
            Assert.Equal(0, result.TotalErros);
            Assert.Single(result.Data["header"]);
            Assert.Equal(2, result.Data["detalhe"].Count);
        }

        [Fact]
        public void Convert_NaoDeveInterromper_QuandoHaLinhasInvalidasEValidas()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "0ABCDEFGH \n0ABC\n3ETESTE123";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.False(result.CompletelyConverted);
            Assert.Equal(3, result.TotalLinhas);
            Assert.Equal(1, result.TotalErros);
            Assert.Single(result.Data["header"]);
            Assert.Single(result.Data["detalhe"]);
        }

        [Fact]
        public void Convert_DeveIgnorarBomNoInicioDoArquivo()
        {
            var converter = new CnabConverter(_configuration);
            var cnab = "\uFEFF0ABCDEFGH ";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.True(result.CompletelyConverted);
            Assert.Equal("ABCDEFGH", result.Data["header"][0]["valor"]);
        }

        [Fact]
        public void Construtor_DeveFalhar_QuandoConfiguracaoNula()
        {
            Assert.Throws<ArgumentNullException>(() => new CnabConverter(null!));
        }

        [Fact]
        public void Construtor_DeveFalhar_QuandoSecaoAusente()
        {
            var configuration = new ConfigurationBuilder().Build();

            var ex = Assert.Throws<InvalidOperationException>(() => new CnabConverter(configuration));

            Assert.Contains("CnabConfiguration", ex.Message);
        }

        [Theory]
        [InlineData("TamanhoLinha", "0", "TamanhoLinha")]
        [InlineData("Nome", "", "Nome")]
        [InlineData("Objetos:0:Identificadores:0:Valor", "", "Valor")]
        [InlineData("Objetos:0:Identificadores:0:Posicao", "11", "excede")]
        [InlineData("Objetos:0:Atributos:1:Ate", "11", "intervalo")]
        [InlineData("Objetos:0:Atributos:1:De", "11", "intervalo")]
        [InlineData("Objetos:0:Atributos:1:Nome", "tipo", "duplicado")]
        [InlineData("Objetos:1:Nome", "header", "duplicado")]
        public void Construtor_DeveFalhar_QuandoLayoutEstruturalmenteInvalido(
            string chave, string valor, string trechoMensagem)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(ConfigBase())
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"CnabConfiguration:Layouts:0:{chave}"] = valor
                })
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() => new CnabConverter(configuration));

            Assert.Contains(trechoMensagem, ex.Message);
        }

        [Fact]
        public void Construtor_DeveFalhar_QuandoObjetoSemIdentificadores()
        {
            var dict = ConfigBase();
            dict.Remove("CnabConfiguration:Layouts:0:Objetos:0:Identificadores:0:Posicao");
            dict.Remove("CnabConfiguration:Layouts:0:Objetos:0:Identificadores:0:Valor");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

            var ex = Assert.Throws<InvalidOperationException>(() => new CnabConverter(configuration));

            Assert.Contains("Identificadores", ex.Message);
        }

        [Fact]
        public void Convert_DeveRetornarCampoVazio_QuandoCampoSoTemEspacos()
        {
            var converter = new CnabConverter(_configuration);

            var result = converter.Convert("0         ", "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.Equal(string.Empty, result.Data["header"][0]["valor"]);
        }
    }
}
