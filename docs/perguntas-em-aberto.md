# Perguntas em aberto

Estado de 1º de outubro de 2026, depois das respostas de 27/09, de 29/09 e de 01/10. As
respondidas saíram daqui e estão na seção 0 de `notas-modernizacao.md`, citadas com um `P` na
frente do número (`P2.2`). Ficaram só as duas que você deixou para o final, agora com a explicação
que você pediu e a pergunta no fim de cada uma.

Os números antigos continuam valendo, e os novos seguem a numeração de cada seção, sem reaproveitar
número. Os nomes que ainda não existem no código são só ilustração, e a sugestão, quando há, vem no
fim de cada pergunta.

Dá para responder pelo número, como antes: "5.9: sim" ou "6.7: b".

---

## 6. Opções e editores

As duas que você deixou para o final, explicadas como você pediu: por que existiam, como
funcionavam, se são necessárias, a importância e o estrago se saírem. Li o 0.7.1a (commit `7833d65`)
e o OverlayApplication (commit `83f4d8d`), que tem uma cópia própria do inspector, mais antiga
(`Core/MyAssemblies/InteractiveEditor.cs`), com a mesma lógica nesses dois pontos.

### 6.2 Visibilidade condicional

**Por que existia.** O OverlayApplication tem um editor só para os três tipos de componente
(retângulo, elipse e texto), o "Component", ligado à `ComponentPreset`. A classe é a mesma para os
três, e o tipo é uma string (`type`); só o texto usa `Text`, `FontName`, `IsBold`, `IsItalic` e
`IsUnderline`. Sem esconder esses campos por objeto, um retângulo mostraria campos de fonte que não
fazem nada. No editor de efeitos, os campos dependem do tipo escolhido na própria tela: `Color
order` e `Target color` só servem para o `Dynamic_Color`, e `Target value`, para os outros.

**Como funcionava.** Com duas peças.

- O `VariablePool`. O objeto ligado implementa uma interface da biblioteca, a `IVarProvider`, com um
  `string[]`. No bind, cada campo olha o pool do objeto, e a primeira posição diz o modo:
  - `{ "blacklist", "Text", "FontName", ... }`: os campos com esses nomes ficam invisíveis e não
    são ligados, e os outros ficam visíveis. É o que o `Init()` põe em todo componente que não é
    texto.
  - `{ "Partial Blacklist", "Effect" }`: o campo continua visível, mas fica fora do binder. É o do
    `EffectArgs`: quem grava o tipo de efeito é o handler da combo (`EffectChanged`), que liga o
    objeto de novo e grava o tipo pelo gancho de conversão (o `bruteForce`), e o pool tira o campo
    do binder para os dois não mexerem no mesmo valor.
  - qualquer outro começo, como o `{ "All" }` padrão do `sProperties`: nada muda.
- O `ToggleFieldVisible(valor, nome)`. Esconde ou mostra um campo à mão, e o inspector desloca as
  linhas de baixo. O `EffectChanged` chama três vezes, conforme o tipo de efeito.

**Os problemas que o código mostra.**

- O objeto de domínio passa a conhecer a biblioteca (`IVarProvider`), e o filtro é uma lista de
  strings sem conferência: um nome errado, ou renomeado, para de esconder sem aviso, e um pool vazio
  lança `IndexOutOfRangeException` no bind.
- A visibilidade é um efeito colateral do bind, e fica: o `{ "All" }` não mexe nela, e o `Unbind`
  não a devolve. Pelo código, ligar um texto depois de um retângulo deixa os campos de fonte
  escondidos.
- Em multi-bind, cada objeto reavalia o campo na sua vez, e o último decide.
- O `Partial Blacklist` existe só para o binder e o handler não disputarem o mesmo campo.

**É necessário?** A necessidade é real; o mecanismo, não. Um editor para tipos parecidos e campos
que dependem de outro campo vão continuar existindo no app reescrito. No rework, já dá:

- por tipo: o `GlobalOptions.Hide<T>` (P6.7) e o `[InspectorIgnore]`. Mas o caso do componente é por
  objeto (a mesma classe, com o tipo numa string), e nenhum dos dois cobre;
- à mão: `effect["TargetColor"].Visible = ...` num handler do `ValueChanged` do tipo de efeito,
  e as linhas de baixo sobem no layout (P7.5);
- o `Partial Blacklist` deixa de ter motivo: o `ValueChanged` avisa a mudança sem religar nada.

**Importância e estrago.** Para o OverlayApplication, a importância é alta: sem ela, o editor de
componentes mostra campos de fonte num retângulo, e o de efeitos, campos que não se aplicam. O
estrago de tirar sem nada no lugar é o app fazer à mão, em cada editor polimórfico: a cada bind e a
cada mudança do objeto, percorrer os campos e acertar o `Visible`, lembrando de mostrar de novo, que
é justamente o que o original esquecia.

**A pergunta.** No lugar do pool, uma regra no nó, sobre o objeto ligado:

```csharp
component["Text"].VisibleWhen = c => ((ComponentPreset)c).type == "Text";
effect["TargetColor"].VisibleWhen = e => ((EffectArgs)e).Effect == Effects.Dynamic_Color;
```

- (a) a regra no nó (`VisibleWhen`), lida junto com o valor (no bind, no `Refresh()`, num aviso do
  objeto e depois de uma gravação) e guardada, para a view e o layout não lerem o objeto; quando o
  resultado muda, a view fica sabendo por evento. Em multi-bind, a linha só aparece se a regra vale
  para todos os objetos, já que ela edita todos de uma vez. O `Visible` à mão continua valendo
  junto.
- (b) só o `Visible` à mão, com o handler no app, como já funciona hoje
- (c) um sucessor do pool: o objeto diz o que esconder, por uma interface

Sugestão: (a). A regra fica no inspector, e não no objeto, como foi com o `INotifyPropertyChanged`
no lugar do `ITwoWayBinderTransmiter` (P2.6), e o inspector a reavalia sozinho, então mostrar de
novo não depende de ninguém lembrar.

### 6.3 Os editores do `EditField()`

**Por que existia.** O `EditField(nome)` devolve o controle WinForms do campo (`TextBox`,
`ComboBox`, `TrackBar` ou `Button`), e o app faz o cast e mexe no que quiser. O 0.7.1a não tinha um
jeito agnóstico de dizer "esta combo lista estes valores", "este campo abre o seletor de cor" ou
"este botão faz isto", então a configuração ia direto no controle. Era a válvula de escape mais
usada.

**Como funcionava.** O `Modify.EditField(nome)` acha o campo pelo nome, ou pega o último criado
quando o nome vem vazio, e devolve o `Control`. O OverlayApplication chama 24 vezes (mais 4
comentadas), quase sempre logo depois do `AddField`, e faz com isso:

| O que o app fazia | Onde | No rework |
|---|---|---|
| Texto e ação de um botão (▲ e ▼ da camada) | Layer | `AddButton(nome, texto, ação)` (P1.6, commit `0c97638`) |
| Faixa e passo do `TrackBar` (Opacity de 0 a 255, de 5 em 5) | Window, Component | `EditorKind.Slider` com o `Range`, ou `[InspectorRange(0, 255, Step = 5)]` |
| Combo com os valores de um enum (CapValues, Type, AutoRevert) | Component, Effect | automático: enum vira `EditorKind.Choice` |
| Combo com true e false (IsBold, IsItalic, IsUnderline) | Component | automático: bool vira `EditorKind.Toggle` |
| O clique abre o seletor ARGB (Color1, Color2, TransparencyKey, BackgroundColor, Start color e Target color) | Window, Component, Effect | `EditorKind.Color`, e a view abre o seletor (P5.5) |
| A combo das tags como menu ("Add Tag" e "Remove Tag", com uma caixa de texto) | Target | o editor de lista (`EditorKind.List`, commit `5395dea`) |
| Reagir à escolha (`EffectChanged`, `TryToSetStartValue`) | Effect | o `ValueChanged` do nó (commit `eb497c6`) |
| Pintar o campo com a cor, e escrever um valor à mão (`SetFieldValue`) | Effect, Layer | o editor `Color` mostra a cor; o `AddDisplay(nome, getter)` com o `Refresh()` |
| Combo com uma lista de strings para um membro string (KeyBinder com os nomes de `Keys`, Bind to Variable com `AVAILABLE_VARS`) | Object, Effect | falta |
| O clique abre o diálogo de fonte (Font) | Component | falta; no app, o corpo do handler estava todo comentado |
| Combo editável (`DropDown`) ou não (`DropDownList`), e a ordem dos controles (`BringToFront`) | vários | detalhe de view, para a válvula da P7.4 |

**É necessário?** O `EditField()` em si, não: a P7.4 já decidiu o sucessor, um callback por
plataforma quando o controle é criado e um agnóstico quando a linha termina de ser montada. É o
`EditField()` na hora certa, quando o controle nasce (e nasce de novo, se a view for refeita), e sem
depender de "o último campo criado". O que é necessário é a configuração agnóstica cobrir o que o
app fazia por ele, para a válvula ser exceção. Pela tabela, faltam só duas coisas: a escolha numa
lista para um membro que não é enum, e o seletor de fonte.

**Importância e estrago.** A importância era alta, porque era o único jeito de configurar boa parte
dos editores; hoje, quase tudo virou configuração. O estrago de tirar a válvula sem nada no lugar
seria tornar impossível o que a configuração não cobre; com a P7.4, isso some, e o que faltar vira
um callback no app.

**A pergunta.** A lista de escolha, no nó:

```csharp
inspector["keyStr"].Choices = () => Enum.GetNames<Keys>();
inspector["TargetVariableName"].Choices = () => AVAILABLE_VARS;
```

- (a) o `Choices` no nó: uma função que devolve os valores, lida quando a view abre a lista (então a
  lista pode mudar com o objeto), com o editor `Choice`; o valor escolhido é gravado como qualquer
  outro, convertido para o tipo do membro. A fonte fica para a válvula da P7.4, até aparecer de
  novo.
- (b) as duas viram editor: o `Choices` e um `EditorKind.Font`, que abre o diálogo de fonte da
  plataforma e grava o nome da família
- (c) nenhuma: a lista e a fonte vão pela válvula da P7.4, no app

Sugestão: (a). A lista aparece em dois editores do app e é agnóstica. A fonte apareceu uma vez, sem
funcionar, e mexe em quatro membros (`FontName`, `IsBold`, `IsItalic` e `IsUnderline`), o que pede
um desenho próprio.
