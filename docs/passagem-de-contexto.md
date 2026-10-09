# Passagem de contexto

Para retomar o trabalho num contexto novo. Estado de 9 de outubro de 2026, na branch
`rework-claude`, com as respostas do neko de 27/09, 29/09, 01/10 e 02/10 aplicadas; o último
commit de código é o `b0deeb2`. Ler isto inteiro antes de mexer em qualquer coisa.

Em 02/10 o neko pediu duas passagens: esta, para continuar as views do InteractiveEditor, e uma
para a parte em C# da PixieLib, que é outro trabalho, numa sessão própria
(`docs/passagem-pixielib.md`, seção 3).

Em 09/10, a pedido do neko, a branch das views novas (`claude/vibrant-fermi-smwjw5`, aberta da
`main` em 25/09, sem os commits do rework) entrou nesta, numa sessão do projeto da PixieLib: o
núcleo passou a usar os primitivos da PixieLib, cada framework ganhou um projeto, e as views do
Avalonia e do ImGui e o host do Terminal.Gui foram refeitos sobre o núcleo do rework (seção 5). As
escolhas que eu fiz nisso o neko aceitou no mesmo dia (P7.20 a P7.23), e o que elas pediam entrou
nos commits `ad79532` a `b0deeb2`.

---

## 1. Antes de qualquer commit

Regra do neko: **só subir para o GitHub se o author for ele**.

- Author e committer: `Rafael Sakamoto <rafael.sakamoto1@hotmail.com>`. Num clone novo, configurar
  antes do primeiro commit:

  ```
  git config user.name "Rafael Sakamoto"
  git config user.email "rafael.sakamoto1@hotmail.com"
  git config commit.gpgsign false
  ```

- Sem trailers no fim da mensagem, nem os de coautoria nem os de sessão, mesmo que a sessão os
  indique: em 09/10 o histórico desde o `99518fa` foi reescrito para tirá-los (commit `0770e7d`).
  Um clone de antes disso diverge da branch; trazer só os commits novos para cima dela, sem
  `--force`.
- Depois do push, conferir no GitHub que os commits aparecem com o login `Sakamoto0110`. Com `N`
  sendo quantos commits olhar, devem sair duas linhas por commit (author e committer), todas com
  `Sakamoto0110`:

  ```
  repo=Sakamoto0110/InteractiveEditor_Rework
  curl -s "https://api.github.com/repos/$repo/commits?sha=rework-claude&per_page=N" |
    grep '"login"'
  ```

- A branch de trabalho é a `rework-claude`, a pedido do neko, mesmo que a sessão sugira outra:
  `git push -u origin rework-claude`. Na dúvida, perguntar.
- Mensagens de commit em inglês, com prefixo: `(refactor)`, `(docs)`, `(fix)`, `(feat)`. Mudança
  de código e atualização das notas em commits separados, o de `(docs)` citando o hash do outro.

## 2. Como o neko trabalha

- Nunca chamar de Rafael: esse nome só aparece na identidade do git (seção 1) e nunca foi usado na
  conversa; quando o próprio nome aparece, é o sobrenome, Sakamoto. Chamar de neko quando o assunto
  é construir ferramentas e frameworks, como este, e de void no código de mais baixo nível, mesmo em
  C#; na dúvida, void.
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
- `docs/perguntas-em-aberto.md`: sem pergunta em aberto. As do merge de 09/10 (7.20 a 7.23) ele
  aceitou no mesmo dia, com as sugestões. As da view WinForms (7.7 a 7.13) ele respondeu em 02/10
  com as sugestões (seção 0 das notas, Views). Em 29/09 ele aceitou as sugestões das que sobravam
  (1.14, 5.9, 5.10, 6.7 e 7.5) e as escolhas que eu tinha deixado para ele confirmar, em 01/10 as
  das cinco escolhas que sobraram da 5.10 e da 7.5 (4.7, 5.11, 5.12, 5.13 e 7.6), e em 02/10 as da
  6.2 e da 6.3; tudo isso está na seção 0 das notas, e a explicação que ele pediu da 6.2 e da 6.3
  (o `VariablePool` e o `EditField()` lidos no 0.7.1a e no OverlayApplication) está na seção 3.12.
  Os números antigos valem, e os novos seguem a numeração de cada seção.
- `docs/passagem-pixielib.md`: a passagem da PixieLib em C# (02/10), para uma sessão própria, no
  repositório `Sakamoto0110/PixieLib`. Ela fica aqui porque a sessão que a escreveu podia ler a
  PixieLib, mas não subir nada nela (o app do Claude no GitHub sem permissão de escrita); o lugar
  dela é lá. A troca dos primitivos do InteractiveEditor pelos da PixieLib, a pergunta 6.8 de lá,
  entrou em 09/10 (commit `424f6d0`), sobre a cópia dela em `external/PixieLib`.
- Relatório "Fluxo e políticas do Inspector": https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk
  (privado). O HTML não está no repositório; para atualizar, ler o artifact pela URL, editar e
  publicar de novo na mesma URL. Ele descreve o código em `a694421` e ficou velho em quase tudo
  depois desta rodada; atualizar só se o neko pedir.
- Projetos da solução (13, desde 09/10): `InteractiveEditor` (o núcleo, `net10.0`), as views
  `InteractiveEditor.WinForms` e `InteractiveEditor.Wpf` (`net10.0-windows`),
  `InteractiveEditor.Avalonia` e `InteractiveEditor.ImGui` (`net10.0`), `DemoObjects` (os tipos de
  teste: `Foo`, `Moo`, `Doo`, `Boo`, `Coo`, `Hoo`, e o `Gadget`, com um membro por editor das
  views), `TuxHost` (o console de verificação, roda no Linux), `NoHost` (local do neko,
  `net10.0-windows`), os hosts `WindowsHost` (mostra um `Gadget` na view WinForms, commit
  `042a7fe`), `WpfHost`, `AvaloniaHost` e `ImGuiHost`, e o `TerminalHost` (Terminal.Gui, roda no
  Linux, com o `--dump` da seção 4). A PixieLib vem do nuget.org, o pacote `PixieLib` 0.1.0 (commit
  `6b7cd80`): só o núcleo a referencia, e as views e os hosts a recebem por ele.

## 4. Como verificar uma mudança

- O `dotnet` 10 pode não estar instalado. Nesta sessão ele não estava, e foi instalado em
  `~/.dotnet` pelo script oficial:

  ```
  curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
  bash dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
  ```

- Build da solução inteira, inclusive os hosts de Windows, no Linux. Tem que dar 0 warnings e 0
  erros nos 13 projetos:

  ```
  dotnet build InteractiveEditorSolution.slnx -p:EnableWindowsTargeting=true
  ```

- O TuxHost imprime 67 linhas desde o commit `095ad28` (eram 69; o `Boo.Secret`, com setter
  privado, deixou de aparecer). Salvar a saída antes da mudança e comparar depois com `cmp`:

  ```
  dotnet run --project TuxHost/TuxHost.csproj
  ```

- O `TerminalHost` tem um modo sem janela, que imprime a árvore de cada objeto de teste e confere o
  binding de cada linha; todas as linhas `bindings:` têm que sair `ok` (desde o commit `6ba0290`):

  ```
  dotnet run --project TerminalHost/TerminalHost.csproj -- --dump
  ```

- O `AvaloniaHost` e o `ImGuiHost` (Silk.NET, com OpenGL) são `net10.0` e abrem no Linux, numa tela
  virtual (`DISPLAY=:99`, com o Xvfb de baixo).

- Para testar um comportamento, fazer um console pequeno fora do repositório, referenciando
  `InteractiveEditor.csproj` (e `DemoObjects.csproj`, se precisar dos tipos de teste). O desta
  sessão chegou a 726 checagens, num arquivo por assunto: bind e multi-bind, grupo e raiz,
  enumeração, `ReadOnly` e `Visible`, falhas do `Create`, valores e `Refresh()`, troca por fora,
  `INotifyPropertyChanged`, o que muda junto, gravação, controle do binder, primitivos, seletor por
  expressão, ordem de declaração, nós manuais, modo manual, coleções (o seletor e o editor de
  lista), cores, membro escondido com `new`, filtro por nome, inspector sem tipo, layout,
  visibilidade condicional, lista de escolha, os eventos `OptionChanged` e `Disposed`, o espaçador,
  a largura máxima e o `(?)` no layout, e um valor por objeto (`SetValues`). Um segundo
  console testa a assembly ausente; um terceiro, o `probe-windows` (`net10.0-windows`, desde o
  commit `e6cca32`), as conversões dos primitivos com o WinForms e o WPF (21 checagens); e um
  quarto, o `probe-view` (WinForms, desde o commit `8dfaab4`), a view, com 136 checagens; e um
  quinto, o `probe-wpf` (desde o commit `a93b75c`), a view WPF, com 139. Os três últimos rodam no
  Wine (abaixo). Os cinco ficam no scratchpad da sessão e não passam para a próxima, mas o neko
  recebeu uma cópia deles (`console-de-testes.zip`, com um `LEIA-ME.txt` que diz como rodar; a
  última de 02/10 tem os cinco). Se ele mandar o zip, descompactar fora do
  repositório e corrigir o caminho do clone nos `.csproj`, se for outro; se não, a lista acima
  serve de roteiro para refazer o que for preciso. Desde o commit `424f6d0`, os três de Windows
  referenciam `InteractiveEditor.WinForms` ou `InteractiveEditor.Wpf`, e não mais o alvo `-windows`
  do núcleo, e os primitivos são os da PixieLib; nenhum dos cinco rodou depois do merge de 09/10.
  Em 09/10 foram feitos mais dois, também no scratchpad, que não passam para a próxima sessão: um
  do Avalonia, numa tela virtual, com os eventos de ponteiro e de teclado simulados (106
  checagens: os editores, os grupos, os mistos, as falhas, o `(?)`, as válvulas e o scrubbing), e um
  de WinForms e WPF no Wine para o rótulo de uma linha que não mostra os objetos (commit `e5960e1`).
  A seção 3.5 das notas diz o que o do Avalonia confere.
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
  `5a10ed6`): o caminho é `Base.Value` ou `Derived.Value`, e `Value` sozinho acha o do derivado.
- Os primitivos são em `double`: num teste, `(int)node.GetValue()` de um campo de `PxPoint` lança
  `InvalidCastException`.
- Uma coleção tem sempre a linha do item escolhido embaixo (commit `ba26fe6`): o caminho de um
  campo do item é `Items.Item.MooX`, e o `Items.Add("MooX")` lança; os membros do item entram em
  `inspector["Items.Item"].Add("MooX")`.
- Wine: o `wine64` fica em `/usr/lib/wine/wine64`, fora do PATH. Não desligar o `mscoree` dele
  (`WINEDLLOVERRIDES="mscoree="`): sem ele, o .NET não carrega as próprias DLLs ("Could not load
  file or assembly System.Runtime.dll"). Sem o `fonts-wine`, o WPF cai no mapeamento de fontes
  (`TypefaceMap.MapUnresolvedCharacters`). E não é o Windows: as fontes são substitutas e não há
  tema visual.
- No alvo `-windows` não há `System.IO` nem `System.Net.Http` nos usings implícitos (o SDK de
  desktop tira os dois), e a biblioteca tira os do WinForms (commit `e6cca32`). Um arquivo do núcleo
  que use `File` ou `Path` precisa do `using`, senão o `-windows` não compila; o mesmo vale num
  console de teste com `UseWPF`.
- Testes da view no Wine: o `SendKeys.SendWait` funciona (Enter, Esc, Tab, setas, espaço), o
  `Control.CheckForIllegalCrossThreadCalls = true` faz um acesso de outra thread lançar, e o
  `Application.DoEvents()` deixa a view fazer o layout que ela agenda. Os tooltips ficam num
  `ToolTip` interno da view (`Tips`), que o teste lê por reflection, como a linha sob o mouse
  (`Hovered`). O `Cursor.Position` move o mouse e gera o `MouseMove`, o `Graphics.CopyFromScreen`
  lê os pixels da tela, e uma janela modal se testa com um `Timer` que a encontra no
  `Application.OpenForms` enquanto o `ShowDialog` segura o teste.
- O Xvfb pode cair entre um turno e outro (o `Error creating window handle` no começo do
  `Application.Run` é ele). Subir de novo numa chamada em segundo plano, apagando antes o
  `/tmp/.X11-unix/X99` e o `/tmp/.X99-lock`.
- Um `Label` estreito quebra o texto em duas linhas em vez de cortar: foi o que fez o `(?)` sair
  "(?" e mais alto nos 16 px; por isso ele é desenhado (`HelpMark`).
- No Wine, o Shift e o Ctrl simulados (`keybd_event`) se perdem no movimento do mouse, porque o Wine
  relê as teclas no servidor X a cada evento do mouse. O `probe-view` e o `probe-wpf` pedem a tecla
  de verdade ("ASK shift-down") a quem os roda: o `run-wine.sh`, ao lado de cada um, roda o
  programa, aperta a tecla com o `xdotool` e responde com um arquivo; sem a variável `PROBE_FLAGS`
  (no Windows), o teste usa o `keybd_event`. A saída do programa no Wine termina as linhas com
  `\r\n`, e o script tira o `\r`.
- Um teste da view que muda uma opção de um nó (a faixa, o editor) precisa desfazer no fim: o
  `Count` ficou com uma faixa de 0 a 10 e prendeu o segundo objeto no máximo, dois testes depois.
- Texto num `int`: "7.6" é recusado, porque o texto vira o tipo do membro (P2.7); o "7,6 grava 8"
  das notas é para um número, não para texto.
- Um `dotnet publish` a partir do repositório compila em Release; desde o commit `a1ff0c3`, o
  `.gitignore` cobre as pastas `bin/Release` também.
- O `PxColorRgba` da PixieLib recebe o alfa no fim, `new(r, g, b, a = 255)`; o `PxColorArgb` antigo
  recebia no começo. Um `new(a, r, g, b)` de quatro bytes compila e dá outra cor.
- A PixieLib vem do pacote do nuget.org (commit `6b7cd80`). O que precisar mudar nela vai para o
  repositório `Sakamoto0110/PixieLib`, e volta numa versão nova do pacote, trocando o `Version` no
  `InteractiveEditor.csproj`.
- Dentro do namespace `InteractiveEditor.ImGui`, o nome `ImGui` sozinho é o namespace, e não a
  classe do ImGui.NET; os arquivos usam o alias `Gui`.
- A view do Avalonia faz as linhas quando é carregada (commit `8853770`): um teste que cria a view
  precisa pô-la numa janela e esperar a fila da interface antes de procurar os controles.
- Para fechar um app que roda em segundo plano, não usar `pkill -f` com o nome dele: o padrão casa
  com a linha de comando do próprio shell que roda o `pkill`, e o derruba junto. Matar pelo PID, ou
  `wineserver -k` (com o mesmo `WINEPREFIX`) para o que roda no Wine.
- Eventos de ponteiro simulados no Avalonia: `new PointerPressedEventArgs(alvo, ponteiro, janela,
  ponto, 0, new PointerPointProperties(RawInputModifiers.LeftMouseButton,
  PointerUpdateKind.LeftButtonPressed), KeyModifiers.None)` e o `RaiseEvent` no controle, com um
  `Avalonia.Input.Pointer` só para o teste (o nome `Pointer` sozinho colide com o do
  `System.Reflection`). O `xdotool` dá o mouse de verdade, para conferir que os simulados batem.

## 5. O código hoje, em resumo

- `Inspector.Create<T>()`: a descoberta (`ReflectionDiscovery.AddMembers`) monta a árvore, e cada
  nó passa por `ReflectionPolicy.Apply` e depois `AttributePolicy.Apply`. A camada manual vem
  depois, no próprio inspector (`inspector["Moo.MooX"].Label = ...`).
- `Inspector` não é mais um nó (commit `1997209`): guarda a raiz num `RootNode` interno e expõe o
  `Id`, o `Name`, o `Mode`, as opções (`Options`), o objeto ligado (`Instance`), o indexador e o
  `Node<T>`, a enumeração e `Rows`. `InspectorNode` é abstrato, com as opções como propriedades, o
  indexador por caminho relativo (encadeável), o `GetValue` comum e o `SetValue` abstrato; os nós
  são `MemberNode` (campo ou propriedade), `ButtonNode`, `DisplayNode`, `TypeGroupNode` (commit
  `5a10ed6`), `CollectionNode` e `ItemNode` (commit `ba26fe6`, os dois derivados do `MemberNode`) e
  o `RootNode` interno. A enumeração entrega a árvore inteira, e `Rows` entrega as linhas da view
  (commit `a8bf9db`).
- `ReadOnly` e `Visible` são lidos pelos pais (commit `095ad28`), e um setter não público esconde o
  membro, que o `[InspectorReadOnly]` traz de volta.
- Falhas no `Create` (commit `9db2e52`): viram eventos estáticos (`DiscoveryFailed`, com a
  severidade) e ficam em `inspector.Report`; um membro ilegível sai da árvore, um atributo inválido
  é pulado, e só o fatal sobe. Os tipos ficam em `InteractiveEditor.Diagnostics`.
- `Dispose` (commit `bc4491e`): o inspector e os nós são descartáveis, e o `GlobalOptions` fica
  travado enquanto houver um inspector vivo (mudar a flag lança). Programas de teste precisam
  descartar os inspectors antes de mexer na flag; o TuxHost faz isso no fim do `Print`.
- Bind (commits `62af47f` e `e707583`): `Bind` lança se já houver objeto ligado ou se o objeto não
  servir para a árvore (um tipo derivado serve); `AddBind` põe mais objetos, `RemoveBind` tira um,
  `Unbind()` solta todos, e `Rebind` confere e depois desliga e liga. `GetValue` lê o primeiro
  objeto, `GetValues` todos, `IsMixed` diz se diferiam na última leitura (commit `121520a`), e
  `SetValue` grava em todos. O inspector avisa por `BindRegistered`, `BindRemoved` e `Unbound`.
- Valores (commit `305952f`): cada nó guarda a última leitura e dispara `ValueChanged` com a origem
  quando ela muda; `inspector.Refresh()` relê tudo, e objetos com `INotifyPropertyChanged` avisam
  sozinhos (commit `8543362`). Getter ou setter que lança vira `Failure` e `BindFailed` no nó, sem
  exceção; o uso errado continua lançando. A gravação e os avisos releem também o que muda junto: a
  struct acima do campo gravado e o que fica abaixo de um objeto fechado (commit `14307ea`).
- Gravação (commit `7f3cb63`): o `SetValue` prepara o valor antes de gravar, com as regras de texto,
  a conversão pela cultura do `inspector.Options` (`IParsable<T>` ou `TypeConverter`), as regras de
  valor e a faixa. O que falha no preparo vira `Failure` no nó, e nada é gravado; um valor de um
  tipo sem relação com o do membro lança.
- Nós manuais (commit `42e2129`): `AddButton(nome, texto, ação)` e `AddDisplay(nome, getter)`, no
  inspector ou em qualquer nó; o `ButtonNode` roda a ação no `Press()`, e o `DisplayNode` lê o
  getter como um membro.
- `TypeBinderMode` (commit `056934f`): `Create<T>(TypeBinderMode.Manual)` começa sem membros, e o
  `Add("X")` põe um membro com as camadas de reflection e de atributos. O `Inspector.Create()` sem
  tipo (commit `d1bf798`) acha os nomes no bind, que fixa o tipo até o `Unbind()`; o `Member` do nó
  fica null até lá.
- Filtro por nome (commit `5d53c71`): `GlobalOptions.Hide<T>(nomes)` antes do `Create`, travado
  como as outras opções globais; conta como um `[InspectorIgnore]`, e os testes precisam chamar
  `Unhide<T>()` no fim, porque o registro é global.
- Visibilidade condicional (commit `bdf7ef8`): o `VisibleWhen` do nó é uma regra sobre o objeto
  ligado, relida no fim de cada leitura ou gravação pelo `CheckRules` do `RootNode`, e guardada; o
  `Visible` junta a regra, o valor à mão e os pais, e uma resposta nova dispara o `VisibleChanged`.
- Lista de escolha (commit `cf909c1`): o `Choices` do nó é uma função lida pelo `GetChoices()`
  quando a view abre a lista; sem ela, um enum lista os próprios valores.
- Descoberta: ordem de declaração (commit `bb1184b`), coleções sem os membros do tipo delas
  (commit `4a55bd0`) e cor numa linha `Display` fechada até escolherem o editor, com um aviso
  (commit `47b0997`). O seletor por expressão é o `Node<T>` (commit `c8a2720`).
- Coleções (commit `ba26fe6`): o `CollectionNode` tem o editor `Selector`, o `Items` (o que a
  coleção do primeiro objeto tinha na última leitura) e o `SelectedIndex`; a linha de baixo, `Item`,
  lê e grava o item escolhido como um membro, com os membros do tipo do item embaixo. A escolha
  segue os itens (o primeiro ao ganhar itens; um objeto que muda de lugar leva a escolha junto,
  commit `6951033`; o último se o escolhido sair), outro objeto no lugar escolhido não é troca, e
  uma `ObservableCollection` avisa sozinha. A faixa e o scrubbing do membro vão para a linha do item
  (commit `d681dee`), e uma coleção por referência sem setter público fica com o conteúdo editável,
  só a troca dela é recusada (commit `3fd3a8c`, pelo `Locked` do `MemberNode`). O editor de lista
  (commit `97c1e7d`) é o `EditorKind.List`, com `AddItem()`, `RemoveItem(i)` e `MoveItem(de, para)`
  no `CollectionNode`, que gravam na hora em cada objeto ligado.
- Controle do binder (commit `121520a`): `inspector.Options.BinderControl`, `Automatic` por padrão.
  Sem `ViewToInstance`, o `SetValue` guarda o valor no nó e o `Apply()` grava; sem
  `InstanceToView`, os avisos do objeto e o `Refresh()` não chegam à view, e o `Reload()` relê. A
  view mostra o `ViewValue`, e não o `GetValue()`, que lê o objeto. Os de força são o
  `ForceApply()`, o `ForceReload()` e o `ForceClear()`, cada um com o seu evento.
- Troca por fora (commit `77dda91`): um grupo de tipo class cujo objeto foi trocado depois do bind
  dispara `ObjectReplaced`; sem aceite, o ramo fica comprometido e lança na leitura e na gravação
  até um `Rebind`. Struct fica de fora.
- Binding pela cadeia de pais: só a raiz guarda a instância, struct é gravada de volta no dono, e a
  gravação respeita o `ReadOnly`. O `SetValue` público recusa grupo aberto e a raiz, e a gravação
  de volta passa pelo `WriteTo` interno (commit `739850b`).
- Primitivos (commit `424f6d0`): os da PixieLib, pelo pacote do nuget.org (commit `6b7cd80`):
  `PxPoint`, `PxSize`, `PxRect` e `PxPadding` em `double`, `PxColorRgba` e `PxColorHsl`, sem
  conversão implícita entre as cores. No núcleo ficou só o `PxDock`
  (`InteractiveEditor/Primitives`). O `PxRect`, o `PxSize` e o `PxPadding` são usados pelo passo de
  layout. As conversões com o `System.Drawing` vêm da PixieLib, e as com o WinForms e o WPF (o
  `Point`, o `Size`, o `Rect`, a `Thickness` e a `Color` do WPF, o `Padding` e o `DockStyle` do
  WinForms) são métodos de extensão nos projetos de cada um (`ToWinForms`, `ToWpf` e `ToPrimitive`).
- Passo de layout (commit `9674fca`): `inspector.Layout(largura)` devolve as linhas de cima e o
  tamanho; cada `LayoutRow` tem os retângulos da linha, do rótulo e do editor nas coordenadas de
  onde está, e um grupo tem o painel dele, com as linhas de dentro a partir do canto do painel. As
  opções (`RowHeight`, `RowSpacing`, `Indent`, `LabelWidth`, `LabelSpacing`, `Padding` e `ListRows`)
  ficam no `InspectorOptions`. Não lê os objetos, e roda no Linux.
- Um projeto por framework (P7.20, commit `424f6d0`, no lugar dos dois alvos da P7.1): o núcleo é
  `net10.0`, e as views ficam em `InteractiveEditor.WinForms`, `.Wpf`, `.Avalonia` e `.ImGui`, cada
  uma com o namespace do projeto. As `ViewRules` (`InteractiveEditor/Views`) ficam `internal`, e os
  quatro as veem pelo `InternalsVisibleTo`; a `CultureInUse` e o `ShownChildren` (no nó e no
  inspector) são públicos (P7.21, P7.23; commit `ad79532`).
  As views WinForms e WPF descritas abaixo ficaram em `Views/WinForms` e `Views/Wpf` até o
  `424f6d0`.
- Eventos para as views (commit `4db6457`): o `OptionChanged` do inspector sai quando uma opção de
  um nó (menos as listas de regras) ou de cultura e layout do `InspectorOptions` muda de fato, com
  o nó (null para as do inspector) e o nome da opção (P7.8); o `Disposed`, no fim do `Dispose`
  (P7.13).
- View WinForms (commit `8dfaab4`, em `Views/WinForms`): `inspector.CreateWinFormsView()` dá um
  `WinFormsInspectorView`, um `UserControl` que observa o inspector. As linhas vão nos retângulos
  do `Layout`, um painel por grupo, com um controle por editor (a tabela da 3.5 das notas); o
  layout é refeito a cada opção, bind, regra ou troca por fora, uma vez por rajada, e uma linha
  guarda os controles enquanto o editor não muda. O texto grava no Enter e ao perder o foco, o Esc
  volta, e uma falha deixa o editor vermelho claro, com a mensagem no tooltip. Avisos de outra
  thread passam para a da interface, e o `Disposed` do inspector esvazia a view. Os editores são
  `internal`, e cada controle tem como `Name` o caminho do nó (o rótulo, `caminho#label`; o `(?)`,
  `caminho#help`; o painel do grupo, `caminho#panel`).
- Espaçador e ajuda (P7.14, P7.15; commits `6d8b300`, `af5a584` e `4e6bf5b`): o layout limita o
  editor (`EditorMaxWidth`, 200, encostado à direita, com o resto entre ele e o rótulo) e as linhas
  (`MaxWidth`, sem limite por padrão), e dá o retângulo `Help` (16 px) a um nó com `Help`, colado
  no editor, numa faixa que toda linha reserva quando algum nó da árvore tem `Help`. A view pinta o
  fundo da linha sob o mouse e mostra o `(?)` em cinza, que abre uma janela modal com o texto.
- Um valor por objeto (P7.16, commit `31df507`): `ViewValues` (o que a view mostra de cada objeto)
  e `SetValues(valores)` (um para cada, preparados todos antes de gravar); o valor guardado sem
  `ViewToInstance` é um por objeto, e o `IsMixed` lê o que a view mostra. `ScrubAxis` (horizontal ou
  vertical) é uma opção do nó, ao lado do `ScrubMultiplier`.
- Scrubbing e mistos na view (P7.17 a P7.19, commit `ce15b9f`): o rótulo de um número com
  `ScrubMultiplier` arrasta no eixo dele (`LabelScrub`), gravando "o começo de cada objeto + a
  distância × o passo", com Shift ×10, Ctrl ×0,1 e Esc voltando ao começo. Com objetos diferentes, o
  rótulo fica em itálico e o editor neutro (vazio com "—", indeterminado, sem escolha); um número
  que faz scrubbing mostra o do primeiro. O WindowsHost liga um segundo `Gadget` por um botão
  (commit `98f8a9a`).
- Válvulas e falhas da view (P7.4, 3.11; commit `6632d73`): o `ControlCreated` vem por controle
  (rótulo, marca, editor, painel do grupo, em `RowPart`) e o `RowCreated` ao fim de cada linha, já
  no lugar e com o valor; os args são genéricos no núcleo (`ControlCreatedEventArgs<TControl>`,
  `RowCreatedEventArgs<TControl>`), e cada view declara os eventos com o tipo de controle dela. As
  linhas são feitas quando a view ganha a janela. O `RowFailed` avisa o que falha na própria view:
  uma linha que não pode ser feita fica de fora até o próximo layout, e uma que não mostra os
  objetos (um `ToString` que lança) fica vermelho-claro até sarar. Detalhes na seção 3.5 das notas.
- A view WPF (o corte 5; commit `a93b75c`): `WpfInspectorView`, por `inspector.CreateWpfView()`, em
  `Views/Wpf`, a WinForms traduzida: um `Canvas` por grupo num `ScrollViewer`, preso no canto de
  cima à esquerda; o caminho do nó no `AutomationId` dos controles; a view `IDisposable`; a cor pelo
  diálogo do WinForms; o "—" do texto misto num `TextBlock` sobre a caixa; o slider em double. O que
  as duas views decidem igual está em `Views/ViewRules.cs`. O WpfHost mostra o `Gadget`, com a mesma
  válvula do WindowsHost. Detalhes na seção 3.5 das notas.
- No Wine sem gerenciador de janelas, o mouse que sai de uma janela WPF para onde não há janela não
  avisa o WPF (`IsMouseOver` continua verdadeiro); o `probe-wpf` sai para outra janela do app. Uma
  janela WPF sem conteúdo não dispara o `ContentRendered`, então o probe começa no `Loaded`.
- As views do Avalonia (`CreateAvaloniaView()`, commit `a885789`) e do ImGui (`CreateImGuiView()`,
  commit `1d86b96`) e o host do Terminal.Gui (commit `6ba0290`), da branch das views novas,
  refeitos sobre este núcleo. O Avalonia põe as linhas nos painéis dele, com um expander por grupo;
  o ImGui desenha uma tabela a cada quadro, relendo os objetos antes (`RefreshEachFrame`, ligado
  por padrão). Nenhuma usa o passo de layout (P7.22). O Avalonia tem, como as outras, o `(?)` com a
  janela, as válvulas com o `RowFailed` e o scrubbing (commits `507b528`, `8853770` e `b0deeb2`);
  o ImGui fica sem válvulas e com a ajuda no tooltip do `(?)`. O `TerminalHost` usa só a API
  pública. Detalhes na seção 0 das notas (Views) e na 3.5.

## 6. O que falta

O checklist (seção 6 das notas) diz o que ficou e por quê. Em resumo:

- O que foi decidido em 29/09 já entrou: a árvore do membro escondido com `new` (P5.9, commit
  `5a10ed6`), o filtro por nome (P6.7, commit `5d53c71`), o inspector sem tipo (P1.14, commit
  `d1bf798`), o seletor das coleções e o editor de lista (P5.10, commits `ba26fe6`, `6951033` e
  `97c1e7d`) e o passo de layout (P7.5, commit `9674fca`).
- O que foi decidido em 01/10 também: a coleção só com getter com o conteúdo editável (P4.7, commit
  `3fd3a8c`) e a faixa e o scrubbing do membro da coleção na linha do item (P5.11, commit
  `d681dee`); a P5.12, a P5.13 e a P7.6 confirmaram o que o código já fazia.
- O que foi decidido em 02/10 também: o `VisibleWhen` no nó (P6.2, commit `bdf7ef8`) e o `Choices`
  no nó (P6.3, commit `cf909c1`). Ainda em 02/10, a direção do que falta: as views antes das
  sessões próprias, em cinco cortes (seção 0 das notas, Views). O corte 1, os dois alvos e as
  conversões dos primitivos (P7.1), entrou no commit `e6cca32`. As escolhas do corte 2, a view
  WinForms, foram respondidas com as sugestões (P7.7 a P7.13), e ele entrou nos commits `4db6457`
  (os eventos) e `8dfaab4` (a view). O corte 3, o scrubbing e os mistos (P7.16 a P7.19), entrou nos
  commits `31df507` e `ce15b9f`. O corte 4, as válvulas (P7.4) e a premissa de erros nas views,
  entrou no commit `6632d73`, e o 5, a view WPF, no `a93b75c`.
- Com as views: nada no código; falta o neko conferir os dois hosts no Windows (seção 7).
- Em 09/10, o merge da branch das views novas (commits `36f0afa` a `6f02064`), e as respostas da
  7.20 à 7.23, com as sugestões, aplicadas nos commits `ad79532` a `b0deeb2`. O PR 2, dessa branch
  para a `main`, ficou contido na `rework-claude`; fechar ou não é com o neko.
- Sessões próprias: o cache do modelo de tipo (P5.6). A da PixieLib (P8.7) já foi feita, no
  repositório dela, e os primitivos dela já estão aqui (commit `424f6d0`).

## 7. Próximo passo

Em 09/10 entraram as views novas e as respostas da 7.20 à 7.23. Falta o neko conferir, junto com o
item 2, o AvaloniaHost (o `(?)` e a janela dele, `Count` e `Ratio` à direita pela válvula, o
scrubbing em `Count` e `Ratio`), o ImGuiHost e o TerminalHost.

1. As views estão completas: os cinco cortes da seção 0 das notas (Views) entraram, o último, a view
   WPF, no commit `a93b75c`. O próximo passo é o neko conferir no Windows o WindowsHost e o WpfHost
   (item 2) e responder às escolhas dos cortes 4 e 5 que eu tomei sem perguntar (item 3); o que o
   neko pedir para mudar vem primeiro. Depois, a sessão própria do cache do modelo de tipo (P5.6);
   a da PixieLib já foi feita, e os primitivos dela entraram no `424f6d0`. Para reler o original:
   `Sakamoto0110/InteractiveEditor` (branch `InspectorVariant0.7.1a`) e
   `Sakamoto0110/OverlayApplication`, públicos, clonados só para leitura.
2. O neko viu o corte 2 no Windows em 02/10 ("90% perfeito") e pediu o espaçador e o `(?)` (P7.14,
   P7.15), aplicados nos commits `6d8b300`, `af5a584` e `f3981bd`; depois preferiu o `(?)` colado no
   editor (commit `4e6bf5b`). Falta conferir no WindowsHost o espaçador, o `(?)` e a janela dele, o
   fundo da linha sob o mouse, o scrubbing (`Count` na horizontal, `Ratio` na vertical), os mistos
   (o botão "Bind a second gadget", no fim) e a válvula (`Count` e `Ratio` alinhados à direita); e
   no WpfHost, o mesmo na view WPF, que no Wine sai com o tema e as fontes substitutas.
3. Os cortes 4 e 5 foram feitos em 02/10, quando o neko pediu para rodá-los antes de a cota acabar
   ("pode tentar rodar o 4° corte agora", e depois "ok, pode fazer" para o 5), sem a rodada das
   escolhas: as que não tinham resposta óbvia eu tomei com a minha sugestão e mostrei no fim. No 4:
   quatro eventos em vez de três, com os args genéricos no núcleo; as linhas feitas quando a view
   ganha a janela, e não no construtor; a exceção de uma válvula subindo, como a de um evento do
   inspector; e o `RowFailed`, crítico para a linha que não pode ser feita e contornado para a que
   não mostra os objetos. No 5: a view WPF `IDisposable`; o caminho no `AutomationId`; o diálogo de
   cor do WinForms; o "—" num `TextBlock` sobre a caixa; o slider em double; e as regras comuns em
   `Views/ViewRules.cs`. Se o neko discordar de alguma, mudar.
