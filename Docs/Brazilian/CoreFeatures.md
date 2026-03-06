Clique [aqui](/Docs/Brazilian/README.md) para voltar ao README

# Funções Principais

Esta documentação cobre a API pública do HintServiceMeow, organizada por módulo funcional.

---

## Índice

- [Funções Principais](#funções-principais)
  - [Índice](#índice)
  - [Modelos de Hint](#modelos-de-hint)
    - [AbstractHint](#abstracthint)
    - [Hint](#hint)
    - [DynamicHint](#dynamichint)
  - [PlayerDisplay](#playerdisplay)
  - [Camada de UI](#camada-de-ui)
    - [PlayerUI](#playerui)
    - [CommonHint](#commonhint)
  - [Métodos de Extensão](#métodos-de-extensão)
    - [Extensões de AbstractHint](#extensões-de-abstracthint)
    - [Extensões de PlayerDisplay](#extensões-de-playerdisplay)
    - [Extensões de NW Player](#extensões-de-nw-player)
    - [Extensões de EXILED Player](#extensões-de-exiled-player)
  - [Conteúdo da Hint](#conteúdo-da-hint)
    - [AbstractHintContent](#abstracthintcontent)
    - [StringContent](#stringcontent)
    - [AutoContent](#autocontent)

---

## Modelos de Hint

### AbstractHint

> Namespace: `HintServiceMeow.Core.Models.Hints`

Classe base para todos os tipos de hint. **Todos os hints (`Hint`, `DynamicHint`, etc.) herdam de `AbstractHint`**, que fornece propriedades comuns como conteúdo de texto, tamanho de fonte, velocidade de sincronização e visibilidade. Implementa `INotifyPropertyChanged` para que mudanças de propriedade acionem automaticamente atualizações de exibição.

Ao criar qualquer tipo de hint, as propriedades listadas abaixo estão sempre disponíveis, independentemente do tipo específico de hint.

**Propriedades:**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| Guid | `Guid` (somente leitura) | Identificador único gerado automaticamente |
| Id | `string` | Identificador de string personalizado para busca. Padrão: `""` |
| SyncSpeed | `HintSyncSpeed` | Prioridade de atualização. Padrão: `Normal`. Valores: `Fastest` (192) — atualiza o mais rápido possível, pode atrasar outras hints; `Fast` (160) — planeja uma atualização imediatamente na mudança; `Normal` (128) — velocidade padrão; `Slow` (96) — aguarda outras hints primeiro; `Slowest` (64) — aguarda mais; `UnSync` (32) — sem sincronização automática, ainda atualiza quando outras hints acionam uma sincronização |
| FontSize | `int` | Tamanho da fonte do texto. Padrão: `20` |
| LineHeight | `float` | Espaçamento vertical extra entre linhas |
| Content | `AbstractHintContent` | O provedor de conteúdo para esta hint. Padrão: `StringContent("")` |
| Text | `string?` | Atalho para obter/definir texto estático. Definir isso substitui `Content` por um novo `StringContent` |
| AutoText | `AutoContent.TextUpdateHandler?` | Atalho para obter/definir um delegado de texto dinâmico. Definir isso substitui `Content` por um novo `AutoContent` |
| Hide | `bool` | Se a hint está oculta. Padrão: `false` |

**Exemplo de uso:**

```csharp
// Propriedades sincronizam automaticamente com a tela do jogador
hint.Text = "Texto atualizado";
hint.FontSize = 30;
// Nenhuma chamada de método adicional necessária
```

---

### Hint

> Namespace: `HintServiceMeow.Core.Models.Hints`

Uma hint de posição fixa exibida em coordenadas específicas da tela. Herda de [AbstractHint](#abstracthint).

**Propriedades (além de AbstractHint):**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| YCoordinate | `float` | Posição vertical. Valores maiores movem o texto para baixo na tela. Padrão: `700` |
| XCoordinate | `float` | Deslocamento horizontal. Valores maiores movem o texto para a direita. Padrão: `0` |
| Alignment | `HintAlignment` | Alinhamento do texto. Valores: `Left`, `Right`, `Center`. Padrão: `Center` |
| YCoordinateAlign | `HintVerticalAlign` | Como a coordenada Y se alinha ao texto. Valores: `Top` — Y é a borda superior; `Middle` — Y é o centro vertical; `Bottom` — Y é a borda inferior. Padrão: `Middle` |

![Exemplo de Coordenada Y](Images/YCoordinateExample.jpg)

**Exemplo de uso:**

```csharp
Hint hint = new Hint
{
    Text = "Hello World",
    FontSize = 40,
    YCoordinate = 700,
    Alignment = HintAlignment.Left
};

PlayerDisplay playerDisplay = PlayerDisplay.Get(player);
playerDisplay.AddHint(hint);
```

Como o HSM tem um recurso de atualização automática, qualquer mudança em uma propriedade será automaticamente refletida na tela do jogador sem chamadas de método adicionais.

```csharp
hint.Text = "Algum Texto Novo";
// Nenhuma chamada de método adicional necessária
```

---

### DynamicHint

> Namespace: `HintServiceMeow.Core.Models.Hints`

Uma hint que é automaticamente posicionada para evitar sobreposição com outras hints. Herda de [AbstractHint](#abstracthint).

**Propriedades (além de AbstractHint):**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| TopBoundary | `float` | Limite superior para posicionamento. Padrão: `0` |
| BottomBoundary | `float` | Limite inferior para posicionamento. Padrão: `1000` |
| LeftBoundary | `float` | Limite esquerdo para posicionamento. Padrão: `-1200` |
| RightBoundary | `float` | Limite direito para posicionamento. Padrão: `1200` |
| TargetX | `float` | Posição horizontal preferida. Padrão: `0` |
| TargetY | `float` | Posição vertical preferida. Padrão: `700` |
| TopMargin | `float` | Espaço extra acima da hint durante o arranjo. Padrão: `5` |
| BottomMargin | `float` | Espaço extra abaixo da hint durante o arranjo. Padrão: `5` |
| LeftMargin | `float` | Espaço extra à esquerda durante o arranjo. Padrão: `100` |
| RightMargin | `float` | Espaço extra à direita durante o arranjo. Padrão: `100` |
| Priority | `HintPriority` | Prioridade de arranjo. Hints com maior prioridade são organizadas primeiro. Valores: `Highest` (192), `High` (160), `Medium` (128), `Low` (96), `Lowest` (64). Padrão: `Medium` |
| Strategy | `DynamicHintStrategy` | Comportamento quando não há espaço disponível. Valores: `Hide` — oculta a hint; `StayInPosition` — mantém na posição alvo. Padrão: `Hide` |

**Exemplo de uso:**

```csharp
var dynamicHint = new DynamicHint
{
    Text = "Olá, Hint Dinâmica"
};

PlayerDisplay playerDisplay = PlayerDisplay.Get(player);
playerDisplay.AddHint(dynamicHint);
```

---

## PlayerDisplay

> Namespace: `HintServiceMeow.Core.Utilities`

A classe central para gerenciar a exibição de hints de um jogador. Cada jogador tem uma instância de `PlayerDisplay`.

**Eventos:**

| Evento | Tipo | Descrição |
|--------|------|-----------|
| UpdateAvailable | `UpdateAvailableEventHandler` | Acionado a cada tick quando o display está pronto para atualizar |

**Delegado:** `delegate void UpdateAvailableEventHandler(UpdateAvailableEventArg ev)`

**Propriedades:**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| ReferenceHub | `ReferenceHub?` (somente leitura) | O jogador ao qual este display pertence |
| HintParser | `IHintParser` | O parser que converte hints em rich text. Substituível |
| CompatibilityAdaptor | `ICompatibilityAdaptor` | O adaptador para compatibilidade com outros plug-ins. Substituível |

**Métodos Estáticos:**

| Método | Parâmetros | Retorno | Descrição |
|--------|-----------|---------|-----------|
| Get | `ReferenceHub referenceHub` | `PlayerDisplay` | Obtém ou cria um PlayerDisplay para o jogador |
| Get | `LabApi.Features.Wrappers.Player player` | `PlayerDisplay` | Obtém ou cria um PlayerDisplay (NW/LabApi) |
| Get | `Exiled.API.Features.Player player` | `PlayerDisplay` | Obtém ou cria um PlayerDisplay (somente EXILED) |

**Métodos de Instância:**

| Método | Parâmetros | Retorno | Descrição |
|--------|-----------|---------|-----------|
| AddHint | `AbstractHint? hint` | `void` | Adiciona uma hint ao display |
| AddHint | `IEnumerable<AbstractHint>? hints` | `void` | Adiciona múltiplas hints |
| AddHint | `params AbstractHint[]? hints` | `void` | Adiciona múltiplas hints (params) |
| AddHint | `AbstractHint? hint, string groupName` | `void` | Adiciona uma hint a um grupo específico |
| ShowHint | `AbstractHint hint, float duration = 7f, AfterShowAction afterShow = AfterShowAction.Remove` | `void` | Adiciona uma hint e a remove/oculta automaticamente após `duration` segundos. Valores de `AfterShowAction`: `Remove` — remove a hint; `Hide` — define `Hide = true` |
| ShowHint | `IEnumerable<AbstractHint> hints, float duration = 7f, AfterShowAction afterShow = AfterShowAction.Remove` | `void` | Exibe múltiplas hints com remoção automática |
| RemoveHint | `AbstractHint? hint` | `void` | Remove uma hint |
| RemoveHint | `IEnumerable<AbstractHint>? hints` | `void` | Remove múltiplas hints |
| RemoveHint | `params AbstractHint[]? hints` | `void` | Remove múltiplas hints (params) |
| RemoveHint | `AbstractHint? hint, string groupName` | `void` | Remove uma hint de um grupo específico |
| RemoveHint | `string id` | `void` | Remove todas as hints com o Id fornecido |
| RemoveHint | `Guid id` | `void` | Remove a hint com o Guid fornecido |
| ClearHint | — | `void` | Remove todas as hints pertencentes ao assembly chamador |
| GetHint | `string? id` | `AbstractHint?` | Retorna a primeira hint com o Id correspondente |
| GetHint | `Guid guid` | `AbstractHint?` | Retorna a primeira hint com o Guid correspondente |
| GetHints | `string id` | `IEnumerable<AbstractHint>` | Retorna todas as hints com o Id correspondente |
| GetHints | — | `IEnumerable<AbstractHint>` | Retorna todas as hints pertencentes ao assembly chamador |
| HasHint | `string id` | `bool` | Verifica se existe alguma hint com o Id fornecido |
| HasHint | `Guid guid` | `bool` | Verifica se existe uma hint com o Guid fornecido |
| TryGetHint | `string id, out AbstractHint hint` | `bool` | Tenta obter a primeira hint com o Id correspondente |
| TryGetHint | `Guid guid, out AbstractHint hint` | `bool` | Tenta obter a primeira hint com o Guid correspondente |
| TryGetHints | `string? id, out IEnumerable<AbstractHint> hints` | `bool` | Tenta obter todas as hints com o Id correspondente |
| ForceUpdate | `bool useFastUpdate = false` | `void` | Força uma atualização do display. Use ao trabalhar com `HintSyncSpeed.UnSync` |
| SetMinUpdateInterval | `TimeSpan interval` | `void` | Define o intervalo mínimo entre atualizações |
| AddDisplayOutput | `IDisplayOutput output` | `void` | Adiciona uma saída de display personalizada |
| RemoveDisplayOutput | `IDisplayOutput output` | `void` | Remove uma saída de display |
| RemoveDisplayOutput\<T\> | — | `void` | Remove todas as saídas de display do tipo `T` (onde `T : IDisplayOutput`) |

**Exemplo de uso:**

```csharp
PlayerDisplay pd = PlayerDisplay.Get(player);

// Adicionar uma hint
var hint = new Hint { Text = "Olá", YCoordinate = 500 };
pd.AddHint(hint);

// Exibir uma hint temporária por 5 segundos
pd.ShowHint(new Hint { Text = "Temporário!" }, duration: 5f);

// Encontrar e modificar hints
if (pd.TryGetHint("meu-hint-id", out var found))
{
    found.Text = "Atualizado";
}

// Forçar atualização para hints UnSync
pd.ForceUpdate();
```

---

## Camada de UI

### PlayerUI

> Namespace: `HintServiceMeow.UI.Utilities`

Fachada de UI por jogador que fornece acesso ao [CommonHint](#commonhint).

**Propriedades:**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| ReferenceHub | `ReferenceHub` (somente leitura) | A referência ao jogador subjacente |
| PlayerDisplay | `PlayerDisplay` (somente leitura) | A instância PlayerDisplay do jogador |
| CommonHint | `CommonHint` (somente leitura) | O componente de hint comum |

**Métodos Estáticos:**

| Método | Parâmetros | Retorno | Descrição |
|--------|-----------|---------|-----------|
| Get | `ReferenceHub referenceHub` | `PlayerUI` | Obtém ou cria um PlayerUI para o jogador |
| Get | `LabApi.Features.Wrappers.Player player` | `PlayerUI` | Obtém ou cria um PlayerUI (NW/LabApi) |
| Get | `Exiled.API.Features.Player player` | `PlayerUI` | Obtém ou cria um PlayerUI (somente EXILED) |

---

### CommonHint

> Namespace: `HintServiceMeow.UI.Utilities`

Fornece layouts de hint pré-configurados para casos de uso comuns: descrições de itens, informações de mapa, informações de função e mensagens gerais.

Todas as durações de exibição são configuráveis via configuração do plug-in. O parâmetro `time` em cada sobrecarga é em segundos.

**Métodos — Hints de Item:**

| Método | Parâmetros | Descrição |
|--------|-----------|-----------|
| ShowItemHint | `string itemName` | Exibe apenas o nome do item (duração curta) |
| ShowItemHint | `string itemName, float time` | Exibe apenas o nome do item com duração personalizada |
| ShowItemHint | `string itemName, string description` | Exibe o nome do item e uma linha de descrição |
| ShowItemHint | `string itemName, string description, float time` | Exibe o nome do item e uma linha de descrição com duração personalizada |
| ShowItemHint | `string itemName, string[] description` | Exibe o nome do item e múltiplas linhas de descrição |
| ShowItemHint | `string itemName, string[] description, float time` | Exibe o nome do item e múltiplas linhas de descrição com duração personalizada |

**Métodos — Hints de Mapa:**

| Método | Parâmetros | Descrição |
|--------|-----------|-----------|
| ShowMapHint | `string roomName` | Exibe apenas o nome da sala (duração curta) |
| ShowMapHint | `string roomName, float time` | Exibe apenas o nome da sala com duração personalizada |
| ShowMapHint | `string roomName, string description` | Exibe o nome da sala e uma linha de descrição |
| ShowMapHint | `string roomName, string description, float time` | Exibe o nome da sala e uma linha de descrição com duração personalizada |
| ShowMapHint | `string roomName, string[] description` | Exibe o nome da sala e múltiplas linhas de descrição |
| ShowMapHint | `string roomName, string[] description, float time` | Exibe o nome da sala e múltiplas linhas de descrição com duração personalizada |

**Métodos — Hints de Função:**

| Método | Parâmetros | Descrição |
|--------|-----------|-----------|
| ShowRoleHint | `string roleName` | Exibe apenas o nome da função (duração curta) |
| ShowRoleHint | `string roleName, float time` | Exibe apenas o nome da função com duração personalizada |
| ShowRoleHint | `string roleName, string description` | Exibe o nome da função e uma linha de descrição |
| ShowRoleHint | `string roleName, string description, float time` | Exibe o nome da função e uma linha de descrição com duração personalizada |
| ShowRoleHint | `string roleName, string[] description` | Exibe o nome da função e múltiplas linhas de descrição |
| ShowRoleHint | `string roleName, string[] description, float time` | Exibe o nome da função e múltiplas linhas de descrição com duração personalizada |

**Métodos — Outras Hints:**

| Método | Parâmetros | Descrição |
|--------|-----------|-----------|
| ShowOtherHint | `string messages` | Exibe uma única mensagem como DynamicHint |
| ShowOtherHint | `string messages, float time` | Exibe uma única mensagem com duração personalizada |
| ShowOtherHint | `string[] messages` | Exibe múltiplas mensagens (duração escala com a quantidade) |
| ShowOtherHint | `string[] messages, float time` | Exibe múltiplas mensagens com duração total personalizada |

**Exemplo de uso:**

```csharp
var ui = PlayerUI.Get(player);
ui.CommonHint.ShowRoleHint("SCP-173", new[] { "Mate todos os humanos", "Use suas habilidades" });
ui.CommonHint.ShowMapHint("Zona de Contenção Pesada", "O lugar onde a maioria dos SCPs nasce");
ui.CommonHint.ShowItemHint("Cartão", "Usado para abrir portas");
ui.CommonHint.ShowOtherHint("O servidor está iniciando!");
```

---

## Métodos de Extensão

Todos os métodos de extensão estão reunidos aqui para fácil referência.

### Extensões de AbstractHint

> Namespace: `HintServiceMeow.Core.Extension`

| Método | Estende | Parâmetros | Descrição |
|--------|---------|-----------|-----------|
| HideAfter | `AbstractHint` | `float delay` | Define `Hide = true` após `delay` segundos. Redefine qualquer temporizador de ocultação existente |

```csharp
hint.HideAfter(5f); // Oculta a hint após 5 segundos
```

### Extensões de PlayerDisplay

> Namespace: `HintServiceMeow.Core.Extension`

| Método | Estende | Parâmetros | Descrição |
|--------|---------|-----------|-----------|
| RemoveAfter | `PlayerDisplay` | `AbstractHint hint, float delay` | Remove a hint do display após `delay` segundos. Redefine qualquer temporizador de remoção existente |

```csharp
playerDisplay.RemoveAfter(hint, 10f); // Remove a hint após 10 segundos
```

### Extensões de NW Player

> Namespace: `HintServiceMeow.Core.Extension` / `HintServiceMeow.UI.Extension`

Métodos de extensão para `LabApi.Features.Wrappers.Player`.

| Método | Namespace | Retorno | Descrição |
|--------|-----------|---------|-----------|
| GetPlayerDisplay | Core | `PlayerDisplay` | Obtém o PlayerDisplay do jogador |
| AddHint | Core | `void` | Adiciona uma hint ao display do jogador |
| RemoveHint | Core | `void` | Remove uma hint do display do jogador |
| GetPlayerUi | UI | `PlayerUI` | Obtém a instância PlayerUI do jogador |

```csharp
// Usando extensões do NW player (LabApi)
LabApi.Features.Wrappers.Player player = ...;

// Obter o PlayerDisplay e adicionar uma hint diretamente no objeto do jogador
var hint = new Hint { Text = "Olá da extensão NW!", YCoordinate = 500 };
player.AddHint(hint);

// Depois, remover
player.RemoveHint(hint);

// Acessar PlayerDisplay para operações mais avançadas
PlayerDisplay pd = player.GetPlayerDisplay();
pd.ShowHint(new Hint { Text = "Temporário!" }, duration: 3f);

// Acessar PlayerUI e CommonHint
PlayerUI ui = player.GetPlayerUi();
ui.CommonHint.ShowRoleHint("SCP-096", new[] { "Sente e chore", "Persiga alvos" });
```

### Extensões de EXILED Player

> Namespace: `HintServiceMeow.Core.Extension` / `HintServiceMeow.UI.Extension`

Métodos de extensão para `Exiled.API.Features.Player`. Disponível apenas em builds EXILED.

| Método | Namespace | Retorno | Descrição |
|--------|-----------|---------|-----------|
| GetPlayerDisplay | Core | `PlayerDisplay` | Obtém o PlayerDisplay do jogador |
| AddHint | Core | `void` | Adiciona uma hint ao display do jogador |
| RemoveHint | Core | `void` | Remove uma hint do display do jogador |
| GetPlayerUi | UI | `PlayerUI` | Obtém a instância PlayerUI do jogador |

```csharp
// Usando extensões do EXILED player
Exiled.API.Features.Player player = ...;

// Obter o PlayerDisplay e adicionar uma hint diretamente no objeto do jogador
var hint = new Hint { Text = "Olá da extensão EXILED!", YCoordinate = 500 };
player.AddHint(hint);

// Depois, remover
player.RemoveHint(hint);

// Acessar PlayerDisplay para operações mais avançadas
PlayerDisplay pd = player.GetPlayerDisplay();
pd.ShowHint(new Hint { Text = "Temporário!" }, duration: 3f);

// Acessar PlayerUI e CommonHint
PlayerUI ui = player.GetPlayerUi();
ui.CommonHint.ShowItemHint("Cartão O5", "Concede acesso a todas as áreas");
```

---

## Conteúdo da Hint

Essas classes são usadas internamente pelas hints para gerenciar seu conteúdo de texto. Na maioria dos casos, você não precisa interagir com elas diretamente — use as propriedades `Text` ou `AutoText` em [AbstractHint](#abstracthint).

### AbstractHintContent

> Namespace: `HintServiceMeow.Core.Models.HintContent`

Classe base para provedores de conteúdo de hint.

**Eventos:**

| Evento | Tipo | Descrição |
|--------|------|-----------|
| ContentUpdated | `UpdateHandler` | Acionado quando o conteúdo muda |

**Métodos:**

| Método | Parâmetros | Retorno | Descrição |
|--------|-----------|---------|-----------|
| GetText | — | `string?` | Retorna o conteúdo de texto atual |

---

### StringContent

> Namespace: `HintServiceMeow.Core.Models.HintContent`

Um provedor de conteúdo que armazena texto estático. Herda de [AbstractHintContent](#abstracthintcontent).

**Construtor:** `StringContent(string? content)`

**Propriedades:**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| Text | `string?` | O texto estático. Aciona `ContentUpdated` quando alterado |

---

### AutoContent

> Namespace: `HintServiceMeow.Core.Models.HintContent`

Um provedor de conteúdo que periodicamente invoca um delegado para produzir texto dinâmico. Herda de [AbstractHintContent](#abstracthintcontent).

**Delegado:** `delegate string TextUpdateHandler(AutoContentUpdateArg ev)`

**Construtor:** `AutoContent(TextUpdateHandler? autoText, float defaultUpdateInterval = -1)`

Se `defaultUpdateInterval` for negativo, o padrão é `0.1` segundos.

**Propriedades:**

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| AutoText | `TextUpdateHandler?` | O delegado invocado para produzir texto. Redefinir isso também redefine o próximo tempo de atualização |

**Exemplo de uso:**

```csharp
hint.AutoText = (ev) =>
{
    ev.NextUpdateDelay = TimeSpan.FromSeconds(1); // Atualiza a cada 1 segundo
    return $"Hora: {DateTime.Now:HH:mm:ss}";
};
```

Clique [aqui](/Docs/Brazilian/README.md) para voltar ao README
