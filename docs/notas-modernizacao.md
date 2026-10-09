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

As respostas de 27/09, de 29/09 e de 01/10 às perguntas em aberto entraram aqui, com o número da
pergunta: `P2.2` é a pergunta 2.2 de `perguntas-em-aberto.md`, e um número sem o `P` é uma seção
destas notas. O que ainda depende de resposta continua naquele arquivo. A última rodada de 29/09
aceitou as sugestões das perguntas que sobravam (P1.14, P5.9, P5.10, P6.7 e P7.5) e as escolhas que
eu tinha deixado para ele confirmar. A de 01/10 aceitou as sugestões das cinco escolhas que
sobraram ao aplicar a 5.10 e a 7.5 (P4.7, P5.11, P5.12, P5.13 e P7.6), e a de 02/10, as da 6.2 e da
6.3, explicadas a partir do código original (3.12). Ainda em 02/10, ele aceitou a direção do que
falta (as views antes das sessões próprias, na ordem da seção Views abaixo) e as sugestões das
escolhas da view WinForms (P7.7 a P7.13). Depois de ver a view no Windows, ele pediu e confirmou o
espaçador e a largura máxima (P7.14) e o `(?)` da ajuda longa (P7.15), e respondeu as escolhas do
corte 3, o scrubbing e os valores mistos (P7.16 a P7.19). Em 09/10, a pedido dele ("pode fazer
isso?"), a branch das views novas entrou nesta, com a troca dos primitivos pelos da PixieLib e um
projeto por framework; no mesmo dia ele aceitou as sugestões das escolhas que eu fiz nisso (P7.20 a
P7.23).

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
  para quem chamou. Aplicado no `Create` no commit `9db2e52` (`FailureSeverity`, 3.11).
- **Lançar ou avisar** (P0.2): o uso errado da API por quem chama lança na hora, sem mudar o estado
  do inspector: ligar duas vezes, ligar outro tipo, gravar num grupo ou num ramo comprometido, um
  caminho desconhecido, mudar uma opção global com um inspector vivo. Um ponto fraco avisa por
  evento e segue com um fallback. O atributo inválido é ponto fraco: o `Create` segue sem ele
  (commit `9db2e52`).

### Estrutura

- **Sem compromisso de compatibilidade.** O rework não vai ser portado para nenhum app real, e o
  OverlayApplication vai ser reescrito do zero; o uso real (1.2) serve só de referência.
- **Um projeto; dois alvos quando as views chegarem** (P7.1). Hoje a biblioteca tem só `net10.0`,
  porque não tem código de Windows (commit `cead7b1`, 3.7). Com as views, volta o
  `net10.0;net10.0-windows`, com o código delas só no alvo `-windows`, pelo bloco condicional: o
  núcleo continua rodando fora do Windows, e cada consumidor recebe uma DLL só, a do alvo dele.
  Substitui o "as views entram nesta mesma DLL, com um alvo só" da decisão anterior. Os dois alvos
  voltaram no commit `e6cca32`, por enquanto com as conversões dos primitivos (3.7). Substituído em
  09/10 pelo projeto por framework, logo abaixo.
- **Um projeto por framework** (P7.20, 09/10; proposta minha, aceita): o núcleo volta a ter um
  alvo só, `net10.0`, e cada framework de interface tem um projeto ao lado dele, com o nome do
  projeto como namespace: `InteractiveEditor.WinForms` e `InteractiveEditor.Wpf`
  (`net10.0-windows`), `InteractiveEditor.Avalonia` e `InteractiveEditor.ImGui` (`net10.0`). Os do
  Avalonia e do ImGui vieram assim da branch das views novas, e as views do rework mudaram para os
  outros dois. O que as views decidem igual (`Views/ViewRules.cs`) continua `internal` no núcleo,
  e os quatro projetos o veem pelo `InternalsVisibleTo`, até aparecer uma view de fora, para não
  fixar uma API que ainda pode mudar; a `CultureInUse`, que todo host que mostra valores precisa,
  ficou pública (P7.21, commit `ad79532`).
  Quem usa uma view recebe três DLLs: o núcleo, a do framework (a view) e a `PixieLib` (os
  primitivos). Aplicado nos commits `36f0afa` (o merge) e `424f6d0` (3.7).
- **TuxHost para as verificações, NoHost local**: o TuxHost (`net10.0`) roda em qualquer sistema e é
  o console de verificação versionado; o NoHost voltou a ser só para os seus testes, em
  `net10.0-windows`, versionado como na `main` e com a pasta no `.gitignore`. Aplicado nos commits
  `4c4fe9e`, `b1eba84` e `1e07192`. Em 09/10 entrou o `TerminalHost` (Terminal.Gui, da branch das
  views novas) ao lado do NoHost, e não no lugar dele (commit `36f0afa`).
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
  raiz nem existe (P3.5). Liberado na P9.3 (29/09) e aplicado no commit `1997209`.
- **Id único** (P1.9): cada inspector tem um id, para relacionar um evento ao inspector que o
  disparou. É também o que a trava das opções globais guarda (P1.11). Aplicado no commit
  `1997209`: um `int` sequencial, dado no construtor.
- **Setter abstrato de volta** (P1.2), como no main: o getter é comum, e cada tipo de nó decide o
  setter, com um tipo de nó por comportamento fixo na criação (membro e, depois, botão). Grupo ou
  folha continua decidido em tempo de execução dentro do nó de membro, porque o `Expandable` pode
  mudar depois do `Create`. A ver como fica no código. Aplicado no commit `1997209`: `InspectorNode`
  é abstrato, o `GetValue` é o mesmo para todos (resolve o nó contra o objeto ligado), o `SetValue`
  é abstrato, e os tipos são `MemberNode` (campo ou propriedade) e `RootNode` (interno).
- **Enumeração** (P1.3): o inspector continua enumerável e entrega todos os nós, inclusive os
  ignorados e os de dentro de grupos fechados; as linhas da view saem de um percurso à parte,
  `Rows`. Aplicado no commit `a8bf9db`, no nó, e com ele no inspector.
- **Eventos** (P1.4), sem economia: no inspector, a criação, a descoberta e o ciclo do bind; nos
  nós, `ValueChanged` (no lugar do `ValueApplied`), o objeto do grupo trocado por fora e a falha de
  bind. A lista está na 3.10, e os nomes são exemplos. Os da criação são estáticos, para dar para
  assinar antes do `Create`, e o resultado da criação fica guardado no inspector (P1.9; aplicado no
  commit `9db2e52`: `DiscoveryFinished`, `DiscoveryFailed`, `Created` e `inspector.Report`). O
  `ValueChanged` vale nos dois sentidos e diz de onde veio a mudança, e os nomes seguem a convenção
  do .NET: o evento é `Created`, e `OnCreated` é o método que o dispara (P1.10).
- **Descartável** (P1.5): o `Inspector` e os nós implementam `IDisposable`, já pensando em campos de
  imagem e de recursos. O `Dispose` também solta a trava das opções globais (abaixo). Aplicado no
  commit `bc4491e`: descartar um nó descarta o ramo dele, e descartar o inspector desliga o objeto,
  descarta a árvore e solta a trava; depois disso, `Bind` e `Rebind` lançam
  `ObjectDisposedException`.
- **`TypeBinderMode` continua** (P1.7), porque nem todo inspector vai ser tipado. O modo sai do
  jeito de criar: tipado é automático, e sem tipo é manual; o modo explícito continua para o
  inspector tipado e manual (P1.12). O tipado e manual entrou no commit `056934f`
  (`Create<T>(TypeBinderMode.Manual)` e `Add("X")`).
- **O inspector sem tipo** (P1.14, 29/09): `Inspector.Create()` é manual, e o `Add("X")` procura o
  membro pelo nome no bind (P1.12). Ele não tem as camadas de reflection e de atributos, que
  dependem de um tipo: só a manual, e o bind completa o que ficou sem escolha (o editor, se ainda
  for `Auto`, sai do tipo do membro, e um membro sem setter público fica somente leitura). O
  primeiro bind fixa o tipo, como no tipado (P2.2): um objeto de outro tipo lança, até o `Unbind()`
  soltar tudo e deixar o próximo bind escolher de novo. Um nome que o objeto não tem lança no bind.
  Aplicado no commit `d1bf798` (3.2).
- **Nós manuais** (P1.6): o botão entra, com uma ação no clique; o cabeçalho não, porque dá para
  resolver de outro jeito. O campo só de exibição também entra, com um getter (29/09). Aplicado no
  commit `42e2129`: `AddButton(nome, texto, ação)` e `AddDisplay(nome, getter)`, no inspector ou em
  qualquer nó (3.2).
- **Controle do binder** (P1.8): no lugar de uma flag `AutoApply`, um enum de controle (manual, ou
  automático num sentido ou nos dois) e métodos auxiliares de força: gravar os valores no objeto,
  recarregar do objeto e limpar a view (tudo vazio ou zero), que também podem ser disparados por um
  evento quando algo sai do normal. Não se confundem com o `Refresh()` (P2.6), que é o fluxo
  normal: os de força são override ou fallback, mesmo quando fazem a mesma coisa. Formato (P1.13):
  um enum de flags sem combinação inválida (`Manual = 0`, `ViewToInstance`, `InstanceToView` e
  `Automatic`, os dois sentidos), mais um fluxo normal para ler e gravar à mão; os de força ficam
  só para quando o fluxo normal falhou ou não se encaixa, cada um com o seu evento. Aplicado no
  commit `121520a` (3.3): o fluxo normal é o `Apply()` e o `Reload()`, e os de força são o
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
  corte aplicado no commit `1ec81c0` (seção 7). As opções por inspector sobrescrevem as globais
  (P1.5). O `InspectorOptions` existe desde o commit `7f3cb63`, com a cultura, e tem o modo de
  controle do binder desde o commit `121520a`.
- **Opções globais travadas** (P1.5, P4.4): o `Create` congela o `GlobalOptions`. Enquanto houver um
  inspector vivo, mudar uma opção global lança exceção; a trava cai no `Dispose` do último. Ela
  guarda os ids dos inspectors vivos (P1.11), e a mensagem da exceção diz quais são. Aplicado no
  commit `bc4491e`; um `Create` que falha solta a trava antes de lançar.
- **Chaves em string**: caminho relativo ao nó em que o indexador é chamado (`"Moo.MooX"`), que
  também pode ser encadeado (`inspector["Moo"]["MooX"]`); sem atalho pelo nome do tipo. O seletor
  por expressão também vai existir, ao lado do caminho em string (P6.5). Aplicado no commit
  `c8a2720`: `inspector.Node<Foo>(f => f.Moo.MooX)`, e o mesmo em qualquer nó.
- **Sem configurador**: as opções são propriedades do próprio nó, e a configuração é feita no
  inspector depois do `Create` (`inspector["x"].Label = ...`). Nada de `map`, `Modify`, provider ou
  callback. Aplicado no commit `25b0ee0` (seção 7).
- **`Ignored` é da árvore, `Visible` é da view** (P6.1): um nó ignorado não faz parte deste
  inspector; um nó invisível faz parte, mas está escondido agora, e os filhos vão junto. Aplicado
  no commit `095ad28`: o `Visible` é lido pelos pais, e as `Rows` pulam o ramo invisível.
- **Filtro por nome** (P6.4): pode ser injetado, e é resolvido com a mesma precedência dos
  atributos. Vale por tipo, no `Create` (P6.6). É injetado de fora da classe, por tipo, antes do
  `Create`, e fica travado como as opções globais (P6.7): `GlobalOptions.Hide<Rectangle>("Text")`.
  Serve também para tipos de terceiros, que não dá para anotar, e a camada manual ainda traz o
  membro de volta (`Ignored = false`). Aplicado no commit `5d53c71`: vale para o tipo e os
  derivados, em qualquer ponto da árvore, e `Unhide<T>` tira nomes.
- **Visibilidade condicional** (P6.2, 02/10): uma regra no nó, sobre o objeto ligado, no lugar do
  `VariablePool`: `component["Text"].VisibleWhen = c => ((ComponentPreset)c).type == "Text"`. Ela é
  lida junto com o valor (no bind, no `Refresh()`, num aviso do objeto e depois de uma gravação) e
  guardada, para a view e o layout não lerem o objeto; quando o resultado muda, a view fica sabendo
  por evento. Em multi-bind, a linha só aparece se a regra vale para todos os objetos, e o `Visible`
  à mão continua valendo junto. A regra fica no inspector, e não no objeto, e o inspector a reavalia
  sozinho, então mostrar de novo não depende de ninguém lembrar (3.12). Aplicado no commit
  `bdf7ef8` (3.2).
- **Lista de escolha** (P6.3, 02/10): o `Choices` no nó, uma função que devolve os valores, lida
  quando a view abre a lista (então a lista pode mudar com o objeto), com o editor `Choice`; o valor
  escolhido é gravado como qualquer outro, convertido para o tipo do membro. Era o que faltava do
  que o OverlayApplication fazia pelo `EditField()`; o seletor de fonte fica para a válvula da P7.4,
  até aparecer de novo (3.12). Aplicado no commit `cf909c1` (3.2).
- **Expandir com a flag global** (P4.3): a permissão vale só para o membro ou tipo marcado, e os
  níveis de baixo continuam fechados, porque a flag existe justamente para não propagar. Um
  atributo que propague fica como ideia (seção 7).

### Binding

- **Binding pela cadeia de pais**: só a raiz guarda a instância, e cada nó lê e grava pelo pai a
  cada chamada. Structs são gravadas de volta no dono, e `SetValue` com um pai null lança exceção.
  Aplicado no commit `74cd664` (3.3).
- **Cinco operações** (P2.1): `Bind`, `Unbind()`, `Rebind`, `AddBind` e `RemoveBind`. Ligar lança
  exceção se já houver objeto ligado; `Unbind()`, sem parâmetro, solta tudo; religar é desligar e
  ligar; e o multi-bind põe e tira objetos. O ligar que lança é o `IsTypeBound` do main, que o
  commit `74cd664` também tirou (3.3). `Bind`, `Unbind()` e `Rebind` aplicados no commit `62af47f`;
  `AddBind` e `RemoveBind` aplicados no commit `e707583`, com o multi-bind; o `Rebind` passou a
  aceitar um objeto ou vários.
- **Um tipo só** (P2.2, P2.3): ligar, religar ou pôr no multi-bind um objeto de outro tipo lança.
  Os tipos derivados ficam para uma conversa própria, se aparecer motivo. No `Bind` e no `Rebind`
  (commit `62af47f`), um objeto de um tipo derivado passa, porque a árvore do tipo base serve para
  ele; é o caso do editor de `ComponentPreset` do OverlayApplication, ligado a retângulos e elipses.
  O `AddBind` segue a mesma regra (commit `e707583`), e o mesmo objeto duas vezes lança.
- **Tirar o último objeto** equivale ao `Unbind()`, com o mesmo evento (P2.5; commit `e707583`).
- **Valores mistos** (P2.4): sem scrubbing, a linha indica que as instâncias têm valores
  diferentes; com scrubbing, ela mostra o valor da primeira, e o delta vale para cada uma. O
  `GetValue` devolve o valor da primeira instância, `IsMixed` diz se elas diferem, e `GetValues`
  devolve um valor por objeto (P2.10). Aplicado no commit `e707583`; o indicativo e o scrubbing
  ficam com a view.
- **Objeto → UI** (P2.6): `INotifyPropertyChanged` para quem implementa, e o `Refresh()` do
  inspector para o resto. O `Refresh()` e o `ValueChanged` com a origem entraram no commit
  `305952f`, e o `INotifyPropertyChanged` no commit `8543362`.
- **Texto → valor** (P2.7): a cultura é configurável no inspector inteiro, e toda entrada de texto
  cru tenta virar o tipo do membro; quando não dá, a linha mostra a falha. O `SetValue("5")` num
  `int` também tenta converter antes de gravar (relatório, seção 6). A cultura padrão é a atual, e
  a conversão usa o `IParsable<T>` quando o tipo implementa, e o `TypeConverter` no resto (P2.11).
  Aplicado no commit `7f3cb63`, no `SetValue`; a falha fica no `Failure` do nó (3.3).
- **Quando a view grava** (P2.12): texto e número no Enter e quando o controle perde o foco, com o
  Esc voltando ao valor do objeto; toggle e escolha na hora; slider e scrubbing enquanto arrastam.
  Uma conversão que falha deixa o texto como foi digitado, e o objeto mantém o valor.
- **Faixa** (P2.8): o `SetValue` limita o valor ao `[InspectorRange]`: com (0, 255), 999 grava 255.
  Aplicado no commit `7f3cb63`.
- **Sanitizadores** (P2.9): por campo, numa lista ordenada, e executados sempre nessa ordem. O
  gancho de conversão (o "TheBrute") não volta. São duas listas, as regras de texto antes da
  conversão e as de valor depois, então a ordem entre as duas vem da estrutura (P2.13). Aplicado
  no commit `7f3cb63`: `TextRules` e `ValueRules` no nó.
- **`ReadOnly` passa para os filhos** (P4.1, P4.2): um objeto aninhado somente leitura, por atributo
  ou por acessor privado, deixa os filhos somente leitura, e forçar a gravação num filho lança. O
  `ReadOnly` do próprio nó basta para a view, sem um segundo valor como `IsEffectivelyReadOnly`.
  O nó consulta os pais na hora da leitura (P4.6): uma mudança na camada manual vale na hora para o
  ramo todo, e um filho não reabre enquanto o pai for somente leitura. Aplicado no commit
  `095ad28`.
- **Setter não público some na reflection** (relatório, 3.6; P4.5): o membro com setter private,
  protected ou internal deixa de aparecer, e o `[InspectorReadOnly]` o traz de volta, somente
  leitura. `init`, campo `readonly` e só getter continuam aparecendo, somente leitura. Aplicado no
  commit `095ad28`; o `Boo.Secret` saiu da saída do TuxHost, que ficou com 67 linhas.
- **Coleção só com getter** (P4.7, 01/10): numa coleção guardada por referência, o getter sozinho
  só impede trocar a coleção inteira; os itens e as operações da lista continuam editáveis, e o
  `[InspectorReadOnly]` continua travando tudo. O objeto aninhado fica como a P4.1 decidiu, e a
  coleção struct também, porque mudar um item dela é gravá-la inteira. Aplicado no commit
  `3fd3a8c` (3.2).

### O objeto do grupo

- **O objeto de um grupo não é trocado pelo inspector**: só os filhos editam. Vale para o grupo
  aberto (P3.1); uma class mostrada fechada, com editor próprio, é editada trocando o objeto. No
  main isso vinha do `Inspector.SetValue`, que lançava exceção; o commit `74cd664` tirou essa
  proteção sem registrar (3.10), e o commit `739850b` a trouxe de volta.
- **A raiz também** (P3.5): o objeto ligado só muda pelo bind. Desde o commit `1997209`, o
  `Inspector` nem tem `SetValue`; o `RootNode` interno lança se alguém chegar nele pelo `Parent`.
- **Troca por fora compromete o ramo** (P3.3, P3.4): uma troca (`foo.Moo = new Moo()` depois do
  bind) não pode derrubar o inspector. Ela é detectada no `Refresh()`, o caminho natural, e também
  numa leitura. No ramo comprometido, `GetValue` e `SetValue` lançam, e religar restaura o objeto
  inteiro. O raio é só o ramo trocado, e o resto do dono continua funcionando; o ramo fica
  desativado, e um evento deixa quem assina aceitar o objeto novo na hora (P3.6). O grupo de uma
  struct fica fora da detecção, porque a struct não tem identidade para comparar (P3.2). Aplicado
  no commit `77dda91`.

### Descoberta

- **Um membro, um nó**: a descoberta monta a árvore direto; um `MemberNode` é qualquer membro, com
  ou sem filhos, e o `Inspector` guarda a raiz (um `RootNode` interno, desde o commit `1997209`).
  As duas políticas viram classes estáticas, sem interface nem instâncias, chamadas em ordem no
  `Create`. Aplicado nos commits `cead7b1` e `faf7de7`.
- **Ordem dos irmãos** (P5.1): a de declaração é a preferida, se der para recuperar sem muito
  custo; se não der, fica a da reflection, e o `[InspectorOrder]` resolve o resto. Aplicado no
  commit `bb1184b`, pelos metadados (seção 7). Confirmado em 29/09: os membros do tipo
  base vêm antes dos do derivado, e uma propriedade calculada, que não deixa rastro da posição
  entre os campos, vai para logo antes da próxima propriedade automática.
- **Coleções** (P5.2; relatório, seção 2): em vez dos membros do tipo da coleção (`Capacity`,
  `Count`, `Length`...), o conteúdo. O editor padrão é um seletor, que vira combo box, e vale
  tentar um editor de lista. Pode precisar de configuração a mais. O seletor escolhe o item que
  aparece embaixo, para editar (P5.10): `Items[0]` ou `Items[1]`, com os campos dele. O editor de
  lista (uma linha por item, com adicionar, remover e reordenar) é uma escolha explícita, com um
  tipo de editor novo (`[InspectorEditor(EditorKind.List)]`). A primeira parte entrou no commit
  `4a55bd0`: a descoberta não abre mais uma coleção (qualquer coisa enumerável que não seja
  string), e o `Add` recusa os membros dela. O seletor entrou no commit `ba26fe6` (3.2): a coleção
  é um `CollectionNode`, com o editor `Selector`, e o item escolhido aparece na linha logo abaixo,
  `Items.Item` (um `ItemNode`), com os campos do item embaixo dela. A escolha segue o objeto
  escolhido quando ele muda de lugar (commit `6951033`), e o editor de lista entrou no commit
  `97c1e7d`: `EditorKind.List`, com `AddItem()`, `RemoveItem(i)` e `MoveItem(de, para)` no nó da
  coleção. Respostas de 01/10: a linha do item fica, uma regra só para toda coleção (P5.12); a
  faixa e o scrubbing do membro da coleção passam para a linha do item (P5.11; aplicado no commit
  `d681dee`); e as operações da lista gravam na hora, qualquer que seja o controle do binder, como
  o botão (P5.13).
- **Propriedades calculadas** (P5.3; relatório, seção 2) entram, e o acessor roda por inteiro, como
  em `int X { get { DoSomething(); return _x; } set => _x = value; }`. Para esconder, só o
  `[InspectorIgnore]`.
- **Membro escondido com `new`** (P5.4): os dois aparecem, e nesses casos o nome composto pode ser o
  padrão (`Derived.Value` em vez de `Value`). Na P5.7: o nome composto expande nos campos do tipo
  derivado, e no exemplo `Derived.Value` é o `string Value`, não o `int Value`. A árvore (P5.9):
  só os membros escondidos ganham um nível com o nome do tipo que os declara (`Base.Value` e
  `Derived.Value`), e o resto fica direto no dono, como hoje; `inspector["Value"]`, sem o tipo,
  acha o do derivado, como no C#. Aplicado no commit `5a10ed6`, com um nó de grupo por tipo
  (`TypeGroupNode`), que não tem valor próprio: os membros dele leem e gravam o objeto onde o grupo
  está. O seletor por expressão segue o membro da expressão, então `Node<Base>(b => b.Value)` chega
  ao do tipo base.
- **Tipos com mais de um editor** (P5.5), como o `Color`: são casos de borda, e o comportamento tem
  que ser escolhido explicitamente (expandir em campos int, texto hex ou um seletor aberto por um
  botão). Também é um motivo para usar os primitivos próprios, sem o excesso de propriedades do
  `System.Drawing.Color`. Sem escolha, o membro aparece numa linha `Display`, só leitura, até
  alguém escolher, com um aviso de diagnóstico (P5.8). Aplicado no commit `47b0997` para o
  `System.Drawing.Color`, o `PxColorArgb` e o `PxColorHsl`: sem escolha, a linha fica fechada, com
  os campos ainda na árvore, e o `Create` avisa cada um como contornado. A escolha é o
  `[InspectorEditor]` ou o `[InspectorExpandable]` no membro, o `Editor` ou o `Expandable` no nó,
  ou quem assina o `DiscoveryFailed`, no meio do `Create`, marcando o aviso como tratado.
  Confirmado em 29/09: o "só leitura" é o editor `Display`, que a view não edita, e o nó não fica
  `ReadOnly`, para que expandir nos campos funcione sem mais um passo.
- **Cache** (P5.6): por enquanto, só a lista de membros por tipo; o desenho do cache fica para uma
  sessão própria.

### Views

- **A ordem** (02/10): as views vêm antes das sessões próprias (a PixieLib e o cache do modelo de
  tipo), e o WinForms antes do WPF, porque o original e o OverlayApplication são WinForms. Em
  cortes, um por vez: (1) os dois alvos de volta, com as conversões dos primitivos com o WinForms e
  o WPF (P7.1); (2) a view WinForms percorrendo a árvore pelo passo de layout, com a fábrica, um
  controle por `EditorKind` e a gravação da P2.12 (P7.2, P7.3); (3) o scrubbing no rótulo e o
  indicativo de valores mistos (P2.4); (4) as válvulas de escape (P7.4) e a premissa de erros nas
  views (3.11); (5) a view WPF, pelo mesmo caminho.
- **Verificação das views** (02/10): o WinForms e o WPF rodam no Linux pelo Wine, numa tela
  virtual, com print e com clique e digitação simulados (testado com um app de cada, montado pelo
  `Layout`). O comportamento é verificado aqui; a aparência final, que no Wine sai com fontes
  substitutas e sem o tema visual, ele confere no Windows, uma vez no fim de cada corte.
- **Fábricas com nomes distintos** por plataforma (P7.2), como `CreateWinFormsView` e
  `CreateWpfView`.
- **A forma da view WinForms** (P7.7, 02/10): `WinFormsInspectorView`, um `UserControl`, criado por
  `inspector.CreateWinFormsView()`, um método de extensão, para o `Inspector` ficar igual nos dois
  alvos; o código numa pasta `Views/WinForms`, que o `net10.0` deixa de fora inteira. Descartar a
  view tira as assinaturas dela e não mexe no inspector, que é de quem o criou. O do WPF vai ser o
  `WpfInspectorView`, com nome distinto, para um arquivo que importa os dois não ter ambiguidade.
  Aplicado no commit `8dfaab4`, como as respostas de 7.9 a 7.12 (3.5).
- **A view WPF** (o corte 5, 02/10; commit `a93b75c`): `WpfInspectorView`, um `UserControl`, criado
  por `inspector.CreateWpfView()`, a view WinForms traduzida, com o mesmo comportamento e as mesmas
  checagens. O que o WPF não tem ou faz de outro jeito foi decidido por mim, sem a rodada de
  perguntas, a pedido do neko ("pode fazer"), e fica para o neko confirmar: a view é `IDisposable`,
  porque um controle WPF não tem `Dispose`; o caminho do nó vai no `AutomationId` dos controles,
  porque o `Name` do WPF não aceita ponto e o `Tag` fica livre (P7.16); o botão de cor abre o
  diálogo de cor do sistema pelo WinForms, porque o WPF não tem um; o "—" do texto misto é um texto
  sobre a caixa, porque a caixa do WPF não tem placeholder; o slider usa a faixa em double, com o
  passo como marca. O que as duas views decidem igual (o editor de cada linha, quem faz scrubbing,
  quem mostra mistos) foi para `Views/ViewRules.cs` (3.5).
- **Uma opção mudada depois de a view montar** (P7.8, 02/10): um evento no inspector quando muda
  uma opção que a view mostra, com o nó e qual opção, e só quando o valor muda de fato; a view refaz
  o layout ou a linha. As listas de regras (`TextRules` e `ValueRules`) ficam de fora. Pelo mesmo
  motivo da P6.2: mostrar de novo não pode depender de alguém lembrar. Aplicado no commit `4db6457`:
  o `OptionChanged`, com as opções do nó, o texto do botão e as de cultura e layout do
  `InspectorOptions` (3.10).
- **O controle de cada editor** (P7.9, 02/10): a tabela da 3.5.
- **Recolher um grupo** (P7.10, 02/10): uma seta antes do rótulo do grupo (▶ fechado, ▼ aberto), e
  o clique no rótulo ou na seta alterna o `Collapsed` do nó; o painel do grupo sem borda, só com o
  recuo, como no original.
- **A falha na linha** (P7.11, 02/10): o fundo do editor fica vermelho claro, e a mensagem vai no
  tooltip dele, dentro do retângulo do layout. A proteção das próprias views entrou no corte 4
  (commit `6632d73`; 3.5).
- **Avisos de outra thread** (P7.12, 02/10): a view repassa para a thread da interface o que recebe
  (`BeginInvoke`), e o núcleo continua sem saber de threads.
- **O espaçador e a largura máxima** (P7.14, 02/10): numa view larga, o editor não estica mais a
  linha inteira. Duas opções no `InspectorOptions`, que valem no passo de layout, e por isso em
  qualquer view: a largura máxima do editor (200 por padrão) e a largura máxima das linhas (sem
  limite por padrão: o inspector usa a largura que o host dá, e o host liga o limite quando quiser).
  Abaixo do máximo do editor, nada muda; o que passar dele vira um espaçador entre o rótulo e o
  editor, e os editores ficam alinhados à direita dentro da largura das linhas. Com o limite das
  linhas, o vão para de crescer numa janela muito larga, e o resto da view fica vazio à direita. A
  linha sob o mouse ganha um fundo leve no retângulo dela (`Row`, o equivalente ao backpanel da
  linha do original): o rótulo, o espaçador e o editor, a partir do recuo do nível; num grupo, só a
  linha dele. Era o "largura maior = controle menor" que ele quis evitar: a opção é do editor, e o
  espaçador é o que sobra. Aplicado nos commits `6d8b300` (o layout: `EditorMaxWidth` e `MaxWidth`)
  e `af5a584` (o fundo da linha na view WinForms).
- **O `(?)` da ajuda longa** (P7.15, 02/10; revisto no mesmo dia): só num nó com `Help`, logo antes
  do editor, numa faixa fixa antes da coluna dos editores. A faixa existe em todas as linhas quando
  algum nó da árvore tem `Help` (contando a árvore toda, e não só as linhas visíveis, para os
  editores não pularem quando a única linha com ajuda some), e as linhas sem ajuda a deixam vazia;
  sem nenhum `Help`, ela não existe e nada muda. Numa view larga, ela sai do espaçador; numa
  estreita, custa a largura dela (o `(?)` e o espaço até o editor) a todos os editores por igual,
  então nada se desalinha, e o rótulo fica com a coluna inteira. Antes ficava no fim da coluna do
  rótulo, tirando a largura dele (commit `6d8b300`); colado no editor, fica onde o olho está ao
  editar. Na cor de desabilitado, mas habilitado (no WinForms, um controle desabilitado nem mostra
  tooltip), com o cursor de mão; o clique abre uma janela modal, que bloqueia a de trás até fechar,
  com o rótulo como título, o texto do `Help` rolável e selecionável e um OK (Enter ou Esc fecham).
  O tooltip curto continua no rótulo e no editor. A marca e a janela entraram no commit `af5a584`,
  e a faixa antes dos editores, no `4e6bf5b`.
- **O delta do scrubbing** (P7.16, 02/10): o núcleo ganha duas peças gerais, e não uma operação de
  scrubbing: `ViewValues`, o que a view mostra, um valor por objeto (o par do `ViewValue`), e
  `SetValues(valores)`, um valor por objeto, cada um com o preparo do `SetValue` (regras, faixa e
  conversão) e um evento só. O valor pendente passa a ser um por objeto. A view guarda os valores do
  começo do arraste e grava "o valor do começo de cada objeto + o delta total": calcular a partir do
  começo, e não somar a cada movimento, é o que deixa um `int` com multiplicador pequeno sair do
  lugar. Os valores do começo ficam no objeto da linha da view, que já junta o rótulo e o editor; o
  `Tag` dos controles, que ele usava no original para isso, fica livre para quem usa a biblioteca.
  Aplicado no commit `31df507` (o núcleo) e usado pela view no `ce15b9f` (3.3, 3.5).
- **Quais linhas fazem scrubbing** (P7.17, 02/10): só as que têm `ScrubMultiplier`, como hoje e como
  no original; é opcional por campo.
- **O gesto** (P7.18, 02/10): o cursor ↔ sobre o rótulo de uma linha com scrubbing; o arraste começa
  depois de 3 px, para um clique não mudar nada; o mouse fica capturado; Shift multiplica o passo
  por 10, e Ctrl por 0,1; o Esc durante o arraste volta todos os objetos aos valores do começo; a
  faixa vale por objeto. A direção é uma opção por campo, ao lado do multiplicador, e não uma flag
  global (a regra da P7.17): horizontal por padrão e vertical por escolha, com o cursor ↕ e o
  arraste para cima aumentando o valor. A opção (`ScrubAxis`) entrou no commit `31df507`, e o gesto,
  no `ce15b9f`.
- **O indicativo de mistos** (P7.19, 02/10): um sinal só para a linha, o rótulo em itálico quando o
  `IsMixed` vale, e o editor num estado neutro: texto e número vazios, com "—" em cinza (digitar
  grava o mesmo valor em todos), a caixa de seleção indeterminada, a escolha sem seleção, a cor sem
  cor, com "—". Com scrubbing, o número mostra o valor do primeiro, como na P2.4, e o rótulo
  continua em itálico, para não parecer que todos têm aquele valor. Aplicado no commit `ce15b9f`.
- **O inspector descartado com a view viva** (P7.13, 02/10): um evento `Disposed` no inspector, e a
  view se esvazia ao recebê-lo; um descarte fora de ordem não derruba a view. O evento entrou no
  commit `4db6457`, e a view o usa desde o `8dfaab4`.
- **A view percorre a árvore** (P7.3): um painel por grupo, que recolhe junto.
- **Válvulas de escape** (P7.4): um callback por plataforma quando um controle é criado, e um
  terceiro, agnóstico, quando a linha inteira termina de ser montada. Se ele não puder ser
  agnóstico, são quatro, dois por plataforma. Aplicado no commit `6632d73` com quatro: o terceiro só
  seria agnóstico com os controles como `object`, e quem assinasse teria de converter. O agnóstico
  ficou nos args, genéricos no tipo do controle, no núcleo (3.5).
- **Passo de layout** (P7.5, 29/09): no núcleo, agnóstico. Ele devolve os retângulos de cada linha
  (a linha, o rótulo e o editor), e a view só os aplica; as opções de layout (altura da linha,
  espaçamento, recuo por nível) ficam no `InspectorOptions`. Assim ele é testado no Linux, sem
  WinForms nem WPF. Aplicado no commit `9674fca` (3.4): `inspector.Layout(largura)`, com o
  resultado em árvore, um painel por grupo, como a view vai percorrer (P7.3). Confirmado em 01/10
  (P7.6): os editores numa coluna só, com o rótulo perdendo o recuo a cada nível, e os valores
  padrão como estão.
- **As views do Avalonia e do ImGui e o host do Terminal.Gui** (09/10): vieram da branch
  `claude/vibrant-fermi-smwjw5`, feitas sobre o núcleo antigo (o `Fieldset`), e foram refeitas
  sobre o do rework nos commits `6ba0290` (o `TerminalHost`), `a885789` (o Avalonia) e `1d86b96`
  (o ImGui); o `IInspectorView` e o `ValueText` da branch saíram no `6f02064`. As fábricas seguem
  a P7.2 (`CreateAvaloniaView()` e `CreateImGuiView()`), e as duas views usam as `ViewRules`: o
  editor de cada linha, os grupos recolhíveis, a falha na linha, os mistos e a view vazia no
  `Disposed`. O `TerminalHost` é um host, e não uma view: a árvore das linhas, uma caixa que grava
  pelo núcleo e um log, só com a API pública. Verificado fora do repositório: 65 checagens do
  Avalonia numa tela virtual, 43 do ImGui sem janela, e o `--dump` do `TerminalHost` com todos os
  bindings certos.
- **O que as views novas fazem diferente da WinForms** (P7.22, 09/10): nenhuma usa o passo de
  layout, porque os dois frameworks fazem o próprio (o Avalonia empilha as linhas nos painéis dele,
  e o ImGui desenha uma tabela a cada quadro). O Avalonia ganhou o que faltava, um commit para cada:
  o `(?)` com a janela (`507b528`), as válvulas com o `RowFailed` (`8853770`) e o scrubbing
  (`b0deeb2`); detalhes na 3.5. O ImGui fica sem válvulas, porque não tem controles para entregar,
  e com a ajuda no tooltip do `(?)`, que é o costume dele.
- **Os filhos de um nó** (P7.23, 09/10): o `ShownChildren`, os filhos que uma view mostra embaixo de
  um nó (sem os ignorados e os escondidos, pelo `Order`), ficou público no nó, e o inspector ganhou
  o mesmo para as linhas de cima (commit `ad79532`). O `TerminalHost` usa os dois, e a
  `CultureInUse`, em vez das cópias que tinha.
- **O rótulo de uma linha que não mostra os objetos** (09/10; commit `e5960e1`): nas três views, o
  `Show` da linha mostrava o valor e depois o rótulo no mesmo `Try`, então um `ToString` que lança
  (3.11) deixava a linha sem rótulo. Achado ao testar o `RowFailed` do Avalonia; agora os dois vão
  em separado, e a falha só sai quando os dois passam.

### Primitivos e PixieLib

- **Primitivos com prefixo `Px`**, o mesmo da PixieLib (3.9): `PxPoint`, `PxPointF`, `PxSize` e
  `PxSizeF`. Substitui o `SK` provisório do commit `eaadb07`. Aplicado no commit `bea77a1` (3.4).
- **Um tipo só, em `double`** (P8.1), como no WPF: saem as variantes int e float. Aplicado no
  commit `0acfce7`.
- **Primitivos novos** (P8.2): `PxRect`, `PxPadding` e `PxDock`. Aplicado no commit `fa223d7`.
- **Cores** (P8.3, P8.4): `PxColorArgb` e `PxColorHsl`, com o espaço de cor no fim para não juntar
  dois prefixos. Sem conversão implícita entre as duas: funções estáticas `ToHsl`, `FromHsl`,
  `ToArgb` e `FromArgb`, o que também acaba com o CS0457. Aplicado no commit `2e31357`.
- **Regras de conversão** de 3.4 confirmadas (P8.5). Aplicadas com o `System.Drawing` nos commits
  `0acfce7` e `fa223d7`, e com o WinForms e o WPF no commit `e6cca32`, só no alvo `-windows` (3.4).
- **PixieLib** (P8.6 a P8.9): a precisão padrão é `double`. Por enquanto os primitivos ficam neste
  projeto, sem `PixieLib.dll`; a mudança para a PixieLib, com o sufixo de precisão, fica para uma
  sessão própria. A sessão foi feita, e a mudança entrou em 09/10 (abaixo).
- **Os primitivos da PixieLib** (09/10; a pergunta 6.8 de lá): aplicado no commit `424f6d0`. O
  núcleo referencia a cópia da PixieLib em `external/PixieLib` (o `PixieLib` e o
  `PixieLib.Generators` do commit `7e0ec32` de lá), que o neko pôs na branch das views novas até
  haver um pacote; a cópia não se edita aqui, e o que mudar vai para a PixieLib e volta numa cópia
  nova. A pasta `Primitives` ficou só com o `PxDock`, que é vocabulário da interface. O
  `PxColorArgb` dá lugar ao `PxColorRgba`, com o alfa no fim (`new(r, g, b, a = 255)`): o
  `new(255, 0, 128, 255)` do jeito antigo compilaria e daria outra cor, que era o caso do
  `Gadget.Fill`. As conversões com o `System.Drawing` vêm da PixieLib; as com o WinForms e o WPF
  viraram métodos de extensão nos projetos de cada um (`ToWinForms`, `ToWpf` e `ToPrimitive`), como
  a PixieLib pede. Uma diferença de comportamento: a PixieLib passa um `PxRect` para o `Rectangle`
  arredondando as bordas, e não os campos; com as opções inteiras, nada se move.
- **A PixieLib pelo nuget.org** (09/10): aplicado no commit `6b7cd80`. O pacote que faltava saiu, a
  `PixieLib` 0.1.0 (`net10.0` e `net481`), e o núcleo o referencia no lugar da cópia, que saiu
  inteira com o `PixieLib.Generators`: o pacote já traz os tipos gerados, e nada daqui usa o
  gerador. Da `7e0ec32` à 0.1.0 a API pública não mudou, só os metadados do pacote e o interior do
  `PxText` (os dígitos saem do valor exato; no .NET 10 o texto é o mesmo), e nenhuma linha do editor
  mudou. O build deu 0 avisos, a saída do `TerminalHost --dump` e a do TuxHost ficaram iguais byte a
  byte, o `PixieLib.dll` dos sete hosts é o do pacote, e no Windows o WindowsHost e o WpfHost abrem
  com o `Gadget`. O que mudar na PixieLib volta numa versão nova do pacote.

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
(P6.6), e é injetado de fora da classe, antes do `Create`: `GlobalOptions.Hide<T>(nomes)`, travado
como as opções globais (P6.7). Aplicado no commit `5d53c71`: cada nome conta como um
`[InspectorIgnore]` no membro, logo depois do próprio atributo, então nem um membro que o
`[InspectorReadOnly]` trouxe de volta aparece; só a camada manual o traz (`Ignored = false`), e um
membro posto à mão (`Add`) aparece de qualquer jeito. O registro vale para o tipo e os derivados,
em qualquer ponto da árvore, e um nome que o tipo não tem lança na hora; `Unhide<T>(nomes)` tira
nomes, ou todos, sem nenhum.

**Atributos do inspector** (próprios, para não haver ambiguidade com `System.ComponentModel` ou
DataAnnotations; aplicados no commit `1ec81c0`)

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
  Aplicado no commit `c8a2720` como método genérico, `Node<T>`, porque o `Inspector` não é genérico
  e um indexador não pode ser: `inspector.Node<Foo>(f => f.Moo.MooX)` é o nó de `"Moo.MooX"`, e
  `inspector["Moo"].Node<Moo>(m => m.MooX)` vale a partir de um nó. O tipo tem que ser o que o nó
  guarda, ou um de que ele deriva; outro tipo, ou qualquer coisa além de uma cadeia de membros do
  parâmetro, lança `ArgumentException`.
- Visibilidade condicional (decidido, P6.2): o `VisibleWhen` no nó, uma regra sobre o objeto ligado,
  como `component["Text"].VisibleWhen = c => ((ComponentPreset)c).type == "Text"`. Aplicado no
  commit `bdf7ef8`. A regra é lida junto com os valores, uma vez no fim de cada leitura ou gravação
  (o bind, o `Refresh()`, o `Reload()`, uma gravação, o `Apply()`, um aviso do objeto, uma operação
  da lista e os de força), para cada objeto ligado, e a resposta fica guardada no nó: as `Rows`, o
  layout e a view nunca leem os objetos. Com vários ligados, a linha só aparece se a regra vale para
  todos, e o `Visible` à mão continua valendo junto. Uma resposta nova dispara o `VisibleChanged` do
  nó, com a origem da leitura; uma mudança no bind não avisa, como nos valores. Como a regra pode
  olhar qualquer coisa do objeto, mostrada ou não, um aviso do objeto relê todas, e a gravação do
  próprio inspector as relê no fim, então chega como `Write`. Uma regra que lança mostra a linha e
  vira falha no nó, e, sem nada ligado, nenhuma regra esconde.
- Lista de escolha (decidido, P6.3): o `Choices` no nó, uma função que devolve os valores, como
  `inspector["keyStr"].Choices = () => Enum.GetNames<Keys>()`. Aplicado no commit `cf909c1`. Ela
  aceita qualquer coleção, números também, e põe o editor em `Choice`; o valor escolhido é gravado
  como qualquer outro, convertido para o tipo do membro. A view chama o `GetChoices()` quando abre a
  lista, e ele lê a função na hora (então a lista pode mudar com os objetos); sem função, lista os
  valores do enum que o nó guarda, nullable também, ou nada. Uma função que lança, ou que não
  devolve nada, não lista nada, e a falha fica no nó. O seletor de fonte fica para a válvula da
  P7.4.
- Em vez de `FieldSet_FieldType = typeof(TextBox)`, um enum agnóstico de editor (`EditorKind`: `Text`,
  `Number`, `Toggle`, `Choice`, `Slider`, `Color`, `Button`, `Display`, `Header`, `Separator` e,
  desde os commits `ba26fe6` e `97c1e7d`, `Selector` e `List`). Cada view decide o controle.
- O que a configuração guarda: rótulo, editor, flags (`ReadOnly`, `Disabled`, scrubbing),
  multiplicadores (scrubbing e slider), sanitizadores, visível, recolhido e ação pós-bind.
- A ideia do `TypeSafeLock` (opt-in de quais tipos podem ser expandidos) continua, como atributo do
  inspector (`[InspectorExpandable]` na tabela acima).
- O `TypeBinderMode` do original (`Automatic` / `Manual`, declarado e nunca usado lá) continua
  existindo para quando se quer só os campos declarados, porque nem todo inspector vai ser tipado
  (decidido, P1.7). O modo sai do jeito de criar: tipado é automático, sem tipo é manual, e o modo
  explícito continua para o inspector tipado e manual (P1.12). Aplicado no commit `056934f` para o
  inspector tipado: `Create<T>(TypeBinderMode.Manual)` não acha nenhum membro, e `inspector.Mode`
  diz qual modo montou a árvore. O `Add("X")`, no inspector ou em qualquer nó, põe um membro do
  tipo que o nó guarda: o nome é conferido na hora, e o nó recebe o que a reflection e os atributos
  dizem dele, como no `Create`, com a camada manual depois. Ele aparece mesmo onde elas o
  esconderiam (setter não público, `[InspectorIgnore]`), porque foi posto de propósito, e vem sem os
  membros de baixo, que entram do mesmo jeito (`inspector["Moo"].Add("MooX")`). Um tipo sem
  membros para pôr (int, string) lança, como a descoberta, que nunca abre um. O inspector sem tipo
  (`Inspector.Create()`, P1.14) entrou no commit `d1bf798`. Antes do bind, o `Add("X")` só guarda o
  nome, e o `Member` do nó é null. O primeiro bind no vazio fixa o tipo do primeiro objeto e acha
  cada nome nele, de cima para baixo, antes de mudar qualquer coisa: um nome que falta lança e não
  deixa nada ligado. Depois, outro tipo lança e um derivado serve, como no tipado, até o `Unbind()`
  deixar o próximo bind achar tudo de novo no tipo dele. Sem as camadas de reflection e de
  atributos, o bind completa o que ficou sem escolha: o editor, enquanto for `Auto` ou ainda for o
  que um bind anterior escolheu (quem o define assume), e o somente leitura de um membro sem setter
  público. Com um tipo fixado, o `Add` acha o nome na hora.
- Nós manuais (decidido, P1.6): o botão entra, com uma ação no clique; o cabeçalho não, porque dá
  para resolver de outro jeito. O campo só de exibição também entra, com um getter. Aplicado no
  commit `42e2129`, com dois tipos de nó sem membro atrás. O `ButtonNode` tem o texto do botão
  (`Text`, à parte do rótulo) e roda a ação no `Press()`; uma ação que lança vira falha na linha
  (contornado), e apertar um botão somente leitura, ou num ramo desativado, lança, como gravar num
  nó somente leitura. O `DisplayNode` é somente leitura e lê o getter como um membro é lido: o
  `Refresh()` e o `Reload()` o atualizam, e um getter que lança vira falha na linha. Os dois entram
  depois dos nós que já estão ali, com `Order` valendo como em qualquer linha, e um nome que um
  irmão já usa lança, porque o indexador acha os nós pelo nome. Confirmado em 29/09: um
  membro que recebe um nó à mão abre (`Expandable`), porque o filho foi posto ali de propósito.
- Coleções (decidido, P5.2 e P5.10): o conteúdo, e não os membros do tipo da coleção. Aplicado no
  commit `ba26fe6`. Uma coleção (qualquer coisa enumerável que não seja string) é um
  `CollectionNode`, com o editor `Selector`. O `Items` lista o que a coleção do primeiro objeto
  ligado tinha na última leitura (como o `ViewValue`, segue o controle do binder), e o
  `SelectedIndex` escolhe o lugar que a linha logo abaixo, `Item` (um `ItemNode`), lê em cada objeto
  ligado. A linha do item é lida e gravada como um membro (conversão, regras, faixa e valor
  pendente) e tem embaixo os membros do tipo do item: um `List<Moo>` mostra `Items.Item.MooX`, e um
  `List<int>` mostra `Items.Item` como um número. `Item` é o nome que o C# dá ao indexador.
  Confirmado em 01/10 (P5.12): a linha do item existe para toda coleção, porque assim um item sem
  campos (número, texto) também tem onde ser editado, e um item de tipo com mais de um editor fica
  fechado como um membro ficaria.

  Escolher larga o que as linhas de baixo guardavam e as lê de novo, com a origem `Selection`; um
  lugar fora dos itens listados lança. A escolha segue os itens: o primeiro quando a coleção ganha
  itens depois de não ter nenhum (no bind, por exemplo); um objeto que mudou de lugar leva a escolha
  junto (commit `6951033`); uma struct, um null ou um objeto que saiu ficam só com o lugar, que vai
  para o último item quando passa do fim. -1 é nenhum. O `Unbind()` esvazia a lista, então o
  `Rebind` volta ao primeiro item. No multi-bind, o lugar escolhido vale em cada objeto: um objeto
  sem aquele lugar lê null, e uma gravação para antes de mudar qualquer objeto, dizendo qual lugar
  falta.

  Outro objeto no lugar escolhido não é troca: a linha mostra o que estiver lá, e larga o que as
  linhas de baixo guardavam. Uma troca por fora abaixo do item, ou da própria coleção, continua
  desativando o ramo (P3.3). Um item struct volta para o lugar dele, como uma struct volta para o
  dono. Uma coleção que não aceita um item no lugar de outro (`IEnumerable<T>`, `IReadOnlyList<T>`,
  um dicionário) tem o item somente leitura; uma lista que só é somente leitura em tempo de execução
  recusa o item antes de mudar qualquer coisa. Uma coleção guardada por referência sem setter
  público (só getter, `init` ou campo `readonly`) não fica somente leitura (P4.7; commit `3fd3a8c`):
  os itens e as operações da lista continuam editáveis, e só a troca da coleção inteira é recusada,
  no `SetValue`, no `Apply()` e no `ForceApply()`, até com `init`, que a reflection conseguiria
  chamar. Isso passa por um gancho novo do `MemberNode`, o `Locked`, que diz por que o valor do
  próprio nó não pode ser gravado. O `[InspectorReadOnly]` trava tudo, os itens junto, e a coleção
  struct sem setter continua somente leitura, como qualquer struct (P4.1), porque mudar um item
  dela é gravá-la inteira. Uma coleção que avisa das próprias mudanças (`ObservableCollection`)
  atualiza os itens sem `Refresh()`, e a gravação do próprio inspector não volta como mudança de
  fora.

  A coleção sempre mostra a linha do item, mesmo com o `RequireExpandableAttribute`; o item abre
  como um membro abriria, e um tipo com mais de um editor espera a escolha (P5.8), que ali só pode
  ser no nó (`inspector["Palette.Item"].Editor`), porque o item não tem membro para levar atributo.
  A faixa e o scrubbing do membro da coleção (`[InspectorRange]` e `[InspectorScrub]`) vão para a
  linha do item, que é onde há número para limitar (P5.11; commit `d681dee`): com
  `[InspectorRange(0, 255)]` num `List<int>`, cada item fica nessa faixa. O `[InspectorEditor]`
  continua escolhendo o editor da coleção (seletor ou lista), e o rótulo, a dica e a ordem ficam na
  linha dela. Na camada manual, as opções do item vão direto na linha dele
  (`inspector["Items.Item"].Range`). No inspector sem tipo, e como item de outra coleção, uma
  coleção continua uma linha `Display`.

  O editor de lista (P5.10) entrou no commit `97c1e7d`: `EditorKind.List`, escolhido pelo
  `[InspectorEditor(EditorKind.List)]` ou no nó, mostra uma linha por item, e o item escolhido
  continua na linha de baixo. O `AddItem()` põe um item novo no fim, em cada objeto ligado, e o
  escolhe: um objeto feito com o construtor sem parâmetros do tipo do item, ou o valor vazio do
  tipo (zero, false, texto vazio ou null). O `RemoveItem(i)` tira um, e a escolha fica com o item
  escolhido, ou passa para o que ocupa o lugar dele. O `MoveItem(de, para)` move um, com os do meio
  abrindo espaço, também num array, que não muda de tamanho; a escolha vai junto. O que não pode
  receber a operação lança antes de mudar qualquer objeto: nó somente leitura, ramo desativado,
  coleção null, uma que não muda os itens (nem cresce ou diminui, para pôr e tirar) e um lugar além
  dos itens listados ou dos itens de um dos objetos. Uma coleção que dois objetos guardam muda uma
  vez só, o que a coleção lança no meio vira falha no nó, e o aviso de uma `ObservableCollection`
  sobre a própria operação não volta como mudança de fora. Confirmado em 01/10 (P5.13): as
  operações gravam na hora, qualquer que seja o controle do binder, como o botão.

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
component["FillColor"].Editor = EditorKind.Color;   // sem escolha, Display (commit 47b0997)

component.AddButton("LayerUp", "▲", () => tree.OnLayerUp());   // commit 42e2129
component.AddDisplay("Layer", () => tree.SelectedIndex);

// cortes seguintes:
// component["Text"].VisibleWhen = c => ((ComponentPreset)c).IsText;   // sucessor do VariablePool

component.Rebind(selected);  // a cada seleção: Unbind + Bind (commit 62af47f)
component.AddBind(other);    // multi-bind (commit e707583)
```

No modo automático (o padrão), os membros refletidos entram sozinhos, os atributos ajustam e a
configuração manual tem a palavra final; com `TypeBinderMode.Manual`, entra só o que foi declarado.

### 3.3 Binding

- **Cadeia de pais** (aplicado, commit `74cd664`): só a raiz guarda a instância. Cada nó resolve o
  valor pelo pai a cada `GetValue`/`SetValue`, então nada abaixo da raiz fica velho quando uma
  referência muda. Quando o dono de um membro é uma struct, o setter altera uma cópia boxed, que é
  gravada de volta no dono dela, subindo até a primeira class ou até a raiz; vale para qualquer
  aninhamento de class e struct. Pai null: `GetValue` devolve null e `SetValue` lança
  `InvalidOperationException` dizendo qual pai é null. Struct na raiz: o inspector edita a própria
  cópia, e o host lê o resultado com `inspector.Instance`. Revisto: a troca do objeto de um grupo
  não deve ser seguida em silêncio, e sim comprometer o ramo (seção 0; 3.10). Desde o commit
  `739850b`, a gravação de volta passa por um caminho interno (hoje `WriteTo`), porque o `SetValue`
  público recusa o grupo aberto e a raiz; o `ReadOnly` continua sendo conferido na subida.
- **Ligar, desligar e religar** (aplicado, commit `62af47f`): o `bind(obj)` de novo trocando o
  objeto (commit `74cd664`, que tirou o `IsTypeBound` do main) foi desfeito. `Bind` lança
  `InvalidOperationException` se já houver objeto ligado, e a troca é explícita: `Rebind`, que é
  `Unbind` e `Bind`. Um objeto que não serve para a árvore lança `ArgumentException` na hora, em vez
  de falhar depois no `GetValue` de um nó (P2.2); um tipo derivado serve. O `Rebind` confere antes
  de desligar, então um objeto recusado deixa o anterior ligado. `Unbind()` sem nada ligado não faz
  nada. As cinco operações estão na seção 0 (P2.1); o multi-bind vem depois.
- **ReadOnly** (aplicado, commit `ab51437`): `SetValue` lança `InvalidOperationException` quando o
  nó está marcado como somente leitura, antes de ler qualquer coisa. Vale para o que a reflection
  marca (setter privado, `init`, campo `readonly`), para o `[InspectorReadOnly]` e para a camada
  manual, que continua podendo reabrir (`inspector["x"].ReadOnly = false`). Como a gravação de volta
  de uma struct passa pelo `SetValue` do dono, um membro de struct somente leitura também é
  recusado; já os membros de uma class somente leitura continuam editáveis, porque a edição é no
  próprio objeto (seção 5, item 6). Decidido em 27/09 (P4.1, P4.2): o `ReadOnly` passa para os
  filhos, também numa class, e forçar a gravação num filho lança; o `ReadOnly` do próprio nó passa
  a bastar para a view. O nó consulta os pais na hora da leitura (P4.6). E o setter não público
  (private, protected, internal) passa a esconder o membro (relatório, 3.6; P4.5). Aplicado no
  commit `095ad28`: o filho de uma struct somente leitura agora recusa a gravação pelo próprio
  `ReadOnly` (`'X' is read-only.`), antes da subida. Desde o commit `7f3cb63`, o `SetValue` confere
  o `ReadOnly` antes de converter o valor.
- **Multi-bind** (aplicado, commit `e707583`): a raiz guarda uma lista de objetos. `AddBind` põe um
  ou mais (sem nada ligado, liga), `RemoveBind` tira um, e tirar o último é o mesmo que o `Unbind()`
  (P2.5); todo objeto tem que servir para a árvore (P2.3), e o mesmo objeto duas vezes lança.
  `GetValue` lê o primeiro, `GetValues` lê um valor por objeto, e `IsMixed` diz quando eles diferem
  (P2.10). `SetValue` grava em todos, e um dono null em qualquer um deles interrompe antes de algum
  mudar. Na view (P2.4): sem scrubbing, a linha indica que as instâncias diferem; com scrubbing,
  mostra o valor da primeira, e o delta vale para cada uma, como no original. O inspector avisa por
  `BindRegistered`, `BindRemoved` e `Unbound`. No commit `121520a`, confirmado em 29/09: o `IsMixed`
  passou a seguir o que a view mostra (a última leitura, e nunca misto com um valor guardado, que
  vai para todos), sem ler os objetos, para a linha não juntar uma leitura ao vivo com o valor que
  ela mostra. Desde o commit `31df507` (P7.16): o `ViewValues` dá o que a view mostra de cada
  objeto, e o `SetValues(valores)` grava um valor em cada um, com o preparo do `SetValue` em todos
  antes de gravar qualquer um (um que falha deixa todos como estavam) e um evento só; o valor
  guardado sem `ViewToInstance` passou a ser um por objeto (o mesmo, depois de um `SetValue`), e o
  `IsMixed` lê o que a view mostra, guardados inclusive: um valor dado a todos não é misto, e os do
  scrubbing, cada objeto a partir do seu, continuam sendo. O scrubbing por delta da P2.4 é a view
  gravando "o começo de cada um + o delta" por ele.
- **Objeto → UI** (decidido, P2.6): `INotifyPropertyChanged` no lugar de `ITwoWayBinderTransmiter`.
  O objeto deixa de guardar referência ao inspector (`BindedTo`), e vários inspectors podem observar
  o mesmo objeto. Um `Refresh()` manual cobre quem não implementa a interface; ele é o fluxo normal,
  e não se confunde com os métodos de força (abaixo). Aplicado no commit `305952f`: cada nó guarda
  o que os objetos tinham na última leitura, e o `ValueChanged` só dispara quando isso muda, com a
  origem (`Write`, `Instance`, `Refresh` ou `Force`); ligar e desligar recomeça em silêncio. No
  commit `8543362`, um vigia interno (`InstanceWatcher`) assina o `PropertyChanged` dos objetos
  ligados e dos objetos dos grupos: o aviso relê o membro (`Instance`), um aviso sem nome relê
  todos, e a troca do objeto de um grupo é conferida na hora. A gravação feita pelo próprio
  inspector continua `Write`, e o que o mesmo setter muda de tabela chega como `Instance`. Desde o
  commit `ba26fe6`, o vigia assina também a coleção que avisa (`ObservableCollection`) e o item
  escolhido nela, e a escolha de outro item chega como `Selection` (3.2).
  Corrigido no commit `14307ea`: a gravação e o aviso releem também o que muda junto, que é a
  struct mais de cima do campo gravado, com o ramo dela, e os membros de baixo de uma struct ou de
  um objeto fechado; e o aviso da struct que o inspector grava de volta no dono não chega mais como
  `Instance`.
- **Controle do binder** (decidido, P1.8): um enum de controle no lugar de uma flag `AutoApply`
  (manual, ou automático num sentido ou nos dois) e métodos auxiliares de força: gravar os valores
  no objeto, recarregar do objeto e limpar a view, deixando tudo vazio ou zero. Eles também podem
  ser disparados por um evento, quando algo sai do normal. O enum é de flags, sem combinação
  inválida, e o modo manual tem um fluxo normal para ler e gravar à mão; os de força ficam para
  quando esse fluxo falhou ou não se encaixa, cada um com o seu evento (P1.13). Aplicado no commit
  `121520a`, com o modo em `inspector.Options.BinderControl` (`Automatic` por padrão):

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
  - Confirmado em 29/09: o `Refresh()` fica no sentido objeto → view, então não faz nada
    sem `InstanceToView`; o `Reload()` descarta os valores guardados, e o `Refresh()` não; o
    `ForceReload()` reativa os ramos desativados; e o `ValueChanged` também sai para um valor
    guardado (`Pending`), já que a view mudou, mesmo sem o objeto mudar.
- **UI → objeto** (decidido, P2.7): toda entrada de texto cru tenta virar o tipo do membro, com a
  cultura configurada no inspector; se não der, a linha mostra a falha. O `SetValue("5")` num `int`
  também tenta converter antes de gravar. O original usa `Convert.ToDouble` com a cultura atual, e o
  `ONLY_NUMBERS` aceita tanto `.` quanto `,`. A cultura padrão é a atual, e a conversão usa o
  `IParsable<T>` quando o tipo implementa, e o `TypeConverter` no resto (P2.11). A view grava o
  texto no Enter e ao perder o foco, e toggle, escolha, slider e scrubbing na hora (P2.12).
  Aplicado no commit `7f3cb63`, no `SetValue`, antes de tocar em qualquer objeto: o texto passa
  pelas regras de texto e vira o tipo do membro (vazio é null num membro nullable, e um enum sai
  pelo nome); um número vira o tipo numérico do membro ou um enum, e um `double` num `int`
  arredonda, como o `Convert` (7,6 grava 8). O que falha nesse preparo (texto que não converte,
  número grande demais para o membro, regra que lança) vai para o `Failure` do nó como recuperado,
  com `BindFailed`, e nada é gravado; é o que a linha mostra. Um valor de um tipo sem relação com o
  do membro (um `Moo` num `int`, null num `int`) é uso errado e lança `ArgumentException`.
- **Faixa** (decidido, P2.8): o `SetValue` limita o valor ao `[InspectorRange]`; com (0, 255), 999
  grava 255. Aplicado no commit `7f3cb63`, depois das regras de valor, para número e para texto.
- **Sanitizadores** (decidido, P2.9): por campo, numa lista ordenada, executados sempre nessa
  ordem. No original eles misturam texto e número: no caminho do `TextBox` o `POSITIVE_NUMBERS`
  recebe `string` e não faz nada; no scrubbing, o delegate multicast devolve só o resultado do
  último; e com três ou mais funções encadeadas o `GetTextboxData` reaplica funções anteriores.
  São duas listas: as regras de texto antes da conversão, e as de valor depois (P2.13). Aplicado
  no commit `7f3cb63`: `TextRules` e `ValueRules` no nó, e cada regra é uma função. As de texto só
  rodam quando chega texto, e as de valor rodam sempre, depois da conversão. As prontas são
  `TextRule.Digits`, `Number`, `MaxLength(n)` e `Only(caracteres)`, e `ValueRule.Min` e `Max`; uma
  regra que lança vira falha no nó, e nada é gravado.
- **Sem gancho de conversão** (decidido, P2.9): o "TheBrute" não volta.
- `ApplyFunction` vira o evento `ValueChanged` do nó (P1.4).
- **Eventos de ciclo de vida**: a lista decidida está na 3.10 (P1.4), com um payload de falha
  tipado (mensagem, motivo, sugestão, caminho).

### 3.4 Layout agnóstico e os primitivos

- O original posiciona cada linha à mão (`Y = índice × (altura + espaçamento)`), desloca as seguintes
  quando uma some e rola a lista movendo painel por painel. Decidido (P7.5): um passo de layout no
  núcleo, agnóstico, que produz os retângulos de cada linha (linha, rótulo, editor) a partir das
  opções, da profundidade, da visibilidade e do recolhimento. A view só aplica.
- Aplicado no commit `9674fca`: `inspector.Layout(largura)` devolve um `InspectorLayout`, com as
  linhas de cima (`Rows`) e a área que elas ocupam (`Size`, a largura dada e a altura que precisam,
  com o espaço em volta). Cada `LayoutRow` tem o nó, a profundidade e os retângulos da linha, do
  rótulo e do editor, nas coordenadas de onde a linha está: a área do inspector para as de cima, o
  painel do grupo para as de dentro. Um grupo tem o painel logo abaixo da linha dele, um recuo para
  dentro, com as linhas dele, que começam do canto do painel; é o que a view vai percorrer, um
  painel por grupo (P7.3). As linhas são as mesmas de `Rows`: sem as ignoradas e as escondidas, e
  os irmãos por `Order`. O passo não lê os objetos ligados.
- O rótulo de cada nível perde o recuo, então os editores ficam numa coluna só, com a mesma largura
  em qualquer profundidade. Um cabeçalho (o grupo de um tipo, P5.9) ocupa a linha inteira com o
  rótulo, e o editor fica sem largura no fim dela. O editor de lista tem uma linha para cada item
  que mostra, até `ListRows`, e mais uma para os botões; o rótulo fica na primeira. Um grupo
  recolhido mantém as linhas dele, e o painel fica sem altura, então as de baixo sobem; assim a
  view monta os controles uma vez e só recolhe o painel. Numa área estreita demais, o rótulo fica
  com o que houver, e o editor, sem largura.
- As opções, no `InspectorOptions`, na unidade da view (pixel no WinForms, a unidade independente
  do WPF): `RowHeight` (23, o `FieldHeight` do OverlayApplication), `RowSpacing` (2), `Indent`
  (16), `LabelWidth` (120, no nível de cima), `LabelSpacing` (4), `Padding` (4 em volta) e
  `ListRows` (5). Uma altura que não é maior que zero, ou um comprimento que não é um número de
  zero para cima, lança. A largura é do `Layout`, e não uma opção, porque é a da view na hora.
  Confirmado em 01/10 (P7.6): os editores numa coluna só e os valores padrão.
- O espaçador, a largura máxima e o `(?)` (P7.14, P7.15; commit `6d8b300`): três opções a mais,
  `EditorMaxWidth` (200; null é sem limite), `MaxWidth` (null, sem limite) e `HelpWidth` (16). O
  editor fica com o resto da linha até o `EditorMaxWidth`, encostado à direita, e o que sobra entre
  ele e o rótulo é o espaçador, que não tem retângulo; abaixo do limite, nada muda. O `MaxWidth`
  limita a largura que as linhas ocupam, com o espaço em volta, e o `Size` do layout fica com ela.
  Um nó com `Help` ganha o retângulo `Help` na `LayoutRow`. Revisto no commit `4e6bf5b` (P7.15):
  quando algum nó da árvore tem `Help`, toda linha deixa antes do editor uma faixa de `HelpWidth`
  mais `LabelSpacing`, e o `(?)` vai nela, colado no editor; o rótulo fica com a coluna inteira, e
  os editores e os `(?)` formam colunas em qualquer profundidade. Numa linha larga, a faixa sai do
  espaçador; numa estreita, tira a mesma largura de todos os editores, e numa linha sem espaço
  nenhum o `(?)` fica sem largura, sem passar por cima do rótulo. Antes, no commit `6d8b300`, ele
  ficava no fim da coluna do rótulo, que encolhia. Um cabeçalho e uma linha de separação não têm. As
  três opções avisam pelo `OptionChanged`.
- É aqui que os primitivos entram. `Location`, `Size`, `Margins` e `DockStyle` das opções eram tipos
  do `System.Drawing` e do WinForms; no rework viram primitivos próprios.

**Nomes** (prefixo `Px`, decidido: o mesmo da PixieLib, ver 3.9)

| Antes | Agora | Situação |
|---|---|---|
| `Point`, `PointF` | `PxPoint` | Aplicado (`eaadb07` com `SK`, `bea77a1` com `Px`); um tipo só, em `double`, no commit `0acfce7` (P8.1) |
| `Size`, `SizeF` | `PxSize` | Aplicado; idem |
| `Color` | `PxColorArgb` | Aplicado (`ArgbColor` no `eaadb07`); espaço de cor explícito, ponte para `System.Drawing.Color`; `PxColorArgb` no commit `2e31357` (P8.3) |
| `ColorHSL` | `PxColorHsl` | Aplicado (`HslColor` no `eaadb07`); `PxColorHsl`, em `double`, no commit `2e31357` (P8.3) |
| (novo) | `PxRect` | Aplicado no commit `fa223d7` (P8.2): resultado do passo de layout |
| (novo) | `PxPadding` | Aplicado no commit `fa223d7` (P8.2): margens (`Padding` no WinForms, `Thickness` no WPF) |
| (novo) | `PxDock` (enum) | Aplicado no commit `fa223d7` (P8.2): substitui o `DockStyle` nas opções, com os mesmos valores |

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
  `System.Drawing` nos commits `0acfce7` (`Point`, `PointF`, `Size` e `SizeF`) e `fa223d7`
  (`Rectangle` e `RectangleF`): a volta para os tipos em int arredonda, como o `Point.Round` do
  próprio `System.Drawing`, e a volta para os em float estreita. O `ToString` dos primitivos em
  `double` não depende mais da cultura atual.
- Entre as cores, nenhuma conversão implícita (decidido, P8.4): funções estáticas `ToHsl`,
  `FromHsl`, `ToArgb` e `FromArgb`. Aplicado no commit `2e31357`: `PxColorArgb.FromHsl` e `ToHsl`,
  `PxColorHsl.FromArgb` e `ToArgb`, com as contas dos dois sentidos no `PxColorHsl`, e o CS0457
  saiu junto. O `PxColorArgb` continua com as conversões implícitas com o `System.Drawing.Color`,
  que não perdem nada; o `PxColorHsl` perdeu as dele, porque ir para o `System.Drawing.Color` é ir
  para ARGB. Confirmado em 29/09: o `PxColorHsl` também passou para `double`, a precisão
  padrão (P8.1, P8.6).
- Com o WinForms e o WPF (P7.1): aplicado no commit `e6cca32`, em arquivos `*.Windows.cs`, que só o
  alvo `-windows` compila (3.7). O `PxPoint` e o `Point` do WPF: implícitas nos dois sentidos. O
  `PxSize` e o `PxRect` com o `Size` e o `Rect` do WPF: implícitas deles para o `Px` e explícitas de
  volta, sem clamp, porque os dois lançam `ArgumentException` com largura ou altura negativa. O
  `PxPadding`: implícita do `Padding` do WinForms (int) e explícita de volta, arredondando como o
  `PxPoint` faz para o `Point`; com a `Thickness` do WPF (double), implícitas nos dois sentidos. O
  `PxColorArgb` e a `Color` do WPF: implícitas nos dois sentidos, como com o `System.Drawing.Color`
  (a do WPF também guarda o scRGB em float; a conversão fica com os bytes, que é o que um controle
  mostra), e o `PxColorHsl` continua sem nenhuma (P8.4). O `PxDock` e o `DockStyle`, que têm os
  mesmos valores: um enum não declara conversões (nem num bloco `extension`, CS9282), então são os
  métodos de extensão `ToDockStyle()` e `ToPxDock()`, e um valor fora do enum lança. O `Rect` e a
  `Color` do WPF não estavam na lista de 27/09; entraram pela mesma regra, porque a view WPF vai pôr
  os controles nos `PxRect` do layout, e a `Color`, como o `System.Drawing.Color`, era do original
  (commit `cd4ccf2`).

### 3.5 Apresentação

- A view não herda de `Inspector`: recebe um e o observa. Assim a raiz e os nós aninhados são
  tratados do mesmo jeito.
- Os stubs antigos (`Presentation/WF`, `Presentation/WPF` e as fábricas `Create<T>(host)` do
  `Inspector.Windows.cs`) saíram no commit `cead7b1`; a apresentação começa do zero quando for a vez
  dela.
- Cada plataforma traduz o enum de editor para controles (`TextBox`, `ComboBox` preenchido com os
  valores do enum, `TrackBar`/`Slider`, `CheckBox`...) e implementa o scrubbing com captura de mouse
  no rótulo. No WinForms (decidido, P7.9):

  | Editor | Controle | Observação |
  |---|---|---|
  | `Text` | `TextBox` | grava no Enter e ao perder o foco, e o Esc volta (P2.12) |
  | `Number` | `TextBox` | como o texto; a conversão e a faixa são do núcleo, e o `NumericUpDown` (em `decimal`, com limites próprios) duplicaria isso |
  | `Toggle` | `CheckBox` | grava na hora |
  | `Choice` | `ComboBox` só de escolha | a lista vem do `GetChoices()` cada vez que abre (P6.3) |
  | `Slider` | `TrackBar` | posições pela faixa e pelo passo (o `TrackBar` só conta em int); sem faixa, vira um `Number` |
  | `Color` | botão pintado com a cor | o clique abre o `ColorDialog` do sistema |
  | `Button` | `Button` | com o `Text` do nó, chama o `Press()` |
  | `Display` | `TextBox` só leitura | dá para selecionar e copiar |
  | `Header` | só o rótulo, em negrito | o grupo de um tipo (P5.9) |
  | `Separator` | uma linha horizontal | |
  | `Selector` | `ComboBox` só de escolha | os itens, e a escolha vai para o `SelectedIndex` |
  | `List` | `ListBox` e quatro botões | adicionar, remover, subir e descer, na linha a mais que o layout reserva |
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
  tem vários controles; se ele não puder ser agnóstico, são quatro, dois por plataforma. Aplicado
  no corte 4, abaixo.

**A view WinForms** (o corte 2, aplicado no commit `8dfaab4`, com as respostas de 7.7 a 7.13)

- **Os arquivos**, em `Views/WinForms`, um conceito cada: a fábrica (`WinFormsViewFactory`, com o
  `CreateWinFormsView()`), a view (`WinFormsInspectorView`), a linha (`WinFormsRow`, o rótulo e o
  editor de um nó), a base dos editores e um arquivo por editor, e a seta desenhada (`Arrow`).
- **O layout.** As linhas vão nos retângulos do `Layout(largura)`, um painel por grupo, dentro de um
  painel com rolagem. A view refaz o layout a cada `OptionChanged`, mudança no bind,
  `VisibleChanged` ou `ObjectReplaced`, e numa mudança de largura; uma rajada de mudanças (um
  handler que esconde três linhas) vira um layout só, quando a thread da interface chegar nele. Uma
  linha guarda os controles enquanto o nó mantém o editor, então um layout não tira o foco nem o que
  está sendo digitado, e um editor novo ganha controles novos. A primeira montagem com a largura
  certa é na criação da janela da view: montada depois, uma caixa de texto com foco ficava com a
  rolagem de quando ainda era estreita, mostrando só o fim do texto (visto no Wine, com um teste
  para isso). A ordem do Tab segue as linhas, e sai de um grupo para a linha de baixo dele.
- **Os valores.** O `ValueChanged` mostra o valor do nó (`ViewValue`), na cultura do inspector. Uma
  caixa de texto com foco e um texto digitado guarda o que foi digitado quando chega um valor novo,
  até o Enter ou o Esc, para não perder a edição. A lista de escolha é lida quando a caixa abre e
  também quando ganha o foco, para as setas do teclado escolherem entre todos os valores (P6.3).
- **A falha** (P7.11). O fundo vermelho claro e a mensagem do nó no tooltip do editor, com o motivo
  embaixo. No editor de cor, o fundo é o valor, então a falha fica na borda, vermelha. O que o
  núcleo recusa como uso errado numa gravação da view (um nó somente leitura, um ramo desativado)
  aparece na linha do mesmo jeito, em vez de derrubar a view; o resto da premissa nas views veio no
  corte 4, abaixo.
- **As setas** (P7.10). A do grupo e as de subir e descer do editor de lista são desenhadas, e não
  caracteres: no Wine, a fonte não tinha o ▲ e o ▼, que saíam como quadrados.
- **O `(?)` e a linha sob o mouse** (P7.15, P7.14; commit `af5a584`). A marca é um controle próprio
  (`HelpMark`), que desenha o "(?)" numa linha só, sem margem: um `Label` acrescentava a margem dele
  e quebrava o texto nos 16 px, mostrando "(?" mais alto que o rótulo (visto no print do Wine; o
  texto ocupa de 13 a 15 px nas fontes medidas, e um teste confere a tinta na tela). O clique abre a
  `HelpDialog`, modal sobre o form da view. Para o fundo, a view segue o mouse em todos os controles
  das linhas e pinta o retângulo `Row` da linha de baixo dele no contêiner, com um pouco da cor de
  destaque sobre o fundo (segue as cores do sistema); o rótulo, a marca, a caixa de seleção e o
  quadro da lista deixam o fundo passar, e o `TrackBar`, que não deixa, pega a cor.
- **O scrubbing e os mistos** (o corte 3, P7.16 a P7.19; commit `ce15b9f`). O rótulo de uma linha
  que faz scrubbing (um número com `ScrubMultiplier`, que pode ser gravado agora) tem o cursor do
  eixo dela e uma `LabelScrub`, que guarda os valores do começo do arraste (`ViewValues`) e grava, a
  cada movimento, "o começo de cada objeto + a distância × o passo" pelo `SetValues`. O Esc chega
  por um filtro de mensagens, só durante o arraste, porque o rótulo não tem foco. Um nó somente
  leitura, ou num ramo desativado, não faz scrubbing. O itálico do misto fica só nas linhas com
  valor próprio: um grupo e uma coleção guardam objetos sempre diferentes, então o `IsMixed` deles
  vale, mas a view não os marca. A caixa de texto mostra o "—" pelo `PlaceholderText`.
- **As válvulas** (o corte 4, P7.4; commit `6632d73`). O `ControlCreated` vem para cada controle que
  a view faz para um nó (o rótulo, a marca de ajuda, o editor e o painel de um grupo, com a parte em
  `RowPart`), e o `RowCreated` vem depois, uma vez por linha, com a linha inteira, já no lugar e
  mostrando o valor. Cada um vem uma vez, e de novo só para uma linha refeita (um nó que trocou de
  editor). Ficaram quatro, dois por plataforma, como a P7.4 previa: o terceiro só seria agnóstico
  com os controles como `object`, e quem assinasse teria de converter. O agnóstico são os args, no
  núcleo, genéricos no tipo do controle (`ControlCreatedEventArgs<TControl>` e
  `RowCreatedEventArgs<TControl>`): o WinForms usa `Control`, e o WPF, `FrameworkElement`. A
  view continua dona do lugar, do valor e do estado (habilitado, a cor da falha, o tooltip); o resto
  é de quem assina. Para quem assina logo depois do `CreateWinFormsView` ver todos os controles, as
  linhas são feitas quando a view ganha a janela, e não mais no construtor. Uma exceção de quem
  assina sobe pela view, como a de um evento do inspector (3.11): o layout para naquela linha, a
  view continua usável, e a próxima mudança faz o resto. O WindowsHost alinha dois números à direita
  por uma válvula.
- **As falhas da própria view** (o corte 4, 3.11; commit `6632d73`), avisadas pelo `RowFailed`, com
  os args de falha do núcleo. Uma linha que não pode ser feita ou posta no lugar (um controle que a
  plataforma recusa) fica de fora, com o ramo, até o próximo layout (crítico); esse caso não tem
  teste, porque não há como provocá-lo sem um gancho só para teste. Uma linha que não consegue
  mostrar os objetos (um `ToString` ou um `Equals` deles que lança) guarda o que mostrava e fica
  vermelho-claro, com a mensagem no tooltip, até se mostrar inteira de novo (contornado); o aviso
  vem uma vez, e não a cada nova tentativa, e as outras linhas seguem normais.
- **Ainda não**: um nó posto à mão depois de a view montar (`Add`, `AddButton`, `AddDisplay`) só
  aparece no layout seguinte, porque pôr um nó não é uma opção e não avisa; fica assim até aparecer
  motivo.
- **Verificado no Wine**, com um app de teste que monta a view para um tipo com todos os editores e
  usa o teclado de verdade (`SendKeys`), o foco e os cliques dos controles: 71 checagens (o lugar de
  cada controle no layout, os valores, a gravação do texto com Enter, foco e Esc, a falha, cada
  editor, um aviso de outra thread, as opções mudadas depois, recolher, o seletor e a lista, o ramo
  desativado, o controle do binder, a largura, o Tab, a rolagem e o descarte). Com a view quebrada
  de propósito (o texto recusado sobrescrito, sem passar para a thread da interface, sem layout nas
  opções, sem esvaziar no `Disposed` e sem a cor da falha), 13 falham. O WindowsHost mostra um
  `Gadget`, do `DemoObjects`, com um membro por editor (commit `042a7fe`), para a conferência no
  Windows. Com o espaçador, o `(?)` e a linha sob o mouse, o app passou a 93 checagens (as posições,
  o `MaxWidth`, a marca na tela, a janela modal, que desabilita o form de trás, e o fundo pela cor
  dos pixels na tela); quebrando cada parte de propósito, 6 falham, e a marca como `Label` também
  falha. Dois membros do `Gadget` ganharam `Help` (commit `f3981bd`). Com o corte 3, 117 checagens:
  o arraste é de mouse de verdade (o `mouse_event` do Windows), inclusive um pixel por vez num `int`
  com 0,1 por pixel, o Esc, o eixo vertical e os dois objetos andando juntos; quebrando o scrubbing
  e o misto de propósito, 8 falham. No Wine, o Shift e o Ctrl simulados se perdem no movimento do
  mouse, porque o Wine relê as teclas no servidor X; o teste então pede a um script de fora
  (`run-wine.sh`, que usa o `xdotool`) que aperte a tecla de verdade. Dois membros do `Gadget` fazem
  scrubbing, um em cada eixo, e um botão do WindowsHost liga e desliga um segundo `Gadget`, para os
  mistos (commit `98f8a9a`). Com o corte 4, 135 checagens: os controles e as linhas das válvulas, na
  ordem, uma vez cada, de novo para um editor trocado, o que a válvula ajustou ficando, a exceção de
  uma válvula subindo com a view usável depois, e um `ToString` que lança deixando só a linha dele
  vermelha, avisada uma vez, até sarar. Sem a proteção da linha, o app para com a exceção sem
  tratamento; sem o painel na válvula, a checagem dele falha.

**A view WPF** (o corte 5; commit `a93b75c`), em `Views/Wpf`, um conceito por arquivo, como a
WinForms: a fábrica (`WpfViewFactory`), a view (`WpfInspectorView`), a linha (`WpfRow`), a base dos
editores e um arquivo por editor, a seta, a marca `(?)`, a janela de ajuda e o scrubbing. O que as
duas decidem igual ficou em `Views/ViewRules.cs`: o editor de cada linha, quem faz scrubbing e quem
mostra mistos, que antes estavam na linha e no scrubbing do WinForms.

- **O layout.** As linhas vão nos retângulos do `Layout(largura)` num `Canvas`, um por grupo, dentro
  de um `ScrollViewer`, com a largura do viewport. O canvas fica preso no canto de cima à esquerda:
  com a altura do layout e o alinhamento padrão (`Stretch`), o WPF o centralizava numa view mais
  alta, e as linhas começavam 110 px abaixo (visto no print do Wine, com uma checagem para isso). As
  linhas são feitas no `Loaded`, o par da criação da janela no WinForms. A ordem do Tab segue as
  linhas pelo `TabIndex`, com navegação local em cada canvas.
- **A identidade dos controles.** O caminho do nó vai no `AutomationId` (o rótulo com `#label`, a
  marca com `#help`, o painel com `#panel`), porque o `Name` do WPF não aceita o ponto dos caminhos,
  e o `Tag` fica livre para quem usa a biblioteca (P7.16).
- **O descarte.** Um controle WPF não tem `Dispose`, então a view é `IDisposable`: descartar tira as
  assinaturas dela e deixa o inspector como está (P7.13). Sem isso, a view vive enquanto o inspector
  viver.
- **Os editores.** Os mesmos da tabela, com os controles do WPF (`TextBox`, `CheckBox`, `ComboBox`,
  `Slider`, `Button`, `ListBox`). O slider usa a faixa em double, parando no passo, sem as posições
  inteiras do `TrackBar`. A cor abre o diálogo de cor do sistema pelo WinForms, com a janela da view
  como dona, porque o WPF não tem um. O "—" do texto misto é um `TextBlock` sobre a caixa, que deixa
  o mouse passar, porque a caixa do WPF não tem placeholder. A lista de escolha recarrega quando
  abre e quando a própria caixa ganha o foco; o foco dos itens da lista aberta, que também sobe até
  a caixa, não conta.
- **O fundo da linha sob o mouse.** Um retângulo atrás dos controles, no canvas da linha; os
  controles do WPF deixam o fundo passar, inclusive o slider. Ao sair da view, o fundo apaga pelo
  `MouseLeave`, porque fora das janelas dele o WPF guarda a última posição que o mouse teve dentro.
- **O scrubbing.** O mesmo gesto, com a distância medida em relação à view, em unidades do WPF. O
  Esc chega pelo `InputManager` durante o arraste, porque o rótulo não tem o foco do teclado.
- **Verificado no Wine**, com um app de teste no molde do da view WinForms (`probe-wpf`): 139
  checagens, as mesmas da WinForms traduzidas, com o mouse e o teclado de verdade (`mouse_event` e
  `keybd_event`), e mais o canto do canvas e a lista aberta com o mouse. Quebrando a view de
  propósito em seis pontos (o canto do canvas, o fundo da linha, o itálico, o limiar do arraste, o
  Esc do texto e a proteção da linha), cada um falha; o limiar só falhou depois de uma checagem
  nova, de 2 px num double, que entrou também no probe da view WinForms (136 checagens), onde o
  mesmo buraco existia. No Wine sem gerenciador de janelas, o mouse que sai para onde não há janela
  não avisa o WPF de que saiu (no Windows avisa), então a checagem sai para outra janela do app. O
  WpfHost mostra o `Gadget` como o WindowsHost, com a mesma válvula (commit `a93b75c`).

A view do Avalonia (09/10, P7.22) não usa os retângulos do layout: cada linha é uma grade com a
coluna do rótulo (160), a das marcas e a do editor, e um grupo é um expander. O que ela ganhou para
ficar com o contrato das outras:

- **O `(?)`** (commit `507b528`). A coluna das marcas tem `HelpWidth` mais `LabelSpacing`, como no
  passo de layout, em todas as linhas quando algum nó da árvore tem `Help`, e some quando nenhum
  tem. Num grupo, a marca fica logo depois do rótulo, no cabeçalho do expander, e pega o clique,
  para não abrir nem fechar o grupo. A janela é modal sobre a da view, com o rótulo como título, o
  texto só leitura e um OK (Enter ou Esc fecham). O tooltip do rótulo perdeu o `Help`.
- **As válvulas e o `RowFailed`** (commit `8853770`): os mesmos eventos, com os args do núcleo no
  `Control` do Avalonia. Para quem assina logo depois do `CreateAvaloniaView()` ver todos os
  controles, as linhas passaram a ser feitas quando a view é carregada (`OnLoaded`), como na WPF,
  e não no construtor. O AvaloniaHost usa a válvula como o WpfHost, com `Count` e `Ratio` à
  direita, e põe o `RowFailed` na linha de status.
- **O scrubbing** (commit `b0deeb2`): o mesmo gesto, com a distância medida em relação à view. O Esc
  vem da janela (o `TopLevel`), num handler de túnel que só existe durante o arraste.
- **Verificado** num console fora do repositório, numa tela virtual, com os eventos de ponteiro e de
  teclado simulados: 106 checagens (as 65 do port e 41 novas). Quebrando de propósito a coluna, a
  marca que deixa o clique passar, as linhas feitas no construtor, a falha avisada a cada leitura, o
  limiar do arraste, o Esc e o misto de um número que faz scrubbing, cada um falha. No AvaloniaHost,
  com o mouse de verdade (xdotool), um arraste de 25 px levou o `Count` de 3 a 28, o Esc no meio do
  arraste do `Ratio` voltou a 0,5, e com a janela da ajuda aberta a de trás não recebeu o texto
  digitado.

### 3.6 Serviços (decidido: descartados)

O service locator do original existia para tirar responsabilidades de um arquivo monolítico e
agrupar funcionalidades. No rework ele não volta:

- localizar e aplicar já estão cobertos pelo `IEnumerable<InspectorNode>`, pelo indexador e por LINQ,
  numa fração das linhas;
- manipular e vincular viram métodos do inspector e dos nós.

### 3.7 Um projeto, uma DLL por alvo (aplicado; substituído em 09/10)

Primeiro veio o multi-target, no commit `cd4ccf2`: `net10.0` e `net10.0-windows` num projeto só, com
o código de Windows em arquivos parciais `*.Windows.cs` (as fábricas `Create<T>(host)` e as
conversões `System.Windows.*` dos primitivos) e nas pastas `Presentation/WF` e `Presentation/WPF`,
fora do alvo `net10.0`.

No commit `cead7b1` esse código saiu (eram stubs e conversões que nada usava), e com ele o segundo
alvo: o `InteractiveEditor.csproj` voltou a ter só `<TargetFramework>net10.0</TargetFramework>`, sem
`UseWPF`, `UseWindowsForms`, `EnableWindowsTargeting` ou blocos condicionais. A biblioteca gera um
binário só, e os hosts WinForms e WPF (`net10.0-windows`) a referenciam normalmente.

- Console de verificação em `net10.0`, que roda sem o runtime WindowsDesktop: era o NoHost; agora é
  o TuxHost (commit `4c4fe9e`), e o NoHost voltou a ser local.
- Quando as views chegarem, a biblioteca volta a precisar do Windows. Havia duas saídas: só
  `net10.0-windows` (projeto sem condições, mas o TuxHost deixa de rodar fora do Windows) ou de novo
  os dois alvos, com o bloco condicional. Decidido em 27/09 (P7.1): os dois alvos, com o código das
  views só no `-windows`; o núcleo e o TuxHost continuam rodando fora do Windows, e cada consumidor
  recebe a DLL do alvo dele. As conversões dos primitivos para o WPF voltam junto.

Verificado no commit `cead7b1`: a solução compila sem erros e sem warnings, e o TuxHost imprime
exatamente o mesmo de antes.

Os dois alvos voltaram no commit `e6cca32`, do jeito do `cd4ccf2`: `net10.0;net10.0-windows`, com
`UseWindowsForms` e `UseWPF` só no `-windows`, o `EnableWindowsTargeting` no projeto, para compilar
fora do Windows, e os arquivos `*.Windows.cs` fora do `net10.0`. Por enquanto, o código de Windows
são as conversões dos primitivos (3.4); as views entram do mesmo jeito.

- **Usings implícitos.** O WinForms põe `System.Drawing` e `System.Windows.Forms` nos usings
  implícitos do alvo `-windows`; o projeto os tira, para o núcleo não ver nomes do WinForms lá, e os
  arquivos de Windows dizem o que usam. O SDK de desktop também tira o `System.IO` e o
  `System.Net.Http` do `-windows`, então um arquivo do núcleo que use um deles precisa do `using`;
  sem ele, é o `-windows` que não compila (dá erro, não muda o sentido de nada).
- **Um projeto de uma plataforma só**, que referencia a DLL direto (sem `ProjectReference` nem
  pacote), compila e roda com as conversões, tanto só WinForms quanto só WPF (testado, rodando no
  Wine). O só WinForms recebe o aviso MSB3277, de versões diferentes do `WindowsBase`: a DLL do
  `-windows` referencia o do WPF, e o projeto tem a fachada do .NET. Vem do `UseWPF` da P7.1, e não
  das conversões; a view WPF na mesma DLL faz o mesmo. Pelo `ProjectReference`, a referência ao
  framework passa junto, e não há aviso.
- **Verificado no commit `e6cca32`**: a solução compila sem erros e sem warnings nos 6 projetos, com
  a biblioteca nos dois alvos; o TuxHost imprime o mesmo, e o probe passa as 646. As conversões têm
  um console `net10.0-windows` à parte, com 21 checagens (valores, implícita ou explícita, o que
  lança, e o `net10.0` sem nenhuma), rodado no Wine; com conversões quebradas de propósito, 5
  falham.

Em 09/10 o desenho mudou de novo, com a branch das views novas (seção 0, Estrutura; P7.20): o
núcleo voltou a ter só `net10.0`, como no `cead7b1`, e cada framework ganhou um projeto
(`InteractiveEditor.WinForms`, `.Wpf`, `.Avalonia` e `.ImGui`). Os arquivos `*.Windows.cs` e as
pastas `Views/WinForms` e `Views/Wpf` saíram do núcleo; as conversões viraram extensões nos projetos
do WinForms e do WPF, e são eles que tiram os usings implícitos do WinForms. No núcleo ficou só o
`Views/ViewRules.cs`, que os quatro veem pelo `InternalsVisibleTo` (P7.21). Commits
`36f0afa` e `424f6d0`. Verificado no `6f02064`: a solução compila sem warnings e sem erros nos 13
projetos (e nos dois da cópia da PixieLib), e o TuxHost imprime as mesmas 67 linhas; no `424f6d0`,
o WindowsHost e o WpfHost mostram o `Gadget` no Wine como antes.

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

### 3.9 PixieLib: primitivos e matemática fora do inspector (aplicado em 09/10)

Discutido e prototipado fora do repositório; nada mudou no código. Fica registrado para quando for a
vez.

- **A ideia**: os primitivos (hoje em `InteractiveEditor/Primitives`, que nada usa ainda) saem do
  inspector e viram a base da PixieLib em C#, que depois ganha vetores, matrizes e transformações 2D
  e 3D.
- **Enquanto isso** (decidido): os primitivos atuais ficam no InteractiveEditor para segurar as
  pontas, já com o prefixo `Px` (commit `bea77a1`). Só saem quando a PixieLib existir.
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
  `double` desde o commit `0acfce7` (`PxPoint`, `PxSize`, `PxRect` e `PxPadding`), passam a sair do
  gerador, com os sufixos de precisão dele.
- **Decidido em 27/09**: a precisão padrão é `double`, como o `Vec2` do C++ (P8.6). A PixieLib
  ainda não é usada (P8.7, P8.8): os primitivos ficam no InteractiveEditor, sem `PixieLib.dll`, e a
  mudança para lá, com o sufixo de precisão (P8.9) e o "onde" acima, fica para uma sessão própria.
- **Aplicado em 09/10**: a PixieLib existe em C# no repositório `Sakamoto0110/PixieLib`
  (`dotnet/`), e o núcleo usa os primitivos dela pela cópia em `external/PixieLib` (seção 0,
  Primitivos e PixieLib; commit `424f6d0`).

### 3.10 O `Inspector`: hoje raso

O `Inspector` tem 463 linhas e, desde o commit `1997209`, não é mais um nó: guarda a raiz num
`RootNode` interno e expõe o `Create<T>()` com o modo (`Mode`) e o `Create()` sem tipo, os eventos e
o relatório da criação, o `Id`, o `Name`, as opções (`Options`), os objetos ligados (`Instance` e
`Instances`), o indexador e o seletor por expressão (`Node<T>`), a enumeração, as `Rows` e o
`Layout(largura)` delas (commit `9674fca`), os nós postos à mão (`Add`, `AddButton` e `AddDisplay`),
o `Refresh()`, o `Dispose`, o bind inteiro (`Bind`, `AddBind`, `RemoveBind`, `Unbind()` e `Rebind`,
com os eventos) e o controle do binder (`Apply()`, `Reload()`, `HasPendingValues` e os três métodos
de força, com os eventos). Deveria ser uma das peças mais completas, porque é o que o host e a view
usam. Levantamento para o desenho, com as decisões de 27/09 e 29/09 no fim.

**Buracos no que já existe** (testado)

- Resolvido no commit `62af47f`: um objeto de outro tipo passava pelo `bind`
  (`Inspector.Create<Foo>().bind(new Bar())` aceitava, a árvore continuava a de `Foo`, e o erro só
  aparecia depois, no `GetValue` de um nó, como `ArgumentException` do reflection). Agora o `Bind`
  lança na hora, sem mexer na árvore nem no que já está ligado (P2.2).
- Resolvido no commit `739850b`: o `SetValue` da raiz aceitava null e objeto de outro tipo sem
  reclamar, e com null o inspector ficava sem objeto, um unbind silencioso. Agora ele lança (P3.5),
  e desde o commit `1997209` nem existe no `Inspector`.
- Resolvido no commit `739850b`: o objeto de um grupo podia ser trocado
  (`inspector["Moo"].SetValue(new Moo())` trocava o `Moo` do objeto ligado). No main, o
  `Inspector.SetValue` (raiz e grupos aninhados) lançava exceção; o commit `74cd664` trocou isso
  pela gravação pelo pai, para a struct voltar ao dono, e a proteção se perdeu. Agora o `SetValue`
  de um grupo aberto lança, e a struct volta ao dono por um caminho interno (P3.1).
- Resolvido no commit `77dda91`: uma troca feita por fora (`foo.Moo = new Moo()`) passava sem
  sinal, e o ramo só seguia o objeto novo; com null, as leituras davam null e só a gravação lançava.
  Agora é a detecção decidida em P3.3 e P3.4 (abaixo).
- A descoberta roda de novo a cada `Create` (3.8). Os nós não podem ser compartilhados entre
  inspectors, porque cada um tem as próprias opções; o que dá para cachear é a lista de membros.

**Decidido** (seção 0)

- Sem `Inspector<T>`, e com o tipo da raiz fixo: ligar um objeto de outro tipo lança (P2.2). A
  classe não é genérica porque nem todo inspector vai ser tipado (P1.7); o motivo anterior, trocar
  a raiz por um objeto de outro tipo, caiu junto com a troca.
- Composição (P1.1): o inspector guarda a raiz como um nó interno e expõe só o que é dele.
  Liberada na P9.3 e aplicada no commit `1997209`. Cada inspector tem um id único, que os eventos e
  a trava global usam (P1.9, P1.11).
- Setter abstrato de volta, com um tipo de nó por comportamento fixo na criação (P1.2). Dentro do nó
  de membro, grupo ou folha continua decidido em tempo de execução, porque o `Expandable` pode mudar
  depois do `Create`. Aplicado no commit `1997209` (`MemberNode` e `RootNode`).
- Enumerável, entregando todos os nós; as linhas da view saem de um percurso à parte, `Rows` (P1.3;
  commit `a8bf9db`).
- Opções por inspector (`InspectorOptions`), a camada entre o `GlobalOptions` e o nó, sobrescrevendo
  o global (P1.5): as de layout e hospedagem (1.1, item 12; altura e espaçamento por editor, 1.2) e
  a cultura (P2.7). O `Create` trava o `GlobalOptions` até o `Dispose` do último inspector vivo
  (P1.5, P4.4, P1.11; commit `bc4491e`). O `InspectorOptions` entrou no commit `7f3cb63`, com a
  cultura.
- `IDisposable` no inspector e nos nós (P1.5; commit `bc4491e`).
- Ciclo do bind: `Bind`, `Unbind`, `Rebind`, `AddBind` e `RemoveBind` (P2.1), os valores mistos do
  multi-bind (P2.4) e o `Refresh()` (P2.6).
- Controle do binder (P1.8, P1.13): um enum de flags e um fluxo normal para ler e gravar à mão; os
  métodos de força, cada um com o seu evento, não se confundem com esse fluxo nem com o `Refresh()`.
  Aplicado no commit `121520a` (3.3).
- `TypeBinderMode` continua (P1.7), e sai do jeito de criar (P1.12). Nós manuais: botão com ação e
  campo só de exibição com getter, sim; cabeçalho, não (P1.6). Os nós manuais entraram no commit
  `42e2129`, o inspector tipado e manual, com o `Add`, no commit `056934f` (3.2), e o sem tipo no
  commit `d1bf798` (P1.14).
- Filtro por nome injetável, com a precedência dos atributos, por tipo (P6.4, P6.6), registrado de
  fora antes do `Create` (P6.7). A visibilidade condicional ficou para o final, e foi decidida em
  02/10 e aplicada no commit `bdf7ef8`: o `VisibleWhen` no nó (P6.2; 3.12).
- Eventos (P1.4), sem economia: alguns só de consumo interno, outros expostos e consumidos também
  pelo próprio inspector. Os nomes seguem a convenção do .NET (P1.10): o evento sem o `On`, e o
  `On` no método que o dispara. Os da criação são estáticos (P1.9):

  | Onde | Evento | Quando | Situação |
  |---|---|---|---|
  | inspector, estático | `Created` | a criação terminou, com as contagens | `9db2e52` |
  | inspector, estático | `DiscoveryFinished` | a árvore está montada | `9db2e52` |
  | inspector, estático | `DiscoveryFailed` | uma falha no `Create`, com a severidade | `9db2e52` |
  | inspector | `BindRegistered` | um ou mais objetos entraram no bind | `e707583` |
  | inspector | `BindRemoved` | objetos saíram do bind | `e707583` |
  | inspector | `Unbound` | o último objeto saiu | `e707583` |
  | inspector | `ForcedApply` | um `ForceApply()` terminou | `121520a` |
  | inspector | `ForcedReload` | um `ForceReload()` terminou | `121520a` |
  | inspector | `ForcedClear` | um `ForceClear()` terminou | `121520a` |
  | nó | `ValueChanged` | um valor mudou, com a origem (no lugar do `ValueApplied`) | `305952f` |
  | nó | `ObjectReplaced` | o objeto do grupo foi trocado por fora; dá para aceitar | `77dda91` |
  | nó | `BindFailed` | falha ao ler ou gravar, com mensagem, motivo, sugestão e caminho | `305952f` |
  | nó | `VisibleChanged` | a regra do `VisibleWhen` deu outra resposta, com a origem (P6.2) | `bdf7ef8` |
  | inspector | `OptionChanged` | uma opção do nó, ou do inspector (sem nó), mudou de fato (P7.8) | `4db6457` |
  | inspector | `Disposed` | o `Dispose` terminou: desligado, árvore descartada, trava solta (P7.13) | `4db6457` |

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
  commit `739850b`, também para o grupo de uma struct: ela muda pelos filhos.
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
- Aplicado no commit `77dda91`. No bind, todo grupo de tipo class guarda o objeto que tem em cada
  objeto ligado. Numa leitura, numa gravação e no `Refresh()`, os grupos no caminho são
  comparados, de cima para baixo. Num grupo aberto, a troca dispara `ObjectReplaced` (com o objeto
  ligado, o anterior e o atual); sem aceite, o ramo fica com `IsCompromised`, `GetValue`,
  `GetValues` e `SetValue` lançam dizendo qual grupo foi trocado, `IsMixed` lê false, e o
  `Refresh()` deixa o ramo de fora. Um objeto fechado é valor: trocá-lo é edição, e quando o
  próprio inspector troca um, o ramo dele guarda o objeto novo. Qualquer mudança no bind restaura.
  Corrigido no commit `14307ea`: quando a troca fica (um objeto fechado trocado por fora, ou o
  objeto novo aceito por quem assina), os grupos de dentro também recomeçam do objeto novo; antes,
  a leitura seguinte os dava como trocados e desativava o ramo.

**Decidido em 29/09**: o filtro por nome é injetado de fora, por tipo (P6.7), e o inspector sem tipo
não tem as camadas de reflection e de atributos, com o tipo fixado no primeiro bind (P1.14).

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
- **Aplicado no `Create`** (commit `9db2e52`), com os tipos em `InteractiveEditor.Diagnostics`, um
  por arquivo: `FailureSeverity`, `InspectorEventArgs`, `InspectorFailureEventArgs` (caminho,
  exceção, mensagem, motivo, sugestão e `Handled`), `InspectorCreatedEventArgs` (as três contagens)
  e `InspectorReport`, que fica em `inspector.Report`. Um membro cuja assinatura não dá para ler
  (uma assembly que ele usa está ausente) sai da árvore, como crítico; o `GetIndexParameters()`
  também lê a assinatura, então ele fica dentro da mesma proteção. Um atributo que não dá para ler
  ou aplicar é pulado, e os outros do mesmo membro valem, como contornado. Os padrões da reflection
  que falham deixam o nó com o nome como rótulo, também contornado. Só o fatal sobe, depois de
  soltar a trava das opções globais; uma exceção de quem assina o evento também sobe por ele.
  Testado com uma biblioteca cuja dependência é apagada antes de rodar.
- **No objeto ligado** (commit `305952f`): um getter que lança não para a leitura nem o bind; o nó
  lê null ali, guarda a falha em `Failure` e avisa por `BindFailed` (recuperado), e a próxima
  leitura tenta de novo. Um setter que lança não para o `SetValue`; os objetos ficam com o que ele
  deixou, e a falha vai para o nó (contornado). Uma leitura que funciona não limpa uma falha de
  gravação; uma gravação que funciona limpa. No preparo da gravação (commit `7f3cb63`), texto que
  não converte, número grande demais e regra que lança também viram falha no nó (recuperado), e
  nada é gravado. No `Apply()` e no `ForceApply()` (commit `121520a`), um nó que não recebe o valor
  mostra a falha (contornado), e os outros seguem. O uso errado continua lançando: somente
  leitura, grupo, dono null e valor de um tipo sem relação com o do membro.
- **Nas views** (commit `6632d73`): na WinForms, uma linha que não pode ser feita fica de fora até o
  próximo layout, e uma que não consegue mostrar os objetos fica vermelho-claro, avisadas pelo
  `RowFailed` (3.5). A WPF tem a mesma proteção (commit `a93b75c`).
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
  é estado, em duas regiões. Aplicado nos commits `305952f` e `77dda91`: `#region Options` e
  `#region State` no `InspectorNode`.
- O payload de falha do original (mensagem, motivo, possível solução, linha e membro de origem;
  1.1, item 9) continua valendo para os eventos de falha.

---

### 3.12 O `VariablePool` e o `EditField()` do original (P6.2, P6.3)

A explicação que ele pediu, lida no 0.7.1a (commit `7833d65`) e no OverlayApplication (commit
`83f4d8d`), que tem uma cópia própria do inspector, mais antiga
(`Core/MyAssemblies/InteractiveEditor.cs`), com a mesma lógica nesses dois pontos. As decisões de
02/10 estão na seção 0.

**Visibilidade condicional (P6.2)**

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

**Os editores do `EditField()` (P6.3)**

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
| Texto e ação de um botão (▲ e ▼ da camada) | Layer | `AddButton(nome, texto, ação)` (P1.6, commit `42e2129`) |
| Faixa e passo do `TrackBar` (Opacity de 0 a 255, de 5 em 5) | Window, Component | `EditorKind.Slider` com o `Range`, ou `[InspectorRange(0, 255, Step = 5)]` |
| Combo com os valores de um enum (CapValues, Type, AutoRevert) | Component, Effect | automático: enum vira `EditorKind.Choice` |
| Combo com true e false (IsBold, IsItalic, IsUnderline) | Component | automático: bool vira `EditorKind.Toggle` |
| O clique abre o seletor ARGB (Color1, Color2, TransparencyKey, BackgroundColor, Start color e Target color) | Window, Component, Effect | `EditorKind.Color`, e a view abre o seletor (P5.5) |
| A combo das tags como menu ("Add Tag" e "Remove Tag", com uma caixa de texto) | Target | o editor de lista (`EditorKind.List`, commit `97c1e7d`) |
| Reagir à escolha (`EffectChanged`, `TryToSetStartValue`) | Effect | o `ValueChanged` do nó (commit `305952f`) |
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
   commits `0acfce7`, `2e31357` e `fa223d7`.
2. **`Fieldset`** (resolvida no commit `cead7b1`): o `Fieldset` e o namespace `Binding` saíram;
   qualquer membro é um `InspectorNode`, e o CS0118 deixou de existir.
3. **Cultura** (resolvida em 27/09 e 29/09): configurável no inspector inteiro (P2.7), com a atual
   como padrão (P2.11). Aplicada no commit `7f3cb63`.
4. **Nós manuais** (resolvida em 27/09 e 29/09): o botão entra, com uma ação no clique, e o campo
   só de exibição, com um getter; o cabeçalho não (P1.6). Aplicada no commit `42e2129`.
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
      parciais excluídos do alvo `net10.0` (3.7; commit `cd4ccf2`).
- [x] Um alvo só (`net10.0`): sem código de Windows na biblioteca, saem o segundo alvo e os blocos
      condicionais (3.7; commit `cead7b1`).
- [x] Dois alvos de novo, com o código de Windows só no `-windows`, em arquivos `*.Windows.cs`
      (P7.1; 3.7; commit `e6cca32`).
- [x] Um projeto por framework: o núcleo em `net10.0`, e as views em `InteractiveEditor.WinForms`,
      `.Wpf`, `.Avalonia` e `.ImGui`, com as regras comuns pelo `InternalsVisibleTo` (P7.20, P7.21;
      3.7; commits `36f0afa` e `424f6d0`). A `CultureInUse` pública (P7.21; commit `ad79532`).
- [x] NoHost em `net10.0` (commit `cd4ccf2`).
- [x] TuxHost: console de verificação em `net10.0`, com a mesma saída do NoHost (commit `4c4fe9e`).
      O NoHost voltou para `net10.0-windows` (commit `b1eba84`), e voltou a ser ignorado pelo
      `.gitignore`, como na `main`, sem sair do repositório (commit `1e07192` e o seguinte).
- [x] Fábricas por plataforma com nomes distintos, para não obrigar o consumidor a referenciar as
      duas plataformas (3.5; decidido, P7.2). A do WinForms, `CreateWinFormsView()`, entrou no
      commit `8dfaab4` (P7.7), a do WPF, `CreateWpfView()`, no `a93b75c`, e as do Avalonia e do
      ImGui, `CreateAvaloniaView()` e `CreateImGuiView()`, no `a885789` e no `1d86b96`.
- [x] Primitivos: nomes provisórios `SKPoint`, `SKPointF`, `SKSize`, `SKSizeF`, `ArgbColor` e
      `HslColor` (commit `eaadb07`).
- [x] Primitivos: prefixo `Px` no lugar do `SK` provisório (`PxPoint`, `PxPointF`, `PxSize` e
      `PxSizeF`), o mesmo da PixieLib (3.9; commit `bea77a1`).
- [x] Primitivos: um tipo só, em `double` (P8.1); saem `PxPointF` e `PxSizeF` (commit `0acfce7`).
- [x] Cores: `PxColorArgb` e `PxColorHsl` (P8.3), sem conversão implícita entre elas: `ToHsl`,
      `FromHsl`, `ToArgb` e `FromArgb` (P8.4), o que acaba com o CS0457 (commit `2e31357`).
- [x] Primitivos: conversões nos dois sentidos com as regras de 3.4 (confirmadas, P8.5), já com o
      `double`, com o `System.Drawing` (commits `0acfce7` e `fa223d7`).
- [x] Conversões dos primitivos com o WinForms e o WPF (pontos, o `Size` do WPF, `Padding`,
      `Thickness` e `DockStyle`, e também o `Rect` e a `Color` do WPF), no alvo `-windows` (3.4;
      commit `e6cca32`).
- [x] Primitivos novos: `PxRect`, `PxPadding` e `PxDock` (decidido, P8.2; commit `fa223d7`).
- [x] PixieLib em C#: primitivos e matemática fora do inspector, em `dotnet/` no repositório
      PixieLib, com source generator para as precisões (3.9; P8.7). Feita lá, numa sessão própria,
      e usada aqui pela cópia em `external/PixieLib` (commit `424f6d0`).

Núcleo (portar a essência)

- [x] Modelo de opções, primeiro corte: `GlobalOptions`, `FieldOptions` e a pilha
      reflection < atributos < manual (seção 7; commit `1ec81c0`).
- [x] Atributos do inspector, com descrição curta (tooltip) e longa (`(?)`) (commit `1ec81c0`).
- [x] Configuração por campo com chave por caminho (`fields["Moo.MooX"]`) (commit `1ec81c0`).
- [x] Opções no próprio nó e configuração direto no inspector, por caminho ou encadeada; saem
      `FieldOptions`, `FieldOptionsCollection`, `OptionsResolver` e o callback do `Create` (commit
      `25b0ee0`).
- [x] Um membro, um nó: a descoberta monta a árvore direto; saem `FieldDescriptor`, `Fieldset`, o
      `Inspector` aninhado e a lista plana, e as políticas perdem a interface e as instâncias
      (commits `cead7b1` e `faf7de7`).
- [x] Premissa de erros no `Create`: severidade, falhas por evento com `Handled`, relatório no
      inspector, membro ilegível fora da árvore e atributo inválido pulado (3.11; P0.1, P0.2; commit
      `9db2e52`).
- [x] Premissa de erros no objeto ligado: getter e setter que lançam viram falha no nó, com
      `BindFailed` (3.11; commit `305952f`).
- [ ] Premissa de erros nas views (3.11).
- [x] Composição e tipos de nó: o `Inspector` guarda a raiz, `InspectorNode` abstrato com o getter
      comum e o setter abstrato, `MemberNode` e `RootNode`, e um id por inspector (P1.1, P1.2, P1.9;
      P9.3; commit `1997209`).
- [x] `IDisposable` no inspector e nos nós (P1.5; commit `bc4491e`).
- [x] `TypeBinderMode` no inspector tipado: `Create<T>(TypeBinderMode.Manual)` e `Add("X")` (P1.7,
      P1.12; commit `056934f`).
- [x] Inspector sem tipo, manual, com o membro procurado pelo nome no bind, sem as camadas de
      reflection e de atributos, e o tipo fixado no primeiro bind (P1.12; P1.14; commit `d1bf798`).
- [x] Enumeração: o inspector entrega todos os nós, e as linhas da view saem de `Rows` (P1.3; P9.4;
      commit `a8bf9db`).
- [x] Objeto de grupo: o inspector não troca, nem o da raiz (P3.1, P3.5; P9.2; commit `739850b`).
      A proteção do main tinha se perdido no commit `74cd664`.
- [x] Troca por fora compromete o ramo, detectada no `Refresh()` e numa leitura, com `GetValue` e
      `SetValue` lançando até religar (P3.3, P3.4); só o ramo trocado, com um evento que aceita o
      objeto novo (P3.6), e sem struct (P3.2). Commit `77dda91`.
- [x] `InspectorOptions` (por inspector), com a cultura (P2.7; commit `7f3cb63`). Sobrescrever o
      global (P1.5) vale quando uma opção existir nas duas camadas; hoje nenhuma existe.
- [x] Trava do `GlobalOptions` enquanto houver um inspector vivo, solta no `Dispose` (P1.5, P4.4,
      P1.11; commit `bc4491e`).
- [x] Nós manuais: botão com ação e campo só de exibição com getter (P1.6); o cabeçalho não entra
      (commit `42e2129`).
- [x] Configuração de editor que cubra o que hoje sai por `EditField()`: dos usos do
      OverlayApplication, faltava a lista de escolha para um membro que não é enum, o `Choices`
      (P6.3; commit `cf909c1`); o seletor de fonte fica para a válvula da P7.4.
- [x] Visibilidade condicional, por regra e por instância: o `VisibleWhen` no nó (sucessor do
      `VariablePool`; P6.2; commit `bdf7ef8`).
- [x] `Visible` só da view, passando para os filhos (P6.1; commit `095ad28`).
- [x] Binding respeitar o `ReadOnly` das opções no `SetValue` (commit `ab51437`).
- [x] `ReadOnly` passando para os filhos, no lugar de um `ReadOnly` efetivo (P4.1, P4.2; P4.6;
      commit `095ad28`).
- [x] Setter não público escondido pela reflection, com o `[InspectorReadOnly]` trazendo o membro
      de volta (relatório, 3.6; P4.5; commit `095ad28`).
- [x] Rebind: `bind` de novo troca o objeto (commit `74cd664`). Revisto: vira `Rebind`, e o `Bind`
      volta a lançar se já houver objeto ligado (seção 0).
- [x] Binding: ligar que lança se já houver objeto ligado ou se o tipo for outro (P2.2), `Unbind()`
      e `Rebind` (P9.1; commit `62af47f`).
- [x] Multi-bind: `AddBind` e `RemoveBind`, um tipo só, com os valores mistos (P2.3 a P2.5; P2.10;
      commit `e707583`). O scrubbing por delta entrou com a view (commits `31df507` e `ce15b9f`).
- [x] `Refresh()` com o `ValueChanged` dizendo a origem (P2.6, P1.10; commit `305952f`).
- [x] Objeto → UI por `INotifyPropertyChanged` (P2.6; commit `8543362`).
- [x] Controle do binder: o enum de flags, o fluxo normal para ler e gravar à mão e os métodos de
      força, cada um com o seu evento (P1.8; P1.13; commit `121520a`).
- [x] Conversão de texto para valor, com a cultura do inspector e a falha indicada na linha; o
      `SetValue("5")` também converte (P2.7; P2.11; commit `7f3cb63`). A hora de gravar (P2.12)
      fica com a view.
- [x] `SetValue` limitando o valor à faixa do `[InspectorRange]` (P2.8; commit `7f3cb63`).
- [x] Sanitizadores tipados (sucessores das `CapFunction`), por campo, em lista ordenada (P2.9;
      P2.13; commit `7f3cb63`).
- [x] Seletor por expressão ao lado do caminho em string (P6.5; commit `c8a2720`).
- [x] Filtros: blacklist/whitelist, `TypeSafeLock` e o filtro por nome injetável, com a precedência
      dos atributos, por tipo, registrado com `GlobalOptions.Hide<T>` (P6.4; P6.6; P6.7; commit
      `5d53c71`). A lista negra é o `Hide<T>`; a lista branca é o modo manual, com o `Add`
      (commit `056934f`); e o `TypeSafeLock` é o `[InspectorExpandable]` com a flag global (commit
      `1ec81c0`).
- [x] Eventos da criação, estáticos, com o resultado guardado no inspector (P1.4, P1.9; commit
      `9db2e52`).
- [x] Eventos do bind no inspector: `BindRegistered`, `BindRemoved` e `Unbound` (P1.4; commit
      `e707583`).
- [x] Eventos dos nós: `ValueChanged`, com a origem, e `BindFailed` (P1.4, P1.10; commit `305952f`).
- [x] Evento do objeto do grupo trocado por fora: `ObjectReplaced` (P1.4, P3.6; commit `77dda91`).
- [ ] Cache do modelo de tipo: por enquanto só a lista de membros (P5.6; sessão própria).
- [x] Ordem de declaração dos irmãos, se der para recuperar sem muito custo (P5.1; commit
      `bb1184b`).
- [x] Coleções sem os membros do tipo delas (`Capacity`, `Count`, `Length`...) (P5.2; commit
      `4a55bd0`).
- [x] Coleções pelo conteúdo: o seletor (combo box) escolhe o item que aparece na linha de baixo
      (P5.2; P5.10; commit `ba26fe6`).
- [x] Editor de lista, uma linha por item, com adicionar, remover e reordenar: uma escolha
      explícita, `EditorKind.List` (P5.2; P5.10; commit `97c1e7d`).
- [x] Coleção só com getter com o conteúdo editável, e só a troca dela recusada (P4.7; commit
      `3fd3a8c`).
- [x] Faixa e scrubbing do membro da coleção na linha do item (P5.11; commit `d681dee`).
- [x] Tipos com mais de um editor, como o `Color`: escolha explícita e, sem ela, uma linha
      `Display` com aviso (P5.5; P5.8; commit `47b0997`).

Apresentação

- [x] Passo de layout agnóstico, no núcleo, que gera os retângulos de cada linha, com as opções de
      layout no `InspectorOptions` (P7.5; commit `9674fca`).
- [x] View WinForms: editores por tipo, grupos recolhíveis, cabeçalho e scroll (sem paginação),
      percorrendo a árvore (P7.3; P7.7 a P7.13; commits `4db6457` e `8dfaab4`).
- [x] Espaçador e largura máxima: o editor até 200, o resto entre ele e o rótulo, um limite para as
      linhas e o fundo da linha sob o mouse (P7.14; commits `6d8b300` e `af5a584`).
- [x] O `(?)` da ajuda longa, colado no editor, numa faixa antes dos editores, com a janela modal
      (P7.15; commits `6d8b300`, `af5a584` e `4e6bf5b`).
- [x] Scrubbing no rótulo e indicativo de valores mistos: um valor por objeto no núcleo, o arraste
      nos dois eixos, com Shift, Ctrl e Esc, e o itálico com o editor neutro (P2.4; P7.16 a P7.19;
      commits `31df507` e `ce15b9f`).
- [x] Válvulas de escape: o `ControlCreated` por controle e o `RowCreated` ao fim de cada linha,
      com os args agnósticos no núcleo, e as falhas da própria view na linha (P7.4; 3.11; commit
      `6632d73`).
- [x] View WPF, pelo mesmo caminho (o corte 5; commit `a93b75c`), com as regras que as duas
      views decidem igual em `Views/ViewRules.cs`.
- [x] Views do Avalonia e do ImGui e o host do Terminal.Gui, da branch das views novas, refeitos
      sobre o núcleo do rework (commits `6ba0290`, `a885789`, `1d86b96` e `6f02064`).
- [x] No Avalonia, o `(?)` com a janela, as válvulas com o `RowFailed` e o scrubbing (P7.22; commits
      `507b528`, `8853770` e `b0deeb2`); o ImGui fica como está.
- [x] Os filhos que uma view mostra embaixo de um nó, na API pública: o `ShownChildren` (P7.23;
      commit `ad79532`).
- [x] O rótulo de uma linha que não mostra os objetos, nas três views (3.11; commit `e5960e1`).

Pendências da primeira revisão (já conhecidas)

- [x] Objeto intermediário null no `bind`: o bind não lê mais nada; `GetValue` devolve null e
      `SetValue` lança dizendo qual pai é null (commit `74cd664`).
- [x] Setter privado e campo `readonly` gravados pelo `SetValue`: agora ele respeita o `ReadOnly`
      que as opções marcam (commit `ab51437`).
- [x] `FieldDescriptor.Type` com o tipo dono e o namespace `Binding` escondendo o tipo `Binding` do
      WinForms e do WPF: os dois saíram (commit `cead7b1`).
- [x] Membro escondido com `new`: os dois aparecem (P5.4), cada um no grupo do tipo que o declara
      (`Base.Value` e `Derived.Value`), e o nome sem o tipo acha o do derivado (P5.7, P5.9; commit
      `5a10ed6`). Só quando o tipo muda: com o mesmo tipo (`public new int Value`), a reflection
      devolve um só, e não há grupo (testado).
- [x] `/NoHost` no `.gitignore`: agora só `NoHost/bin` e `NoHost/obj` são ignorados (commit
      `59b27e6`). Revertido no commit `1e07192`: o NoHost voltou a ser ignorado por inteiro.
- [x] Structs, inclusive aninhadas em classes e em outras structs: o valor alterado é gravado de
      volta no dono (commit `74cd664`).

---

## 7. Modelo de opções (primeiro corte, aplicado)

Aplicado nos commits `1ec81c0` (biblioteca) e `d6069cc` (NoHost e objeto de teste). Revisto no
commit `25b0ee0`: as opções passaram para o próprio nó e a configuração é feita no inspector; saíram
`FieldOptions`, `FieldOptionsCollection`, `OptionsResolver` e o callback do `Create`.

**Três camadas de opções**

- `GlobalOptions` (estática): valem para o processo inteiro: `RequireExpandableAttribute` e, desde o
  commit `5d53c71`, o filtro por nome (`Hide<T>` e `Unhide<T>`, P6.7). Decidido (P1.5, P4.4) e
  aplicado no commit `bc4491e`: o `Create`
  trava as opções globais, e mudar uma delas com um inspector vivo lança; a trava cai no `Dispose`
  do último. O TuxHost descarta cada inspector depois de imprimir, para poder ligar a flag.
- `InspectorOptions` (`inspector.Options`): existe desde o commit `7f3cb63`, com a cultura
  (`Culture`; null é a atual, na hora de cada conversão), e desde o commit `121520a` com o modo de
  controle do binder (`BinderControl`, `Automatic` por padrão). As de layout entraram com o passo
  de layout, no commit `9674fca` (3.4): `RowHeight`, `RowSpacing`, `Indent`, `LabelWidth`,
  `LabelSpacing`, `Padding` e `ListRows`, e no commit `6d8b300`, `EditorMaxWidth`, `MaxWidth` e
  `HelpWidth` (P7.14, P7.15). Ainda não há um padrão global de layout para elas
  sobrescreverem (P1.5); os valores padrão ficam no próprio `InspectorOptions`.
- Por campo: propriedades do próprio nó (`InspectorNode`): `Label`, `Tooltip` (curta), `Help`
  (longa, para o `(?)`), `Order`, `Ignored`, `Visible`, `ReadOnly`, `Editor` (`EditorKind`), `Range`
  (`NumericRange`), `ScrubMultiplier`, `ScrubAxis` (commit `31df507`), `Expandable`, `Collapsed` e
  as listas `TextRules` e `ValueRules` (commit `7f3cb63`), o `VisibleWhen` (commit `bdf7ef8`) e o
  `Choices` (commit `cf909c1`); mais `Path` e `IsGroup`, que vêm da árvore. O `Visible` é da view, e
  o `Ignored`, da árvore (P6.1); desde o commit `095ad28`, o `Visible` e o `ReadOnly` são lidos
  pelos pais. Desde o commit `4db6457`, uma mudança de fato em qualquer uma delas, menos nas listas
  de regras, sai pelo `OptionChanged` do inspector (P7.8), como as de cultura e de layout do
  `InspectorOptions`.

**A pilha** (fixa e nessa ordem; cada camada só mexe no que decide, e a seguinte sobrescreve)

1. `ReflectionPolicy`: rótulo = nome do membro; editor pelo tipo (números → `Number`, `bool` →
   `Toggle`, enum → `Choice`, texto → `Text`, objetos → `Display`); `ReadOnly` quando não há setter
   público (setter privado, `init`, campo `readonly`), menos numa coleção por referência, em que só
   a troca dela é recusada (P4.7; commit `3fd3a8c`); objeto aninhado expansível, a menos que a flag
   global exija o atributo. Um setter não público esconde o membro (relatório, 3.6; P4.5; commit
   `095ad28`), e o `[InspectorReadOnly]` o traz de volta. Uma coleção não abre nos membros do tipo
   dela (P5.2; commit `4a55bd0`): é um seletor, sempre aberto na linha do item escolhido (P5.10;
   commit `ba26fe6`). Um tipo com mais de um editor fica fechado até a escolha (P5.5; commit
   `47b0997`).
2. `AttributePolicy`: os dez atributos `[Inspector*]` da seção 3.2. O `[InspectorExpandable]` vale
   no membro ou no tipo. Para um tipo com mais de um editor, o `[InspectorEditor]` e o
   `[InspectorExpandable]` são a escolha; sem nenhum dos dois, o `Create` avisa (P5.8). O filtro por
   nome do `GlobalOptions.Hide<T>` entra aqui, logo depois do `[InspectorIgnore]` (P6.7; commit
   `5d53c71`). A faixa e o scrubbing de uma coleção vão para a linha do item (P5.11; commit
   `d681dee`).
3. Manual: o que for definido no inspector depois do `Create`.

`Ignored`, `Visible`, `Order` e `Expandable` valem nas linhas (`Rows`, commit `a8bf9db`), que são o
que a view mostra, então podem mudar a qualquer momento, inclusive depois do bind: um nó ignorado
sai com a subárvore; um objeto que não é expansível aparece como campo `Display`, sem os filhos;
irmãos saem por `Order`. Nos empates vale a ordem de declaração, desde o commit `bb1184b` (P5.1). A
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
commit `c8a2720`). Não há `map`, `Modify`, provider nem callback.

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

Desde o commit `a8bf9db`, a enumeração entrega a árvore inteira, então a regra alcança também os
nós ignorados e os de dentro de objetos não expansíveis (antes ela só via as linhas da view). Para
percorrer só o que a view mostra, `inspector.Rows`.

**Exceções**: um caminho desconhecido lança `KeyNotFoundException` com o nome do nó em que a busca
começou e o caminho (`'Foo' has no field at path 'Moo.Nope'.`). Exceções das políticas e de
atributos inválidos (por exemplo `[InspectorRange(10, 1)]`) sobem sem tratamento. Revisto pela
premissa (seção 0; 3.11): uma exceção dentro da descoberta vira evento e fallback, e só o uso errado
da API continua lançando; o atributo inválido é ponto fraco, e o `Create` segue sem ele (P0.2).
Aplicado no commit `9db2e52`.

**Verificado**

- 36 testes num probe fora do repositório: padrões da reflection, detecção de somente leitura,
  cada atributo, precedência manual > atributos > reflection, ignorar com subárvore, ordem, flag
  global, exceções e binding pela árvore resolvida.
- Os testes pegam erro de verdade: com a precedência invertida de propósito numa cópia da
  biblioteca, 9 deles falham.
- O `Program.cs` antigo do NoHost, rodado contra a biblioteca nova, imprime exatamente o mesmo de
  antes.
- A solução compila sem erros e sem warnings.
- Na revisão `25b0ee0`: o NoHost, reescrito no formato novo, imprime exatamente o mesmo; a solução
  continua compilando sem erros e sem warnings.

**Confirmado em 27/09** (P4.3): com a flag global ligada, a permissão de expandir vale por membro ou
por tipo e não passa para os níveis de baixo, porque a flag existe justamente para não propagar. No
TuxHost, `Boo.Details` expande pelo atributo, mas `Details.Doo` não, porque o tipo `Doo` não tem o
atributo. É o comportamento do `TypeSafeLock` do original. Um atributo que propague fica como
ideia.

**As views**: os cinco cortes entraram, os dois alvos (P7.1), a view WinForms (P7.2, P7.3), o
scrubbing com os valores mistos, as válvulas com a premissa de erros nas views e a view WPF, nos
commits `e6cca32`, `8dfaab4`, `ce15b9f`, `6632d73` e `a93b75c`. Em 09/10 vieram as do Avalonia e
do ImGui e o host do Terminal.Gui (commits `6ba0290`, `a885789` e `1d86b96`), cada framework num
projeto (commit `424f6d0`), e os primitivos passaram a ser os da PixieLib; com as respostas do mesmo
dia, o Avalonia ganhou o `(?)`, as válvulas e o scrubbing (commits `507b528`, `8853770` e
`b0deeb2`). O que vem depois é a sessão própria do cache do modelo de tipo. Já entraram o
`ValueChanged` (commit `305952f`), os sanitizadores (commit `7f3cb63`), os nós manuais, botão e
campo só de exibição (commit `42e2129`), o layout no `InspectorOptions` (commit `9674fca`), a
visibilidade condicional (commit `bdf7ef8`) e os itens de escolha (commit `cf909c1`); o gancho de
conversão saiu da lista (P2.9).
