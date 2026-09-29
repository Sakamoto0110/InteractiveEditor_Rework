# Passagem de contexto

Para retomar o trabalho num contexto novo. Estado de 29 de setembro de 2026, na branch
`rework-claude`, depois de aplicar no código o que a segunda rodada de respostas do Rafael (29/09)
decidiu; o último commit de código é o `7482c5f`. Ler isto inteiro antes de mexer em qualquer coisa.

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
- Depois do push, conferir no GitHub que o commit aparece com o login `Sakamoto0110`.
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
- `docs/perguntas-em-aberto.md`: o que continua em aberto: 1.14 (o inspector sem tipo), 5.9, 5.10,
  6.7 e 7.5 (o passo de layout, que ficou sem resposta na primeira rodada), cada uma com exemplo e
  sugestão, mais 6.2 e 6.3, que ficaram para o final, a pedido dele. Os números antigos valem, e os
  novos seguem a numeração de cada seção.
- Relatório "Fluxo e políticas do Inspector": https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk
  (privado). O HTML não está no repositório; para atualizar, ler o artifact pela URL, editar e
  publicar de novo na mesma URL. Ele descreve o código em `9a1fffa` e ficou velho em quase tudo
  depois desta rodada; atualizar só se o Rafael pedir.
- Projetos da solução: `InteractiveEditor` (a biblioteca, `net10.0`), `DemoObjects` (os tipos de
  teste: `Foo`, `Moo`, `Doo`, `Boo`), `TuxHost` (o console de verificação, roda no Linux),
  `NoHost` (local do Rafael, `net10.0-windows`), `WindowsHost` e `WpfHost`.

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
  sessão chegou a 405 checagens, num arquivo por assunto: bind e multi-bind, grupo e raiz,
  enumeração, `ReadOnly` e `Visible`, falhas do `Create`, valores e `Refresh()`, troca por fora,
  `INotifyPropertyChanged`, o que muda junto, gravação, controle do binder, primitivos, seletor,
  ordem de declaração, nós manuais, modo manual, coleções e cores. Um segundo console testa a
  assembly ausente. Os dois ficam no scratchpad da sessão e não passam para a próxima; a lista
  acima serve de roteiro para refazer o que for preciso.
- Antes de dar uma mudança por pronta, conferir também que os testes pegam o erro: desfazer a
  mudança (ou quebrar de propósito uma cópia) e ver os testes novos falharem.
- Para rodar um app `net10.0-windows` no Linux (se ele não tocar em WinForms ou WPF):
  `dotnet exec --runtimeconfig`.

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
- Com a ordem de declaração, o membro escondido com `new` vem depois do da base, e o indexador pega
  o último com o nome, que é o do derivado.
- Os primitivos são em `double`: num teste, `(int)node.GetValue()` de um campo de `PxPoint` lança
  `InvalidCastException`.

## 5. O código hoje, em resumo

- `Inspector.Create<T>()`: a descoberta (`ReflectionDiscovery.AddMembers`) monta a árvore, e cada
  nó passa por `ReflectionPolicy.Apply` e depois `AttributePolicy.Apply`. A camada manual vem
  depois, no próprio inspector (`inspector["Moo.MooX"].Label = ...`).
- `Inspector` não é mais um nó (commit `0bc2f9d`): guarda a raiz num `RootNode` interno e expõe o
  `Id`, o `Name`, o `Mode`, as opções (`Options`), o objeto ligado (`Instance`), o indexador e o
  `Node<T>`, a enumeração e `Rows`. `InspectorNode` é abstrato, com as opções como propriedades, o
  indexador por caminho relativo (encadeável), o `GetValue` comum e o `SetValue` abstrato; os nós
  são `MemberNode` (campo ou propriedade), `ButtonNode`, `DisplayNode` e o `RootNode` interno. A
  enumeração entrega a árvore inteira, e `Rows` entrega as linhas da view (commit `b456398`).
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
  `Add("X")` põe um membro com as camadas de reflection e de atributos; o inspector sem tipo espera
  a 1.14 de `perguntas-em-aberto.md`.
- Descoberta: ordem de declaração (commit `6c17a15`), coleções sem os membros do tipo delas
  (commit `1b9fcf6`) e cor numa linha `Display` fechada até escolherem o editor, com um aviso
  (commit `7482c5f`). O seletor por expressão é o `Node<T>` (commit `e06b6f7`).
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
  sem conversão implícita entre as cores. Ainda sem uso na biblioteca.
- Um alvo só, `net10.0`: uma DLL, sem código de Windows na biblioteca.

## 6. O que falta

O checklist (seção 6 das notas) diz o que ficou e por quê. Em resumo:

- Esperando resposta, em `perguntas-em-aberto.md`: 1.14 (o inspector sem tipo), 5.9 (a árvore do
  membro escondido com `new`), 5.10 (o que o seletor das coleções faz), 6.7 (onde o filtro por nome
  é injetado) e 7.5 (o passo de layout).
- Para o final, a pedido dele: 6.2 e 6.3, as explicações do `VariablePool` e do `EditField()`.
- Com as views: os dois alvos no mesmo projeto (P7.1), as fábricas com nomes distintos (P7.2), a
  view percorrendo a árvore (P7.3), os callbacks por plataforma e o agnóstico por linha (P7.4), a
  premissa de erros nas views e as conversões dos primitivos com o WinForms e o WPF.
- Sessões próprias: a PixieLib (P8.7) e o cache do modelo de tipo (P5.6).
- As escolhas que fiz sem ele estão marcadas nas notas com "a confirmar"; vale listá-las para ele
  quando ele voltar.

## 7. Próximo passo

1. A parte de código decidida até 29/09 está aplicada, um conceito por commit, cada um com o seu
   commit de notas. Não há código decidido esperando.
2. Quando ele responder 1.14, 5.9, 5.10, 6.7 e 7.5, registrar nas notas (seção 0) e aplicar.
3. Depois do código, 6.2 e 6.3: explicar o `VariablePool` e o `EditField()` (por que existiam, como
   funcionavam, se são necessários, a importância e o estrago se saírem). Para isso, adicionar à
   sessão `Sakamoto0110/InteractiveEditor` (branch `InspectorVariant0.7.1a`) e
   `Sakamoto0110/OverlayApplication`.
