# Notas de modernização

Base de leitura: `Sakamoto0110/InteractiveEditor`, branch `InspectorVariant0.7.1a`
(commit `7833d65`, abril de 2021; .NET Framework 4.8 + WinForms; 3.445 linhas em 36 arquivos
na biblioteca), comparado com o estado atual deste repositório.

Nada aqui foi aplicado no código. São propostas para discutir.

Convenção: **[original]** é como era no 0.7.1a, **[rework]** é como está hoje aqui,
**[proposta]** é o que eu sugiro.

---

## 1. A essência da ferramenta

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

- paginação (`_NextPage` / `_PrevPage` e os botões ◀ ▶);
- botões Apply, Reload e Unbind (criados e escondidos) e o modo de aplicar sob demanda
  (`AutoUpdateEnabled` desliga a gravação, mas nada aplica depois);
- o `(?)` de ajuda (`EnableQuestionMark` só aparece nos flags padrão; o rótulo `(?)` é reaproveitado
  como seta de recolher);
- `MemberSafeLock` e `GlobalOptions.RequireAttrMemberSafeLock`;
- `TypeBinderMode` (atribuído, nunca lido);
- `InspectorOptions.VerticalSpacing`, `FieldHeight`, `HeaderHeight`, `FooterHeight` e `ShowText`
  (declarados, nunca lidos).

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

- Manter a forma `map.Modify(...)`, que é a cara da ferramenta.
- Chavear pelo caminho completo (`FullPath`), não pelo nome curto. No original o mapa é um
  `Dictionary` chaveado por `finfo.Name` com `if (!map.ContainsKey(...))`: dois membros com o mesmo
  nome em níveis diferentes fazem o segundo sumir sem aviso.
- Opcional: seletor por expressão, `map.For(f => f.Moo.MooX)`, que o compilador checa e que
  acompanha renomeações.
- Em vez de `FieldSet_FieldType = typeof(TextBox)`, um enum agnóstico de editor (por exemplo `Text`,
  `Number`, `Toggle`, `Choice`, `Slider`, `Color`, `Button`, `Separator`, `Header`). Cada view decide
  o controle.
- O que a configuração guarda: rótulo, editor, flags (`ReadOnly`, `Disabled`, scrubbing),
  multiplicadores (scrubbing e slider), sanitizadores, visível, recolhido e ação pós-bind.
- Opcional e aditivo: ler atributos padrão do .NET quando existirem, como `[DisplayName]`,
  `[Description]` (que alimentaria o `(?)`), `[Browsable(false)]`, `[ReadOnly]`, `[Range]`,
  `[MaxLength]` e `[Category]`. O configurador continua com a palavra final.
- `TypeSafeLock` continua como opt-in de tipos. É uma trava de segurança sem equivalente padrão.

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

**Nomes (proposta)**

| Hoje | Proposta | Motivo |
|---|---|---|
| `Point`, `PointF` | `LayoutPoint`, `LayoutPointF` | Diz para que serve e não colide com `System.Drawing` nem com `System.Windows` |
| `Size`, `SizeF` | `LayoutSize`, `LayoutSizeF` | Idem |
| (novo) | `LayoutRect` | Resultado do passo de layout |
| (novo) | `LayoutPadding` | Margens (`Padding` no WinForms, `Thickness` no WPF) |
| (novo) | `LayoutDock` (enum) | Substitui o `DockStyle` nas opções |
| `Color` | `ArgbColor` | Espaço de cor explícito; ponte para `System.Drawing.Color` e `System.Windows.Media.Color` |
| `ColorHSL` | `HslColor` | Idem, com o acrônimo em PascalCase |

Alternativa ao prefixo `Layout`: um prefixo curto da biblioteca, como faz o SkiaSharp
(`SKPoint`, `SKSize`, `SKColor`).

**Regras de conversão (proposta)**

- `System.Drawing` (int ↔ int, float ↔ float): implícitas nos dois sentidos, sem perda.
  `System.Drawing.Primitives` existe fora do Windows (testado no Linux), então essas conversões
  ficam no build comum.
- Para o WPF (double): implícita para pontos. Para `System.Windows.Size`, explícita (ou com clamp),
  porque o `Size` do WPF lança exceção com largura ou altura negativa (comportamento documentado) e
  o primitivo aceita negativos.
- Do WPF para os tipos int ou float: explícitas, porque perdem precisão.
- `ArgbColor` ↔ `HslColor`: declarar num lugar só. Hoje a conversão `Color → ColorHSL` existe nas
  duas structs e dá CS0457 no primeiro uso.
- `Padding`, `Thickness` e `DockStyle`: só no build Windows (ver 3.7).

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

### 3.6 Serviços

O `IOBServiceProvider.Request<T>()` cria um provider e um serviço novos a cada acesso
(`Owner.LocateField` → `new FieldLocatorService`), e os serviços não têm estado. No rework:

- localizar e invocar já estão cobertos por `IEnumerable`, LINQ e o indexador;
- manipular e vincular viram métodos do inspector e dos nós;
- para manter a API familiar (`Locate`, `Modify`, `Binder`), dá para expor propriedades que devolvem
  sempre o mesmo objeto leve.

### 3.7 Tirar a dependência de Windows sem dividir o projeto

Protótipo feito numa cópia fora do repositório:

- `InteractiveEditor.csproj` com `<TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>`;
  `UseWPF` e `UseWindowsForms` só no alvo `-windows`.
- `Presentation/WF/**` e `Presentation/WPF/**` fora do alvo `net10.0`; as conversões
  `System.Windows.*` dos primitivos e os `Create<T>(host)` atrás de `#if WINDOWS` (o SDK define esse
  símbolo no alvo `-windows`).
- Resultado: compila para os dois alvos. O build `net10.0` não referencia WPF nem WinForms. O NoHost
  apontado para `net10.0` rodou no Linux só com `Microsoft.NETCore.App`, com saída idêntica à atual.

Ressalva: é um projeto, um nome de assembly e um pacote, mas **dois binários** (um por alvo). Apps
WinForms e WPF recebem o `-windows` automaticamente; console, testes e CI recebem o `net10.0`. Se
"uma DLL" precisar ser um binário único, isso não atende, e o caminho é continuar só em
`net10.0-windows`.

Detalhes:

- no Linux, quem referencia o projeto precisa de `EnableWindowsTargeting=true` para o restore (no
  Windows não precisa);
- em vez de `#if`, arquivos parciais (`ArgbColor.Windows.cs` etc.) excluídos do alvo `net10.0`
  deixam os primitivos mais legíveis.

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
- Estado global estático (`GlobalOptions`, `Logger`) e exceções engolidas com `Console.WriteLine`.
- O receptor two-way só trata `int`, `float` e `double`, e assume `TextBox`.

---

## 5. Ressalvas e decisões em aberto

1. **Dois binários por alvo** (3.7) atendem à sua definição de "uma DLL"?
2. **Primitivos**: prefixo `Layout` ou um prefixo curto da biblioteca? Manter as variantes int e
   float (como o `System.Drawing`) ou um tipo só em double (como o WPF)?
3. **`Fieldset`**: na primeira revisão sugeri trocar para `FieldNode`. Depois de ler o original,
   recomendo manter: lá o nome faz sentido (é o conjunto rótulo + controle + `(?)` de uma linha).
   Pelo mesmo motivo, o namespace `Binding` poderia voltar a ser `Fields`, como no original, o que
   também resolve o CS0118.
4. **Atributos padrão** (`DisplayName`, `Range`...): adotar como fonte extra de configuração, ou
   manter só o configurador?
5. **Chaves**: string por caminho, seletor por expressão, ou os dois?
6. **Paginação** (prevista no original) ou só scroll?
7. **Modo manual** (campos que não são membros: botões, separadores, cabeçalhos): manter no mesmo
   inspector que o automático?
8. **Cultura** para converter texto em número: invariante ou a atual?

---

## 6. Lista de coisas pra fazer (proposta; nada aplicado)

Estrutura

- [ ] Multi-target `net10.0;net10.0-windows` num projeto só, com o código de plataforma em arquivos
      parciais excluídos do alvo `net10.0` (3.7).
- [ ] Fábricas por plataforma com nomes distintos, para não obrigar o consumidor a referenciar as
      duas plataformas (3.5).
- [ ] Primitivos: nomes (`Layout*`, `ArgbColor`, `HslColor`), conversões nos dois sentidos com as
      regras de 3.4, e os novos `LayoutRect`, `LayoutPadding` e `LayoutDock`.

Núcleo (portar a essência)

- [ ] Configuração por campo com chave por `FullPath` e `map.Modify(...)`.
- [ ] Rebind, unbind e multi-bind.
- [ ] Objeto → UI por `INotifyPropertyChanged`, com `Refresh()` manual.
- [ ] Conversão de texto para valor com `TypeConverter` / `IParsable<T>` e cultura definida.
- [ ] Sanitizadores tipados (sucessores das `CapFunction`), separados em texto e valor, em lista
      ordenada.
- [ ] Filtros: blacklist/whitelist, `TypeSafeLock` e um sucessor tipado do `IVarProvider`.
- [ ] Eventos de ciclo de vida com payload de falha tipado.
- [ ] Montagem manual (botão, separador, cabeçalho) além da automática.
- [ ] Cache do modelo de tipo.

Apresentação

- [ ] Passo de layout agnóstico que gera os retângulos de cada linha.
- [ ] Views WinForms e WPF: editores por tipo, scrubbing, grupos recolhíveis, cabeçalho e scroll.

Pendências da primeira revisão (já conhecidas)

- [ ] Objeto intermediário null no `bind`; setter privado e campo `readonly` editáveis;
      `FieldDescriptor.Type` com o tipo dono; membro escondido com `new`; namespace `Binding`
      escondendo o tipo `Binding` do WinForms e do WPF; `/NoHost` no `.gitignore`.
- [ ] Structs: ficam para depois, como combinado.
