Clique [aqui](/Docs/Brazilian/README.md) para voltar ao README

## Introdução
### Configurar dependências
1. Crie seu projeto C#
2. Inclua o arquivo .dll baixado do lançamento nas dependências do seu projeto
### Exiba sua primeira hint
Os blocos de código a seguir mostram como usar os recursos mais utilizados do HSM.

---
Crie uma hint "Hello World" na tela do jogador.
```CSharp
Player player = Player.Get(xxx);

// Hint é uma hint simples que pode ser exibida na tela do jogador.
Hint hint1 = new Hint
{
    Text = "Hello World" // Você pode definir as propriedades da hint dentro de um par de chaves ({})
};

// Você pode definir as propriedades da hint assim:
hint1.FontSize = 40;
hint1.YCoordinate = 700;
hint1.Alignment = HintAlignment.Left;
// Após definir as propriedades, você não precisa chamar nenhum método para solicitar uma atualização. Todas as atualizações serão feitas automaticamente pelo HSM (HintServiceMeow).

// Você pode exibir uma hint para um jogador adicionando-a ao PlayerDisplay do jogador.
// Você também pode removê-la removendo-a do PlayerDisplay.
PlayerDisplay playerDisplay = PlayerDisplay.Get(player);
playerDisplay.AddHint(hint1);
// playerDisplay.RemoveHint(hint);

```
---
Use `AutoText` para criar uma hint que atualiza o conteúdo automaticamente.

Use extensões para adicionar ou remover hints de forma simples.
```CSharp
Hint hint2 = new Hint
{
    AutoText = ev => DateTime.Now.ToString("HH:mm:ss"), // Você também pode usar uma função para definir o texto da hint, e a hint se atualizará automaticamente.
    Alignment = HintAlignment.Right, // Você pode definir as propriedades da hint em qualquer ordem, e também pode optar por não definir algumas propriedades, pois todas têm um valor padrão.
    YCoordinate = 200
};

// Você também pode usar métodos de extensão para facilitar
player.AddHint(hint2); // Isso é equivalente a playerDisplay.AddHint(hint);
// player.RemoveHint(hint); // Isso é equivalente a playerDisplay.RemoveHint(hint);

```
---
Use `NextUpdateDelay` para personalizar a taxa de atualização do seu AutoText.

Use `PlayerDisplay::ShowHint(Hint, float)` para exibir uma hint por um determinado tempo.
```CSharp
Hint hint3 = new Hint()
{
    YCoordinate = 300,
    Alignment = HintAlignment.Right,
    AutoText = ev =>
    {
        ev.NextUpdateDelay = TimeSpan.FromSeconds(2f); // Você pode definir o atraso da próxima atualização no argumento do evento, e a hint se atualizará após o atraso. Isso é útil quando você deseja atualizar a hint após um determinado intervalo de tempo.

        return "TPS: " + Server.Tps.ToString("F2");
    },
};

// Se você quiser exibir uma hint temporariamente, pode usar ShowHint
playerDisplay.ShowHint(hint3, 12f); // Exibe uma hint por 12 segundos e depois a oculta.

```
---
Use DynamicHint para ajudar a evitar conflitos.
```CSharp
// DynamicHint é uma hint que pode se posicionar automaticamente para evitar sobreposição com outras hints.
DynamicHint dynamicHint = new DynamicHint
{
    Text = "Olá, Hint Dinâmica",
    TargetX = 100f,
};

playerDisplay.AddHint(dynamicHint);

```
---
Use CommonHint para desenvolver sua UI rapidamente.
```CSharp
// PlayerUI::CommonHint é um conjunto de hints predefinidas que ajudam a exibir hints facilmente
PlayerUI ui = PlayerUI.Get(player);
ui.CommonHint.ShowRoleHint("SCP173", ["Mate todos os humanos", "Use suas habilidades"]);
ui.CommonHint.ShowMapHint("Zona de Contenção Pesada", "O lugar onde a maioria dos SCPs nasce");
ui.CommonHint.ShowItemHint("Cartão", "Usado para abrir portas");
ui.CommonHint.ShowOtherHint("O servidor está iniciando!");
```
---
Os blocos de código acima criarão uma UI como esta:
![Visualização da hint](Images/GettingStartedExample.jpg)
Rotulado:
![Visualização da hint rotulada](Images/GettingStartedExampleLabeled.jpg)

Leia [Funções Principais](CoreFeatures.md) para saber mais

Clique [aqui](/Docs/Brazilian/README.md) para voltar ao README
