using Microsoft.Extensions.Configuration;
using Rkd.Cnab;
using Rkd.Cnab.Models.Config;
using Xunit;

namespace Rkd.Cnab.Tests
{
    public sealed class CnabConverterTests
    {
        private readonly IConfiguration _configuration;

        public CnabConverterTests()
        {
            var cnabConfig = new CnabConfigRoot
            {
                Layouts =
                {
                    new CnabLayout
                    {
                        Nome = "LAYOUT_TESTE_240",
                        TamanhoLinha = 10,
                        Objetos =
                        {
                            new CnabObjeto
                            {
                                Nome = "header",
                                Identificadores =
                                {
                                    new CnabIdentificador { Posicao = 1, Valor = "0" }
                                },
                                Atributos =
                                {
                                    new CnabAtributo { Nome = "tipo", De = 1, Ate = 1 },
                                    new CnabAtributo { Nome = "valor", De = 2, Ate = 10 }
                                }
                            },
                            new CnabObjeto
                            {
                                Nome = "detalhe",
                                Identificadores =
                                {
                                    new CnabIdentificador { Posicao = 1, Valor = "3" },
                                    new CnabIdentificador { Posicao = 2, Valor = "E" }
                                },
                                Atributos =
                                {
                                    new CnabAtributo { Nome = "tipo", De = 1, Ate = 1 },
                                    new CnabAtributo { Nome = "segmento", De = 2, Ate = 2 },
                                    new CnabAtributo { Nome = "conteudo", De = 3, Ate = 10 }
                                }
                            }
                        }
                    }
                }
            };

            var dict = new Dictionary<string, string?>
            {
                ["CnabConfiguration:Layouts:0:Nome"] = cnabConfig.Layouts[0].Nome,
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

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
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
            var cnab = "9XXXXXXXX";

            var result = converter.Convert(cnab, "LAYOUT_TESTE_240");

            Assert.True(result.Success);
            Assert.False(result.CompletelyConverted);
            Assert.Single(result.Erros);
        }
    }
}
