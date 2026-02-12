# Rkd.Cnab

<p align="center">
  <img src="https://raw.githubusercontent.com/rkdcoder/Rkd.Cnab/master/src/Rkd.Cnab/Media/icon.png" width="128" alt="Rkd.Cnab logo" />
</p>

[![NuGet](https://img.shields.io/nuget/v/Rkd.Cnab.svg)](https://www.nuget.org/packages/Rkd.Cnab)
[![Build & Publish](https://github.com/rkdcoder/Rkd.Cnab/actions/workflows/master.yml/badge.svg)](https://github.com/rkdcoder/Rkd.Cnab/actions/workflows/master.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Rkd.Cnab** é uma biblioteca .NET leve, previsível e orientada a configuração para **processamento de arquivos CNAB (240 / 400)**.

O foco da biblioteca é **engenharia prática**: layouts totalmente externos, tolerância a erro, identificação realista de registros e retorno estruturado para auditoria, integração e ETL.

> **Princípio central:** o layout muda, o código não.

---

## 🚀 Características

- **Orientado a Configuração**: layouts definidos integralmente via `appsettings.json`.
- **Identificação Composta**: suporte nativo a múltiplas regras de identificação por linha (CNAB real).
- **Fail-fast estrutural**: erros de configuração ou layout inexistente falham imediatamente.
- **Processamento resiliente**: linhas inválidas não interrompem o processamento.
- **Resposta auditável**: dados convertidos + lista de erros + metadados.
- **Sem `dynamic`**: estrutura previsível, segura e amigável ao consumidor.
- **Pronto para NuGet e produção**.

---

## 📦 Instalação

Via **NuGet Package Manager**:

```powershell
Install-Package Rkd.Cnab
```

Via **.NET CLI**:

```bash
dotnet add package Rkd.Cnab
```

---

## ⚙️ Configuração (`appsettings.json`)

A biblioteca lê automaticamente a seção **`CnabConfiguration`** da aplicação hospedeira.

### Exemplo — Layout CNAB 240

```json
{
  "CnabConfiguration": {
    "Layouts": [
      {
        "Nome": "CNAB240_Extrato_Conta_Corrente",
        "TamanhoLinha": 240,
        "Objetos": [
          {
            "Nome": "headerArquivo",
            "Identificadores": [{ "Posicao": 8, "Valor": "0" }],
            "Atributos": [
              { "Nome": "codigoBanco", "De": 1, "Ate": 3 },
              { "Nome": "empresaNome", "De": 73, "Ate": 102 }
            ]
          },
          {
            "Nome": "detalheSegmentoE",
            "Identificadores": [
              { "Posicao": 8, "Valor": "3" },
              { "Posicao": 14, "Valor": "E" }
            ],
            "Atributos": [
              { "Nome": "dataLancamento", "De": 143, "Ate": 150 },
              { "Nome": "valorLancamento", "De": 151, "Ate": 168 }
            ]
          },
          {
            "Nome": "trailerArquivo",
            "Identificadores": [{ "Posicao": 8, "Valor": "9" }],
            "Atributos": [{ "Nome": "totalRegistros", "De": 24, "Ate": 29 }]
          }
        ]
      }
    ]
  }
}
```

### Glossário da Configuração

- **Layouts**: conjunto de layouts suportados pela aplicação.
- **Nome**: identificador lógico do layout (usado no código).
- **TamanhoLinha**: tamanho fixo da linha CNAB.
- **Objetos**: tipos de registros (header, detalhe, trailer, segmentos).
- **Identificadores**: regras **AND** para reconhecer a linha (posição + valor).
- **Atributos**: mapeamento posicional dos campos (base 1).

---

## 💻 Como Usar

### ASP.NET Core (exemplo recomendado)

#### 1️⃣ Modelo de Upload

```csharp
public class CnabUploadModel
{
    public IFormFile Arquivo { get; set; }
    public string Layout { get; set; }
}
```

#### 2️⃣ Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Rkd.Cnab;

[ApiController]
[Route("api/[controller]")]
public class CnabController : ControllerBase
{
    private readonly CnabConverter _converter;

    public CnabController(IConfiguration configuration)
    {
        _converter = new CnabConverter(configuration);
    }

    [HttpPost("processar")]
    public async Task<IActionResult> Processar([FromForm] CnabUploadModel model)
    {
        if (model.Arquivo == null || model.Arquivo.Length == 0)
            return BadRequest("Arquivo inválido.");

        string conteudo;
        using (var reader = new StreamReader(model.Arquivo.OpenReadStream()))
        {
            conteudo = await reader.ReadToEndAsync();
        }

        var resultado = _converter.Convert(conteudo, model.Layout);

        return resultado.Success
            ? Ok(resultado)
            : BadRequest(resultado);
    }
}
```

---

## 📄 Estrutura da Resposta

O método `Convert` retorna um objeto **`CnabResponse`**:

```json
{
  "success": true,
  "completelyConverted": false,
  "message": "Conversão concluída com inconsistências (verifique a lista de erros).",
  "layoutUtilizado": "CNAB240_Extrato_Conta_Corrente",
  "totalLinhas": 120,
  "totalErros": 2,
  "data": {
    "headerArquivo": [{ "codigoBanco": "001", "empresaNome": "EMPRESA TESTE" }],
    "detalheSegmentoE": [
      { "dataLancamento": "20240131", "valorLancamento": "00000000150000" }
    ]
  },
  "erros": [
    {
      "motivo": "Tamanho inválido. Esperado: 240, Encontrado: 238",
      "conteudo": "001000..."
    }
  ]
}
```

### Entendendo os Flags

- **Success**
  - `true`: processamento ocorreu normalmente.
  - `false`: erro estrutural (layout inexistente, configuração inválida).

- **CompletelyConverted**
  - `true`: todas as linhas foram reconhecidas.
  - `false`: arquivo processado, mas com linhas inválidas.

---

## 🔧 Tratamento de Erros

Quando `CompletelyConverted` for `false`, a lista `Erros` conterá:

- **Motivo**: descrição objetiva do problema.
- **Conteudo**: linha original que falhou.

Isso permite:

- Auditoria
- Ajuste rápido de layout
- Correção sem interromper produção

---

## 🧪 Testes

A biblioteca acompanha uma suíte de testes **rápida e determinística**, baseada em:

- Configuração em memória
- Zero IO
- Foco em contratos e comportamento

Ideal para CI/CD.

---

## 📝 Licença

Distribuído sob a licença **MIT**. Consulte o arquivo `LICENSE` para mais informações.
