
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white)](https://discord.gg/H3TACT3Buh) [![GitHub Release](https://img.shields.io/github/v/release/MeowServer/HintServiceMeow)](https://github.com/MeowServer/HintServiceMeow/releases)

## Introdução
**HintServiceMeow (HSM)** é um framework para SCP: Secret Laboratory que permite que plug-ins exibam texto em uma posição selecionada na tela de um jogador.

---

## Instalação

Para instalar esse plug-in, siga os passos abaixo:

1. Vá à [página de lançamento](https://github.com/MeowServer/HintServiceMeow/releases) e baixe o arquivo mais recente `HintServiceMeow.dll`. Em seguida, cole-o na pasta de plug-ins.
2. Se você estiver usando a **LabAPI** (API padrão), coloque o arquivo `Harmony.dll` na pasta de **dependências**.
3. Reinicie o servidor.
4. Ajuste as configurações conforme necessário.
5. Reinicie o servidor novamente para aplicar as mudanças de configuração.

---

## Documentação

Aqui estão alguns recursos úteis para você começar:

- [Introdução](/Docs/Brazilian/GettingStarted.md)
- [Funções Principais](/Docs/Brazilian/CoreFeatures.md)
- [Registro de Alterações](/Docs/Brazilian/CHANGELOG.md)

---

## FAQ

### 1. Por que o plug-in não funciona?
- Certifique-se de que o **HintServiceMeow** está instalado corretamente.
- Verifique se há algum plug-in em conflito com o **HintServiceMeow**.
- Veja se ocorre algum erro durante a ativação dos plug-ins.

### 2. Por que as hints se sobrepõem?
- Isso pode acontecer quando múltiplos plug-ins colocam suas hints na mesma posição. Você pode verificar o arquivo de configuração de cada plug-in para ajustar a posição da UI.
- Se um plug-in não permitir que você altere a posição via arquivo de configuração, entre em contato com o autor do plug-in para obter assistência.

---

## Colaboradores

Obrigado a todos que contribuíram para o HintServiceMeow!
Seus pull requests, relatórios de bugs e sugestões ajudam a manter este projeto em funcionamento.

- [@Someone](https://github.com/Someone-193) - Por adicionar verificação de estilo de código.
- [XLittleLeft](https://github.com/XLittleLeft) - Por adicionar suporte ao LabAPI.
- [Firething](https://github.com/Firething) - Por adicionar a tradução para o português.
