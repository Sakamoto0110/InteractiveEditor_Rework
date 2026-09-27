# Notas de modernização

Base de leitura: `Sakamoto0110/InteractiveEditor`, branch `InspectorVariant0.7.1a`
(commit `7833d65`, abril de 2021; .NET Framework 4.8 + WinForms; 3.445 linhas em 36 arquivos
na biblioteca), comparado com o estado atual deste repositório. Como referência de uso real:
`Sakamoto0110/OverlayApplication` (commit `83f4d8d`, fevereiro de 2021), o app para o qual o
inspector foi feito.

O que já foi decidido está na seção 0, e o que já foi aplicado no código está marcado com `[x]` na
seção 6. O resto são propostas para discutir.

Convenção: **[original]** é como era no 0.7.1a, **[rework]** é como está hoje aqui,
**[proposta]** é o que eu sugiro.

---

## 0. Decisões tomadas

- **Modo principal: automático**, montado como uma pilha de políticas (3.2). Precedência:
  **manual > metadados por atributo > descoberta por reflection**.
- **Atributos próprios do inspector**, em vez dos atributos padrão do .NET, para evitar ambiguidade.
  Duas descrições: uma curta, usada como tooltip, e uma longa, para o `(?)` quando ele estiver
  disponível.
- **Service locator descartado.** Localizar e aplicar já estão cobertos pelo `IEnumerable` e pelo
  indexador (3.6).
- **Paginação substituída por scroll.** Um app que quiser páginas implementa por cima.
- **Sem compromisso de compatibilidade.** O rework não vai ser portado para nenhum app real, e o
  OverlayApplication vai ser reescrito do zero; o uso real (1.2) serve só de referência.
- **Uma DLL, um binário** (`net10.0`): sem código de Windows na biblioteca, um alvo só basta.
  Substitui o "um projeto, dois binários" do commit `c537554`; as views entram nesta mesma DLL
  quando existirem. Aplicado no commit `5560223` (3.7).
- **TuxHost para as verificações, NoHost local**: o TuxHost (`net10.0`) roda em qualquer sistema e é
  o console de verificação versionado; o NoHost voltou a ser só para os seus testes, em
  `net10.0-windows`, versionado como na `main` e com a pasta no `.gitignore`. Aplicado nos commits
  `e11b2df`, `176f366` e `9587e12`.
- **Primitivos com prefixo `Px`**, o mesmo da PixieLib (3.9): `PxPoint`, `PxPointF`, `PxSize` e
  `PxSizeF`; cores como `ArgbColor` e `HslColor`. Substitui o `SK` provisório do commit `21cbeda`.
  Aplicado no commit `9e15f6e` (3.4).
- **Opções em três camadas**: global/estática (`GlobalOptions`, com a flag que exige
  `[InspectorExpandable]` para expandir objetos aninhados), por inspector e por campo. Primeiro corte
  aplicado no commit `5319247` (seção 7).
- **Descrições em dois atributos**: `[InspectorTooltip]` (curta) e `[InspectorHelp]` (longa).
- **Chaves em string**: caminho relativo ao nó em que o indexador é chamado (`"Moo.MooX"`), que
  também pode ser encadeado (`inspector["Moo"]["MooX"]`); sem atalho pelo nome do tipo. Seletor por
  expressão fica para depois.
- **Sem configurador**: as opções são propriedades do próprio nó, e a configuração é feita no
  inspector depois do `Create` (`inspector["x"].Label = ...`). Nada de `map`, `Modify`, provider ou
  callback. Aplicado no commit `bf6f74f` (seção 7).
- **Exceções explodem**: caminho desconhecido lança, e exceções das políticas sobem sem ser
  engolidas.
- **Binding pela cadeia de pais**: só a raiz guarda a instância, e cada nó lê e grava pelo pai a
  cada chamada. Structs são gravadas de volta no dono, e `SetValue` com um pai null lança exceção.
  Aplicado no commit `2a1cf94` (3.3).
- **Um membro, um nó**: a descoberta monta a árvore direto; `InspectorNode` é qualquer membro, com
  ou sem filhos, e `Inspector` é a raiz (por enquanto só o objeto, o `Create` e o `bind`; ver
  3.10). As duas políticas viram classes estáticas, sem interface nem instâncias, chamadas em ordem
  no `Create`. Aplicado nos commits `5560223` e `6622a41`.

---

## 1. A essência da ferramenta

### 1.1 O que o 0.7.1a entrega

O que o 0.7.1a entrega e que deveria continuar valendo, em qualquer plataforma:

1. **Inspector automático a partir de um tipo.** `InspectorController.BuildInspector<Foo>(options, configurator)`
   gera um painel com um campo por membro público, sem escrever UI campo a campo.
2. **Objetos aninhados viram grupos.** Cada membro de tipo classe vira um cabeçalho recolhível,
   com os filhos recuados por nível (`NestedTypeXOffset`).
3. **Customização por campo, declarativa.** O `BindingConfigurator` recebe o mapa e ajusta cada
   campo com `map.Modify("x", (ref BindingArgs a) => ...)`: tipo de controle, rótulo, flags,
   sanitizadores e ação pós-bind.
4. **Edição ao vivo nos dois sentidos.** Editar na UI grava no objeto na hora
   (`ValueChanged` → `SetValue` → `ApplyFunction`). O objeto também pode empurrar mudanças para a UI
   (`ITwoWayBinderTransmiter` / `ITwoWayBinderReciever`).
5. **Scrubbing no rótulo** (`FieldFlags.UseHSliderControl`): arrastar o rótulo na horizontal muda o
   valor numérico, com multiplicador.
6. **Multi-bind.** O mesmo inspector edita vários objetos; no scrubbing, o delta é aplicado a cada um
   (edição relativa).
7. **Sanitização de entrada** (`CapFunction` + `Modifiers`: `ONLY_NUMBERS`, `POSITIVE_NUMBERS`,
   `MAX_SIZE`, `CONTAINS`), encadeável.
8. **Filtros.** Blacklist/whitelist por nome (`BindingFilter`), `TypeSafeLock` como opt-in de quais
   tipos podem ser inspecionados e expandidos, e `IVarProvider` para o próprio objeto esconder campos.
9. **Ciclo de vida observável.** `FieldPreBind`, `FieldBindStarted`, `FieldBindFinished` e
   `FieldBindFailure` (com mensagem, motivo, possível solução, linha e membro de origem), mais
   `BindStarted` / `BindFinished` no inspector.
10. **Montagem manual, além da automática.** `Modify.AddField<TextBox>("nome")` +
    `Binder.BindToVariable(...)` montam campos à mão, inclusive botões, separadores e cabeçalhos que
    não correspondem a membros.
11. **API para mexer depois de criado.** Localizar campo, pegar o controle ou o rótulo, mudar
    visibilidade e multiplicadores, ler e gravar valor.
12. **Opções de layout e hospedagem.** Margens, altura de campo, espaçamento, largura automática,
    recuo, recolher, janela própria (`CreateOwnWindow`) ou anexar a um form existente, dock.

Previstos no 0.7.1a, mas incompletos ou sem uso:

- paginação (`_NextPage` / `_PrevPage` e os botões ◀ ▶); decidido: vira scroll;
- botões Apply, Reload e Unbind (criados e escondidos) e o modo de aplicar sob demanda
  (`AutoUpdateEnabled` desliga a gravação, mas nada aplica depois);
- o `(?)` de ajuda (`EnableQuestionMark` só aparece nos flags padrão; o rótulo `(?)` é reaproveitado
  como seta de recolher);
- `MemberSafeLock` e `GlobalOptions.RequireAttrMemberSafeLock`;
- `TypeBinderMode` (atribuído, nunca lido);
- `InspectorOptions.VerticalSpacing`, `FieldHeight`, `HeaderHeight`, `FooterHeight` e `ShowText`
  (declarados, nunca lidos).

### 1.2 O que o uso real mostra (OverlayApplication)

No OverlayApplication o inspector ainda estava embutido no app (`Core/MyAssemblies/InteractiveEditor.cs`,
criado com `GenerateMyEditor<T>(form, nome, x, y, largura, altura, flags)`). O uso está em
`DefaultOverlayFactoryPreInit.cs`, `EffectFactoryWindow.cs`, `TreeViewHandler.cs`, `Properties.cs` e
`OverlayObjectComponent.cs`.

- **O modo manual é o modo principal.** Todos os editores do app são montados campo a campo:
  `Modify.AddField<TextBox>("PosX", FieldFlags.UseHSliderControl)` +
  `Binder.BindToVariable(null, "x", ONLY_NUMBERS)` + `Modify.SetHSliderMultiplier(0.01f)`.
  - O rótulo quase nunca é o nome do membro (`PosX` → `x`, `UID` → `UIDp`, `Color1` → `FillColor`,
    `TopLeftCurvature` → `TopLeftRad`).
  - Também há campos sem membro: botões (`LayerUp` / `LayerDown`, com ação) e campos só de exibição
    (`UID`, `Name`, e `Layer`, preenchido com `SetFieldValue`).
- **`EditField()` é a válvula de escape mais usada**, sempre para suprir algo que a biblioteca não
  oferecia:
  - `TrackBar`: `Minimum`, `Maximum` e `TickFrequency`;
  - `ComboBox`: `DataSource` com valores de enum, `true`/`false` e listas de strings, mais
    `DropDownStyle`;
  - seletores: o clique abre o seletor de cor ARGB ou o diálogo de fonte;
  - botões: texto e ação de clique.
- **Rebind por seleção.** Na árvore, selecionar um objeto escolhe o editor pelo tipo
  (`Dictionary<Type, EditorType>`), mostra esse editor, chama `UnbindObject()` e depois
  `BindToObject(obj, multiSelect)`. Com seleção múltipla, entra o multi-bind.
- **Editor polimórfico com filtro por instância.** Um único editor "Component" atende retângulo,
  elipse e texto; os componentes que não são texto preenchem
  `VariablePool = { "blacklist", "Text", "FontName", "IsBold", ... }` para esconder os campos de texto.
- **Visibilidade condicional.** O editor de efeitos mostra ou esconde campos conforme o tipo de efeito
  (`Modify.ToggleFieldVisible(b, "Color order")`).
- **Objeto → UI ao vivo.** Os setters de `x`, `y`, `w` e `h` dos componentes chamam
  `_BindedTo?.EditFieldValueByVariableName("_x", _x)`: arrastar o objeto no overlay atualiza o
  inspector.
- **Campos em vez de propriedades.** O editor de componentes liga em `_x`, `_y`, `_w` e `_h` (campos
  públicos), porque a biblioteca só enxergava campos; com isso, editar pela UI grava direto no campo
  e pula o setter de `x`. O rework já descobre propriedades, o que elimina esse contorno.
- **Gancho de conversão em uso**: `BindToObject(ActiveEffect, bruteForce: ...)` no editor de efeitos.
- **Vários inspectors na mesma janela**, com posição absoluta e empilhados por código (o
  `TreeViewHandler` reposiciona cada editor abaixo do de camadas), e com `FieldHeight = 23` e
  `Horizontal_Spacing = 0` por editor. É o tipo de configuração de layout que faz sentido resolver
  antes da view.
- **Parâmetros de sanitizador compartilhados por posição.** `MAX_SIZE + CONTAINS` com
  `{ "rgba", "4" }` funciona porque, nessa versão, o `MAX_SIZE` lê `args[1]` e o `CONTAINS` lê
  `args[0]`. No 0.7.1a o `MAX_SIZE` passou a ler `args[0]`, e a mesma combinação quebraria
  (`Convert.ToInt32("rgba")`).

**O que isso muda nas prioridades**

1. O modo automático é o principal (seção 0), mas o manual continua necessário e tem precedência:
   campos declarados, botões e campos só de exibição convivem com os refletidos no mesmo inspector.
2. A configuração agnóstica precisa cobrir o que hoje sai por `EditField()`: faixa e passo de slider,
   itens de escolha (enum, bool, lista), seletores (cor, fonte...) e ação de botão. O que sobrar vai
   por uma válvula de escape por plataforma.
3. Rebind, multi-bind, filtro por instância, visibilidade condicional e objeto → UI deixam de ser
   desejáveis e passam a ser obrigatórios.
4. Cada sanitizador precisa ter os próprios parâmetros, em vez de um array compartilhado lido por
   posição.

---

## 2. Do original para o rework

| [original] | Papel | [rework] hoje | Situação |
|---|---|---|---|
| `Mapping.ApplyFilter` + `AddNestedMembers` | Descobrir membros, recursivo | `ReflectionDiscovery.AddMembers` (monta os nós direto) | Coberto e ampliado: campos **e** propriedades, guarda de ciclo por caminho |
| `BindingArgs`, `MapHandler.Modify`, `BindingConfigurator` | Configuração por campo | não existe | **Principal peça a portar** |
| `Mapping.ResolveTypes`, `DefaultControlMapping` | Escolher o controle pelo tipo | não existe | Vira a escolha de um *tipo de editor* agnóstico |
| `BindingService.DoBind` | Montar grupos, filhos e recuo | `Inspector.Create<T>()` monta a árvore | Coberto pela árvore (recuo = profundidade) |
| `Fieldset.BindToObject` (busca por nome + `goto`) | Achar a instância aninhada | `GetValue`/`SetValue` pela cadeia de pais, a cada chamada | Coberto, e mais correto |
| `Fieldset` (Panel + Label + controle + binding + conversão + scrubbing) | Uma linha do inspector | `InspectorNode` (binding e opções); a view ainda não existe | Dividir em nó e view |
| `Inspector` (Panel, layout manual, scroll, botões) | Raiz e view ao mesmo tempo | `Inspector` (só a raiz); a view ainda não existe | Idem |
| `FieldLocatorService` | Localizar campos | `IEnumerable<InspectorNode>` + indexador | Coberto |
| `InvokerService` | Aplicar algo a todos os campos | `foreach` / LINQ | Coberto |
| `ManipulatorService` | Mexer depois de criado | não existe | Portar como métodos nos nós e na configuração |
| `ITwoWayBinder*` | Objeto → UI | não existe | `INotifyPropertyChanged` |
| `IVarProvider` | Filtro vindo do objeto | não existe | Interface tipada |
| `InspectorOptions`, `GlobalOptions` | Opções | não existe | Opções por inspector, com primitivos agnósticos |
| `Modifiers` (`CapFunction`, flags) | Sanitização e flags | não existe | Pipeline tipado |
| `InspectorController` | Fachada pública | `Inspector.Create` | Ver 3.6 |

---

## 3. Teorias

### 3.1 Camadas, tudo numa DLL

1. **Descoberta** (agnóstica): tipo → árvore de nós. Os membros são lidos uma vez e cacheados por
   `Type`; os nós são de cada inspector, porque guardam as opções (3.10).
2. **Configuração** (agnóstica): o sucessor do `BindingArgs`. Diz *como* cada membro aparece, sem
   citar controles de nenhuma plataforma.
3. **Binding** (agnóstico): a árvore de nós ligada a uma ou mais instâncias. Lê, grava, converte,
   sanitiza e avisa mudanças.
4. **Layout** (agnóstico): calcula posições e tamanhos das linhas a partir das opções, usando os
   primitivos.
5. **Apresentação** (WinForms e WPF): só aplica o layout e traduz cada tipo de editor para um controle.

Só a camada 5 conhece tipos de UI. É a separação que faltava no 0.7.1a, onde o `Fieldset` fazia as
cinco coisas ao mesmo tempo.

### 3.2 Configuração por campo (sucessor do `BindingArgs`)

**Pilha de políticas** (decidido: o automático é o modo principal)

O modelo de cada campo é montado em camadas, e cada camada pode sobrescrever o que a anterior
definiu:

1. **Descoberta por reflection**: membros, tipo, getter e setter, e o editor padrão pelo tipo.
2. **Metadados por atributo**: atributos próprios do inspector, no tipo e nos membros.
3. **Manual**: o que for definido no inspector depois do `Create` (`inspector["x"].Label = ...`).

As camadas 1 e 2 são `ReflectionPolicy` e `AttributePolicy`, classes estáticas chamadas nessa ordem
no `Create`; uma camada nova é mais uma classe e uma linha ali. Os filtros por nome e por tipo
(blacklist/whitelist, opt-in de tipos) saem com uma regra em massa depois do `Create` (seção 7); o
filtro por instância fica com a visibilidade condicional.

**Atributos do inspector** (próprios, para não haver ambiguidade com `System.ComponentModel` ou
DataAnnotations; aplicados no commit `5319247`)

| Atributo | Para quê |
|---|---|
| `[InspectorIgnore]` | Não mostrar o membro |
| `[InspectorLabel("...")]` | Rótulo |
| `[InspectorTooltip("...")]` | Descrição curta, usada como tooltip |
| `[InspectorHelp("...")]` | Descrição longa, para o `(?)` |
| `[InspectorReadOnly]` | Somente leitura |
| `[InspectorEditor(EditorKind...)]` | Tipo de editor |
| `[InspectorRange(min, max, Step = ...)]` | Faixa (slider, limite) |
| `[InspectorScrub(multiplicador)]` | Scrubbing no rótulo |
| `[InspectorOrder(n)]` | Ordem de exibição |
| `[InspectorExpandable]` | Opt-in de expansão, no membro ou no tipo (sucessor do `TypeSafeLock`) |

**Configuração**

- A configuração é feita no próprio inspector, por caminho (`inspector["Moo.MooX"]`) ou encadeada
  (`inspector["Moo"]["MooX"]`), sem `map`, `Modify`, provider ou callback (decidido; seção 7).
- Chave pelo caminho, relativo ao nó em que o indexador é chamado, e não pelo nome curto. No
  original o mapa é um `Dictionary` chaveado por `finfo.Name` com `if (!map.ContainsKey(...))`: dois
  membros com o mesmo nome em níveis diferentes fazem o segundo sumir sem aviso.
- Depois: seletor por expressão (por exemplo `inspector[f => f.Moo.MooX]`), que o compilador checa e
  que acompanha renomeações.
- Em vez de `FieldSet_FieldType = typeof(TextBox)`, um enum agnóstico de editor (`EditorKind`: `Text`,
  `Number`, `Toggle`, `Choice`, `Slider`, `Color`, `Button`, `Display`, `Header`, `Separator`). Cada
  view decide o controle.
- O que a configuração guarda: rótulo, editor, flags (`ReadOnly`, `Disabled`, scrubbing),
  multiplicadores (scrubbing e slider), sanitizadores, visível, recolhido e ação pós-bind.
- A ideia do `TypeSafeLock` (opt-in de quais tipos podem ser expandidos) continua, como atributo do
  inspector (`[InspectorExpandable]` na tabela acima).
- O `TypeBinderMode` do original (`Automatic` / `Manual`, declarado e nunca usado lá) pode continuar
  existindo para quando se quer só os campos declarados; o padrão passa a ser `Automatic`.

Esboço do editor de componentes do OverlayApplication na configuração atual. As linhas marcadas
são de cortes seguintes, com nomes provisórios:

```csharp
var component = Inspector.Create<ComponentPreset>();
component["X"].Label = "PosX";
component["X"].ScrubMultiplier = 1;
component["ScaleX"].ScrubMultiplier = 0.01;
component["Opacity"].Editor = EditorKind.Slider;
component["Opacity"].Range = new NumericRange(0, 255, 5);
component["FillColor"].Label = "Color1";

// cortes seguintes:
// editor de cor automático (hoje System.Drawing.Color ainda vira um grupo com A, R, G, B...)
// component["Text"].VisibleWhen = c => ((ComponentPreset)c).IsText;   // sucessor do VariablePool
// component.AddButton("LayerUp", "▲", () => tree.OnLayerUp());
// component.AddDisplay("Layer", () => tree.SelectedIndex);

component.bind(selected);    // rebind a cada seleção (já funciona)
component.bind(selection);   // cortes seguintes: multi-bind
```

No modo automático (o padrão), os membros refletidos entram sozinhos, os atributos ajustam e a
configuração manual tem a palavra final; com `TypeBinderMode.Manual`, entra só o que foi declarado.

### 3.3 Binding

- **Cadeia de pais** (aplicado, commit `2a1cf94`): só a raiz guarda a instância. Cada nó resolve o
  valor pelo pai a cada `GetValue`/`SetValue`, então nada abaixo da raiz fica velho quando uma
  referência muda. Quando o dono de um membro é uma struct, o setter altera uma cópia boxed, que é
  gravada de volta no dono dela, subindo até a primeira class ou até a raiz; vale para qualquer
  aninhamento de class e struct. Pai null: `GetValue` devolve null e `SetValue` lança
  `InvalidOperationException` dizendo qual pai é null. Struct na raiz: o inspector edita a própria
  cópia, e o host lê o resultado com `GetValue()`.
- **Rebind** (aplicado, commit `2a1cf94`): `bind(obj)` de novo troca o objeto atual, como no
  original (o `BindToObject` desfaz o bind anterior). O `IsTypeBound`, que bloqueava, saiu.
- **ReadOnly** (aplicado, commit `6117bb1`): `SetValue` lança `InvalidOperationException` quando o
  nó está marcado como somente leitura, antes de ler qualquer coisa. Vale para o que a reflection
  marca (setter privado, `init`, campo `readonly`), para o `[InspectorReadOnly]` e para a camada
  manual, que continua podendo reabrir (`inspector["x"].ReadOnly = false`). Como a gravação de volta
  de uma struct passa pelo `SetValue` do dono, um membro de struct somente leitura também é
  recusado; já os membros de uma class somente leitura continuam editáveis, porque a edição é no
  próprio objeto (seção 5, item 6).
- **Multi-bind**: `Bind(a, b, c)`. Valores diferentes aparecem como "misto" (o original mostra só o
  primeiro); o scrubbing aplica o delta em cada instância, como no original.
- **Objeto → UI**: `INotifyPropertyChanged` no lugar de `ITwoWayBinderTransmiter`. O objeto deixa de
  guardar referência ao inspector (`BindedTo`), e vários inspectors podem observar o mesmo objeto.
  Um `Refresh()` manual cobre quem não implementa a interface.
- **UI → objeto**: texto → valor por `TypeConverter` ou `IParsable<T>`, com cultura definida. O
  original usa `Convert.ToDouble` com a cultura atual, e o `ONLY_NUMBERS` aceita tanto `.` quanto `,`.
- **Sanitizadores**: separar os de texto (dígitos, tamanho máximo, caracteres permitidos) dos de
  valor (mínimo, máximo), numa lista ordenada. No original eles misturam texto e número: no caminho
  do `TextBox` o `POSITIVE_NUMBERS` recebe `string` e não faz nada; no scrubbing, o delegate
  multicast devolve só o resultado do último; e com três ou mais funções encadeadas o
  `GetTextboxData` reaplica funções anteriores.
- **Gancho de conversão** (o "TheBrute"): manter como ponto de extensão, por campo ou global.
- `ApplyFunction` vira um evento `ValueApplied`.
- **Eventos de ciclo de vida**: manter os quatro, com um payload de falha tipado (mensagem, motivo,
  sugestão, caminho).

### 3.4 Layout agnóstico e os primitivos

- O original posiciona cada linha à mão (`Y = índice × (altura + espaçamento)`), desloca as seguintes
  quando uma some e rola a lista movendo painel por painel. Proposta: um passo de layout que produz
  os retângulos de cada linha (linha, rótulo, editor) a partir das opções, da profundidade, da
  visibilidade e do recolhimento. A view só aplica.
- É aqui que os primitivos entram. `Location`, `Size`, `Margins` e `DockStyle` das opções eram tipos
  do `System.Drawing` e do WinForms; no rework viram primitivos próprios.

**Nomes** (prefixo `Px`, decidido: o mesmo da PixieLib, ver 3.9)

| Antes | Agora | Situação |
|---|---|---|
| `Point`, `PointF` | `PxPoint`, `PxPointF` | Aplicado (`21cbeda` com `SK`, `9e15f6e` com `Px`) |
| `Size`, `SizeF` | `PxSize`, `PxSizeF` | Aplicado |
| `Color` | `ArgbColor` | Aplicado; espaço de cor explícito, ponte para `System.Drawing.Color` e `System.Windows.Media.Color` |
| `ColorHSL` | `HslColor` | Aplicado; idem, com o acrônimo em PascalCase |
| (novo) | `PxRect` | Proposto: resultado do passo de layout |
| (novo) | `PxPadding` | Proposto: margens (`Padding` no WinForms, `Thickness` no WPF) |
| (novo) | `PxDock` (enum) | Proposto: substitui o `DockStyle` nas opções |

Com os nomes novos, um arquivo WinForms ou WPF que importa `InteractiveEditor.Primitives` deixou de
ter ambiguidade (CS0104) com `System.Drawing`, `System.Windows` e `System.Windows.Media` (testado).
O `SK` provisório colidia com o SkiaSharp (`SKPoint`, `SKSize` e `SKColor`); o `Px` não colide com
ele nem com o `System.Numerics` (testado, todos importados no mesmo arquivo).

**Regras de conversão (proposta)**

- `System.Drawing` (int ↔ int, float ↔ float): implícitas nos dois sentidos, sem perda.
  `System.Drawing.Primitives` existe fora do Windows (testado no Linux), então essas conversões
  ficam no build comum.
- Para o WPF (double): implícita para pontos. Para `System.Windows.Size`, explícita (ou com clamp),
  porque o `Size` do WPF lança exceção com largura ou altura negativa (comportamento documentado) e
  o primitivo aceita negativos.
- Do WPF para os tipos int ou float: explícitas, porque perdem precisão.
- `ArgbColor` ↔ `HslColor`: declarar num lugar só. Hoje a conversão `ArgbColor → HslColor` existe
  nas duas structs e dá CS0457 no primeiro uso.
- `PxPadding` e `PxDock` ↔ `Padding`, `Thickness` e `DockStyle`: junto com as views (ver 3.7).

### 3.5 Apresentação

- A view não herda de `Inspector`: recebe um e o observa. Assim a raiz e os nós aninhados são
  tratados do mesmo jeito.
- Os stubs antigos (`Presentation/WF`, `Presentation/WPF` e as fábricas `Create<T>(host)` do
  `Inspector.Windows.cs`) saíram no commit `5560223`; a apresentação começa do zero quando for a vez
  dela.
- Cada plataforma traduz o enum de editor para controles (`TextBox`, `ComboBox` preenchido com os
  valores do enum, `TrackBar`/`Slider`, `CheckBox`...) e implementa o scrubbing com captura de mouse
  no rótulo.
- Fábricas com nomes distintos por plataforma (por exemplo `CreateWinFormsView` / `CreateWpfView`).
  Com overloads que diferem só pelo tipo `Control`, um projeto só WinForms que referencia a DLL
  diretamente não compila (CS0012, pede `PresentationFramework`); com nomes distintos, compila
  (testado).
- Scroll e empilhamento: usar o que a plataforma já tem (`AutoScroll`, `ScrollViewer`) em vez de
  mover painel por painel.
- Válvula de escape por plataforma, o sucessor limpo do `EditField()`: por exemplo um callback
  `ControlCreated(caminho, controle)` na view de cada plataforma, para o que a configuração agnóstica
  não cobrir.

### 3.6 Serviços (decidido: descartados)

O service locator do original existia para tirar responsabilidades de um arquivo monolítico e
agrupar funcionalidades. No rework ele não volta:

- localizar e aplicar já estão cobertos pelo `IEnumerable<InspectorNode>`, pelo indexador e por LINQ,
  numa fração das linhas;
- manipular e vincular viram métodos do inspector e dos nós.

### 3.7 Um projeto, um binário (aplicado)

Primeiro veio o multi-target, no commit `c537554`: `net10.0` e `net10.0-windows` num projeto só, com
o código de Windows em arquivos parciais `*.Windows.cs` (as fábricas `Create<T>(host)` e as
conversões `System.Windows.*` dos primitivos) e nas pastas `Presentation/WF` e `Presentation/WPF`,
fora do alvo `net10.0`.

No commit `5560223` esse código saiu (eram stubs e conversões que nada usava), e com ele o segundo
alvo: o `InteractiveEditor.csproj` voltou a ter só `<TargetFramework>net10.0</TargetFramework>`, sem
`UseWPF`, `UseWindowsForms`, `EnableWindowsTargeting` ou blocos condicionais. A biblioteca gera um
binário só, e os hosts WinForms e WPF (`net10.0-windows`) a referenciam normalmente.

- Console de verificação em `net10.0`, que roda sem o runtime WindowsDesktop: era o NoHost; agora é
  o TuxHost (commit `e11b2df`), e o NoHost voltou a ser local.
- Quando as views chegarem, elas entram nesta mesma DLL, e a biblioteca volta a precisar do Windows.
  Há duas saídas: só `net10.0-windows` (projeto sem condições, mas o TuxHost deixa de rodar fora do
  Windows) ou de novo os dois alvos, com o bloco condicional. As conversões dos primitivos para o
  WPF voltam junto.

Verificado no commit `5560223`: a solução compila sem erros e sem warnings, e o TuxHost imprime
exatamente o mesmo de antes.

### 3.8 Performance

- Descoberta uma vez por tipo (cache), em vez de reflection a cada bind (o original chama
  `GetField(nome)` a cada `BindToObject`).
- Busca por caminho num dicionário, em vez de varrer a lista por nome. O original chama `LocateName`
  dentro de laços, o que vira O(n²) ao mudar a visibilidade de grupos.
- Um passo de layout por mudança, com a view suspendendo o redesenho (`SuspendLayout`, `BeginInit`),
  em vez de mover cada painel.
- Accessors compilados só se o profiling pedir; o reflection do .NET 10 já é bem mais rápido que o do
  .NET Framework 4.8.
- Desinscrever eventos ao desfazer o bind e ao descartar a view (o `Dispose` do original é
  incompleto).
- Leitura pela cadeia de pais: um getter por nível a cada `GetValue` (profundidade 3 = 3 chamadas de
  reflection). Irrelevante na escala do editor; se pesar, cachear os valores por ciclo de refresh.

### 3.9 PixieLib: primitivos e matemática fora do inspector (adiado)

Discutido e prototipado fora do repositório; nada mudou no código. Fica registrado para quando for a
vez.

- **A ideia**: os primitivos (hoje em `InteractiveEditor/Primitives`, que nada usa ainda) saem do
  inspector e viram a base da PixieLib em C#, que depois ganha vetores, matrizes e transformações 2D
  e 3D.
- **Enquanto isso** (decidido): os primitivos atuais ficam no InteractiveEditor para segurar as
  pontas, já com o prefixo `Px` (commit `9e15f6e`). Só saem quando a PixieLib existir.
- **Onde** (proposta): no próprio repositório `Sakamoto0110/PixieLib`, numa pasta `dotnet/` ao lado
  da `cpp/` que já existe (o lado C++, de 2023, usa o prefixo `px` e já tem `Vec2` em `double`). É a
  mesma biblioteca em duas linguagens, então não há "duas PixieLib". No NuGet, `PixieLib` está
  livre; `Pixie` não.
- **Nomes** (decidido): o prefixo provisório `SK` virou `Px`, espelhando o `pxVec2` do C++
  (`PxPoint`, `PxSize`, `PxVec2`, `PxMat3`...), e os primitivos atuais já usam (3.4). Não colide
  com `System.Drawing`, `System.Numerics` nem SkiaSharp (CS0104, testado). As cores continuam
  `ArgbColor` e `HslColor`; se viram `PxColor` fica em aberto (seção 5).
- **Alvos** (decidido): a NekoLib vai usar a PixieLib também no .NET Framework, então ela compila
  para `net481` além do .NET moderno.
- **Precisões** (decidido): `float`, `double` e `int` saem de um modelo só, por um source generator
  (`PxVec2f`, `PxVec2d`, `PxVec2i`). Generics com matemática genérica (`INumber<T>`) não servem,
  porque não existem no `net481` (testado). O modelo usa marcadores (`__S__` para o sufixo, `__T__`
  para o tipo) e blocos `#if` por tipo (`PX_FLOAT`, `PX_FLOATING`, `PX_INT`); no protótipo, a
  biblioteca compilou nos dois alvos, e `PxVec2i` não tem `Length()`.
- **Repasse para o `System.Numerics`**: a versão `float` repassa as operações para `Vector2`,
  `Vector3`, `Matrix3x2` e `Matrix4x4`, e isso sai de graça: uma função própria que chama
  `Vector2.Add` gera o mesmo `vaddps` que chamar direto (testado no assembly do JIT). O
  `System.Numerics` só tem `float`, então `double` e `int` são implementação própria.
- **`PxPoint` e `PxSize` sobre `PxVec2`** (proposta): o mesmo dado (8 bytes), com nomes e só as
  operações que fazem sentido: ponto + tamanho → ponto, ponto + vetor → ponto, ponto − ponto →
  vetor, tamanho + tamanho → tamanho, tamanho × k → tamanho. Conversão implícita para `PxVec2` (a
  matemática vem dele) e explícita de volta, testadas no protótipo. As quatro variantes atuais
  (`PxPoint`, `PxPointF`, `PxSize` e `PxSizeF`) passam a sair do gerador, com os sufixos de
  precisão dele.
- **Em aberto**: a precisão padrão (`float`, que repassa para o `System.Numerics`, ou `double`, como
  o `Vec2` do C++); e, quando o passo de layout usar os primitivos, o inspector passa a depender da
  PixieLib, uma segunda DLL (exceção à regra de 1 DLL).

### 3.10 O `Inspector`: hoje raso

O `Inspector` tem 45 linhas: `Create<T>()`, `bind(object)`, o `GetValue`/`SetValue` da raiz e o
`Name`; o `Target` só serve para o nome. Deveria ser uma das peças mais completas, porque é o que o
host e a view usam. Levantamento para o desenho, sem nada decidido.

**Buracos no que já existe** (testado)

- O `bind` aceita objeto de outro tipo: `Inspector.Create<Foo>().bind(new Bar())` passa, e o erro só
  aparece depois, no `GetValue` de um nó, como `ArgumentException` do reflection.
- O `SetValue` da raiz aceita null e objeto de outro tipo sem reclamar; com null, o inspector fica
  sem objeto, um unbind silencioso. Ele precisa aceitar valor novo, porque a cópia de uma struct na
  raiz volta por ele (3.3), mas só do tipo do alvo.
- A descoberta roda de novo a cada `Create` (3.8). Os nós não podem ser compartilhados entre
  inspectors, porque cada um tem as próprias opções; o que dá para cachear é a lista de membros.

**O que as notas já põem no inspector, hoje espalhado**

- Opções por inspector (`InspectorOptions`), a camada entre o `GlobalOptions` e o nó (seção 0), com
  as de layout e hospedagem (1.1, item 12; altura e espaçamento por editor, 1.2).
- Ciclo do bind: unbind, multi-bind com valores mistos e `Refresh()` (3.3).
- Eventos: `BindStarted` e `BindFinished` (1.1, item 9), os de ciclo de vida com payload de falha e
  o `ValueApplied` (3.3). São o que a view observa (3.5).
- Nós manuais: botão, exibição e cabeçalho (`AddButton`, `AddDisplay`; seção 5, item 4).
- `TypeBinderMode`: automático ou só o que foi declarado (3.2).
- Filtro por instância (sucessor do `IVarProvider`) e visibilidade condicional, que dependem do
  objeto ligado (3.2).
- Aplicar sob demanda (Apply e Reload), previsto e nunca terminado no original (1.1); sem decisão.

**Em aberto**

- `Inspector<T>`, com `bind(T)` checado pelo compilador, ou a checagem do tipo no próprio `bind`, em
  tempo de execução?
- O que fica no inspector e o que fica nos nós (eventos por nó ou só na raiz, por exemplo).

---

## 4. O que não portar do 0.7.1a

- Mapa chaveado pelo nome curto (colisões somem sem aviso).
- Resolução do objeto aninhado por nome com `goto` (`Fieldset.BindToObject`). Pela leitura, um nome
  não encontrado prende o laço para sempre e um objeto intermediário null dá `NullReferenceException`.
- `FieldLocatorService.ToArray`: `newArr[--index]` com `index = -1` estoura no primeiro item.
- `Inspector.Dispose`: remove e descarta o `BackPanel` dentro do laço de campos, e assume
  `OwnerForm` não nulo.
- `BackpanelHelper.SuspendDrawing` manda `WM_PAINT` em vez de `WM_SETREDRAW` (a constante existe, mas
  não é usada).
- `ComboBox` de enum nunca recebe itens, e a leitura usa `SelectedValue` (nulo sem `DataSource`).
- `TypeSafeLock._FilterType` é ignorado; vale o `FilterType` do `BindingFilter`.
- Exceções engolidas com `Console.WriteLine`, e o `Logger` estático. (O `GlobalOptions` voltou, mas
  de propósito: é a camada global das opções, seção 7.)
- O receptor two-way só trata `int`, `float` e `double`, e assume `TextBox`.

---

## 5. Decisões em aberto

Já resolvidas (seção 0): um binário, prefixo dos primitivos, atributos próprios, paginação, modo
principal e precedência, service locator, camadas de opções, descrições, chaves, configuração no
inspector, exceções e binding pela cadeia de pais.

1. **Primitivos**: manter as variantes int e float (como o `System.Drawing`) ou um tipo só em double
   (como o WPF)? E criar os novos `PxRect`, `PxPadding` e `PxDock`? A proposta de 3.9 gera as três
   precisões de um modelo só, e deixa em aberto só a padrão. As cores ganham o prefixo (`PxColor`)
   ou continuam com o espaço de cor no nome (`ArgbColor`, `HslColor`)?
2. **`Fieldset`** (resolvida no commit `5560223`): o `Fieldset` e o namespace `Binding` saíram;
   qualquer membro é um `InspectorNode`, e o CS0118 deixou de existir.
3. **Cultura** para converter texto em número: invariante ou a atual?
4. **Nós manuais** (botão, campo só de exibição, cabeçalho): nomes como `inspector.AddButton(...)` e
   `inspector.AddDisplay(...)`, direto no inspector?
5. **Permissão de expandir** com a flag global: vale só para o membro ou tipo marcado (como hoje e
   como no `TypeSafeLock`) ou passa para os níveis de baixo?
6. **ReadOnly num objeto aninhado (class)**: hoje vale só para o próprio membro: não dá para trocar
   o objeto, mas os filhos continuam editáveis. Um `[InspectorReadOnly]` explícito num objeto
   aninhado deveria passar para os filhos?
7. **O `Inspector`** (3.10): o que ele concentra (opções, ciclo do bind, eventos, nós manuais) e se
   vira `Inspector<T>`. Vem antes das views, porque é o que elas observam.

---

## 6. Lista de coisas pra fazer (`[x]` = aplicado)

Estrutura

- [x] Multi-target `net10.0;net10.0-windows` num projeto só, com o código de plataforma em arquivos
      parciais excluídos do alvo `net10.0` (3.7; commit `c537554`).
- [x] Um alvo só (`net10.0`): sem código de Windows na biblioteca, saem o segundo alvo e os blocos
      condicionais (3.7; commit `5560223`).
- [x] NoHost em `net10.0` (commit `c537554`).
- [x] TuxHost: console de verificação em `net10.0`, com a mesma saída do NoHost (commit `e11b2df`).
      O NoHost voltou para `net10.0-windows` (commit `176f366`), e voltou a ser ignorado pelo
      `.gitignore`, como na `main`, sem sair do repositório (commit `9587e12` e o seguinte).
- [ ] Fábricas por plataforma com nomes distintos, para não obrigar o consumidor a referenciar as
      duas plataformas (3.5).
- [x] Primitivos: nomes provisórios `SKPoint`, `SKPointF`, `SKSize`, `SKSizeF`, `ArgbColor` e
      `HslColor` (commit `21cbeda`).
- [x] Primitivos: prefixo `Px` no lugar do `SK` provisório (`PxPoint`, `PxPointF`, `PxSize` e
      `PxSizeF`), o mesmo da PixieLib (3.9; commit `9e15f6e`).
- [ ] Primitivos: conversões nos dois sentidos com as regras de 3.4, incluindo o CS0457 entre
      `ArgbColor` e `HslColor`.
- [ ] Primitivos novos: `PxRect`, `PxPadding` e `PxDock` (depende da seção 5).
- [ ] PixieLib em C#: primitivos e matemática fora do inspector, em `dotnet/` no repositório
      PixieLib, com source generator para as precisões (3.9; adiado).

Núcleo (portar a essência)

- [x] Modelo de opções, primeiro corte: `GlobalOptions`, `FieldOptions` e a pilha
      reflection < atributos < manual (seção 7; commit `5319247`).
- [x] Atributos do inspector, com descrição curta (tooltip) e longa (`(?)`) (commit `5319247`).
- [x] Configuração por campo com chave por caminho (`fields["Moo.MooX"]`) (commit `5319247`).
- [x] Opções no próprio nó e configuração direto no inspector, por caminho ou encadeada; saem
      `FieldOptions`, `FieldOptionsCollection`, `OptionsResolver` e o callback do `Create` (commit
      `bf6f74f`).
- [x] Um membro, um nó: a descoberta monta a árvore direto; saem `FieldDescriptor`, `Fieldset`, o
      `Inspector` aninhado e a lista plana, e as políticas perdem a interface e as instâncias
      (commits `5560223` e `6622a41`).
- [ ] `Inspector` completo: hoje só `Create<T>`, `bind` e o valor da raiz; o `bind` e o `SetValue`
      da raiz aceitam objeto de outro tipo (3.10).
- [ ] `InspectorOptions` (por inspector), podendo sobrescrever o global.
- [ ] Nós manuais: botão, campo só de exibição e cabeçalho, direto no inspector.
- [ ] Configuração de editor que cubra o que hoje sai por `EditField()`: itens de escolha, seletores
      (cor, fonte) e ação de botão (faixa, passo e scrubbing já existem).
- [ ] Visibilidade condicional, por regra e por instância (sucessor do `VariablePool`).
- [x] Binding respeitar o `ReadOnly` das opções no `SetValue` (commit `6117bb1`).
- [ ] `ReadOnly` efetivo no nó, para a view desabilitar o editor: o filho de uma struct somente
      leitura tem `ReadOnly = false` nas opções, mas o `SetValue` recusa.
- [x] Rebind: `bind` de novo troca o objeto (commit `2a1cf94`).
- [ ] Unbind e multi-bind.
- [ ] Objeto → UI por `INotifyPropertyChanged`, com `Refresh()` manual.
- [ ] Conversão de texto para valor com `TypeConverter` / `IParsable<T>` e cultura definida.
- [ ] Sanitizadores tipados (sucessores das `CapFunction`), separados em texto e valor, em lista
      ordenada.
- [ ] Filtros: blacklist/whitelist, `TypeSafeLock` e um sucessor tipado do `IVarProvider`.
- [ ] Eventos de ciclo de vida com payload de falha tipado.
- [ ] Cache do modelo de tipo.

Apresentação

- [ ] Passo de layout agnóstico que gera os retângulos de cada linha.
- [ ] Views WinForms e WPF: editores por tipo, scrubbing, grupos recolhíveis, cabeçalho e scroll
      (sem paginação).
- [ ] Válvula de escape por plataforma para ajustar o controle criado.

Pendências da primeira revisão (já conhecidas)

- [x] Objeto intermediário null no `bind`: o bind não lê mais nada; `GetValue` devolve null e
      `SetValue` lança dizendo qual pai é null (commit `2a1cf94`).
- [x] Setter privado e campo `readonly` gravados pelo `SetValue`: agora ele respeita o `ReadOnly`
      que as opções marcam (commit `6117bb1`).
- [x] `FieldDescriptor.Type` com o tipo dono e o namespace `Binding` escondendo o tipo `Binding` do
      WinForms e do WPF: os dois saíram (commit `5560223`).
- [ ] Membro escondido com `new`: não quebra mais o `Create`, mas aparece duas vezes, e o indexador
      acha o do tipo derivado, que vem primeiro.
- [x] `/NoHost` no `.gitignore`: agora só `NoHost/bin` e `NoHost/obj` são ignorados (commit
      `7740b61`). Revertido no commit `9587e12`: o NoHost voltou a ser ignorado por inteiro.
- [x] Structs, inclusive aninhadas em classes e em outras structs: o valor alterado é gravado de
      volta no dono (commit `2a1cf94`).

---

## 7. Modelo de opções (primeiro corte, aplicado)

Aplicado nos commits `5319247` (biblioteca) e `16f52c0` (NoHost e objeto de teste). Revisto no
commit `bf6f74f`: as opções passaram para o próprio nó e a configuração é feita no inspector; saíram
`FieldOptions`, `FieldOptionsCollection`, `OptionsResolver` e o callback do `Create`.

**Três camadas de opções**

- `GlobalOptions` (estática): valem para o processo inteiro. Por enquanto só
  `RequireExpandableAttribute`.
- `InspectorOptions`: ainda não existe. Chega com o passo de layout (altura de campo, espaçamento,
  recuo...) e poderá sobrescrever o global por inspector.
- Por campo: propriedades do próprio nó (`InspectorNode`): `Label`, `Tooltip` (curta), `Help`
  (longa, para o `(?)`), `Order`, `Ignored`, `Visible`, `ReadOnly`, `Editor` (`EditorKind`), `Range`
  (`NumericRange`), `ScrubMultiplier`, `Expandable` e `Collapsed`; mais `Path` e `IsGroup`, que vêm
  da árvore.

**A pilha** (fixa e nessa ordem; cada camada só mexe no que decide, e a seguinte sobrescreve)

1. `ReflectionPolicy`: rótulo = nome do membro; editor pelo tipo (números → `Number`, `bool` →
   `Toggle`, enum → `Choice`, texto → `Text`, objetos → `Display`); `ReadOnly` quando não há setter
   público (setter privado, `init`, campo `readonly`); objeto aninhado expansível, a menos que a
   flag global exija o atributo.
2. `AttributePolicy`: os dez atributos `[Inspector*]` da seção 3.2. O `[InspectorExpandable]` vale
   no membro ou no tipo.
3. Manual: o que for definido no inspector depois do `Create`.

`Ignored`, `Order` e `Expandable` valem na enumeração do inspector, que é o que a view mostra, então
podem mudar a qualquer momento, inclusive depois do bind: um nó ignorado sai com a subárvore; um
objeto que não é expansível aparece como campo `Display`, sem os filhos; irmãos saem por `Order`,
mantendo a ordem de declaração nos empates. O nó ignorado continua na árvore, então a camada manual
pode trazê-lo de volta (`Ignored = false`), mesmo quando foi o `[InspectorIgnore]` que o escondeu.

**Configuração**: o `Create` já aplica as duas políticas, e o que se define depois, no próprio
inspector, tem a palavra final. O indexador recebe um caminho relativo ao nó em que é chamado e pode
ser encadeado. Não há `map`, `Modify`, provider nem callback.

```csharp
var inspector = Inspector.Create<Foo>();
inspector["x"].Label = "Renamed X";
inspector["x"].ScrubMultiplier = 1;
inspector["Moo.MooX"].Tooltip = "Moo X position";   // ou inspector["Moo"]["MooX"]
inspector["Moo2"].Ignored = true;
```

Como o inspector também pode ser percorrido, uma regra em massa funciona como uma política
improvisada:

```csharp
foreach (var node in inspector)
    if (node.Editor == EditorKind.Number)
        node.ScrubMultiplier = 1;
```

Ela só alcança o que a enumeração mostra: nós ignorados e os de dentro de objetos não expansíveis
ficam de fora.

**Exceções**: um caminho desconhecido lança `KeyNotFoundException` com o nome do nó em que a busca
começou e o caminho (`'Foo' has no field at path 'Moo.Nope'.`). Exceções das políticas e de
atributos inválidos (por exemplo `[InspectorRange(10, 1)]`) sobem sem tratamento.

**Verificado**

- 36 testes num probe fora do repositório: padrões da reflection, detecção de somente leitura,
  cada atributo, precedência manual > atributos > reflection, ignorar com subárvore, ordem, flag
  global, exceções e binding pela árvore resolvida.
- Os testes pegam erro de verdade: com a precedência invertida de propósito numa cópia da
  biblioteca, 9 deles falham.
- O `Program.cs` antigo do NoHost, rodado contra a biblioteca nova, imprime exatamente o mesmo de
  antes.
- A solução compila sem erros e sem warnings.
- Na revisão `bf6f74f`: o NoHost, reescrito no formato novo, imprime exatamente o mesmo; a solução
  continua compilando sem erros e sem warnings.

**Para confirmar**: com a flag global ligada, a permissão de expandir vale por membro ou por tipo e
não passa para os níveis de baixo. No TuxHost, `Boo.Details` expande pelo atributo, mas
`Details.Doo` não, porque o tipo `Doo` não tem o atributo. É o comportamento do `TypeSafeLock` do
original.

**Próximos cortes**: itens de escolha, sanitizadores, visibilidade condicional, gancho de conversão,
`ValueApplied`, nós manuais (botão, exibição, cabeçalho) e `InspectorOptions` com o layout.
