# Rkd.Cnab

<p align="center">
  <img src="https://raw.githubusercontent.com/rkdcoder/Rkd.Cnab/master/src/Rkd.Cnab/Media/icon.png" width="128" alt="Rkd.Cnab logo" />
</p>

[![NuGet](https://img.shields.io/nuget/v/Rkd.Cnab.svg)](https://www.nuget.org/packages/Rkd.Cnab)
[![Build & Publish](https://github.com/rkdcoder/Rkd.Cnab/actions/workflows/main.yml/badge.svg)](https://github.com/rkdcoder/Rkd.Cnab/actions/workflows/main.yml)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rkd.Cnab.svg)](https://www.nuget.org/packages/Rkd.Cnab)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/rkdcoder/Rkd.Cnab/blob/master/LICENSE)

**Rkd.Cnab** é uma biblioteca .NET leve, previsível e orientada a configuração para **processamento de arquivos CNAB (240 / 400)**.

O foco da biblioteca é **engenharia prática**: layouts totalmente externos, tolerância a erro, identificação realista de registros e retorno estruturado para auditoria, integração e ETL.

> **Princípio central:** o layout muda, o código não.

---

## 🚀 Características

- **Orientado a Configuração**: layouts definidos integralmente via `appsettings.json`.
- **Identificação Composta**: suporte nativo a múltiplas regras de identificação por linha (CNAB real).
- **Fail-fast estrutural**: layout mal configurado falha já na criação do `CnabConverter` (veja "Validação do layout" abaixo).
- **Processamento resiliente**: linhas inválidas não interrompem o processamento.
- **Resposta auditável**: dados convertidos + lista de erros + metadados.
- **Sem `dynamic`**: estrutura previsível, segura e amigável ao consumidor.
- **Leve**: depende apenas de `Microsoft.Extensions.Configuration.Binder`; alvo `net8.0` (compatível com .NET 8, 9 e 10).

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

> Em aplicações **console** (sem o host padrão do ASP.NET Core / Worker Service) é preciso referenciar também
> `Microsoft.Extensions.Configuration.Json` para carregar o `appsettings.json` — veja o exemplo de Console em "Como Usar".

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
- **Nome**: identificador lógico do layout (usado no código; não diferencia maiúsculas/minúsculas).
- **TamanhoLinha**: tamanho fixo da linha CNAB (linhas com outro tamanho viram erro).
- **Objetos**: tipos de registros (header, detalhe, trailer, segmentos). Vale o **primeiro** objeto cujos identificadores casarem.
- **Identificadores**: regras **AND** para reconhecer a linha (posição base 1 + valor; comparação exata, diferencia maiúsculas/minúsculas).
- **Atributos**: mapeamento posicional dos campos (`De`/`Ate` base 1, inclusivos). Os valores são retornados com `Trim()`.

### 🛡️ Validação do layout

Ao criar o `CnabConverter`, o layout é validado e uma `InvalidOperationException` descritiva é lançada se houver:

- seção `CnabConfiguration` ausente ou sem layouts;
- layout sem `Nome` (ou com nome duplicado) ou com `TamanhoLinha` ≤ 0;
- objeto sem `Nome`, com nome duplicado ou **sem identificadores** (casaria com qualquer linha);
- identificador sem `Valor` ou que ultrapasse o `TamanhoLinha`;
- atributo sem `Nome`, duplicado, ou com intervalo inválido (`De` < 1, `Ate` < `De` ou `Ate` > `TamanhoLinha`).

---

## 💻 Como Usar

O `CnabConverter` é **imutável e thread-safe** após criado: registre-o como **singleton** (ou mantenha uma única instância).
Ele recebe um `IConfiguration` e expõe `Convert(string conteudoArquivo, string nomeLayout)`.

### ASP.NET Core (exemplo recomendado)

#### 1️⃣ Registro (`Program.cs`)

```csharp
using Rkd.Cnab;

builder.Services.AddSingleton(sp => new CnabConverter(sp.GetRequiredService<IConfiguration>()));
```

#### 2️⃣ Modelo de Upload

```csharp
public class CnabUploadModel
{
    public IFormFile Arquivo { get; set; } = default!;
    public string Layout { get; set; } = default!;
}
```

#### 3️⃣ Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Rkd.Cnab;

[ApiController]
[Route("api/[controller]")]
public class CnabController : ControllerBase
{
    private readonly CnabConverter _converter;

    public CnabController(CnabConverter converter)
    {
        _converter = converter;
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

> **Codificação:** arquivos CNAB legados costumam estar em ISO-8859-1. Se o seu estiver, abra o
> `StreamReader` com `Encoding.Latin1` para que acentos não alterem o tamanho/posição das linhas.
> Um BOM UTF-8 no início do arquivo é ignorado automaticamente.

### Console

```bash
dotnet add package Rkd.Cnab
dotnet add package Microsoft.Extensions.Configuration.Json
```

```csharp
using Microsoft.Extensions.Configuration;
using Rkd.Cnab;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var converter = new CnabConverter(configuration);

var resultado = converter.Convert(File.ReadAllText("extrato.rem"), "CNAB240_Extrato_Conta_Corrente");

Console.WriteLine($"{resultado.Message} ({resultado.TotalErros} erro(s) em {resultado.TotalLinhas} linha(s))");
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
      { "dataLancamento": "20240131", "valorLancamento": "000000000000150000" }
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
  - `false`: erro estrutural em tempo de execução (conteúdo vazio, layout inexistente). Layout mal configurado não chega aqui: falha na criação do `CnabConverter`.

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
- Foco em contratos e comportamento (conversão, erros por linha e validação do layout)

```bash
dotnet test
```

Ideal para CI/CD.

---

## 📝 Licença

Distribuído sob a licença **MIT**. Consulte o arquivo [LICENSE](https://github.com/rkdcoder/Rkd.Cnab/blob/master/LICENSE) para mais informações.
