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
- **Um projeto, dois binários** (`net10.0` e `net10.0-windows`), com o NoHost em `net10.0`.
  Aplicado no commit `483f2dd` (3.7).
- **Primitivos com prefixo `SK` provisório**, até a nomenclatura final: `SKPoint`, `SKPointF`,
  `SKSize` e `SKSizeF`; cores como `ArgbColor` e `HslColor`. Aplicado no commit `8bec89b` (3.4).
- **Opções em três camadas**: global/estática (`GlobalOptions`, com a flag que exige
  `[InspectorExpandable]` para expandir objetos aninhados), por inspector e por campo. Primeiro corte
  aplicado no commit `8756694` (seção 7).
- **Descrições em dois atributos**: `[InspectorTooltip]` (curta) e `[InspectorHelp]` (longa).
- **Chaves em string**: caminho relativo à raiz (`"Moo.MooX"`); seletor por expressão fica para
  depois.
- **Configurador sem intermediário**: edita as próprias opções por caminho
  (`fields["x"].Label = ...`). Nada de `map`, `Modify` ou provider.
- **Exceções explodem**: caminho desconhecido lança, e exceções do configurador ou das políticas sobem
  sem ser engolidas.

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
| `Mapping.ApplyFilter` + `AddNestedMembers` | Descobrir membros, recursivo | `ReflectionDiscovery.ResolveFor` | Coberto e ampliado: campos **e** propriedades, guarda de ciclo por caminho |
| `BindingArgs`, `MapHandler.Modify`, `BindingConfigurator` | Configuração por campo | não existe | **Principal peça a portar** |
| `Mapping.ResolveTypes`, `DefaultControlMapping` | Escolher o controle pelo tipo | não existe | Vira a escolha de um *tipo de editor* agnóstico |
| `BindingService.DoBind` | Montar grupos, filhos e recuo | `Inspector.Create<T>()` monta a árvore | Coberto pela árvore (recuo = profundidade) |
| `Fieldset.BindToObject` (busca por nome + `goto`) | Achar a instância aninhada | `Inspector.bind` (o pai resolve o filho) | Coberto, e mais correto |
| `Fieldset` (Panel + Label + controle + binding + conversão + scrubbing) | Uma linha do inspector | `Fieldset` (só binding) + `FieldsetView` (vazio) | Dividir em nó, configuração e view |
| `Inspector` (Panel, layout manual, scroll, botões) | Raiz e view ao mesmo tempo | `Inspector` (árvore) + `InspectorView` (stub) | Idem |
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

1. **Descoberta** (agnóstica): tipo → descritores. Feita uma vez e cacheada por `Type`.
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
3. **Manual**: o que o configurador fizer nas opções (`fields["x"].Label = ...`).

Os filtros (blacklist/whitelist, opt-in de tipos, filtro por instância) entram como políticas na
mesma pilha. Uma política nova é só mais uma camada.

**Atributos do inspector** (próprios, para não haver ambiguidade com `System.ComponentModel` ou
DataAnnotations; aplicados no commit `8756694`)

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

- O configurador edita as próprias opções, por caminho (`fields["Moo.MooX"]`), sem `map`, `Modify`
  ou provider (decidido; seção 7).
- Chave pelo caminho completo, relativo à raiz, e não pelo nome curto. No original o mapa é um
  `Dictionary` chaveado por `finfo.Name` com `if (!map.ContainsKey(...))`: dois membros com o mesmo
  nome em níveis diferentes fazem o segundo sumir sem aviso.
- Depois: seletor por expressão (por exemplo `fields[f => f.Moo.MooX]`), que o compilador checa e que
  acompanha renomeações.
- Em vez de `FieldSet_FieldType = typeof(TextBox)`, um enum agnóstico de editor (`EditorKind`: `Text`,
  `Number`, `Toggle`, `Choice`, `Slider`, `Color`, `Button`, `Display`, `Header`, `Separator`). Cada
  view decide o controle.
- O que a configuração guarda: rótulo, editor, flags (`ReadOnly`, `Disabled`, scrubbing),
  multiplicadores (scrubbing e slider), sanitizadores, visível, recolhido e ação pós-bind.
- A ideia do `TypeSafeLock` (opt-in de quais tipos podem ser expandidos) continua, como atributo do
  inspector (`[InspectorExpandable]` na tabela acima).
- O `TypeBinderMode` do original (`Automatic` / `Manual`, declarado e nunca usado lá) pode continuar
  existindo para quando se quer só os campos declarados; o padrão passa a ser `Automatic`.

Esboço do editor de componentes do OverlayApplication no configurador atual. As linhas marcadas
são de cortes seguintes, com nomes provisórios:

```csharp
var component = Inspector.Create<ComponentPreset>(fields =>
{
    fields["X"].Label = "PosX";
    fields["X"].ScrubMultiplier = 1;
    fields["ScaleX"].ScrubMultiplier = 0.01;
    fields["Opacity"].Editor = EditorKind.Slider;
    fields["Opacity"].Range = new NumericRange(0, 255, 5);
    fields["FillColor"].Label = "Color1";

    // cortes seguintes:
    // editor de cor automático (hoje System.Drawing.Color ainda vira um grupo com A, R, G, B...)
    // fields["Text"].VisibleWhen = c => ((ComponentPreset)c).IsText;   // sucessor do VariablePool
    // fields.AddButton("LayerUp", "▲", () => tree.OnLayerUp());
    // fields.AddDisplay("Layer", () => tree.SelectedIndex);
});

component.Bind(selected);    // cortes seguintes: rebind a cada seleção
component.Bind(selection);   // cortes seguintes: multi-bind
```

No modo automático (o padrão), os membros refletidos entram sozinhos, os atributos ajustam e o
configurador tem a palavra final; com `TypeBinderMode.Manual`, entra só o que foi declarado.

### 3.3 Binding

- **Rebind**: `Bind(obj)` troca o objeto atual. O original permite (o `BindToObject` desfaz o bind
  anterior); o rework hoje bloqueia com `IsTypeBound`.
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

**Nomes** (prefixo `SK` provisório, decidido)

| Antes | Agora | Situação |
|---|---|---|
| `Point`, `PointF` | `SKPoint`, `SKPointF` | Aplicado (`8bec89b`) |
| `Size`, `SizeF` | `SKSize`, `SKSizeF` | Aplicado |
| `Color` | `ArgbColor` | Aplicado; espaço de cor explícito, ponte para `System.Drawing.Color` e `System.Windows.Media.Color` |
| `ColorHSL` | `HslColor` | Aplicado; idem, com o acrônimo em PascalCase |
| (novo) | `SKRect` | Proposto: resultado do passo de layout |
| (novo) | `SKPadding` | Proposto: margens (`Padding` no WinForms, `Thickness` no WPF) |
| (novo) | `SKDock` (enum) | Proposto: substitui o `DockStyle` nas opções |

Com os nomes novos, um arquivo WinForms ou WPF que importa `InteractiveEditor.Primitives` deixou de
ter ambiguidade (CS0104) com `System.Drawing`, `System.Windows` e `System.Windows.Media` (testado).
Ressalva do `SK`: o SkiaSharp também tem `SKPoint`, `SKSize` e `SKColor`, então um arquivo que
importe os dois namespaces teria ambiguidade. Enquanto o prefixo for provisório, não é problema.

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
- `SKPadding` e `SKDock` ↔ `Padding`, `Thickness` e `DockStyle`: só no build Windows, em arquivos
  `*.Windows.cs` (ver 3.7).

### 3.5 Apresentação

- A view não herda de `Inspector`: recebe um e o observa. Assim a raiz e os nós aninhados são
  tratados do mesmo jeito.
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

### 3.7 Um projeto, dois binários (aplicado)

Aplicado no commit `483f2dd`:

- `InteractiveEditor.csproj` com `<TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>`;
  `UseWPF` e `UseWindowsForms` só no alvo Windows (condição por `GetTargetPlatformIdentifier`).
- O código de Windows fica fora do alvo `net10.0`: os arquivos parciais `*.Windows.cs`
  (`Inspector.Windows.cs` com as fábricas `Create<T>(host)`, e `Primitives/*.Windows.cs` com as
  conversões `System.Windows.*`), mais `Presentation/WF/**` e `Presentation/WPF/**`. As conversões de
  `System.Drawing` ficam no build comum.
- `EnableWindowsTargeting` no projeto, para o alvo Windows compilar fora do Windows (CI, Linux).
- NoHost em `net10.0`: roda sem o runtime WindowsDesktop.

Verificado: os membros movidos são idênticos (só ganharam `partial`); a solução compila com os mesmos
warnings (agora um jogo por alvo); o NoHost roda no Linux com a mesma saída de antes; e os hosts
WinForms e WPF recebem o binário Windows.

Convenção daqui em diante: tudo que depende de WinForms ou WPF vai em `*.Windows.cs` ou nas pastas
de apresentação; o resto precisa compilar em `net10.0`.

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

Já resolvidas (seção 0): dois binários, prefixo dos primitivos, atributos próprios, paginação,
modo principal e precedência, service locator, camadas de opções, descrições, chaves, configurador
e exceções.

1. **Primitivos**: manter as variantes int e float (como o `System.Drawing`) ou um tipo só em double
   (como o WPF)? E criar os novos `SKRect`, `SKPadding` e `SKDock`?
2. **`Fieldset`**: na primeira revisão sugeri trocar para `FieldNode`. Depois de ler o original,
   recomendo manter: lá o nome faz sentido (é o conjunto rótulo + controle + `(?)` de uma linha).
   Pelo mesmo motivo, o namespace `Binding` poderia voltar a ser `Fields`, como no original, o que
   também resolve o CS0118.
3. **Cultura** para converter texto em número: invariante ou a atual?
4. **Nós manuais** (botão, campo só de exibição, cabeçalho): nomes como `fields.AddButton(...)` e
   `fields.AddDisplay(...)`, no mesmo configurador?
5. **Permissão de expandir** com a flag global: vale só para o membro ou tipo marcado (como hoje e
   como no `TypeSafeLock`) ou passa para os níveis de baixo?

---

## 6. Lista de coisas pra fazer (`[x]` = aplicado)

Estrutura

- [x] Multi-target `net10.0;net10.0-windows` num projeto só, com o código de plataforma em arquivos
      parciais excluídos do alvo `net10.0` (3.7; commit `483f2dd`).
- [x] NoHost em `net10.0` (commit `483f2dd`).
- [ ] Fábricas por plataforma com nomes distintos, para não obrigar o consumidor a referenciar as
      duas plataformas (3.5).
- [x] Primitivos: nomes provisórios `SKPoint`, `SKPointF`, `SKSize`, `SKSizeF`, `ArgbColor` e
      `HslColor` (commit `8bec89b`).
- [ ] Primitivos: conversões nos dois sentidos com as regras de 3.4, incluindo o CS0457 entre
      `ArgbColor` e `HslColor`.
- [ ] Primitivos novos: `SKRect`, `SKPadding` e `SKDock` (depende da seção 5).

Núcleo (portar a essência)

- [x] Modelo de opções, primeiro corte: `GlobalOptions`, `FieldOptions` e a pilha
      reflection < atributos < manual (seção 7; commit `8756694`).
- [x] Atributos do inspector, com descrição curta (tooltip) e longa (`(?)`) (commit `8756694`).
- [x] Configuração por campo com chave por caminho (`fields["Moo.MooX"]`) (commit `8756694`).
- [ ] `InspectorOptions` (por inspector), podendo sobrescrever o global.
- [ ] Nós manuais: botão, campo só de exibição e cabeçalho, no configurador.
- [ ] Configuração de editor que cubra o que hoje sai por `EditField()`: itens de escolha, seletores
      (cor, fonte) e ação de botão (faixa, passo e scrubbing já existem).
- [ ] Visibilidade condicional, por regra e por instância (sucessor do `VariablePool`).
- [ ] Binding respeitar o `ReadOnly` das opções no `SetValue`.
- [ ] Rebind, unbind e multi-bind.
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

- [ ] Objeto intermediário null no `bind`; setter privado e campo `readonly` ainda gravados pelo
      `SetValue` (as opções já marcam `ReadOnly`); `FieldDescriptor.Type` com o tipo dono (o tipo do
      valor agora está em `FieldType`); membro escondido com `new`; namespace `Binding` escondendo o
      tipo `Binding` do WinForms e do WPF.
- [x] `/NoHost` no `.gitignore`: agora só `NoHost/bin` e `NoHost/obj` são ignorados (commit
      `efd0809`).
- [ ] Structs: ficam para depois, como combinado.

---

## 7. Modelo de opções (primeiro corte, aplicado)

Aplicado nos commits `8756694` (biblioteca) e `3f24952` (NoHost e objeto de teste).

**Três camadas de opções**

- `GlobalOptions` (estática): valem para o processo inteiro. Por enquanto só
  `RequireExpandableAttribute`.
- `InspectorOptions`: ainda não existe. Chega com o passo de layout (altura de campo, espaçamento,
  recuo...) e poderá sobrescrever o global por inspector.
- `FieldOptions`: a configuração resolvida de cada nó, em `InspectorNode.Options`: `Path`, `Member`,
  `HasMembers`, `IsGroup`, `Label`, `Tooltip` (curta), `Help` (longa, para o `(?)`), `Order`,
  `Ignored`, `Visible`, `ReadOnly`, `Editor` (`EditorKind`), `Range` (`NumericRange`),
  `ScrubMultiplier`, `Expandable` e `Collapsed`.

**A pilha** (fixa e nessa ordem; cada camada só mexe no que decide, e a seguinte sobrescreve)

1. `ReflectionPolicy`: rótulo = nome do membro; editor pelo tipo (números → `Number`, `bool` →
   `Toggle`, enum → `Choice`, texto → `Text`, objetos → `Display`); `ReadOnly` quando não há setter
   público (setter privado, `init`, campo `readonly`); objeto aninhado expansível, a menos que a flag
   global exija o atributo.
2. `AttributePolicy`: os dez atributos `[Inspector*]` da seção 3.2. O `[InspectorExpandable]` vale no
   membro ou no tipo.
3. Configurador (manual).

Depois das camadas: um nó ignorado some com a subárvore; um objeto que não é expansível vira um campo
`Display` e perde os filhos; irmãos são ordenados por `Order`, mantendo a ordem de declaração nos
empates.

**Configurador**: recebe as próprias opções, já resolvidas pelas camadas de baixo, e edita direto,
por caminho. Não há `map`, `Modify` nem provider.

```csharp
var inspector = Inspector.Create<Foo>(fields =>
{
    fields["x"].Label = "Renamed X";
    fields["x"].ScrubMultiplier = 1;
    fields["Moo.MooX"].Tooltip = "Moo X position";
    fields["Moo2"].Ignored = true;
});
```

Como a coleção também pode ser percorrida, uma regra em massa funciona como uma política
improvisada: `foreach (var f in fields) if (f.Editor == EditorKind.Number) f.ScrubMultiplier = 1;`.

**Exceções**: um caminho desconhecido lança `KeyNotFoundException` com o tipo e o caminho. Exceções do
configurador, das políticas e de atributos inválidos (por exemplo `[InspectorRange(10, 1)]`) sobem
sem tratamento.

**Verificado**

- 36 testes num probe fora do repositório: padrões da reflection, detecção de somente leitura,
  cada atributo, precedência manual > atributos > reflection, ignorar com subárvore, ordem, flag
  global, exceções e binding pela árvore resolvida.
- Os testes pegam erro de verdade: com a precedência invertida de propósito numa cópia da
  biblioteca, 9 deles falham.
- O `Program.cs` antigo do NoHost, rodado contra a biblioteca nova, imprime exatamente o mesmo de
  antes.
- A solução compila sem erros e sem warnings.

**Para confirmar**: com a flag global ligada, a permissão de expandir vale por membro ou por tipo e
não passa para os níveis de baixo. No NoHost, `Boo.Details` expande pelo atributo, mas `Details.Doo`
não, porque o tipo `Doo` não tem o atributo. É o comportamento do `TypeSafeLock` do original.

**Próximos cortes**: itens de escolha, sanitizadores, visibilidade condicional, gancho de conversão,
`ValueApplied`, nós manuais (botão, exibição, cabeçalho), `InspectorOptions` com o layout, e o
binding respeitar o `ReadOnly` (a opção já marca, mas o `SetValue` ainda grava).
