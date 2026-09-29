# Notas de modernização

Base de leitura: `Sakamoto0110/InteractiveEditor`, branch `InspectorVariant0.7.1a`
(commit `7833d65`, abril de 2021; .NET Framework 4.8 + WinForms; 3.445 linhas em 36 arquivos
na biblioteca), comparado com o estado atual deste repositório. Como referência de uso real:
`Sakamoto0110/OverlayApplication` (commit `83f4d8d`, fevereiro de 2021), o app para o qual o
inspector foi feito.

O que já foi decidido está na seção 0, e o que já foi aplicado no código está marcado com `[x]` na
seção 6. O resto são propostas para discutir. As perguntas em aberto estão juntas, numeradas, em
`perguntas-em-aberto.md`, e o que é preciso para retomar o trabalho num contexto novo está em
`passagem-de-contexto.md`.

Convenção: **[original]** é como era no 0.7.1a, **[rework]** é como está hoje aqui,
**[proposta]** é o que eu sugiro.

---

## 0. Decisões tomadas

As respostas de 27/09 e de 29/09 às perguntas em aberto entraram aqui, com o número da pergunta:
`P2.2` é a pergunta 2.2 de `perguntas-em-aberto.md`, e um número sem o `P` é uma seção destas
notas. O que ainda depende de resposta continua naquele arquivo.

### Premissa

- **O inspector não cai por erro interno** (27/09). O projeto tem pontos fracos conhecidos (a
  descoberta por reflection, o binding, o acesso ao objeto ligado e as próprias views), e uma
  exceção num deles não pode derrubar o inspector. Quando há um fallback automático
  (determinístico, sem ambiguidade), ele é aplicado; quando não há, entra um semiautomático,
  suficiente para não cair, mas sem garantia de ser o certo, e um evento avisa quem quiser corrigir
  o estado no meio do caminho. Só o que for grave demais sobe para quem usa a biblioteca. Os
  eventos podem ser muitos, alguns só para consumo interno, porque a verbosidade ajuda no
  diagnóstico. Substitui o "exceções explodem" anterior, que deixava subir as exceções das
  políticas. Detalhes na 3.11.
- **Severidade** (P0.1, 29/09): recuperado (fallback automático, sem ambiguidade), contornado
  (fallback semiautomático: não caiu, mas pode não ser o certo), crítico (sem resolução: o nó que
  falhou sai, e o resto continua) e fatal (o inspector não tem como continuar). Só o fatal sobe
  para quem chamou. Aplicado no `Create` no commit `1535874` (`FailureSeverity`, 3.11).
- **Lançar ou avisar** (P0.2): o uso errado da API por quem chama lança na hora, sem mudar o estado
  do inspector: ligar duas vezes, ligar outro tipo, gravar num grupo ou num ramo comprometido, um
  caminho desconhecido, mudar uma opção global com um inspector vivo. Um ponto fraco avisa por
  evento e segue com um fallback. O atributo inválido é ponto fraco: o `Create` segue sem ele
  (commit `1535874`).

### Estrutura

- **Sem compromisso de compatibilidade.** O rework não vai ser portado para nenhum app real, e o
  OverlayApplication vai ser reescrito do zero; o uso real (1.2) serve só de referência.
- **Um projeto; dois alvos quando as views chegarem** (P7.1). Hoje a biblioteca tem só `net10.0`,
  porque não tem código de Windows (commit `5560223`, 3.7). Com as views, volta o
  `net10.0;net10.0-windows`, com o código delas só no alvo `-windows`, pelo bloco condicional: o
  núcleo continua rodando fora do Windows, e cada consumidor recebe uma DLL só, a do alvo dele.
  Substitui o "as views entram nesta mesma DLL, com um alvo só" da decisão anterior.
- **TuxHost para as verificações, NoHost local**: o TuxHost (`net10.0`) roda em qualquer sistema e é
  o console de verificação versionado; o NoHost voltou a ser só para os seus testes, em
  `net10.0-windows`, versionado como na `main` e com a pasta no `.gitignore`. Aplicado nos commits
  `e11b2df`, `176f366` e `9587e12`.
- **Service locator descartado.** Localizar e aplicar já estão cobertos pela enumeração e pelo
  indexador (3.6).
- **Paginação substituída por scroll.** Um app que quiser páginas implementa por cima.

### O Inspector

- **Não genérico, com o tipo da raiz fixo.** Só o `Create<T>` é genérico. Ligar um objeto de outro
  tipo lança, sem mexer na árvore nem no objeto que já está ligado (P2.2). A classe continua não
  genérica porque nem todo inspector vai ser tipado (P1.7). Revê a decisão anterior, que dava como
  motivo trocar a raiz por um objeto de outro tipo (3.10).
- **Composição** (P1.1): o `Inspector` deixa de herdar de `InspectorNode`, guarda a raiz como um nó
  interno e expõe só o que é dele (indexador, enumeração, bind, eventos e opções). O `SetValue` da
  raiz nem existe (P3.5). Liberado na P9.3 (29/09) e aplicado no commit `0bc2f9d`.
- **Id único** (P1.9): cada inspector tem um id, para relacionar um evento ao inspector que o
  disparou. É também o que a trava das opções globais guarda (P1.11). Aplicado no commit
  `0bc2f9d`: um `int` sequencial, dado no construtor.
- **Setter abstrato de volta** (P1.2), como no main: o getter é comum, e cada tipo de nó decide o
  setter, com um tipo de nó por comportamento fixo na criação (membro e, depois, botão). Grupo ou
  folha continua decidido em tempo de execução dentro do nó de membro, porque o `Expandable` pode
  mudar depois do `Create`. A ver como fica no código. Aplicado no commit `0bc2f9d`: `InspectorNode`
  é abstrato, o `GetValue` é o mesmo para todos (resolve o nó contra o objeto ligado), o `SetValue`
  é abstrato, e os tipos são `MemberNode` (campo ou propriedade) e `RootNode` (interno).
- **Enumeração** (P1.3): o inspector continua enumerável e entrega todos os nós, inclusive os
  ignorados e os de dentro de grupos fechados; as linhas da view saem de um percurso à parte,
  `Rows`. Aplicado no commit `b456398`, no nó, e com ele no inspector.
- **Eventos** (P1.4), sem economia: no inspector, a criação, a descoberta e o ciclo do bind; nos
  nós, `ValueChanged` (no lugar do `ValueApplied`), o objeto do grupo trocado por fora e a falha de
  bind. A lista está na 3.10, e os nomes são exemplos. Os da criação são estáticos, para dar para
  assinar antes do `Create`, e o resultado da criação fica guardado no inspector (P1.9; aplicado no
  commit `1535874`: `DiscoveryFinished`, `DiscoveryFailed`, `Created` e `inspector.Report`). O
  `ValueChanged` vale nos dois sentidos e diz de onde veio a mudança, e os nomes seguem a convenção
  do .NET: o evento é `Created`, e `OnCreated` é o método que o dispara (P1.10).
- **Descartável** (P1.5): o `Inspector` e os nós implementam `IDisposable`, já pensando em campos de
  imagem e de recursos. O `Dispose` também solta a trava das opções globais (abaixo). Aplicado no
  commit `ffee3a7`: descartar um nó descarta o ramo dele, e descartar o inspector desliga o objeto,
  descarta a árvore e solta a trava; depois disso, `Bind` e `Rebind` lançam
  `ObjectDisposedException`.
- **`TypeBinderMode` continua** (P1.7), porque nem todo inspector vai ser tipado. O modo sai do
  jeito de criar: tipado é automático, e sem tipo é manual; o modo explícito continua para o
  inspector tipado e manual (P1.12).
- **Nós manuais** (P1.6): o botão entra, com uma ação no clique; o cabeçalho não, porque dá para
  resolver de outro jeito. O campo só de exibição também entra, com um getter (29/09).
- **Controle do binder** (P1.8): no lugar de uma flag `AutoApply`, um enum de controle (manual, ou
  automático num sentido ou nos dois) e métodos auxiliares de força: gravar os valores no objeto,
  recarregar do objeto e limpar a view (tudo vazio ou zero), que também podem ser disparados por um
  evento quando algo sai do normal. Não se confundem com o `Refresh()` (P2.6), que é o fluxo
  normal: os de força são override ou fallback, mesmo quando fazem a mesma coisa. Formato (P1.13):
  um enum de flags sem combinação inválida (`Manual = 0`, `ViewToInstance`, `InstanceToView` e
  `Automatic`, os dois sentidos), mais um fluxo normal para ler e gravar à mão; os de força ficam
  só para quando o fluxo normal falhou ou não se encaixa, cada um com o seu evento. Aplicado no
  commit `18dc069` (3.3): o fluxo normal é o `Apply()` e o `Reload()`, e os de força são o
  `ForceApply()`, o `ForceReload()` e o `ForceClear()`, com os eventos `ForcedApply`,
  `ForcedReload` e `ForcedClear`.

### Opções e configuração

- **Modo principal: automático**, montado como uma pilha de políticas (3.2). Precedência:
  **manual > metadados por atributo > descoberta por reflection**.
- **Atributos próprios do inspector**, em vez dos atributos padrão do .NET, para evitar ambiguidade.
  Duas descrições: uma curta, usada como tooltip, e uma longa, para o `(?)` quando ele estiver
  disponível.
- **Descrições em dois atributos**: `[InspectorTooltip]` (curta) e `[InspectorHelp]` (longa).
- **Opções em três camadas**: global/estática (`GlobalOptions`, com a flag que exige
  `[InspectorExpandable]` para expandir objetos aninhados), por inspector e por campo. Primeiro
  corte aplicado no commit `5319247` (seção 7). As opções por inspector sobrescrevem as globais
  (P1.5). O `InspectorOptions` existe desde o commit `e136f82`, com a cultura, e tem o modo de
  controle do binder desde o commit `18dc069`.
- **Opções globais travadas** (P1.5, P4.4): o `Create` congela o `GlobalOptions`. Enquanto houver um
  inspector vivo, mudar uma opção global lança exceção; a trava cai no `Dispose` do último. Ela
  guarda os ids dos inspectors vivos (P1.11), e a mensagem da exceção diz quais são. Aplicado no
  commit `ffee3a7`; um `Create` que falha solta a trava antes de lançar.
- **Chaves em string**: caminho relativo ao nó em que o indexador é chamado (`"Moo.MooX"`), que
  também pode ser encadeado (`inspector["Moo"]["MooX"]`); sem atalho pelo nome do tipo. O seletor
  por expressão também vai existir, ao lado do caminho em string (P6.5). Aplicado no commit
  `e06b6f7`: `inspector.Node<Foo>(f => f.Moo.MooX)`, e o mesmo em qualquer nó.
- **Sem configurador**: as opções são propriedades do próprio nó, e a configuração é feita no
  inspector depois do `Create` (`inspector["x"].Label = ...`). Nada de `map`, `Modify`, provider ou
  callback. Aplicado no commit `bf6f74f` (seção 7).
- **`Ignored` é da árvore, `Visible` é da view** (P6.1): um nó ignorado não faz parte deste
  inspector; um nó invisível faz parte, mas está escondido agora, e os filhos vão junto. Aplicado
  no commit `13534b0`: o `Visible` é lido pelos pais, e as `Rows` pulam o ramo invisível.
- **Filtro por nome** (P6.4): pode ser injetado, e é resolvido com a mesma precedência dos
  atributos. Vale por tipo, no `Create` (P6.6); onde ele é injetado está em aberto (P6.7).
- **Expandir com a flag global** (P4.3): a permissão vale só para o membro ou tipo marcado, e os
  níveis de baixo continuam fechados, porque a flag existe justamente para não propagar. Um
  atributo que propague fica como ideia (seção 7).

### Binding

- **Binding pela cadeia de pais**: só a raiz guarda a instância, e cada nó lê e grava pelo pai a
  cada chamada. Structs são gravadas de volta no dono, e `SetValue` com um pai null lança exceção.
  Aplicado no commit `2a1cf94` (3.3).
- **Cinco operações** (P2.1): `Bind`, `Unbind()`, `Rebind`, `AddBind` e `RemoveBind`. Ligar lança
  exceção se já houver objeto ligado; `Unbind()`, sem parâmetro, solta tudo; religar é desligar e
  ligar; e o multi-bind põe e tira objetos. O ligar que lança é o `IsTypeBound` do main, que o
  commit `2a1cf94` também tirou (3.3). `Bind`, `Unbind()` e `Rebind` aplicados no commit `4dec125`;
  `AddBind` e `RemoveBind` aplicados no commit `c201877`, com o multi-bind; o `Rebind` passou a
  aceitar um objeto ou vários.
- **Um tipo só** (P2.2, P2.3): ligar, religar ou pôr no multi-bind um objeto de outro tipo lança.
  Os tipos derivados ficam para uma conversa própria, se aparecer motivo. No `Bind` e no `Rebind`
  (commit `4dec125`), um objeto de um tipo derivado passa, porque a árvore do tipo base serve para
  ele; é o caso do editor de `ComponentPreset` do OverlayApplication, ligado a retângulos e elipses.
  O `AddBind` segue a mesma regra (commit `c201877`), e o mesmo objeto duas vezes lança.
- **Tirar o último objeto** equivale ao `Unbind()`, com o mesmo evento (P2.5; commit `c201877`).
- **Valores mistos** (P2.4): sem scrubbing, a linha indica que as instâncias têm valores
  diferentes; com scrubbing, ela mostra o valor da primeira, e o delta vale para cada uma. O
  `GetValue` devolve o valor da primeira instância, `IsMixed` diz se elas diferem, e `GetValues`
  devolve um valor por objeto (P2.10). Aplicado no commit `c201877`; o indicativo e o scrubbing
  ficam com a view.
- **Objeto → UI** (P2.6): `INotifyPropertyChanged` para quem implementa, e o `Refresh()` do
  inspector para o resto. O `Refresh()` e o `ValueChanged` com a origem entraram no commit
  `eb497c6`, e o `INotifyPropertyChanged` no commit `b6a99d8`.
- **Texto → valor** (P2.7): a cultura é configurável no inspector inteiro, e toda entrada de texto
  cru tenta virar o tipo do membro; quando não dá, a linha mostra a falha. O `SetValue("5")` num
  `int` também tenta converter antes de gravar (relatório, seção 6). A cultura padrão é a atual, e
  a conversão usa o `IParsable<T>` quando o tipo implementa, e o `TypeConverter` no resto (P2.11).
  Aplicado no commit `e136f82`, no `SetValue`; a falha fica no `Failure` do nó (3.3).
- **Quando a view grava** (P2.12): texto e número no Enter e quando o controle perde o foco, com o
  Esc voltando ao valor do objeto; toggle e escolha na hora; slider e scrubbing enquanto arrastam.
  Uma conversão que falha deixa o texto como foi digitado, e o objeto mantém o valor.
- **Faixa** (P2.8): o `SetValue` limita o valor ao `[InspectorRange]`: com (0, 255), 999 grava 255.
  Aplicado no commit `e136f82`.
- **Sanitizadores** (P2.9): por campo, numa lista ordenada, e executados sempre nessa ordem. O
  gancho de conversão (o "TheBrute") não volta. São duas listas, as regras de texto antes da
  conversão e as de valor depois, então a ordem entre as duas vem da estrutura (P2.13). Aplicado
  no commit `e136f82`: `TextRules` e `ValueRules` no nó.
- **`ReadOnly` passa para os filhos** (P4.1, P4.2): um objeto aninhado somente leitura, por atributo
  ou por acessor privado, deixa os filhos somente leitura, e forçar a gravação num filho lança. O
  `ReadOnly` do próprio nó basta para a view, sem um segundo valor como `IsEffectivelyReadOnly`.
  O nó consulta os pais na hora da leitura (P4.6): uma mudança na camada manual vale na hora para o
  ramo todo, e um filho não reabre enquanto o pai for somente leitura. Aplicado no commit
  `13534b0`.
- **Setter não público some na reflection** (relatório, 3.6; P4.5): o membro com setter private,
  protected ou internal deixa de aparecer, e o `[InspectorReadOnly]` o traz de volta, somente
  leitura. `init`, campo `readonly` e só getter continuam aparecendo, somente leitura. Aplicado no
  commit `13534b0`; o `Boo.Secret` saiu da saída do TuxHost, que ficou com 67 linhas.

### O objeto do grupo

- **O objeto de um grupo não é trocado pelo inspector**: só os filhos editam. Vale para o grupo
  aberto (P3.1); uma class mostrada fechada, com editor próprio, é editada trocando o objeto. No
  main isso vinha do `Inspector.SetValue`, que lançava exceção; o commit `2a1cf94` tirou essa
  proteção sem registrar (3.10), e o commit `a2d8ffe` a trouxe de volta.
- **A raiz também** (P3.5): o objeto ligado só muda pelo bind. Desde o commit `0bc2f9d`, o
  `Inspector` nem tem `SetValue`; o `RootNode` interno lança se alguém chegar nele pelo `Parent`.
- **Troca por fora compromete o ramo** (P3.3, P3.4): uma troca (`foo.Moo = new Moo()` depois do
  bind) não pode derrubar o inspector. Ela é detectada no `Refresh()`, o caminho natural, e também
  numa leitura. No ramo comprometido, `GetValue` e `SetValue` lançam, e religar restaura o objeto
  inteiro. O raio é só o ramo trocado, e o resto do dono continua funcionando; o ramo fica
  desativado, e um evento deixa quem assina aceitar o objeto novo na hora (P3.6). O grupo de uma
  struct fica fora da detecção, porque a struct não tem identidade para comparar (P3.2). Aplicado
  no commit `55e7173`.

### Descoberta

- **Um membro, um nó**: a descoberta monta a árvore direto; um `MemberNode` é qualquer membro, com
  ou sem filhos, e o `Inspector` guarda a raiz (um `RootNode` interno, desde o commit `0bc2f9d`).
  As duas políticas viram classes estáticas, sem interface nem instâncias, chamadas em ordem no
  `Create`. Aplicado nos commits `5560223` e `6622a41`.
- **Ordem dos irmãos** (P5.1): a de declaração é a preferida, se der para recuperar sem muito
  custo; se não der, fica a da reflection, e o `[InspectorOrder]` resolve o resto. Aplicado no
  commit `6c17a15`, pelos metadados (seção 7). Escolhas minhas, a confirmar: os membros do tipo
  base vêm antes dos do derivado, e uma propriedade calculada, que não deixa rastro da posição
  entre os campos, vai para logo antes da próxima propriedade automática.
- **Coleções** (P5.2; relatório, seção 2): em vez dos membros do tipo da coleção (`Capacity`,
  `Count`, `Length`...), o conteúdo. O editor padrão é um seletor, que vira combo box, e vale
  tentar um editor de lista. Pode precisar de configuração a mais; o que o seletor faz com o item
  escolhido está em aberto (P5.10).
- **Propriedades calculadas** (P5.3; relatório, seção 2) entram, e o acessor roda por inteiro, como
  em `int X { get { DoSomething(); return _x; } set => _x = value; }`. Para esconder, só o
  `[InspectorIgnore]`.
- **Membro escondido com `new`** (P5.4): os dois aparecem, e nesses casos o nome composto pode ser o
  padrão (`Derived.Value` em vez de `Value`). Na P5.7: o nome composto expande nos campos do tipo
  derivado, e no exemplo `Derived.Value` é o `string Value`, não o `int Value`. Como fica a árvore
  está em aberto (P5.9).
- **Tipos com mais de um editor** (P5.5), como o `Color`: são casos de borda, e o comportamento tem
  que ser escolhido explicitamente (expandir em campos int, texto hex ou um seletor aberto por um
  botão). Também é um motivo para usar os primitivos próprios, sem o excesso de propriedades do
  `System.Drawing.Color`. Sem escolha, o membro aparece numa linha `Display`, só leitura, até
  alguém escolher, com um aviso de diagnóstico (P5.8).
- **Cache** (P5.6): por enquanto, só a lista de membros por tipo; o desenho do cache fica para uma
  sessão própria.

### Views

- **Fábricas com nomes distintos** por plataforma (P7.2), como `CreateWinFormsView` e
  `CreateWpfView`.
- **A view percorre a árvore** (P7.3): um painel por grupo, que recolhe junto.
- **Válvulas de escape** (P7.4): um callback por plataforma quando um controle é criado, e um
  terceiro, agnóstico, quando a linha inteira termina de ser montada. Se ele não puder ser
  agnóstico, são quatro, dois por plataforma.

### Primitivos e PixieLib

- **Primitivos com prefixo `Px`**, o mesmo da PixieLib (3.9): `PxPoint`, `PxPointF`, `PxSize` e
  `PxSizeF`. Substitui o `SK` provisório do commit `21cbeda`. Aplicado no commit `9e15f6e` (3.4).
- **Um tipo só, em `double`** (P8.1), como no WPF: saem as variantes int e float. Aplicado no
  commit `2951ce3`.
- **Primitivos novos** (P8.2): `PxRect`, `PxPadding` e `PxDock`. Aplicado no commit `f1de920`.
- **Cores** (P8.3, P8.4): `PxColorArgb` e `PxColorHsl`, com o espaço de cor no fim para não juntar
  dois prefixos. Sem conversão implícita entre as duas: funções estáticas `ToHsl`, `FromHsl`,
  `ToArgb` e `FromArgb`, o que também acaba com o CS0457. Aplicado no commit `3544a8e`.
- **Regras de conversão** de 3.4 confirmadas (P8.5). Aplicadas com o `System.Drawing` nos commits
  `2951ce3` e `f1de920`; as do WinForms e do WPF vêm com as views.
- **PixieLib** (P8.6 a P8.9): a precisão padrão é `double`. Por enquanto os primitivos ficam neste
  projeto, sem `PixieLib.dll`; a mudança para a PixieLib, com o sufixo de precisão, fica para uma
  sessão própria.

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
filtro por instância fica com a visibilidade condicional. Um filtro por nome também pode ser
injetado, resolvido com a mesma precedência dos atributos (P6.4). Ele vale por tipo, no `Create`
(P6.6); onde ele é injetado está em aberto (P6.7).

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
- Seletor por expressão (por exemplo `inspector[f => f.Moo.MooX]`), que o compilador checa e que
  acompanha renomeações. Decidido (P6.5): os dois convivem, o caminho em string e o seletor.
  Aplicado no commit `e06b6f7` como método genérico, `Node<T>`, porque o `Inspector` não é genérico
  e um indexador não pode ser: `inspector.Node<Foo>(f => f.Moo.MooX)` é o nó de `"Moo.MooX"`, e
  `inspector["Moo"].Node<Moo>(m => m.MooX)` vale a partir de um nó. O tipo tem que ser o que o nó
  guarda, ou um de que ele deriva; outro tipo, ou qualquer coisa além de uma cadeia de membros do
  parâmetro, lança `ArgumentException`.
- Em vez de `FieldSet_FieldType = typeof(TextBox)`, um enum agnóstico de editor (`EditorKind`: `Text`,
  `Number`, `Toggle`, `Choice`, `Slider`, `Color`, `Button`, `Display`, `Header`, `Separator`). Cada
  view decide o controle.
- O que a configuração guarda: rótulo, editor, flags (`ReadOnly`, `Disabled`, scrubbing),
  multiplicadores (scrubbing e slider), sanitizadores, visível, recolhido e ação pós-bind.
- A ideia do `TypeSafeLock` (opt-in de quais tipos podem ser expandidos) continua, como atributo do
  inspector (`[InspectorExpandable]` na tabela acima).
- O `TypeBinderMode` do original (`Automatic` / `Manual`, declarado e nunca usado lá) continua
  existindo para quando se quer só os campos declarados, porque nem todo inspector vai ser tipado
  (decidido, P1.7). O modo sai do jeito de criar: tipado é automático, sem tipo é manual, e o modo
  explícito continua para o inspector tipado e manual (P1.12).
- Nós manuais (decidido, P1.6): o botão entra, com uma ação no clique; o cabeçalho não, porque dá
  para resolver de outro jeito. O campo só de exibição também entra, com um getter.

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
// cor: o editor é escolhido explicitamente (P5.5); hoje System.Drawing.Color vira um grupo
// component["Text"].VisibleWhen = c => ((ComponentPreset)c).IsText;   // sucessor do VariablePool
// component.AddButton("LayerUp", "▲", () => tree.OnLayerUp());
// component.AddDisplay("Layer", () => tree.SelectedIndex);    // decidido (P1.6)

component.Rebind(selected);  // a cada seleção: Unbind + Bind (commit 4dec125)
component.AddBind(other);    // cortes seguintes: multi-bind
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
  cópia, e o host lê o resultado com `inspector.Instance`. Revisto: a troca do objeto de um grupo
  não deve ser seguida em silêncio, e sim comprometer o ramo (seção 0; 3.10). Desde o commit
  `a2d8ffe`, a gravação de volta passa por um caminho interno (hoje `WriteTo`), porque o `SetValue`
  público recusa o grupo aberto e a raiz; o `ReadOnly` continua sendo conferido na subida.
- **Ligar, desligar e religar** (aplicado, commit `4dec125`): o `bind(obj)` de novo trocando o
  objeto (commit `2a1cf94`, que tirou o `IsTypeBound` do main) foi desfeito. `Bind` lança
  `InvalidOperationException` se já houver objeto ligado, e a troca é explícita: `Rebind`, que é
  `Unbind` e `Bind`. Um objeto que não serve para a árvore lança `ArgumentException` na hora, em vez
  de falhar depois no `GetValue` de um nó (P2.2); um tipo derivado serve. O `Rebind` confere antes
  de desligar, então um objeto recusado deixa o anterior ligado. `Unbind()` sem nada ligado não faz
  nada. As cinco operações estão na seção 0 (P2.1); o multi-bind vem depois.
- **ReadOnly** (aplicado, commit `6117bb1`): `SetValue` lança `InvalidOperationException` quando o
  nó está marcado como somente leitura, antes de ler qualquer coisa. Vale para o que a reflection
  marca (setter privado, `init`, campo `readonly`), para o `[InspectorReadOnly]` e para a camada
  manual, que continua podendo reabrir (`inspector["x"].ReadOnly = false`). Como a gravação de volta
  de uma struct passa pelo `SetValue` do dono, um membro de struct somente leitura também é
  recusado; já os membros de uma class somente leitura continuam editáveis, porque a edição é no
  próprio objeto (seção 5, item 6). Decidido em 27/09 (P4.1, P4.2): o `ReadOnly` passa para os
  filhos, também numa class, e forçar a gravação num filho lança; o `ReadOnly` do próprio nó passa
  a bastar para a view. O nó consulta os pais na hora da leitura (P4.6). E o setter não público
  (private, protected, internal) passa a esconder o membro (relatório, 3.6; P4.5). Aplicado no
  commit `13534b0`: o filho de uma struct somente leitura agora recusa a gravação pelo próprio
  `ReadOnly` (`'X' is read-only.`), antes da subida. Desde o commit `e136f82`, o `SetValue` confere
  o `ReadOnly` antes de converter o valor.
- **Multi-bind** (aplicado, commit `c201877`): a raiz guarda uma lista de objetos. `AddBind` põe um
  ou mais (sem nada ligado, liga), `RemoveBind` tira um, e tirar o último é o mesmo que o
  `Unbind()` (P2.5); todo objeto tem que servir para a árvore (P2.3), e o mesmo objeto duas vezes
  lança. `GetValue` lê o primeiro, `GetValues` lê um valor por objeto, e `IsMixed` diz quando eles
  diferem (P2.10). `SetValue` grava em todos, e um dono null em qualquer um deles interrompe antes
  de algum mudar. Na view (P2.4): sem scrubbing, a linha indica que as instâncias diferem; com
  scrubbing, mostra o valor da primeira, e o delta vale para cada uma, como no original. O
  inspector avisa por `BindRegistered`, `BindRemoved` e `Unbound`. Escolha minha no commit
  `18dc069`, a confirmar: o `IsMixed` passou a seguir o que a view mostra (a última leitura, e
  nunca misto com um valor guardado, que vai para todos), sem ler os objetos, para a linha não
  juntar uma leitura ao vivo com o valor que ela mostra.
- **Objeto → UI** (decidido, P2.6): `INotifyPropertyChanged` no lugar de `ITwoWayBinderTransmiter`.
  O objeto deixa de guardar referência ao inspector (`BindedTo`), e vários inspectors podem observar
  o mesmo objeto. Um `Refresh()` manual cobre quem não implementa a interface; ele é o fluxo normal,
  e não se confunde com os métodos de força (abaixo). Aplicado no commit `eb497c6`: cada nó guarda
  o que os objetos tinham na última leitura, e o `ValueChanged` só dispara quando isso muda, com a
  origem (`Write`, `Instance`, `Refresh` ou `Force`); ligar e desligar recomeça em silêncio. No
  commit `b6a99d8`, um vigia interno (`InstanceWatcher`) assina o `PropertyChanged` dos objetos
  ligados e dos objetos dos grupos: o aviso relê o membro (`Instance`), um aviso sem nome relê
  todos, e a troca do objeto de um grupo é conferida na hora. A gravação feita pelo próprio
  inspector continua `Write`, e o que o mesmo setter muda de tabela chega como `Instance`.
  Corrigido no commit `0602d0a`: a gravação e o aviso releem também o que muda junto, que é a
  struct mais de cima do campo gravado, com o ramo dela, e os membros de baixo de uma struct ou de
  um objeto fechado; e o aviso da struct que o inspector grava de volta no dono não chega mais como
  `Instance`.
- **Controle do binder** (decidido, P1.8): um enum de controle no lugar de uma flag `AutoApply`
  (manual, ou automático num sentido ou nos dois) e métodos auxiliares de força: gravar os valores
  no objeto, recarregar do objeto e limpar a view, deixando tudo vazio ou zero. Eles também podem
  ser disparados por um evento, quando algo sai do normal. O enum é de flags, sem combinação
  inválida, e o modo manual tem um fluxo normal para ler e gravar à mão; os de força ficam para
  quando esse fluxo falhou ou não se encaixa, cada um com o seu evento (P1.13). Aplicado no commit
  `18dc069`, com o modo em `inspector.Options.BinderControl` (`Automatic` por padrão):

  | Sentido | Sozinho (flag ligada) | À mão (fluxo normal) | Forçado |
  |---|---|---|---|
  | view → objeto (`ViewToInstance`) | `SetValue` grava na hora | `SetValue` guarda, `Apply()` grava | `ForceApply()` |
  | objeto → view (`InstanceToView`) | `INotifyPropertyChanged` e `Refresh()` | `Reload()` | `ForceReload()` |
  | só a view | | | `ForceClear()` |

  - Sem `ViewToInstance`, o `SetValue` prepara o valor como antes (regras, conversão e faixa, com a
    falha na linha) e o nó o guarda: `HasPendingValue`, e `ValueChanged` com a origem `Pending`,
    sem mudar o objeto. O `Apply()` grava o que está guardado (`Write`); um nó que não pode receber
    o valor agora (somente leitura, dono null, getter acima que lança) fica com ele e mostra a
    falha, como contornado, e num ramo desativado o valor espera o `Rebind`, que o descarta. O
    inspector diz se há algo guardado por `HasPendingValues`.
  - Sem `InstanceToView`, os avisos do objeto e o `Refresh()` não chegam à view. O `Reload()` relê
    tudo em qualquer modo e descarta os valores guardados (`ValueChanged` com `Reload`), como o
    botão Reload do original; o `Refresh()` guarda os valores pendentes, para um `Refresh()`
    periódico não apagar o que a pessoa digitou.
  - O que a view mostra é o `ViewValue`: o valor guardado, ou o que o primeiro objeto tinha na
    última leitura que o modo deixou passar. Ele não lê o objeto; o `GetValue()` continua lendo.
  - Os de força valem em qualquer modo, e cada um dispara o seu evento no inspector
    (`ForcedApply`, `ForcedReload` e `ForcedClear`). O `ForceApply()` grava o que a view mostra, os
    valores guardados e os lidos por último, cada objeto com o seu, tomados antes de gravar
    qualquer coisa (um setter que muda outro membro não apaga o que a view tinha); ficam de fora os
    grupos, os somente leitura, os ramos desativados, o nó cuja leitura falhou e o dono null. O
    `ForceReload()` recomeça do que os objetos têm: descarta os valores guardados e as falhas, e os
    ramos desativados aceitam o objeto novo, como um `Rebind` com os mesmos objetos, mas sem os
    eventos de bind. O `ForceClear()` esvazia a view (zero, false, texto vazio ou null) e não mexe
    nos objetos; a próxima leitura traz os valores de volta, e um `ForceApply()` logo depois esvazia
    os objetos. Nos três, o `ValueChanged` sai com `Force` onde a view mudou.
  - Escolhas minhas, a confirmar: o `Refresh()` fica no sentido objeto → view, então não faz nada
    sem `InstanceToView`; o `Reload()` descarta os valores guardados, e o `Refresh()` não; o
    `ForceReload()` reativa os ramos desativados; e o `ValueChanged` também sai para um valor
    guardado (`Pending`), já que a view mudou, mesmo sem o objeto mudar.
- **UI → objeto** (decidido, P2.7): toda entrada de texto cru tenta virar o tipo do membro, com a
  cultura configurada no inspector; se não der, a linha mostra a falha. O `SetValue("5")` num `int`
  também tenta converter antes de gravar. O original usa `Convert.ToDouble` com a cultura atual, e o
  `ONLY_NUMBERS` aceita tanto `.` quanto `,`. A cultura padrão é a atual, e a conversão usa o
  `IParsable<T>` quando o tipo implementa, e o `TypeConverter` no resto (P2.11). A view grava o
  texto no Enter e ao perder o foco, e toggle, escolha, slider e scrubbing na hora (P2.12).
  Aplicado no commit `e136f82`, no `SetValue`, antes de tocar em qualquer objeto: o texto passa
  pelas regras de texto e vira o tipo do membro (vazio é null num membro nullable, e um enum sai
  pelo nome); um número vira o tipo numérico do membro ou um enum, e um `double` num `int`
  arredonda, como o `Convert` (7,6 grava 8). O que falha nesse preparo (texto que não converte,
  número grande demais para o membro, regra que lança) vai para o `Failure` do nó como recuperado,
  com `BindFailed`, e nada é gravado; é o que a linha mostra. Um valor de um tipo sem relação com o
  do membro (um `Moo` num `int`, null num `int`) é uso errado e lança `ArgumentException`.
- **Faixa** (decidido, P2.8): o `SetValue` limita o valor ao `[InspectorRange]`; com (0, 255), 999
  grava 255. Aplicado no commit `e136f82`, depois das regras de valor, para número e para texto.
- **Sanitizadores** (decidido, P2.9): por campo, numa lista ordenada, executados sempre nessa
  ordem. No original eles misturam texto e número: no caminho do `TextBox` o `POSITIVE_NUMBERS`
  recebe `string` e não faz nada; no scrubbing, o delegate multicast devolve só o resultado do
  último; e com três ou mais funções encadeadas o `GetTextboxData` reaplica funções anteriores.
  São duas listas: as regras de texto antes da conversão, e as de valor depois (P2.13). Aplicado
  no commit `e136f82`: `TextRules` e `ValueRules` no nó, e cada regra é uma função. As de texto só
  rodam quando chega texto, e as de valor rodam sempre, depois da conversão. As prontas são
  `TextRule.Digits`, `Number`, `MaxLength(n)` e `Only(caracteres)`, e `ValueRule.Min` e `Max`; uma
  regra que lança vira falha no nó, e nada é gravado.
- **Sem gancho de conversão** (decidido, P2.9): o "TheBrute" não volta.
- `ApplyFunction` vira o evento `ValueChanged` do nó (P1.4).
- **Eventos de ciclo de vida**: a lista decidida está na 3.10 (P1.4), com um payload de falha
  tipado (mensagem, motivo, sugestão, caminho).

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
| `Point`, `PointF` | `PxPoint` | Aplicado (`21cbeda` com `SK`, `9e15f6e` com `Px`); um tipo só, em `double`, no commit `2951ce3` (P8.1) |
| `Size`, `SizeF` | `PxSize` | Aplicado; idem |
| `Color` | `PxColorArgb` | Aplicado (`ArgbColor` no `21cbeda`); espaço de cor explícito, ponte para `System.Drawing.Color`; `PxColorArgb` no commit `3544a8e` (P8.3) |
| `ColorHSL` | `PxColorHsl` | Aplicado (`HslColor` no `21cbeda`); `PxColorHsl`, em `double`, no commit `3544a8e` (P8.3) |
| (novo) | `PxRect` | Aplicado no commit `f1de920` (P8.2): resultado do passo de layout |
| (novo) | `PxPadding` | Aplicado no commit `f1de920` (P8.2): margens (`Padding` no WinForms, `Thickness` no WPF) |
| (novo) | `PxDock` (enum) | Aplicado no commit `f1de920` (P8.2): substitui o `DockStyle` nas opções, com os mesmos valores |

Com os nomes novos, um arquivo WinForms ou WPF que importa `InteractiveEditor.Primitives` deixou de
ter ambiguidade (CS0104) com `System.Drawing`, `System.Windows` e `System.Windows.Media` (testado).
O `SK` provisório colidia com o SkiaSharp (`SKPoint`, `SKSize` e `SKColor`); o `Px` não colide com
ele nem com o `System.Numerics` (testado, todos importados no mesmo arquivo).

**Regras de conversão** (confirmadas em 27/09, P8.5)

- `System.Drawing` (int ↔ int, float ↔ float): implícitas nos dois sentidos, sem perda.
  `System.Drawing.Primitives` existe fora do Windows (testado no Linux), então essas conversões
  ficam no build comum.
- Para o WPF (double): implícita para pontos. Para `System.Windows.Size`, explícita (ou com clamp),
  porque o `Size` do WPF lança exceção com largura ou altura negativa (comportamento documentado) e
  o primitivo aceita negativos.
- Do WPF para os tipos int ou float: explícitas, porque perdem precisão.
- Com o `double` (P8.1), o princípio continua o mesmo (implícita quando não perde nada, explícita
  quando perde ou pode lançar), mas o `System.Drawing` inverte: dele para o `Px` fica implícita, e
  do `Px` para ele, explícita. Com o WPF, pontos ficam implícitos nos dois sentidos. Aplicado com o
  `System.Drawing` nos commits `2951ce3` (`Point`, `PointF`, `Size` e `SizeF`) e `f1de920`
  (`Rectangle` e `RectangleF`): a volta para os tipos em int arredonda, como o `Point.Round` do
  próprio `System.Drawing`, e a volta para os em float estreita. O `ToString` dos primitivos em
  `double` não depende mais da cultura atual.
- Entre as cores, nenhuma conversão implícita (decidido, P8.4): funções estáticas `ToHsl`,
  `FromHsl`, `ToArgb` e `FromArgb`. Aplicado no commit `3544a8e`: `PxColorArgb.FromHsl` e `ToHsl`,
  `PxColorHsl.FromArgb` e `ToArgb`, com as contas dos dois sentidos no `PxColorHsl`, e o CS0457
  saiu junto. O `PxColorArgb` continua com as conversões implícitas com o `System.Drawing.Color`,
  que não perdem nada; o `PxColorHsl` perdeu as dele, porque ir para o `System.Drawing.Color` é ir
  para ARGB. Escolha minha, a confirmar: o `PxColorHsl` também passou para `double`, a precisão
  padrão (P8.1, P8.6).
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
- Fábricas com nomes distintos por plataforma (decidido, P7.2; por exemplo `CreateWinFormsView` /
  `CreateWpfView`). Com overloads que diferem só pelo tipo `Control`, um projeto só WinForms que
  referencia a DLL diretamente não compila (CS0012, pede `PresentationFramework`); com nomes
  distintos, compila (testado).
- A view percorre a árvore (decidido, P7.3): um painel por grupo, que recolhe junto, em vez de
  montar as linhas a partir de uma lista plana com o recuo pela profundidade.
- Scroll e empilhamento: usar o que a plataforma já tem (`AutoScroll`, `ScrollViewer`) em vez de
  mover painel por painel.
- Válvula de escape por plataforma, o sucessor limpo do `EditField()` (decidido, P7.4): um callback
  `ControlCreated(caminho, controle)` na view de cada plataforma, para o que a configuração agnóstica
  não cobrir. Mais um, agnóstico, quando a linha inteira termina de ser montada, porque cada linha
  tem vários controles; se ele não puder ser agnóstico, são quatro, dois por plataforma.

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
- Quando as views chegarem, a biblioteca volta a precisar do Windows. Havia duas saídas: só
  `net10.0-windows` (projeto sem condições, mas o TuxHost deixa de rodar fora do Windows) ou de novo
  os dois alvos, com o bloco condicional. Decidido em 27/09 (P7.1): os dois alvos, com o código das
  views só no `-windows`; o núcleo e o TuxHost continuam rodando fora do Windows, e cada consumidor
  recebe a DLL do alvo dele. As conversões dos primitivos para o WPF voltam junto.

Verificado no commit `5560223`: a solução compila sem erros e sem warnings, e o TuxHost imprime
exatamente o mesmo de antes.

### 3.8 Performance

- Descoberta uma vez por tipo (cache), em vez de reflection a cada bind (o original chama
  `GetField(nome)` a cada `BindToObject`). Por enquanto, só a lista de membros por tipo; o desenho
  do cache fica para uma sessão própria (P5.6).
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
  com `System.Drawing`, `System.Numerics` nem SkiaSharp (CS0104, testado). As cores viram
  `PxColorArgb` e `PxColorHsl` (decidido, P8.3).
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
  matemática vem dele) e explícita de volta, testadas no protótipo. Os primitivos atuais, em
  `double` desde o commit `2951ce3` (`PxPoint`, `PxSize`, `PxRect` e `PxPadding`), passam a sair do
  gerador, com os sufixos de precisão dele.
- **Decidido em 27/09**: a precisão padrão é `double`, como o `Vec2` do C++ (P8.6). A PixieLib
  ainda não é usada (P8.7, P8.8): os primitivos ficam no InteractiveEditor, sem `PixieLib.dll`, e a
  mudança para lá, com o sufixo de precisão (P8.9) e o "onde" acima, fica para uma sessão própria.

### 3.10 O `Inspector`: hoje raso

O `Inspector` tem 386 linhas e, desde o commit `0bc2f9d`, não é mais um nó: guarda a raiz num
`RootNode` interno e expõe o `Create<T>()` com os eventos e o relatório da criação, o `Id`, o
`Name`, as opções (`Options`), os objetos ligados (`Instance` e `Instances`), o indexador, a
enumeração, as `Rows`, o `Refresh()`, o `Dispose`, o bind inteiro (`Bind`, `AddBind`,
`RemoveBind`, `Unbind()` e `Rebind`, com os eventos) e o controle do binder (`Apply()`,
`Reload()`, `HasPendingValues` e os três métodos de força, com os eventos).
Deveria ser uma das peças mais completas, porque é o que o host e a view usam. Levantamento para o
desenho, com as decisões de 27/09 e 29/09 no fim.

**Buracos no que já existe** (testado)

- Resolvido no commit `4dec125`: um objeto de outro tipo passava pelo `bind`
  (`Inspector.Create<Foo>().bind(new Bar())` aceitava, a árvore continuava a de `Foo`, e o erro só
  aparecia depois, no `GetValue` de um nó, como `ArgumentException` do reflection). Agora o `Bind`
  lança na hora, sem mexer na árvore nem no que já está ligado (P2.2).
- Resolvido no commit `a2d8ffe`: o `SetValue` da raiz aceitava null e objeto de outro tipo sem
  reclamar, e com null o inspector ficava sem objeto, um unbind silencioso. Agora ele lança (P3.5),
  e desde o commit `0bc2f9d` nem existe no `Inspector`.
- Resolvido no commit `a2d8ffe`: o objeto de um grupo podia ser trocado
  (`inspector["Moo"].SetValue(new Moo())` trocava o `Moo` do objeto ligado). No main, o
  `Inspector.SetValue` (raiz e grupos aninhados) lançava exceção; o commit `2a1cf94` trocou isso
  pela gravação pelo pai, para a struct voltar ao dono, e a proteção se perdeu. Agora o `SetValue`
  de um grupo aberto lança, e a struct volta ao dono por um caminho interno (P3.1).
- Resolvido no commit `55e7173`: uma troca feita por fora (`foo.Moo = new Moo()`) passava sem
  sinal, e o ramo só seguia o objeto novo; com null, as leituras davam null e só a gravação lançava.
  Agora é a detecção decidida em P3.3 e P3.4 (abaixo).
- A descoberta roda de novo a cada `Create` (3.8). Os nós não podem ser compartilhados entre
  inspectors, porque cada um tem as próprias opções; o que dá para cachear é a lista de membros.

**Decidido** (seção 0)

- Sem `Inspector<T>`, e com o tipo da raiz fixo: ligar um objeto de outro tipo lança (P2.2). A
  classe não é genérica porque nem todo inspector vai ser tipado (P1.7); o motivo anterior, trocar
  a raiz por um objeto de outro tipo, caiu junto com a troca.
- Composição (P1.1): o inspector guarda a raiz como um nó interno e expõe só o que é dele.
  Liberada na P9.3 e aplicada no commit `0bc2f9d`. Cada inspector tem um id único, que os eventos e
  a trava global usam (P1.9, P1.11).
- Setter abstrato de volta, com um tipo de nó por comportamento fixo na criação (P1.2). Dentro do nó
  de membro, grupo ou folha continua decidido em tempo de execução, porque o `Expandable` pode mudar
  depois do `Create`. Aplicado no commit `0bc2f9d` (`MemberNode` e `RootNode`).
- Enumerável, entregando todos os nós; as linhas da view saem de um percurso à parte, `Rows` (P1.3;
  commit `b456398`).
- Opções por inspector (`InspectorOptions`), a camada entre o `GlobalOptions` e o nó, sobrescrevendo
  o global (P1.5): as de layout e hospedagem (1.1, item 12; altura e espaçamento por editor, 1.2) e
  a cultura (P2.7). O `Create` trava o `GlobalOptions` até o `Dispose` do último inspector vivo
  (P1.5, P4.4, P1.11; commit `ffee3a7`). O `InspectorOptions` entrou no commit `e136f82`, com a
  cultura.
- `IDisposable` no inspector e nos nós (P1.5; commit `ffee3a7`).
- Ciclo do bind: `Bind`, `Unbind`, `Rebind`, `AddBind` e `RemoveBind` (P2.1), os valores mistos do
  multi-bind (P2.4) e o `Refresh()` (P2.6).
- Controle do binder (P1.8, P1.13): um enum de flags e um fluxo normal para ler e gravar à mão; os
  métodos de força, cada um com o seu evento, não se confundem com esse fluxo nem com o `Refresh()`.
  Aplicado no commit `18dc069` (3.3).
- `TypeBinderMode` continua (P1.7), e sai do jeito de criar (P1.12). Nós manuais: botão com ação e
  campo só de exibição com getter, sim; cabeçalho, não (P1.6).
- Filtro por nome injetável, com a precedência dos atributos, por tipo (P6.4, P6.6); onde ele é
  injetado está em aberto (P6.7). A visibilidade condicional ficou para o final (P6.2).
- Eventos (P1.4), sem economia: alguns só de consumo interno, outros expostos e consumidos também
  pelo próprio inspector. Os nomes seguem a convenção do .NET (P1.10): o evento sem o `On`, e o
  `On` no método que o dispara. Os da criação são estáticos (P1.9):

  | Onde | Evento | Quando | Situação |
  |---|---|---|---|
  | inspector, estático | `Created` | a criação terminou, com as contagens | `1535874` |
  | inspector, estático | `DiscoveryFinished` | a árvore está montada | `1535874` |
  | inspector, estático | `DiscoveryFailed` | uma falha no `Create`, com a severidade | `1535874` |
  | inspector | `BindRegistered` | um ou mais objetos entraram no bind | `c201877` |
  | inspector | `BindRemoved` | objetos saíram do bind | `c201877` |
  | inspector | `Unbound` | o último objeto saiu | `c201877` |
  | inspector | `ForcedApply` | um `ForceApply()` terminou | `18dc069` |
  | inspector | `ForcedReload` | um `ForceReload()` terminou | `18dc069` |
  | inspector | `ForcedClear` | um `ForceClear()` terminou | `18dc069` |
  | nó | `ValueChanged` | um valor mudou, com a origem (no lugar do `ValueApplied`) | `eb497c6` |
  | nó | `ObjectReplaced` | o objeto do grupo foi trocado por fora; dá para aceitar | `55e7173` |
  | nó | `BindFailed` | falha ao ler ou gravar, com mensagem, motivo, sugestão e caminho | `eb497c6` |

  Os args da criação, no exemplo da resposta: `ErrorCount` (todos), `UnhandledErrorCount` (os que
  caíram em fallback automático) e `CriticalErrorCount` (os graves, sem resolução, que não
  derrubaram; provavelmente o nó que falhou sai). Os eventos da criação acontecem antes de alguém
  conseguir assinar no inspector, então eles são estáticos, e o resultado da criação fica guardado
  no inspector (P1.9). O `ValueChanged` diz de onde veio a mudança, e os nomes seguem a convenção
  do .NET: `Created` é o evento, e `OnCreated` o método que o dispara (P1.10).

**O objeto do grupo** (decidido, P3.1 a P3.5; seção 0)

- `SetValue` num grupo aberto lança exceção, como o `Inspector.SetValue` do main; uma class mostrada
  fechada continua sendo editada trocando o objeto (P3.1). No main, o getter era comum e o setter
  era abstrato: cada tipo de nó decidia o seu (o `Fieldset` gravava, o `Inspector` lançava). A
  gravação de volta de uma struct passa por um caminho interno, que continua respeitando o
  `ReadOnly`. A raiz entra na mesma regra: o objeto ligado só muda pelo bind (P3.5). Aplicado no
  commit `a2d8ffe`, também para o grupo de uma struct: ela muda pelos filhos.
- Detecção: no bind, cada grupo guarda o objeto que viu, só para comparar. Se a cadeia de pais
  devolver outro objeto (ou null), o grupo fica comprometido. A comparação acontece no `Refresh()`,
  o caminho natural, e também numa leitura (P3.3). No ramo comprometido, `GetValue` e `SetValue`
  lançam, a view desativa o ramo, e religar restaura o objeto inteiro (P3.4).
- A troca não pode derrubar o inspector (relatório, seção 8). O exemplo da resposta: um método do
  próprio objeto troca um filho (`void Work() { moo = new Moo(); }`) e alguém chama `foo.Work()`
  depois do bind. Detectar não precisa de exceção: ler o objeto antigo não falha no .NET, porque o
  grupo o guarda e ele continua vivo; é a comparação que acusa a troca.
- Decidido em 29/09 (P3.6): o raio é só o ramo trocado (o dono continua o mesmo objeto, e os
  outros membros dele seguem certos), e a reação é desativar o ramo e avisar por evento, em que
  quem assina pode aceitar o objeto novo na hora. O grupo de uma struct fica fora da detecção
  (P3.2).
- Aplicado no commit `55e7173`. No bind, todo grupo de tipo class guarda o objeto que tem em cada
  objeto ligado. Numa leitura, numa gravação e no `Refresh()`, os grupos no caminho são
  comparados, de cima para baixo. Num grupo aberto, a troca dispara `ObjectReplaced` (com o objeto
  ligado, o anterior e o atual); sem aceite, o ramo fica com `IsCompromised`, `GetValue`,
  `GetValues` e `SetValue` lançam dizendo qual grupo foi trocado, `IsMixed` lê false, e o
  `Refresh()` deixa o ramo de fora. Um objeto fechado é valor: trocá-lo é edição, e quando o
  próprio inspector troca um, o ramo dele guarda o objeto novo. Qualquer mudança no bind restaura.
  Corrigido no commit `0602d0a`: quando a troca fica (um objeto fechado trocado por fora, ou o
  objeto novo aceito por quem assina), os grupos de dentro também recomeçam do objeto novo; antes,
  a leitura seguinte os dava como trocados e desativava o ramo.

**Em aberto** (em `perguntas-em-aberto.md`): onde o filtro por nome é injetado (P6.7).

### 3.11 Erros: o inspector não cai

A premissa está na seção 0: uma exceção num ponto fraco não derruba o inspector. O que ela muda,
com as respostas de 29/09 (P0.1, P0.2, P1.9):

- **Os pontos fracos**: a descoberta (tipos de terceiros, tipos que não carregam, atributos
  inválidos), o binding e o acesso ao objeto ligado (getters e setters que lançam, objetos trocados
  por fora, pais null) e as views (controles que falham ao ser criados).
- **Automático e semiautomático**: o automático resolve sem ambiguidade; o semiautomático só garante
  que nada caia, e o evento dá a quem assina a chance de corrigir o estado no meio do caminho.
  Os args levam um `Handled`, como no WinForms; quem corrige marca, e o fallback só vale quando
  ninguém marcou. É daí que sai a contagem dos que caíram em fallback.
- **Aplicado no `Create`** (commit `1535874`), com os tipos em `InteractiveEditor.Diagnostics`, um
  por arquivo: `FailureSeverity`, `InspectorEventArgs`, `InspectorFailureEventArgs` (caminho,
  exceção, mensagem, motivo, sugestão e `Handled`), `InspectorCreatedEventArgs` (as três contagens)
  e `InspectorReport`, que fica em `inspector.Report`. Um membro cuja assinatura não dá para ler
  (uma assembly que ele usa está ausente) sai da árvore, como crítico; o `GetIndexParameters()`
  também lê a assinatura, então ele fica dentro da mesma proteção. Um atributo que não dá para ler
  ou aplicar é pulado, e os outros do mesmo membro valem, como contornado. Os padrões da reflection
  que falham deixam o nó com o nome como rótulo, também contornado. Só o fatal sobe, depois de
  soltar a trava das opções globais; uma exceção de quem assina o evento também sobe por ele.
  Testado com uma biblioteca cuja dependência é apagada antes de rodar.
- **No objeto ligado** (commit `eb497c6`): um getter que lança não para a leitura nem o bind; o nó
  lê null ali, guarda a falha em `Failure` e avisa por `BindFailed` (recuperado), e a próxima
  leitura tenta de novo. Um setter que lança não para o `SetValue`; os objetos ficam com o que ele
  deixou, e a falha vai para o nó (contornado). Uma leitura que funciona não limpa uma falha de
  gravação; uma gravação que funciona limpa. No preparo da gravação (commit `e136f82`), texto que
  não converte, número grande demais e regra que lança também viram falha no nó (recuperado), e
  nada é gravado. No `Apply()` e no `ForceApply()` (commit `18dc069`), um nó que não recebe o valor
  mostra a falha (contornado), e os outros seguem. O uso errado continua lançando: somente
  leitura, grupo, dono null e valor de um tipo sem relação com o do membro.
- **Falta**: a mesma proteção nas views, quando elas existirem.
- **Severidade** (decidido, P0.1): recuperado, contornado, crítico e fatal; só o fatal sobe para
  quem chamou.
- **O que continua lançando** (decidido, P0.2): o uso errado da API por quem chama (ligar duas
  vezes, outro tipo, gravar num grupo ou num ramo comprometido, um caminho desconhecido, mudar uma
  opção global com um inspector vivo). É bug de quem chamou, e o estado do inspector não muda. O
  atributo inválido é ponto fraco: evento, fallback, e o `Create` segue.
- **Eventos da criação** (decidido, P1.9): a descoberta roda dentro do `Create`, antes de alguém
  conseguir assinar um evento no inspector. Por isso eles são estáticos, com o inspector (e o id
  dele) nos args, e o resultado da criação fica guardado no inspector para conferir depois.
- **Estado no nó**: com o ramo comprometido e os erros, o nó passa a ter estado além das opções.
  Sugestão sua (relatório, seção 8): separar no arquivo o que é informação do nó (as opções) do que
  é estado, em duas regiões. Aplicado nos commits `eb497c6` e `55e7173`: `#region Options` e
  `#region State` no `InspectorNode`.
- O payload de falha do original (mensagem, motivo, possível solução, linha e membro de origem;
  1.1, item 9) continua valendo para os eventos de falha.

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
inspector, exceções, binding pela cadeia de pais, `Inspector` não genérico, objeto do grupo e
capacidades do binding. As respostas de 27/09 e de 29/09 resolveram a lista abaixo e reviram duas
dessas decisões (as exceções, pela premissa, e o motivo do `Inspector` não genérico); o que sobrou
está numerado em `perguntas-em-aberto.md`.

1. **Primitivos** (resolvida em 27/09): um tipo só, em `double` (P8.1); `PxRect`, `PxPadding` e
   `PxDock` entram (P8.2); as cores viram `PxColorArgb` e `PxColorHsl` (P8.3). Aplicada nos
   commits `2951ce3`, `3544a8e` e `f1de920`.
2. **`Fieldset`** (resolvida no commit `5560223`): o `Fieldset` e o namespace `Binding` saíram;
   qualquer membro é um `InspectorNode`, e o CS0118 deixou de existir.
3. **Cultura** (resolvida em 27/09 e 29/09): configurável no inspector inteiro (P2.7), com a atual
   como padrão (P2.11). Aplicada no commit `e136f82`.
4. **Nós manuais** (resolvida em 27/09 e 29/09): o botão entra, com uma ação no clique, e o campo
   só de exibição, com um getter; o cabeçalho não (P1.6).
5. **Permissão de expandir** (resolvida em 27/09): vale só para o membro ou tipo marcado, como hoje
   e como no `TypeSafeLock` (P4.3).
6. **ReadOnly num objeto aninhado (class)** (resolvida em 27/09): passa para os filhos, e forçar a
   gravação num filho lança (P4.1).
7. **O `Inspector`** (3.10; resolvida em 27/09 e 29/09, P1.1 a P1.13), com a composição liberada
   na P9.3.

---

## 6. Lista de coisas pra fazer (`[x]` = aplicado)

Estrutura

- [x] Multi-target `net10.0;net10.0-windows` num projeto só, com o código de plataforma em arquivos
      parciais excluídos do alvo `net10.0` (3.7; commit `c537554`).
- [x] Um alvo só (`net10.0`): sem código de Windows na biblioteca, saem o segundo alvo e os blocos
      condicionais (3.7; commit `5560223`).
- [ ] Dois alvos de novo quando as views chegarem, com o código delas só no `-windows` (P7.1; 3.7).
- [x] NoHost em `net10.0` (commit `c537554`).
- [x] TuxHost: console de verificação em `net10.0`, com a mesma saída do NoHost (commit `e11b2df`).
      O NoHost voltou para `net10.0-windows` (commit `176f366`), e voltou a ser ignorado pelo
      `.gitignore`, como na `main`, sem sair do repositório (commit `9587e12` e o seguinte).
- [ ] Fábricas por plataforma com nomes distintos, para não obrigar o consumidor a referenciar as
      duas plataformas (3.5; decidido, P7.2).
- [x] Primitivos: nomes provisórios `SKPoint`, `SKPointF`, `SKSize`, `SKSizeF`, `ArgbColor` e
      `HslColor` (commit `21cbeda`).
- [x] Primitivos: prefixo `Px` no lugar do `SK` provisório (`PxPoint`, `PxPointF`, `PxSize` e
      `PxSizeF`), o mesmo da PixieLib (3.9; commit `9e15f6e`).
- [x] Primitivos: um tipo só, em `double` (P8.1); saem `PxPointF` e `PxSizeF` (commit `2951ce3`).
- [x] Cores: `PxColorArgb` e `PxColorHsl` (P8.3), sem conversão implícita entre elas: `ToHsl`,
      `FromHsl`, `ToArgb` e `FromArgb` (P8.4), o que acaba com o CS0457 (commit `3544a8e`).
- [x] Primitivos: conversões nos dois sentidos com as regras de 3.4 (confirmadas, P8.5), já com o
      `double`, com o `System.Drawing` (commits `2951ce3` e `f1de920`).
- [ ] Conversões dos primitivos com o WinForms e o WPF (pontos, o `Size` do WPF, `Padding`,
      `Thickness` e `DockStyle`), no alvo `-windows`, junto com as views (3.4).
- [x] Primitivos novos: `PxRect`, `PxPadding` e `PxDock` (decidido, P8.2; commit `f1de920`).
- [ ] PixieLib em C#: primitivos e matemática fora do inspector, em `dotnet/` no repositório
      PixieLib, com source generator para as precisões (3.9; adiado para uma sessão própria, P8.7).

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
- [x] Premissa de erros no `Create`: severidade, falhas por evento com `Handled`, relatório no
      inspector, membro ilegível fora da árvore e atributo inválido pulado (3.11; P0.1, P0.2; commit
      `1535874`).
- [x] Premissa de erros no objeto ligado: getter e setter que lançam viram falha no nó, com
      `BindFailed` (3.11; commit `eb497c6`).
- [ ] Premissa de erros nas views (3.11).
- [x] Composição e tipos de nó: o `Inspector` guarda a raiz, `InspectorNode` abstrato com o getter
      comum e o setter abstrato, `MemberNode` e `RootNode`, e um id por inspector (P1.1, P1.2, P1.9;
      P9.3; commit `0bc2f9d`).
- [x] `IDisposable` no inspector e nos nós (P1.5; commit `ffee3a7`).
- [ ] `TypeBinderMode`, com o modo tirado do jeito de criar (P1.7, P1.12).
- [x] Enumeração: o inspector entrega todos os nós, e as linhas da view saem de `Rows` (P1.3; P9.4;
      commit `b456398`).
- [x] Objeto de grupo: o inspector não troca, nem o da raiz (P3.1, P3.5; P9.2; commit `a2d8ffe`).
      A proteção do main tinha se perdido no commit `2a1cf94`.
- [x] Troca por fora compromete o ramo, detectada no `Refresh()` e numa leitura, com `GetValue` e
      `SetValue` lançando até religar (P3.3, P3.4); só o ramo trocado, com um evento que aceita o
      objeto novo (P3.6), e sem struct (P3.2). Commit `55e7173`.
- [x] `InspectorOptions` (por inspector), com a cultura (P2.7; commit `e136f82`). Sobrescrever o
      global (P1.5) vale quando uma opção existir nas duas camadas; hoje nenhuma existe.
- [x] Trava do `GlobalOptions` enquanto houver um inspector vivo, solta no `Dispose` (P1.5, P4.4,
      P1.11; commit `ffee3a7`).
- [ ] Nós manuais: botão com ação e campo só de exibição com getter (P1.6); o cabeçalho não entra.
- [ ] Configuração de editor que cubra o que hoje sai por `EditField()`: itens de escolha, seletores
      (cor, fonte) e ação de botão (faixa, passo e scrubbing já existem; P6.3, para o final).
- [ ] Visibilidade condicional, por regra e por instância (sucessor do `VariablePool`; P6.2, para o
      final).
- [x] `Visible` só da view, passando para os filhos (P6.1; commit `13534b0`).
- [x] Binding respeitar o `ReadOnly` das opções no `SetValue` (commit `6117bb1`).
- [x] `ReadOnly` passando para os filhos, no lugar de um `ReadOnly` efetivo (P4.1, P4.2; P4.6;
      commit `13534b0`).
- [x] Setter não público escondido pela reflection, com o `[InspectorReadOnly]` trazendo o membro
      de volta (relatório, 3.6; P4.5; commit `13534b0`).
- [x] Rebind: `bind` de novo troca o objeto (commit `2a1cf94`). Revisto: vira `Rebind`, e o `Bind`
      volta a lançar se já houver objeto ligado (seção 0).
- [x] Binding: ligar que lança se já houver objeto ligado ou se o tipo for outro (P2.2), `Unbind()`
      e `Rebind` (P9.1; commit `4dec125`).
- [x] Multi-bind: `AddBind` e `RemoveBind`, um tipo só, com os valores mistos (P2.3 a P2.5; P2.10;
      commit `c201877`). O scrubbing por delta fica com a view.
- [x] `Refresh()` com o `ValueChanged` dizendo a origem (P2.6, P1.10; commit `eb497c6`).
- [x] Objeto → UI por `INotifyPropertyChanged` (P2.6; commit `b6a99d8`).
- [x] Controle do binder: o enum de flags, o fluxo normal para ler e gravar à mão e os métodos de
      força, cada um com o seu evento (P1.8; P1.13; commit `18dc069`).
- [x] Conversão de texto para valor, com a cultura do inspector e a falha indicada na linha; o
      `SetValue("5")` também converte (P2.7; P2.11; commit `e136f82`). A hora de gravar (P2.12)
      fica com a view.
- [x] `SetValue` limitando o valor à faixa do `[InspectorRange]` (P2.8; commit `e136f82`).
- [x] Sanitizadores tipados (sucessores das `CapFunction`), por campo, em lista ordenada (P2.9;
      P2.13; commit `e136f82`).
- [x] Seletor por expressão ao lado do caminho em string (P6.5; commit `e06b6f7`).
- [ ] Filtros: blacklist/whitelist, `TypeSafeLock` e o filtro por nome injetável, com a precedência
      dos atributos, por tipo (P6.4; P6.6; onde injetar, P6.7).
- [x] Eventos da criação, estáticos, com o resultado guardado no inspector (P1.4, P1.9; commit
      `1535874`).
- [x] Eventos do bind no inspector: `BindRegistered`, `BindRemoved` e `Unbound` (P1.4; commit
      `c201877`).
- [x] Eventos dos nós: `ValueChanged`, com a origem, e `BindFailed` (P1.4, P1.10; commit `eb497c6`).
- [x] Evento do objeto do grupo trocado por fora: `ObjectReplaced` (P1.4, P3.6; commit `55e7173`).
- [ ] Cache do modelo de tipo: por enquanto só a lista de membros (P5.6; sessão própria).
- [x] Ordem de declaração dos irmãos, se der para recuperar sem muito custo (P5.1; commit
      `6c17a15`).
- [ ] Coleções pelo conteúdo, com um seletor (combo box) ou um editor de lista (P5.2; P5.10).
- [ ] Tipos com mais de um editor, como o `Color`: escolha explícita e, sem ela, uma linha
      `Display` com aviso (P5.5; P5.8).

Apresentação

- [ ] Passo de layout agnóstico que gera os retângulos de cada linha.
- [ ] Views WinForms e WPF: editores por tipo, scrubbing, grupos recolhíveis, cabeçalho e scroll
      (sem paginação), percorrendo a árvore (P7.3).
- [ ] Válvula de escape por plataforma para ajustar o controle criado, e um callback agnóstico ao
      fim de cada linha (P7.4).

Pendências da primeira revisão (já conhecidas)

- [x] Objeto intermediário null no `bind`: o bind não lê mais nada; `GetValue` devolve null e
      `SetValue` lança dizendo qual pai é null (commit `2a1cf94`).
- [x] Setter privado e campo `readonly` gravados pelo `SetValue`: agora ele respeita o `ReadOnly`
      que as opções marcam (commit `6117bb1`).
- [x] `FieldDescriptor.Type` com o tipo dono e o namespace `Binding` escondendo o tipo `Binding` do
      WinForms e do WPF: os dois saíram (commit `5560223`).
- [ ] Membro escondido com `new`: não quebra mais o `Create`, mas aparece duas vezes, e o indexador
      acha o do tipo derivado; desde o commit `6c17a15`, o da base vem primeiro, e o indexador
      pega o último com o nome. Só quando o tipo muda: com o mesmo tipo
      (`public new int Value`), aparece uma vez só (testado). Decidido (P5.4): os dois aparecem,
      com o nome composto, que expande nos campos do tipo derivado (P5.7); falta a árvore (P5.9).
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
  `RequireExpandableAttribute`. Decidido (P1.5, P4.4) e aplicado no commit `ffee3a7`: o `Create`
  trava as opções globais, e mudar uma delas com um inspector vivo lança; a trava cai no `Dispose`
  do último. O TuxHost descarta cada inspector depois de imprimir, para poder ligar a flag.
- `InspectorOptions` (`inspector.Options`): existe desde o commit `e136f82`, com a cultura
  (`Culture`; null é a atual, na hora de cada conversão), e desde o commit `18dc069` com o modo de
  controle do binder (`BinderControl`, `Automatic` por padrão). As de layout (altura de campo,
  espaçamento, recuo...) chegam com o passo de layout e sobrescrevem o global por inspector (P1.5).
- Por campo: propriedades do próprio nó (`InspectorNode`): `Label`, `Tooltip` (curta), `Help`
  (longa, para o `(?)`), `Order`, `Ignored`, `Visible`, `ReadOnly`, `Editor` (`EditorKind`), `Range`
  (`NumericRange`), `ScrubMultiplier`, `Expandable`, `Collapsed` e as listas `TextRules` e
  `ValueRules` (commit `e136f82`); mais `Path` e `IsGroup`, que vêm da árvore. O `Visible` é da
  view, e o `Ignored`, da árvore (P6.1); desde o commit `13534b0`, o `Visible` e o `ReadOnly` são
  lidos pelos pais.

**A pilha** (fixa e nessa ordem; cada camada só mexe no que decide, e a seguinte sobrescreve)

1. `ReflectionPolicy`: rótulo = nome do membro; editor pelo tipo (números → `Number`, `bool` →
   `Toggle`, enum → `Choice`, texto → `Text`, objetos → `Display`); `ReadOnly` quando não há setter
   público (setter privado, `init`, campo `readonly`); objeto aninhado expansível, a menos que a
   flag global exija o atributo. Um setter não público esconde o membro (relatório, 3.6; P4.5;
   commit `13534b0`), e o `[InspectorReadOnly]` o traz de volta.
2. `AttributePolicy`: os dez atributos `[Inspector*]` da seção 3.2. O `[InspectorExpandable]` vale
   no membro ou no tipo.
3. Manual: o que for definido no inspector depois do `Create`.

`Ignored`, `Visible`, `Order` e `Expandable` valem nas linhas (`Rows`, commit `b456398`), que são o
que a view mostra, então podem mudar a qualquer momento, inclusive depois do bind: um nó ignorado
sai com a subárvore; um objeto que não é expansível aparece como campo `Display`, sem os filhos;
irmãos saem por `Order`. Nos empates vale a ordem de declaração, desde o commit `6c17a15` (P5.1). A
reflection devolve primeiro as propriedades, depois os campos, e os membros do próprio tipo antes
dos herdados (testado), então a ordem sai dos metadados: os tipos base vêm primeiro; dentro de um
tipo, os campos ficam na ordem deles, e cada propriedade automática fica no lugar do campo de apoio,
que o compilador emite onde a propriedade é declarada. Uma propriedade calculada não tem campo de
apoio, então vai para logo antes da próxima propriedade automática, ou para o fim; um override fica
onde a propriedade foi declarada primeiro. Se a ordem não puder ser lida, fica a da reflection, e a
falha sai como contornado. O nó ignorado continua na árvore,
então a camada manual pode trazê-lo de volta (`Ignored = false`), mesmo quando foi o
`[InspectorIgnore]` que o escondeu.

**Configuração**: o `Create` já aplica as duas políticas, e o que se define depois, no próprio
inspector, tem a palavra final. O indexador recebe um caminho relativo ao nó em que é chamado e pode
ser encadeado; o seletor por expressão chega ao mesmo nó (`inspector.Node<Foo>(f => f.Moo.MooX)`,
commit `e06b6f7`). Não há `map`, `Modify`, provider nem callback.

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

Desde o commit `b456398`, a enumeração entrega a árvore inteira, então a regra alcança também os
nós ignorados e os de dentro de objetos não expansíveis (antes ela só via as linhas da view). Para
percorrer só o que a view mostra, `inspector.Rows`.

**Exceções**: um caminho desconhecido lança `KeyNotFoundException` com o nome do nó em que a busca
começou e o caminho (`'Foo' has no field at path 'Moo.Nope'.`). Exceções das políticas e de
atributos inválidos (por exemplo `[InspectorRange(10, 1)]`) sobem sem tratamento. Revisto pela
premissa (seção 0; 3.11): uma exceção dentro da descoberta vira evento e fallback, e só o uso errado
da API continua lançando; o atributo inválido é ponto fraco, e o `Create` segue sem ele (P0.2).
Aplicado no commit `1535874`.

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

**Confirmado em 27/09** (P4.3): com a flag global ligada, a permissão de expandir vale por membro ou
por tipo e não passa para os níveis de baixo, porque a flag existe justamente para não propagar. No
TuxHost, `Boo.Details` expande pelo atributo, mas `Details.Doo` não, porque o tipo `Doo` não tem o
atributo. É o comportamento do `TypeSafeLock` do original. Um atributo que propague fica como
ideia.

**Próximos cortes**: itens de escolha, visibilidade condicional, nós manuais (botão e campo só de
exibição) e o layout no `InspectorOptions`. Já entraram o `ValueChanged` (commit `eb497c6`) e os
sanitizadores (commit `e136f82`); o gancho de conversão saiu da lista (P2.9).
