# Passagem de contexto

Para retomar o trabalho num contexto novo. Estado de 27 de setembro de 2026, na branch
`rework-claude`, depois do commit `805729d`. Ler isto inteiro antes de mexer em qualquer coisa.

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
- O NoHost fica versionado, ignorado pelo `.gitignore` e dentro da solução. Não tirar da solução.
- O ideal é uma DLL só. Se aparecer outra, dizer para que ela serve.

## 3. Onde está cada coisa

- `docs/notas-modernizacao.md`: as notas completas. Decisões na seção 0, o `Inspector` na 3.10, a
  PixieLib na 3.9, as decisões em aberto na seção 5, o checklist na 6 e o modelo de opções na 7.
- `docs/perguntas-em-aberto.md`: as 55 perguntas em aberto, numeradas e com exemplos. O Rafael vai
  responder pelo número ("2.3: sim").
- Relatório "Fluxo e políticas do Inspector": https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk
  (privado). O HTML não está no repositório; para atualizar, ler o artifact pela URL, editar e
  publicar de novo na mesma URL.
- Projetos da solução: `InteractiveEditor` (a biblioteca, `net10.0`), `DemoObjects` (os tipos de
  teste: `Foo`, `Moo`, `Doo`, `Boo`), `TuxHost` (o console de verificação, roda no Linux),
  `NoHost` (local do Rafael, `net10.0-windows`), `WindowsHost` e `WpfHost`.

## 4. Como verificar uma mudança

- O `dotnet` 10 pode não estar no PATH. Neste ambiente ele estava em `~/.dotnet`:

  ```
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
  `InteractiveEditor.csproj` (e `DemoObjects.csproj`, se precisar dos tipos de teste).
- Para rodar um app `net10.0-windows` no Linux (se ele não tocar em WinForms ou WPF):
  `dotnet exec --runtimeconfig`.

Pegadinhas já vistas:

- C# 14: `field` é palavra-chave dentro de acessores de propriedade. Por isso os padrões usam
  `fi` e `pi` (`FieldInfo fi`, `PropertyInfo pi`).
- Blocos `extension` aceitam propriedades e operadores, mas não conversões (CS9282).
- Quase todos os `.cs` têm BOM, e o final muda de arquivo para arquivo: com ou sem quebra de linha
  no fim, às vezes com um espaço sobrando. Ao editar, manter o BOM e o final como estão.

## 5. O código hoje, em resumo

- `Inspector.Create<T>()`: a descoberta (`ReflectionDiscovery.AddMembers`) monta a árvore, e cada
  nó passa por `ReflectionPolicy.Apply` e depois `AttributePolicy.Apply`. A camada manual vem
  depois, no próprio inspector (`inspector["Moo.MooX"].Label = ...`).
- `Inspector` herda de `InspectorNode` e é só a raiz. `InspectorNode` é concreto, um por membro,
  com as opções como propriedades, indexador por caminho relativo (encadeável) e enumeração que
  entrega as linhas da view.
- Binding pela cadeia de pais: só a raiz guarda a instância, struct é gravada de volta no dono, e
  o `SetValue` respeita o `ReadOnly`. Chamar o `bind` de novo troca o objeto (vai mudar, ver 6).
- Primitivos em `InteractiveEditor/Primitives`: `PxPoint`, `PxPointF`, `PxSize`, `PxSizeF`,
  `ArgbColor` e `HslColor`. Ainda sem uso.
- Um alvo só, `net10.0`: uma DLL, sem código de Windows na biblioteca.

## 6. Decidido em 27/09, ainda não aplicado

- `Inspector` não genérico: ele precisa poder trocar a instância da raiz, inclusive por uma de
  outro tipo.
- Eventos: alguns no inspector, a maior parte nos nós.
- O objeto de um grupo não é trocado pelo inspector, só os filhos editam. Uma troca detectada
  compromete a branch, que pode ser desativada.
- Binding: ligar lança se já houver objeto ligado, `Unbind()` (sem parâmetro) solta tudo, religar
  é desligar e ligar, e o multi-bind põe e tira objetos. Os nomes são exemplos.
- Duas regressões registradas: o commit `2a1cf94` tirou o `SetValue` que lançava no `Inspector`
  do main (raiz e grupos aninhados) e o `IsTypeBound`, que impedia um segundo bind.

## 7. Próximo passo

1. O Rafael lê o relatório e o `perguntas-em-aberto.md` e responde pelo número.
2. Com as respostas, registrar as decisões nas notas (seção 0 e as seções citadas em cada
   pergunta) e tirar as perguntas respondidas do arquivo.
3. Aplicar o que ele liberar da seção 9 das perguntas: ligar que lança, `Unbind()` e religar; a
   regra do objeto do grupo; o inspector guardando a raiz; e os percursos com nome no lugar do
   `IEnumerable`.
4. Antes de começar as views, fechar o alvo da biblioteca (pergunta 7.1).
