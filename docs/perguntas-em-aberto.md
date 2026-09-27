# Perguntas em aberto

Todas as perguntas sem resposta até 27 de setembro de 2026, juntadas das notas
(`notas-modernizacao.md`) e das conversas. Cada uma termina com a origem entre parênteses e, quando
eu já tinha sugerido algo, traz a sugestão. As marcadas com **(antes das views)** são as que eu
resolveria antes de começar a apresentação. O fluxo atual está explicado no relatório "Fluxo e
políticas do Inspector" (https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk).

Dá para responder pelo número, por exemplo: "2.3: sim" ou "3.1: só o grupo aberto".

---

## 1. O Inspector

1. **Herança ou composição** (antes das views). O `Inspector` continua sendo um `InspectorNode`, e
   com isso herda o `GetValue`/`SetValue` e as opções de membro (`Label`, `ReadOnly`, `Range`...),
   que não fazem sentido na raiz? Ou passa a guardar a raiz como um nó interno? Sugestão: guardar
   a raiz. (conversa de 27/09)
2. **Setter abstrato de volta** (antes das views). Voltar ao desenho do main, com o getter comum e o
   setter abstrato, e um tipo de nó por comportamento fixo na criação (raiz, membro e, depois,
   botão, exibição e cabeçalho)? Sugestão: sim, com grupo ou folha decidido em tempo de execução
   dentro do nó de membro, porque o `Expandable` pode mudar depois do `Create`. (3.10)
3. **Enumerável** (antes das views). O inspector e o nó deixam de ser `IEnumerable`? Sugestão: sim,
   com um nome por percurso: `Nodes` (todos os nós), `Rows` (as linhas da view) e `Children` (os
   filhos diretos). (3.10)
4. **Eventos** (antes das views). Já decidido: alguns no inspector, a maior parte nos nós. Falta a
   lista: quais ficam em cada um (bind começou e terminou, falha com payload, `ValueApplied`, branch
   comprometida...)? (3.10, 3.3 e 1.1, item 9)
5. **`InspectorOptions`**. O que entra nas opções por inspector (altura de campo, espaçamento,
   recuo...) e como elas sobrescrevem o `GlobalOptions`? (seção 7)
6. **Nós manuais**. Botão, campo só de exibição e cabeçalho entram direto no inspector, com nomes
   como `AddButton(...)` e `AddDisplay(...)`? (seção 5, item 4)
7. **`TypeBinderMode`**. Continua existindo um modo "só o que foi declarado", além do automático?
   (3.2)
8. **Aplicar sob demanda**. O Apply e o Reload do original, que nunca foram terminados, entram?
   (1.1 e 3.10)

## 2. Binding

1. **Nomes**. Os métodos citados eram exemplos. Quais ficam, e em PascalCase (`Bind`, `Unbind()`,
   `Rebind`, `AddBind`, `RemoveBind`) ou minúsculos, como o `bind` de hoje? (seção 0)
2. **Troca da instância da raiz por outro tipo**. A árvore é refeita. A configuração feita na árvore
   anterior se perde, ou o inspector guarda uma árvore por tipo? (3.10)
3. **Multi-bind com tipos diferentes**. Pôr mais um objeto no bind exige o mesmo tipo dos que já
   estão ligados? Sugestão: sim. (3.10)
4. **Valores mistos**. Com vários objetos, cada nó tem um valor por objeto. Como o `GetValue`
   devolve isso, e como a view mostra "misto"? O scrubbing aplica o delta em cada objeto, como no
   original? (3.3)
5. **Tirar o último objeto**. Tirar o último objeto do bind equivale ao `Unbind()`? Sugestão: sim.
   (conversa de 27/09)
6. **Objeto → UI** (antes das views). `INotifyPropertyChanged` para quem implementa e um `Refresh()`
   manual para o resto? (3.3)
7. **Texto → valor** (antes das views). `TypeConverter` ou `IParsable<T>`, com a cultura invariante
   ou a atual? Hoje não há conversão: `SetValue("5")` num `int` lança. (seção 5, item 3; 3.3)
8. **Faixa no `SetValue`**. O `SetValue` deve limitar, ou recusar, valores fora do
   `[InspectorRange]`? Hoje ele aceita 999 numa faixa de 0 a 255. (relatório)
9. **Sanitizadores e gancho de conversão**. Os sanitizadores ficam separados em texto e valor, numa
   lista ordenada? E o gancho de conversão (o "TheBrute") é por campo, global ou os dois? (3.3)

## 3. O objeto do grupo

1. **Quais grupos**. A regra vale para todo membro com filhos ou só para o grupo aberto? Uma class
   mostrada fechada, com editor próprio (um `Font` com o diálogo de fonte), é editada trocando o
   objeto. Sugestão: só o grupo aberto. (3.10)
2. **Structs**. Struct não tem identidade, então a detecção de troca não se aplica a ela, e editar
   um filho já regrava a struct no dono. Fica assim? (3.10)
3. **Quando detectar**. A cada leitura pela cadeia de pais, só no `Refresh()`, ou nos dois?
   (proposta da 3.10)
4. **O que "desativada" faz**. Os filhos recusam gravação e a view desabilita a branch. As leituras
   continuam? E um novo bind limpa o estado? (proposta da 3.10)
5. **A raiz**. A raiz entra na mesma regra, e o objeto ligado só muda pelo bind? (proposta da 3.10)

## 4. ReadOnly e expansão

1. **ReadOnly num objeto aninhado (class)**. Um `[InspectorReadOnly]` explícito num objeto aninhado
   passa para os filhos? Hoje vale só para o próprio membro. (seção 5, item 6)
2. **ReadOnly efetivo** (antes das views). O filho de uma struct somente leitura tem
   `ReadOnly = false`, mas o `SetValue` recusa. Como o nó expõe o valor efetivo, para a view
   desabilitar o editor? (checklist)
3. **Expandir com a flag global**. Com `RequireExpandableAttribute` ligada, a permissão vale só para
   o membro ou tipo marcado (como hoje e como no `TypeSafeLock`) ou passa para os níveis de baixo?
   (seção 5, item 5; seção 7)
4. **`GlobalOptions` ao vivo**. A flag é lida só no `Create`, e mudar depois não afeta um inspector
   que já existe. Fica assim? (relatório)

## 5. Descoberta

1. **Ordem dos irmãos**. A reflection devolve as propriedades antes dos campos, e os membros
   próprios antes dos herdados. Fica assim, com `[InspectorOrder]` para quem quiser outra ordem, ou
   a descoberta tenta recuperar a ordem de declaração? (relatório; seção 7)
2. **Coleções e arrays**. Hoje viram grupos com `Capacity`, `Count` e `Length`, sem os itens. Como
   devem aparecer? (relatório)
3. **Propriedades calculadas**. `PxPoint.IsEmpty` aparece como filho. A descoberta esconde
   propriedades calculadas só de leitura, ou isso fica para o `[InspectorIgnore]`? (relatório)
4. **Membro escondido com `new`** (antes das views). Aparece duas vezes, e o indexador acha o do
   tipo derivado. Fica só o do tipo derivado? (checklist)
5. **Tipos com editor próprio**. `System.Drawing.Color` vira um grupo com `A`, `R`, `G`, `B`... Como
   a descoberta reconhece os tipos que deveriam ter editor próprio (cor, fonte)? (3.2)
6. **Cache**. Só a lista de membros pode ser cacheada por tipo, porque os nós guardam as opções de
   cada inspector. Confirma? (3.1 e 3.10)

## 6. Opções e editores

1. **`Visible`**. Está sem uso: ninguém escreve nem lê. Sai, ou fica para a visibilidade
   condicional? E qual a diferença para o `Ignored`? (seção 7; relatório)
2. **Visibilidade condicional**. O formato é algo como `node.VisibleWhen = obj => ...`, o sucessor
   do `VariablePool`? (3.2)
3. **Editores do `EditField()`**. Como declarar itens de escolha (enum, bool, lista), seletores
   (cor, fonte) e a ação de um botão? (1.2; checklist)
4. **Filtro pelo objeto**. Existe um sucessor tipado do `IVarProvider`, uma interface que o próprio
   objeto implementa para esconder campos? (3.2; checklist)
5. **Seletor por expressão**. `inspector[f => f.Moo.MooX]`, que o compilador checa: entra quando?
   (3.2)

## 7. Views e estrutura

1. **Alvo da biblioteca** (antes das views; a primeira decisão). Com as views, a biblioteca volta a
   precisar do Windows. Só `net10.0-windows` (e o TuxHost deixa de rodar fora do Windows), os dois
   alvos de novo, com o bloco condicional, ou as views num projeto à parte (uma segunda DLL, só
   delas, e o núcleo continua `net10.0`)? (3.7; conversa de 27/09)
2. **Fábricas**. Nomes distintos por plataforma (`CreateWinFormsView`, `CreateWpfView`), para um
   projeto só WinForms não precisar referenciar o WPF? (3.5)
3. **Lista ou árvore**. A view monta as linhas a partir de uma lista plana, como a enumeração de
   hoje, ou percorre a árvore pelos filhos? (conversa de 27/09)
4. **Válvula de escape**. Um callback por plataforma, como `ControlCreated(caminho, controle)`, para
   o que a configuração agnóstica não cobrir? (3.5)
5. **Passo de layout** (antes das views). Fica no núcleo, agnóstico, e devolve os retângulos de cada
   linha (linha, rótulo e editor)? Ele depende dos itens 8.1 e 8.2. (3.4)

## 8. Primitivos e PixieLib

1. **Precisão** (antes das views). Manter as variantes int e float (como o `System.Drawing`) ou um
   tipo só em double (como o WPF)? (seção 5, item 1)
2. **Primitivos novos** (antes das views). Criar `PxRect`, `PxPadding` e `PxDock`? (seção 5, item 1)
3. **Cores**. Ganham o prefixo (`PxColor`) ou continuam com o espaço de cor no nome (`ArgbColor`,
   `HslColor`)? (seção 5, item 1)
4. **CS0457**. A conversão `ArgbColor → HslColor` existe nas duas structs e dá ambiguidade no
   primeiro uso. Em qual das duas ela fica? (3.4)
5. **Regras de conversão**. Confirma as de 3.4? Implícitas com o `System.Drawing`, explícitas
   saindo do WPF, e o `Size` do WPF explícito ou com clamp, porque ele não aceita negativos. (3.4)
6. **PixieLib: precisão padrão**. `float`, que repassa para o `System.Numerics`, ou `double`, como o
   `Vec2` do C++? (3.9)
7. **PixieLib: segunda DLL**. Quando o layout usar os primitivos, o inspector passa a depender da
   PixieLib. Aceita essa exceção à regra de uma DLL? (3.9)
8. **PixieLib: onde e como**. Confirma as propostas de 3.9: a pasta `dotnet/` no repositório
   `Sakamoto0110/PixieLib`, e `PxPoint`/`PxSize` sobre `PxVec2`? (3.9)
9. **Sufixo de precisão**. Os primitivos atuais usam o `F` do `System.Drawing` (`PxPointF`); o
   gerador da PixieLib usaria `f`, `d` e `i` (`PxVec2f`). Qual fica? (3.9)

## 9. O que eu posso aplicar assim que você responder

1. Ligar que lança se já houver objeto ligado, `Unbind()` e religar, ainda sem o multi-bind. Posso?
2. A regra do objeto do grupo: o `SetValue` de um grupo lança, e a gravação de volta da struct vai
   por um caminho interno. Depende de 3.1.
3. O inspector guardando a raiz, sem herdar do nó. Depende de 1.1.
4. Tirar o `IEnumerable` e dar nome aos percursos. Depende de 1.3.
