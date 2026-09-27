# Passagem de contexto

Para retomar o trabalho num contexto novo. Estado de 27 de setembro de 2026, na branch
`rework-claude`, depois das respostas do Rafael às perguntas em aberto e do commit `b456398`. Ler
isto inteiro antes de mexer em qualquer coisa.

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
- `docs/perguntas-em-aberto.md`: as 22 perguntas que continuam em aberto depois das respostas de
  27/09, cada uma com exemplo e sugestão. Os números antigos valem, os novos seguem a numeração de
  cada seção, e a seção 0 é a da premissa de erros. 6.2 e 6.3 ficaram para o final, a pedido dele.
- Relatório "Fluxo e políticas do Inspector": https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk
  (privado). O HTML não está no repositório; para atualizar, ler o artifact pela URL, editar e
  publicar de novo na mesma URL. Ele descreve o código em `9a1fffa`: as partes de bind (seções 1 e
  6), enumeração (4 e 5) e "decidido, ainda não aplicado" (8) ficaram velhas depois de `4dec125`,
  `a2d8ffe` e `b456398`.
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

- O TuxHost imprime 69 linhas. Salvar a saída antes da mudança e comparar depois com `cmp`:

  ```
  dotnet run --project TuxHost/TuxHost.csproj
  ```

- Para testar um comportamento, fazer um console pequeno fora do repositório, referenciando
  `InteractiveEditor.csproj` (e `DemoObjects.csproj`, se precisar dos tipos de teste). O desta
  sessão tinha 62 checagens: bind (null, segundo bind, outro tipo, `Rebind` recusado, tipo
  derivado, struct na raiz), grupo e raiz (grupo aberto, objeto fechado, structs aninhadas, struct
  somente leitura, pai null) e enumeração (árvore, linhas, ignorados, flag global).
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

## 5. O código hoje, em resumo

- `Inspector.Create<T>()`: a descoberta (`ReflectionDiscovery.AddMembers`) monta a árvore, e cada
  nó passa por `ReflectionPolicy.Apply` e depois `AttributePolicy.Apply`. A camada manual vem
  depois, no próprio inspector (`inspector["Moo.MooX"].Label = ...`).
- `Inspector` herda de `InspectorNode` e é só a raiz. `InspectorNode` é concreto, um por membro,
  com as opções como propriedades e indexador por caminho relativo (encadeável). A enumeração
  entrega a árvore inteira, e `Rows` entrega as linhas da view (commit `b456398`).
- Bind (commit `4dec125`): `Bind` lança se já houver objeto ligado ou se o objeto não servir para a
  árvore (um tipo derivado serve); `Unbind()` solta; `Rebind` confere e depois desliga e liga.
- Binding pela cadeia de pais: só a raiz guarda a instância, struct é gravada de volta no dono, e a
  gravação respeita o `ReadOnly`. O `SetValue` público recusa grupo aberto e a raiz, e a gravação
  de volta passa por um `Write` interno (commit `a2d8ffe`). Uma troca feita por fora
  (`foo.Moo = new Moo()`) ainda passa sem sinal.
- Primitivos em `InteractiveEditor/Primitives`: `PxPoint`, `PxPointF`, `PxSize`, `PxSizeF`,
  `ArgbColor` e `HslColor`. Ainda sem uso.
- Um alvo só, `net10.0`: uma DLL, sem código de Windows na biblioteca.

## 6. Decidido, ainda não aplicado

Está tudo na seção 0 das notas e no checklist (seção 6). O principal:

- A premissa: exceção interna não derruba o inspector (3.11), com a severidade e a linha entre
  lançar e avisar ainda em aberto (0.1 e 0.2).
- Composição: o `Inspector` guarda a raiz em vez de herdar de `InspectorNode` (P1.1). Aplicar
  depende da 9.3, que o Rafael não tinha entendido e foi explicada de novo.
- Setter abstrato, com um tipo de nó por comportamento (P1.2); eventos (P1.4); `IDisposable` e a
  trava do `GlobalOptions` enquanto houver inspector vivo (P1.5); `TypeBinderMode` (P1.7); enum de
  controle do binder e métodos de força (P1.8).
- Binding: multi-bind de um tipo só (P2.3), valores mistos (P2.4), `INotifyPropertyChanged` e
  `Refresh()` (P2.6), conversão de texto com a cultura do inspector (P2.7), faixa que limita o
  valor (P2.8) e sanitizadores ordenados, sem o "TheBrute" (P2.9).
- Troca por fora compromete o ramo, detectada no `Refresh()` e numa leitura (P3.3, P3.4).
- `ReadOnly` passando para os filhos (P4.1, P4.2) e setter privado escondido pela reflection.
- Descoberta: ordem de declaração se der, coleções pelo conteúdo, `Color` com escolha explícita.
- Views com dois alvos no mesmo projeto (P7.1), fábricas com nomes distintos, a view percorrendo a
  árvore e um callback agnóstico por linha.
- Primitivos em `double`, um tipo só (P8.1); `PxRect`, `PxPadding` e `PxDock` (P8.2);
  `PxColorArgb` e `PxColorHsl`, sem conversão implícita entre elas (P8.3, P8.4). A PixieLib fica
  para uma sessão própria.

Duas decisões anteriores mudaram: o tipo da raiz ficou fixo (ligar outro tipo lança, P2.2), então o
`Inspector` é não genérico porque nem todo inspector vai ser tipado (P1.7); e as views trazem de
volta os dois alvos, no lugar de um alvo só.

## 7. Próximo passo

1. O Rafael responde as perguntas de `perguntas-em-aberto.md` pelo número. As que destravam
   código: 9.3 (composição), 3.2 e 3.6 (detecção da troca), 0.1 e 0.2 (premissa) e 4.5 e 4.6
   (`ReadOnly`).
2. Com as respostas, registrar as decisões nas notas (seção 0 e as seções citadas) e tirar as
   respondidas do arquivo.
3. Aplicar o que ele liberar. A composição (9.3) é a próxima peça natural, antes dos eventos e da
   premissa de erros.
4. 6.2 e 6.3 ficaram para o final: explicar o `VariablePool` e o `EditField()` (por que existiam,
   como funcionavam, se são necessários, a importância e o estrago se saírem). Para isso, adicionar
   à sessão `Sakamoto0110/InteractiveEditor` (branch `InspectorVariant0.7.1a`) e
   `Sakamoto0110/OverlayApplication`.
