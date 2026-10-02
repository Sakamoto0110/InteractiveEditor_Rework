# Passagem de contexto

Para retomar o trabalho num contexto novo. Estado de 2 de outubro de 2026, na branch
`rework-claude`, com as respostas do Rafael de 27/09, 29/09, 01/10 e 02/10 aplicadas; o último
commit de código é o `ccc3a5d`. Ler isto inteiro antes de mexer em qualquer coisa.

---

## 1. Antes de qualquer commit

Regra do Rafael: **só subir para o GitHub se o author for ele**.

- Author e committer: `Rafael Sakamoto <rafael.sakamoto1@hotmail.com>`. Num clone novo, configurar
  antes do primeiro commit:

  ```
  git config user.name "Rafael Sakamoto"
  git config user.email "rafael.sakamoto1@hotmail.com"
  git config commit.gpgsign false
  ```

- No fim da mensagem, os trailers de coautoria e de sessão que a própria sessão indicar.
- Depois do push, conferir no GitHub que os commits aparecem com o login `Sakamoto0110`. Com `N`
  sendo quantos commits olhar, devem sair duas linhas por commit (author e committer), todas com
  `Sakamoto0110`:

  ```
  repo=Sakamoto0110/InteractiveEditor_Rework
  curl -s "https://api.github.com/repos/$repo/commits?sha=rework-claude&per_page=N" |
    grep '"login"'
  ```

- A branch de trabalho é a `rework-claude`, a pedido do Rafael, mesmo que a sessão sugira outra:
  `git push -u origin rework-claude`. Na dúvida, perguntar.
- Mensagens de commit em inglês, com prefixo: `(refactor)`, `(docs)`, `(fix)`, `(feat)`. Mudança
  de código e atualização das notas em commits separados, o de `(docs)` citando o hash do outro.

## 2. Como o Rafael trabalha

- Conversa em português, sem emojis. As notas também são em português, com linhas de até 100
  colunas e referências a commits.
- O foco é simplificar ao máximo, mas sem juntar arquivos só para diminuir a contagem: um conceito
  por arquivo.
- Redesenhar com calma, uma coisa por vez, e tratar a causa em vez de remendar sintomas.
- Decisão dele vai logo para as notas (seção 0), e o que é só proposta fica marcado como proposta.
- Os nomes de métodos que ele cita são exemplos, não para levar ao pé da letra.
- Mudança de desenho só depois de ele confirmar. Quando ele pedir, aplicar, verificar e subir.
- Ele responde as perguntas pelo número ("2.3: sim"), às vezes com texto livre e com adendos
  ("2.7.1"). Quando ele pede opinião ou sugestão, dar uma recomendação, não uma lista de opções.
- O NoHost fica versionado, ignorado pelo `.gitignore` e dentro da solução. Não tirar da solução.
- O ideal é uma DLL só. Se aparecer outra, dizer para que ela serve.

## 3. Onde está cada coisa

- `docs/notas-modernizacao.md`: as notas completas. Decisões na seção 0, agrupadas por tema e com
  o número da pergunta (`P2.2` é a pergunta 2.2); o `Inspector` na 3.10, a premissa de erros na
  3.11, a PixieLib na 3.9, o que sobrou da lista antiga de decisões em aberto na seção 5, o
  checklist na 6 e o modelo de opções na 7.
- `docs/perguntas-em-aberto.md`: sem pergunta em aberto. As da view WinForms (7.7 a 7.13) ele
  respondeu em 02/10 com as sugestões (seção 0 das notas, Views). Em 29/09 ele aceitou as sugestões
  das que sobravam (1.14, 5.9, 5.10, 6.7 e 7.5) e as escolhas que eu tinha deixado para ele
  confirmar, em 01/10 as das cinco escolhas que sobraram da 5.10 e da 7.5 (4.7, 5.11, 5.12, 5.13 e
  7.6), e em 02/10 as da 6.2 e da 6.3; tudo isso está na seção 0 das notas, e a explicação que ele
  pediu da 6.2 e da 6.3 (o `VariablePool` e o `EditField()` lidos no 0.7.1a e no OverlayApplication)
  está na seção 3.12. Os números antigos valem, e os novos seguem a numeração de cada seção.
- Relatório "Fluxo e políticas do Inspector": https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk
  (privado). O HTML não está no repositório; para atualizar, ler o artifact pela URL, editar e
  publicar de novo na mesma URL. Ele descreve o código em `9a1fffa` e ficou velho em quase tudo
  depois desta rodada; atualizar só se o Rafael pedir.
- Projetos da solução: `InteractiveEditor` (a biblioteca, `net10.0` e `net10.0-windows`),
  `DemoObjects` (os tipos de teste: `Foo`, `Moo`, `Doo`, `Boo`), `TuxHost` (o console de
  verificação, roda no Linux), `NoHost` (local do Rafael, `net10.0-windows`), `WindowsHost` e
  `WpfHost`.

## 4. Como verificar uma mudança

- O `dotnet` 10 pode não estar instalado. Nesta sessão ele não estava, e foi instalado em
  `~/.dotnet` pelo script oficial:

  ```
  curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
  bash dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
  ```

- Build da solução inteira, inclusive os hosts de Windows, no Linux. Tem que dar 0 warnings e 0
  erros nos 6 projetos:

  ```
  dotnet build InteractiveEditorSolution.slnx -p:EnableWindowsTargeting=true
  ```

- O TuxHost imprime 67 linhas desde o commit `13534b0` (eram 69; o `Boo.Secret`, com setter
  privado, deixou de aparecer). Salvar a saída antes da mudança e comparar depois com `cmp`:

  ```
  dotnet run --project TuxHost/TuxHost.csproj
  ```

- Para testar um comportamento, fazer um console pequeno fora do repositório, referenciando
  `InteractiveEditor.csproj` (e `DemoObjects.csproj`, se precisar dos tipos de teste). O desta
  sessão chegou a 646 checagens, num arquivo por assunto: bind e multi-bind, grupo e raiz,
  enumeração, `ReadOnly` e `Visible`, falhas do `Create`, valores e `Refresh()`, troca por fora,
  `INotifyPropertyChanged`, o que muda junto, gravação, controle do binder, primitivos, seletor por
  expressão, ordem de declaração, nós manuais, modo manual, coleções (o seletor e o editor de
  lista), cores, membro escondido com `new`, filtro por nome, inspector sem tipo, layout,
  visibilidade condicional e lista de escolha. Um segundo console testa a assembly ausente, e um
  terceiro, o `probe-windows` (`net10.0-windows`, desde o commit `ccc3a5d`), as conversões dos
  primitivos com o WinForms e o WPF (21 checagens), rodando no Wine (abaixo). Os três ficam no
  scratchpad da sessão e não passam para a próxima, mas o Rafael recebeu uma cópia deles
  (`console-de-testes.zip`, com um `LEIA-ME.txt` que diz como rodar; a de 02/10 depois do
  `ccc3a5d` já tem os três). Se ele mandar o zip, descompactar fora do repositório e corrigir o
  caminho do clone nos `.csproj`, se for outro; se não, a lista acima serve de roteiro para refazer
  o que for preciso.
- Antes de dar uma mudança por pronta, conferir também que os testes pegam o erro: desfazer a
  mudança (ou quebrar de propósito uma cópia) e ver os testes novos falharem.
- Para rodar um app `net10.0-windows` no Linux (se ele não tocar em WinForms ou WPF):
  `dotnet exec --runtimeconfig`.
- Para rodar o WinForms e o WPF no Linux, o Wine (testado em 02/10). O runtime de desktop do .NET
  (`Microsoft.WindowsDesktop.App`) só existe para Windows; o Wine dá as APIs do Windows, e o app vai
  publicado para `win-x64` com o runtime junto. O Xvfb já vem no ambiente; o resto se instala:

  ```
  apt-get install -y --no-install-recommends wine64 libwine fonts-wine x11-utils imagemagick xdotool
  Xvfb :99 -screen 0 1024x768x24 &        # numa chamada em segundo plano
  export WINEPREFIX=<scratchpad>/wineprefix DISPLAY=:99 WINEDEBUG=-all
  export WINEDLLOVERRIDES="mshtml=;winedbg.exe=d"
  dotnet publish App.csproj -r win-x64 --self-contained -o "$WINEPREFIX/drive_c/app"
  /usr/lib/wine/wine64 'C:\app\App.exe'
  ```

  O print sai com `import -window root print.png`, `xwininfo -root -tree` diz quando a janela abriu,
  e o `xdotool` clica e digita (`xdotool mousemove X Y click 1`, `xdotool type "12"`). Um app
  `Exe` (e não `WinExe`) escreve no terminal, então um console de teste com `Console.WriteLine` roda
  igual.

Pegadinhas já vistas:

- C# 14: `field` é palavra-chave dentro de acessores de propriedade. Por isso os padrões usam
  `fi` e `pi` (`FieldInfo fi`, `PropertyInfo pi`).
- Blocos `extension` aceitam propriedades e operadores, mas não conversões (CS9282).
- Quase todos os `.cs` têm BOM, e o final muda de arquivo para arquivo: com ou sem quebra de linha
  no fim, às vezes com um espaço sobrando. Ao editar, manter o BOM e o final como estão.
- `git add NoHost/Program.cs` reclama que a pasta está no `.gitignore` e sai com erro, o que corta
  um `&& git commit` encadeado. O arquivo é versionado, então `git add -u` resolve.
- Reflection preguiçosa: `GetMembers` funciona mesmo quando uma assembly usada por um membro está
  ausente; quem lança é a leitura da assinatura (`PropertyType`, `FieldType` e também
  `GetIndexParameters()`). Para testar, uma biblioteca com uma dependência apagada da pasta de saída
  depois do build, rodando com `dotnet X.dll` (o `dotnet run` copiaria a DLL de volta).
- Os eventos da criação são estáticos: um console de teste precisa tirar a assinatura no fim, senão
  ela vale para os testes seguintes.
- O `Refresh()` não faz nada sem `InstanceToView` (controle do binder); a leitura à mão é o
  `Reload()`. E a view mostra o `ViewValue`: o `GetValue()` lê o objeto na hora.
- Um membro escondido com `new` fica no grupo do tipo que o declara (`TypeGroupNode`, commit
  `c9868a9`): o caminho é `Base.Value` ou `Derived.Value`, e `Value` sozinho acha o do derivado.
- Os primitivos são em `double`: num teste, `(int)node.GetValue()` de um campo de `PxPoint` lança
  `InvalidCastException`.
- Uma coleção tem sempre a linha do item escolhido embaixo (commit `eb58a3e`): o caminho de um
  campo do item é `Items.Item.MooX`, e o `Items.Add("MooX")` lança; os membros do item entram em
  `inspector["Items.Item"].Add("MooX")`.
- Wine: o `wine64` fica em `/usr/lib/wine/wine64`, fora do PATH. Não desligar o `mscoree` dele
  (`WINEDLLOVERRIDES="mscoree="`): sem ele, o .NET não carrega as próprias DLLs ("Could not load
  file or assembly System.Runtime.dll"). Sem o `fonts-wine`, o WPF cai no mapeamento de fontes
  (`TypefaceMap.MapUnresolvedCharacters`). E não é o Windows: as fontes são substitutas e não há
  tema visual.
- No alvo `-windows` não há `System.IO` nem `System.Net.Http` nos usings implícitos (o SDK de
  desktop tira os dois), e a biblioteca tira os do WinForms (commit `ccc3a5d`). Um arquivo do núcleo
  que use `File` ou `Path` precisa do `using`, senão o `-windows` não compila; o mesmo vale num
  console de teste com `UseWPF`.
- O `.gitignore` só cobre o `bin/Debug` de cada projeto: um `dotnet publish` (Release) a partir do
  repositório deixa pastas `bin/Release` sem ignorar. Apagar antes de commitar.

## 5. O código hoje, em resumo

- `Inspector.Create<T>()`: a descoberta (`ReflectionDiscovery.AddMembers`) monta a árvore, e cada
  nó passa por `ReflectionPolicy.Apply` e depois `AttributePolicy.Apply`. A camada manual vem
  depois, no próprio inspector (`inspector["Moo.MooX"].Label = ...`).
- `Inspector` não é mais um nó (commit `0bc2f9d`): guarda a raiz num `RootNode` interno e expõe o
  `Id`, o `Name`, o `Mode`, as opções (`Options`), o objeto ligado (`Instance`), o indexador e o
  `Node<T>`, a enumeração e `Rows`. `InspectorNode` é abstrato, com as opções como propriedades, o
  indexador por caminho relativo (encadeável), o `GetValue` comum e o `SetValue` abstrato; os nós
  são `MemberNode` (campo ou propriedade), `ButtonNode`, `DisplayNode`, `TypeGroupNode` (commit
  `c9868a9`), `CollectionNode` e `ItemNode` (commit `eb58a3e`, os dois derivados do `MemberNode`) e
  o `RootNode` interno. A enumeração entrega a árvore inteira, e `Rows` entrega as linhas da view
  (commit `b456398`).
- `ReadOnly` e `Visible` são lidos pelos pais (commit `13534b0`), e um setter não público esconde o
  membro, que o `[InspectorReadOnly]` traz de volta.
- Falhas no `Create` (commit `1535874`): viram eventos estáticos (`DiscoveryFailed`, com a
  severidade) e ficam em `inspector.Report`; um membro ilegível sai da árvore, um atributo inválido
  é pulado, e só o fatal sobe. Os tipos ficam em `InteractiveEditor.Diagnostics`.
- `Dispose` (commit `ffee3a7`): o inspector e os nós são descartáveis, e o `GlobalOptions` fica
  travado enquanto houver um inspector vivo (mudar a flag lança). Programas de teste precisam
  descartar os inspectors antes de mexer na flag; o TuxHost faz isso no fim do `Print`.
- Bind (commits `4dec125` e `c201877`): `Bind` lança se já houver objeto ligado ou se o objeto não
  servir para a árvore (um tipo derivado serve); `AddBind` põe mais objetos, `RemoveBind` tira um,
  `Unbind()` solta todos, e `Rebind` confere e depois desliga e liga. `GetValue` lê o primeiro
  objeto, `GetValues` todos, `IsMixed` diz se diferiam na última leitura (commit `18dc069`), e
  `SetValue` grava em todos. O inspector avisa por `BindRegistered`, `BindRemoved` e `Unbound`.
- Valores (commit `eb497c6`): cada nó guarda a última leitura e dispara `ValueChanged` com a origem
  quando ela muda; `inspector.Refresh()` relê tudo, e objetos com `INotifyPropertyChanged` avisam
  sozinhos (commit `b6a99d8`). Getter ou setter que lança vira `Failure` e `BindFailed` no nó, sem
  exceção; o uso errado continua lançando. A gravação e os avisos releem também o que muda junto: a
  struct acima do campo gravado e o que fica abaixo de um objeto fechado (commit `0602d0a`).
- Gravação (commit `e136f82`): o `SetValue` prepara o valor antes de gravar, com as regras de texto,
  a conversão pela cultura do `inspector.Options` (`IParsable<T>` ou `TypeConverter`), as regras de
  valor e a faixa. O que falha no preparo vira `Failure` no nó, e nada é gravado; um valor de um
  tipo sem relação com o do membro lança.
- Nós manuais (commit `0c97638`): `AddButton(nome, texto, ação)` e `AddDisplay(nome, getter)`, no
  inspector ou em qualquer nó; o `ButtonNode` roda a ação no `Press()`, e o `DisplayNode` lê o
  getter como um membro.
- `TypeBinderMode` (commit `f12ebf9`): `Create<T>(TypeBinderMode.Manual)` começa sem membros, e o
  `Add("X")` põe um membro com as camadas de reflection e de atributos. O `Inspector.Create()` sem
  tipo (commit `4e6b3d0`) acha os nomes no bind, que fixa o tipo até o `Unbind()`; o `Member` do nó
  fica null até lá.
- Filtro por nome (commit `f2ec855`): `GlobalOptions.Hide<T>(nomes)` antes do `Create`, travado
  como as outras opções globais; conta como um `[InspectorIgnore]`, e os testes precisam chamar
  `Unhide<T>()` no fim, porque o registro é global.
- Visibilidade condicional (commit `c589e2a`): o `VisibleWhen` do nó é uma regra sobre o objeto
  ligado, relida no fim de cada leitura ou gravação pelo `CheckRules` do `RootNode`, e guardada; o
  `Visible` junta a regra, o valor à mão e os pais, e uma resposta nova dispara o `VisibleChanged`.
- Lista de escolha (commit `b92267c`): o `Choices` do nó é uma função lida pelo `GetChoices()`
  quando a view abre a lista; sem ela, um enum lista os próprios valores.
- Descoberta: ordem de declaração (commit `6c17a15`), coleções sem os membros do tipo delas
  (commit `1b9fcf6`) e cor numa linha `Display` fechada até escolherem o editor, com um aviso
  (commit `7482c5f`). O seletor por expressão é o `Node<T>` (commit `e06b6f7`).
- Coleções (commit `eb58a3e`): o `CollectionNode` tem o editor `Selector`, o `Items` (o que a
  coleção do primeiro objeto tinha na última leitura) e o `SelectedIndex`; a linha de baixo, `Item`,
  lê e grava o item escolhido como um membro, com os membros do tipo do item embaixo. A escolha
  segue os itens (o primeiro ao ganhar itens; um objeto que muda de lugar leva a escolha junto,
  commit `a5abdad`; o último se o escolhido sair), outro objeto no lugar escolhido não é troca, e
  uma `ObservableCollection` avisa sozinha. A faixa e o scrubbing do membro vão para a linha do item
  (commit `2b5d24d`), e uma coleção por referência sem setter público fica com o conteúdo editável,
  só a troca dela é recusada (commit `53e915c`, pelo `Locked` do `MemberNode`). O editor de lista
  (commit `5395dea`) é o `EditorKind.List`, com `AddItem()`, `RemoveItem(i)` e `MoveItem(de, para)`
  no `CollectionNode`, que gravam na hora em cada objeto ligado.
- Controle do binder (commit `18dc069`): `inspector.Options.BinderControl`, `Automatic` por padrão.
  Sem `ViewToInstance`, o `SetValue` guarda o valor no nó e o `Apply()` grava; sem
  `InstanceToView`, os avisos do objeto e o `Refresh()` não chegam à view, e o `Reload()` relê. A
  view mostra o `ViewValue`, e não o `GetValue()`, que lê o objeto. Os de força são o
  `ForceApply()`, o `ForceReload()` e o `ForceClear()`, cada um com o seu evento.
- Troca por fora (commit `55e7173`): um grupo de tipo class cujo objeto foi trocado depois do bind
  dispara `ObjectReplaced`; sem aceite, o ramo fica comprometido e lança na leitura e na gravação
  até um `Rebind`. Struct fica de fora.
- Binding pela cadeia de pais: só a raiz guarda a instância, struct é gravada de volta no dono, e a
  gravação respeita o `ReadOnly`. O `SetValue` público recusa grupo aberto e a raiz, e a gravação
  de volta passa pelo `WriteTo` interno (commit `a2d8ffe`).
- Primitivos em `InteractiveEditor/Primitives` (commits `2951ce3`, `3544a8e` e `f1de920`):
  `PxPoint`, `PxSize`, `PxRect` e `PxPadding` em `double`, `PxDock`, `PxColorArgb` e `PxColorHsl`,
  sem conversão implícita entre as cores. O `PxRect`, o `PxSize` e o `PxPadding` são usados pelo
  passo de layout. As conversões com o `System.Drawing` ficam nos dois alvos, e as com o WinForms e
  o WPF (o `Point`, o `Size`, o `Rect`, a `Thickness` e a `Color` do WPF, o `Padding` e o
  `DockStyle` do WinForms) só no `-windows`, nos arquivos `*.Windows.cs` (commit `ccc3a5d`).
- Passo de layout (commit `166ec4a`): `inspector.Layout(largura)` devolve as linhas de cima e o
  tamanho; cada `LayoutRow` tem os retângulos da linha, do rótulo e do editor nas coordenadas de
  onde está, e um grupo tem o painel dele, com as linhas de dentro a partir do canto do painel. As
  opções (`RowHeight`, `RowSpacing`, `Indent`, `LabelWidth`, `LabelSpacing`, `Padding` e `ListRows`)
  ficam no `InspectorOptions`. Não lê os objetos, e roda no Linux.
- Dois alvos, `net10.0` e `net10.0-windows` (P7.1, commit `ccc3a5d`): cada consumidor recebe uma
  DLL, a do alvo dele. O código de Windows fica em arquivos `*.Windows.cs`, que o `net10.0` não
  compila; hoje são só as conversões dos primitivos, e as views vão entrar do mesmo jeito.

## 6. O que falta

O checklist (seção 6 das notas) diz o que ficou e por quê. Em resumo:

- O que foi decidido em 29/09 já entrou: a árvore do membro escondido com `new` (P5.9, commit
  `c9868a9`), o filtro por nome (P6.7, commit `f2ec855`), o inspector sem tipo (P1.14, commit
  `4e6b3d0`), o seletor das coleções e o editor de lista (P5.10, commits `eb58a3e`, `a5abdad` e
  `5395dea`) e o passo de layout (P7.5, commit `166ec4a`).
- O que foi decidido em 01/10 também: a coleção só com getter com o conteúdo editável (P4.7, commit
  `53e915c`) e a faixa e o scrubbing do membro da coleção na linha do item (P5.11, commit
  `2b5d24d`); a P5.12, a P5.13 e a P7.6 confirmaram o que o código já fazia.
- O que foi decidido em 02/10 também: o `VisibleWhen` no nó (P6.2, commit `c589e2a`) e o `Choices`
  no nó (P6.3, commit `b92267c`). Ainda em 02/10, a direção do que falta: as views antes das
  sessões próprias, em cinco cortes (seção 0 das notas, Views). O corte 1, os dois alvos e as
  conversões dos primitivos (P7.1), entrou no commit `ccc3a5d`. As escolhas do corte 2, a view
  WinForms, foram respondidas com as sugestões (P7.7 a P7.13).
- Com as views: as fábricas com nomes distintos (P7.2), a view percorrendo a árvore (P7.3), os
  callbacks por plataforma e o agnóstico por linha (P7.4) e a premissa de erros nas views.
- Sessões próprias: a PixieLib (P8.7) e o cache do modelo de tipo (P5.6).

## 7. Próximo passo

1. Seguir as views, decididas em 02/10 antes das sessões próprias, nos cortes da seção 0 das notas
   (Views), um por vez. O corte 1, os dois alvos e as conversões (P7.1), entrou no commit `ccc3a5d`.
   Faltam a view WinForms (P7.2, P7.3), o scrubbing e os valores mistos, as válvulas e a premissa de
   erros (P7.4), e o WPF. A view WinForms segue as respostas de 7.7 a 7.13 (seção 0 das notas),
   verificada no Wine. Para reler o original: `Sakamoto0110/InteractiveEditor` (branch
   `InspectorVariant0.7.1a`) e `Sakamoto0110/OverlayApplication`, públicos, clonados só para
   leitura.
