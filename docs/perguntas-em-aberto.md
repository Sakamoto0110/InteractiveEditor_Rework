# Perguntas em aberto

Estado de 29 de setembro de 2026, depois das respostas de 27/09 e de 29/09. As respondidas saíram
daqui e estão na seção 0 de `notas-modernizacao.md`, citadas com um `P` na frente do número
(`P2.2`). O que ficou: os desdobramentos das últimas respostas e as duas que ficaram para o final.

Os números antigos continuam valendo, e os novos seguem a numeração de cada seção, sem reaproveitar
número. Os nomes que ainda não existem no código são só ilustração, e a sugestão, quando há, vem no
fim de cada pergunta.

Dá para responder pelo número, como antes: "5.9: sim" ou "6.7: b".

---

## 5. Descoberta

### 5.9 A árvore de um membro escondido com `new`

Na 5.7 você explicou que o nome composto expande nos campos do tipo derivado: `Derived.Value` é o
`string Value`, e não o `int Value`. Pelo que entendi, o nome do tipo vira um nível do caminho, e
com isso o ponto deixa de ser problema:

```text
Derived                          a raiz
├─ Base                          grupo do tipo base
│  └─ Value    int               inspector["Base.Value"]
└─ Derived                       grupo do tipo derivado
   └─ Value    string            inspector["Derived.Value"]
```

É isso? E sobram dois detalhes:

- os membros que não estão escondidos (um `Name` declarado só no `Base`, por exemplo) ficam direto
  na raiz, como hoje, ou também vão para o grupo do tipo que os declara?
- `inspector["Value"]`, sem o tipo, acha o do derivado, como no C#, ou lança por ser ambíguo?

Sugestão: só os membros escondidos ganham o nível do tipo, e o resto fica direto na raiz, como
hoje; `inspector["Value"]` acha o do derivado, como no C#.

### 5.10 Coleções: o que o seletor faz com o conteúdo

A P5.2 decidiu mostrar o conteúdo, e não os membros do tipo da coleção, com um seletor (combo box)
como editor padrão e um editor de lista como alternativa. Falta o que o seletor faz.

```csharp
public List<Moo> Items { get; set; } = [new Moo(), new Moo()];

// seletor: uma combo box com os dois itens. Escolher um deles faz o quê?
// (a) nada: a combo só mostra o conteúdo
// (b) escolhe o item que aparece embaixo, para editar (Items[0] ou Items[1], com MooX, MooY...)
// (c) grava o item escolhido em outro membro (um SelectedItem, por exemplo)

// editor de lista: uma linha por item, com adicionar, remover e reordenar
```

Sugestão: (b), porque é o jeito de editar o conteúdo com um seletor; e o editor de lista como
escolha explícita (`[InspectorEditor(EditorKind.List)]`, um tipo de editor novo).

## 6. Opções e editores

### 6.7 Onde o filtro por nome é injetado

A P6.4 e a P6.6 decidiram um filtro por nome, injetado, por tipo, aplicado no `Create` com a
precedência dos atributos. Falta o ponto de injeção.

```csharp
// (a) um atributo na própria classe
[InspectorHide(nameof(Text), nameof(FontName), nameof(IsBold))]
class Rectangle : ComponentPreset { ... }

// (b) um registro de fora, por tipo, antes do Create, travado como as opções globais
GlobalOptions.Hide<Rectangle>("Text", "FontName", "IsBold");

var inspector = Inspector.Create<Rectangle>();   // os três já saem escondidos
inspector["Text"].Ignored = false;               // a camada manual ainda traz de volta
```

Sugestão: (b), porque "injetado" pede algo de fora da classe, e serve também para tipos de
terceiros, que não dá para anotar. O (a) seria um `[InspectorIgnore]` em lote, que já existe membro
a membro.

## Para o final

### 6.2 Visibilidade condicional e 6.3 Editores do `EditField()`

Você pediu que eu explique com calma: por que isso existia, como funcionava, se é necessário, a
importância e o estrago se sair. Isso vem depois da parte de código (29/09). Para explicar, vou
reler o código original (`Sakamoto0110/InteractiveEditor`, branch `InspectorVariant0.7.1a`) e o
OverlayApplication, que ainda não estão nesta sessão.
