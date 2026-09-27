# Perguntas em aberto

Todas as perguntas sem resposta até 27 de setembro de 2026, juntadas das notas
(`notas-modernizacao.md`) e das conversas. Cada uma traz um exemplo: como é hoje, quando já existe,
e como ficaria cada opção. Os nomes de métodos e propriedades que ainda não existem são só
ilustração. Quando eu já tinha sugerido algo, a sugestão vem no fim, junto com a origem. As
marcadas com **(antes das views)** são as que eu resolveria antes de começar a apresentação. O
fluxo atual está explicado no relatório "Fluxo e políticas do Inspector"
(https://claude.ai/artifact/N2gTyxg93rniNGogj2U4wk).

Dá para responder pelo número, por exemplo: "2.3: sim" ou "3.1: só o grupo aberto".

---

## 1. O Inspector

### 1.1 Herança ou composição (antes das views)

O `Inspector` continua sendo um `InspectorNode`, e com isso herda o `GetValue`/`SetValue` e as
opções de membro, que não fazem sentido na raiz? Ou passa a guardar a raiz como um nó interno?

```csharp
// hoje: tudo isso compila
var inspector = Inspector.Create<Foo>();
inspector.Label = "Foo";                     // nada lê: a raiz não é um membro
inspector.Range = new NumericRange(0, 1);    // idem
inspector.SetValue(null);                    // desliga o objeto sem passar pelo bind

// composição: o Inspector guarda a raiz e expõe só o que é dele
public class Inspector
{
    private readonly RootNode Root;          // interno: o topo da cadeia de pais

    public InspectorNode this[string path] => Root[path];
    public void Bind(object instance) { ... }
}
```

Sugestão: composição. Origem: conversa de 27/09.

### 1.2 Setter abstrato de volta (antes das views)

Voltar ao desenho do main, com o getter comum e o setter abstrato, e um tipo de nó por
comportamento fixo na criação?

```csharp
// no main
public abstract class InspectorNode
{
    public virtual object? GetValue() { ... }        // comum
    public abstract void SetValue(object? value);    // cada tipo decide
}
public class Fieldset : InspectorNode { ... }        // grava
public class Inspector : InspectorNode { ... }       // lança: não troca a instância

// proposta
class MemberNode : InspectorNode { ... }    // grava; recusa quando é um grupo aberto
class ButtonNode : InspectorNode { ... }    // depois: sem valor, SetValue lança
class DisplayNode : InspectorNode { ... }   // depois: só leitura, SetValue lança
```

Grupo ou folha fica decidido em tempo de execução dentro do `MemberNode`, porque o `Expandable`
pode mudar depois do `Create` (`inspector["Details"].Expandable = false` transforma o grupo numa
linha só), e a classe de um objeto não muda. Sugestão: sim, desse jeito. Origem: 3.10.

### 1.3 Enumerável (antes das views)

O inspector e o nó deixam de ser `IEnumerable`?

```csharp
// hoje: o foreach entrega só as linhas da view
foreach (var node in inspector)
    node.ScrubMultiplier = 1;          // não alcança ignorados nem o que está em grupo fechado
inspector.Count();                     // conta linhas visíveis, não nós

// proposta: um nome por percurso
foreach (var node in inspector.Nodes)              // todos os nós
    if (node.Editor == EditorKind.Number)
        node.ScrubMultiplier = 1;
view.Show(inspector.Rows);                         // as linhas da view
foreach (var child in inspector["Moo"].Children)   // os filhos diretos
    ...
```

Sugestão: sim. Origem: 3.10.

### 1.4 Eventos (antes das views)

Já decidido: alguns no inspector, a maior parte nos nós. Falta a lista. O original tinha
`FieldPreBind`, `FieldBindStarted`, `FieldBindFinished` e `FieldBindFailure` nos campos, e
`BindStarted` e `BindFinished` no inspector.

```csharp
// no inspector
inspector.Bound += ...;         // um objeto foi ligado
inspector.Unbound += ...;       // tudo foi desligado

// nos nós
node.ValueApplied += ...;       // um valor foi gravado
node.Compromised += ...;        // o objeto do grupo foi trocado por fora (seção 3)
node.BindFailed += ...;         // falha, com mensagem, motivo, sugestão e caminho
```

Origem: 3.10, 3.3 e 1.1, item 9.

### 1.5 `InspectorOptions`

O que entra nas opções por inspector, e como elas sobrescrevem o `GlobalOptions`? No
OverlayApplication, cada editor tinha `FieldHeight = 23` e `Horizontal_Spacing = 0`.

```csharp
GlobalOptions.RequireExpandableAttribute = true;          // o processo inteiro

var inspector = Inspector.Create<ComponentPreset>();
inspector.Options.FieldHeight = 23;                       // só este inspector
inspector.Options.HorizontalSpacing = 0;
inspector.Options.RequireExpandableAttribute = false;     // sobrescreve o global?
```

Origem: seção 7.

### 1.6 Nós manuais

Botão, campo só de exibição e cabeçalho entram direto no inspector?

```csharp
component.AddHeader("Camada");
component.AddButton("LayerUp", "▲", () => tree.OnLayerUp());
component.AddDisplay("Layer", () => tree.SelectedIndex);
```

Origem: seção 5, item 4; o esboço de 3.2.

### 1.7 `TypeBinderMode`

Continua existindo um modo "só o que foi declarado", além do automático?

```csharp
var auto = Inspector.Create<ComponentPreset>();                          // todos os membros
var manual = Inspector.Create<ComponentPreset>(TypeBinderMode.Manual);   // começa vazio
manual.Add("X").Label = "PosX";                                          // só o declarado
```

Origem: 3.2.

### 1.8 Aplicar sob demanda

O Apply e o Reload do original, que nunca foram terminados, entram? Lá, o `AutoUpdateEnabled`
desligava a gravação, mas nada aplicava depois.

```csharp
inspector.AutoApply = false;    // as edições ficam pendentes
inspector.Apply();              // grava tudo no objeto
inspector.Reload();             // descarta as pendentes e relê do objeto
```

Origem: 1.1 e 3.10.

## 2. Binding

### 2.1 Nomes

Os métodos citados eram exemplos. Quais ficam, e com que caixa?

```csharp
inspector.Bind(foo);        inspector.Unbind();         inspector.Rebind(bar);
inspector.AddBind(foo2);    inspector.RemoveBind(foo2);

inspector.bind(foo);        // ou minúsculo, como o bind de hoje
```

Origem: seção 0.

### 2.2 Trocar a instância da raiz por outro tipo

A árvore é refeita. A configuração feita na árvore anterior se perde, ou o inspector guarda uma
árvore por tipo?

```csharp
var inspector = Inspector.Create<Foo>();
inspector["x"].Label = "PosX";
inspector.Bind(foo);

inspector.Rebind(boo);      // outro tipo: a árvore passa a ser a de Boo
inspector.Rebind(foo);      // de volta a Foo: o rótulo continua "PosX" ou volta a ser "x"?
```

Origem: 3.10.

### 2.3 Multi-bind com tipos diferentes

Pôr mais um objeto no bind exige o mesmo tipo dos que já estão ligados?

```csharp
inspector.Bind(foo1);
inspector.AddBind(foo2);        // mesmo tipo: ok
inspector.AddBind(boo);         // outro tipo: lança?
inspector.AddBind(fooDerived);  // um tipo derivado de Foo: aceita?
```

Sugestão: o mesmo tipo; o derivado fica para você decidir. Origem: 3.10.

### 2.4 Valores mistos

Com vários objetos, cada nó tem um valor por objeto. O que o `GetValue` devolve, e o que a view
mostra?

```csharp
inspector.Bind(a);              // a.x == 1
inspector.AddBind(b);           // b.x == 2

inspector["x"].GetValue();      // 1? um marcador de "misto"? a lista { 1, 2 }?
inspector["x"].SetValue(5);     // grava 5 nos dois
// scrubbing com delta +3: a.x == 4 e b.x == 5, a edição relativa do original
```

Origem: 3.3.

### 2.5 Tirar o último objeto

Tirar o último objeto do bind equivale ao `Unbind()`, com o mesmo evento?

```csharp
inspector.Bind(a);
inspector.RemoveBind(a);        // o mesmo que inspector.Unbind()?
```

Sugestão: sim. Origem: conversa de 27/09.

### 2.6 Objeto → UI (antes das views)

`INotifyPropertyChanged` para quem implementa, e um `Refresh()` manual para o resto? No
OverlayApplication, arrastar o objeto no overlay atualizava o inspector.

```csharp
class Component : INotifyPropertyChanged { ... }
component.X = 10;               // o objeto avisa, e a view atualiza a linha sozinha

class Plain { public int X; }
plain.X = 10;                   // ninguém avisa
inspector.Refresh();            // a view relê tudo
```

Origem: 3.3.

### 2.7 Texto → valor (antes das views)

`TypeConverter` ou `IParsable<T>`, e com a cultura invariante ou a atual? Hoje não há conversão
nenhuma.

```csharp
inspector["x"].SetValue("5");   // hoje: ArgumentException, a string não vira int

// testado com double.Parse
// pt-BR:     "1,5" → 1.5    "1.5" → 15    (o ponto é separador de milhar)
// invariant: "1.5" → 1.5    "1,5" → 15    (a vírgula é separador de milhar)
```

O original usava `Convert.ToDouble` com a cultura atual, e o `ONLY_NUMBERS` aceitava tanto `.`
quanto `,`. Origem: seção 5, item 3; 3.3.

### 2.8 A faixa no `SetValue`

O `SetValue` deve limitar, ou recusar, valores fora do `[InspectorRange]`?

```csharp
[InspectorRange(0, 255)] public int Opacity { get; set; }

inspector["Opacity"].SetValue(999);   // hoje: grava 999 (testado)
// limitar: grava 255
// recusar: lança ArgumentOutOfRangeException
```

Origem: relatório.

### 2.9 Sanitizadores e gancho de conversão

Os sanitizadores ficam separados em texto e valor, numa lista ordenada? E o gancho de conversão
(o "TheBrute") é por campo, global ou os dois?

```csharp
node.TextRules.Add(TextRule.Digits);        // antes de converter: o que dá para digitar
node.TextRules.Add(TextRule.MaxLength(4));
node.ValueRules.Add(ValueRule.Min(0));      // depois de converter

node.Converter = text => ParseHex(text);    // gancho por campo
GlobalOptions.Converter = ...;              // ou global
```

No original, o `MAX_SIZE` e o `CONTAINS` liam o mesmo array de parâmetros por posição, e a mesma
combinação que funcionava no OverlayApplication quebraria no 0.7.1a. Origem: 3.3; 1.2.

## 3. O objeto do grupo

### 3.1 Quais grupos

A regra vale para todo membro com filhos ou só para o grupo aberto?

```csharp
public Moo Details { get; set; }        // grupo aberto: o inspector não troca o Moo

public Font Caption { get; set; }       // class mostrada fechada, com o diálogo de fonte
inspector["Caption"].Expandable = false;
inspector["Caption"].SetValue(new Font("Arial", 12));   // aqui trocar o objeto é a edição
```

Sugestão: só o grupo aberto. Origem: 3.10.

### 3.2 Structs

Struct não tem identidade, então a detecção de troca não se aplica a ela. Fica assim?

```csharp
public PxPoint Pos { get; set; }
inspector["Pos.X"].SetValue(5);   // já regrava o Pos inteiro no dono
host.Pos = new PxPoint(1, 2);     // troca por fora: comparar valores não separa isso de uma edição
```

Origem: 3.10.

### 3.3 Quando detectar

A cada leitura pela cadeia de pais, só no `Refresh()`, ou nos dois?

```csharp
inspector.Bind(foo);                // cada grupo guarda o objeto que viu (o foo.Moo)
foo.Moo = new Moo();                // troca por fora

inspector["Moo.MooX"].GetValue();   // (a) detecta aqui, na próxima leitura
inspector.Refresh();                // (b) ou só aqui
```

Origem: proposta da 3.10.

### 3.4 O que "desativada" faz

Os filhos recusam gravação e a view desabilita a branch. E o resto?

```csharp
foo.Moo = new Moo();                  // troca detectada: o grupo Moo fica comprometido
inspector["Moo.MooX"].SetValue(1);    // lança (proposta)
inspector["Moo.MooX"].GetValue();     // lê o objeto novo, o antigo, ou devolve null?
inspector.Rebind(foo);                // limpa o estado?
```

Origem: proposta da 3.10.

### 3.5 A raiz

A raiz entra na mesma regra, e o objeto ligado só muda pelo bind?

```csharp
inspector.SetValue(outroFoo);   // hoje: troca o objeto ligado sem passar pelo bind
// proposta: lança; com a composição de 1.1, o método nem existe no Inspector
```

Origem: proposta da 3.10.

## 4. ReadOnly e expansão

### 4.1 ReadOnly num objeto aninhado (class)

Um `[InspectorReadOnly]` explícito num objeto aninhado passa para os filhos?

```csharp
[InspectorReadOnly] public Moo Details { get; set; }

inspector["Details"].SetValue(new Moo());   // recusa: o membro é somente leitura
inspector["Details.MooX"].SetValue(5);      // hoje grava. Deveria recusar também?
```

Origem: seção 5, item 6.

### 4.2 ReadOnly efetivo (antes das views)

Como o nó expõe o valor efetivo, para a view desabilitar o editor?

```csharp
public PxPoint Pos { get; private set; }

inspector["Pos"].ReadOnly;                  // true
inspector["Pos.X"].ReadOnly;                // false: a view mostraria o editor habilitado
inspector["Pos.X"].SetValue(5);             // mas lança 'Pos' is read-only. (testado)
inspector["Pos.X"].IsEffectivelyReadOnly;   // proposta: true
```

Origem: checklist.

### 4.3 Expandir com a flag global

Com a flag ligada, a permissão vale só para o membro ou tipo marcado, como hoje e como no
`TypeSafeLock`, ou passa para os níveis de baixo?

```csharp
GlobalOptions.RequireExpandableAttribute = true;

[InspectorExpandable] public Moo Details { get; set; }   // Details abre
// dentro do Moo: public Doo Doo { get; set; }           // hoje Details.Doo fica fechado. Abre?
```

Origem: seção 5, item 5; seção 7.

### 4.4 `GlobalOptions` ao vivo

A flag é lida só no `Create`. Fica assim?

```csharp
var before = Inspector.Create<Foo>();
GlobalOptions.RequireExpandableAttribute = true;

before["Moo"].IsGroup;                    // hoje: continua true
Inspector.Create<Foo>()["Moo"].IsGroup;   // false
```

Origem: relatório.

## 5. Descoberta

### 5.1 Ordem dos irmãos

A reflection devolve as propriedades antes dos campos, e os membros próprios antes dos herdados.
Fica assim, com `[InspectorOrder]` para quem quiser outra ordem, ou a descoberta tenta recuperar a
ordem de declaração?

```csharp
class Mixed
{
    public int FieldA;
    public int PropB { get; set; }
    public int FieldC;
    public int PropD { get; set; }
}
// hoje sai:   PropB, PropD, FieldA, FieldC   (testado)
// declarado:  FieldA, PropB, FieldC, PropD
```

Origem: relatório; seção 7.

### 5.2 Coleções e arrays

Como devem aparecer?

```csharp
public List<int> Items { get; set; } = [1, 2];
// hoje (testado):
//   Items        grupo
//     Capacity   Number, editável
//     Count      Number, somente leitura
// opções: uma linha só com a contagem; os itens como filhos (Items[0], Items[1]);
// ou um editor de lista
```

Um `int[]` mostra `Length`, `LongLength`, `Rank`, `SyncRoot`, `IsReadOnly`, `IsFixedSize` e
`IsSynchronized`. Origem: relatório.

### 5.3 Propriedades calculadas

A descoberta esconde propriedades calculadas só de leitura, ou isso fica para o
`[InspectorIgnore]`?

```csharp
public PxPoint Pos { get; set; }
// hoje: Pos.X, Pos.Y e Pos.IsEmpty (Toggle, somente leitura)
```

Origem: relatório.

### 5.4 Membro escondido com `new` (antes das views)

Quando o tipo muda, os dois aparecem. Fica só o do tipo derivado?

```csharp
class Base { public int Value { get; set; } }
class Derived : Base { public new string Value { get; set; } = ""; }
// hoje (testado): duas linhas Value, a string e a int; inspector["Value"] acha a string
// com o mesmo tipo (public new int Value) aparece uma só
```

Origem: checklist.

### 5.5 Tipos com editor próprio

Como a descoberta reconhece os tipos que deveriam ter um editor, em vez de virar grupo?

```csharp
public System.Drawing.Color Fill { get; set; }
// hoje (testado): um grupo com R, G, B, A, IsKnownColor, IsEmpty, IsNamedColor, IsSystemColor e
// Name, todos somente leitura. Nada ali edita a cor.
// desejado: uma linha só, com o editor Color (o seletor ARGB do OverlayApplication)
```

Origem: 3.2.

### 5.6 Cache

Só a lista de membros pode ser cacheada por tipo, porque os nós guardam as opções de cada
inspector. Confirma?

```csharp
var a = Inspector.Create<Foo>();
var b = Inspector.Create<Foo>();
a["x"].Label = "PosX";          // não pode mudar b["x"]: os nós são de cada inspector
// por tipo, dá para guardar só o que o GetMembers devolve para Foo
```

Origem: 3.1 e 3.10.

## 6. Opções e editores

### 6.1 `Visible`

Está sem uso: ninguém escreve nem lê. Sai, ou fica para a visibilidade condicional? E qual a
diferença para o `Ignored`?

```csharp
node.Ignored = true;    // hoje: sai da enumeração, com a subárvore
node.Visible = false;   // hoje: nada acontece

// uma divisão possível:
// Ignored: não faz parte deste inspector
// Visible: faz parte, mas está escondido agora (a regra de 6.2 liga e desliga)
```

Origem: seção 7; relatório.

### 6.2 Visibilidade condicional

O formato é algo como o `VisibleWhen`, o sucessor do `VariablePool`?

```csharp
component["Text"].VisibleWhen = c => ((ComponentPreset)c).IsText;

// no original: VariablePool = { "blacklist", "Text", "FontName", "IsBold", ... }
// e Modify.ToggleFieldVisible(b, "Color order") no editor de efeitos
```

Origem: 3.2; 1.2.

### 6.3 Editores do `EditField()`

Como declarar o que o OverlayApplication fazia pelo `EditField()`: itens de escolha, seletores e a
ação de um botão?

```csharp
inspector["ColorOrder"].Choices = ["rgba", "argb", "bgra"];   // lista de strings
inspector["Mode"].Choices = Enum.GetValues<Mode>();           // enum: precisa, ou é automático?
inspector["Fill"].Editor = EditorKind.Color;                   // abre o seletor ARGB
inspector["Opacity"].Range = new NumericRange(0, 255, 5);     // faixa e passo (já existe)
```

Origem: 1.2; checklist.

### 6.4 Filtro pelo objeto

Existe um sucessor tipado do `IVarProvider`, uma interface que o próprio objeto implementa para
esconder campos?

```csharp
class Rectangle : ComponentPreset, IInspectorFilter
{
    public bool Shows(string path) => path is not ("Text" or "FontName" or "IsBold");
}
```

Origem: 3.2; checklist.

### 6.5 Seletor por expressão

Um seletor que o compilador checa e que acompanha renomeações: entra quando? Com o `Inspector`
não genérico, o tipo vai na chamada.

```csharp
inspector["Moo.MooX"].Label = "X";                    // hoje: erro de digitação só em execução
inspector.Node((Foo f) => f.Moo.MooX).Label = "X";    // proposta
```

Origem: 3.2.

## 7. Views e estrutura

### 7.1 Alvo da biblioteca (antes das views; a primeira decisão)

Com as views, a biblioteca volta a precisar do Windows. Qual das três?

```xml
<!-- (a) só Windows: uma DLL, e o TuxHost deixa de rodar fora do Windows -->
<TargetFramework>net10.0-windows</TargetFramework>

<!-- (b) dois alvos num projeto: o código das views só no -windows, com o bloco condicional -->
<TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>

<!-- (c) projeto à parte: InteractiveEditor (net10.0) e InteractiveEditor.Views
     (net10.0-windows), duas DLLs -->
```

Origem: 3.7; conversa de 27/09.

### 7.2 Fábricas

Nomes distintos por plataforma, para um projeto só WinForms não precisar referenciar o WPF?

```csharp
// overloads: um projeto só WinForms não compila (CS0012, pede PresentationFramework)
InspectorView.Create(inspector, panel);     // Control do WinForms
InspectorView.Create(inspector, grid);      // Control do WPF

// nomes distintos: compila (testado)
inspector.CreateWinFormsView(panel);
inspector.CreateWpfView(grid);
```

Origem: 3.5.

### 7.3 Lista ou árvore

A view monta as linhas a partir de uma lista plana ou percorre a árvore pelos filhos?

```csharp
// (a) lista plana, como a enumeração de hoje: recuo pela profundidade
foreach (var node in inspector.Rows)
    AddRow(node, depth: node.Path.Count(c => c == '.'));

// (b) árvore: um painel por grupo, que recolhe junto
void Build(InspectorNode node, Panel parent)
{
    foreach (var child in node.Children) ...
}
```

Origem: conversa de 27/09.

### 7.4 Válvula de escape

Um callback por plataforma, para o que a configuração agnóstica não cobrir?

```csharp
view.ControlCreated += (path, control) =>
{
    if (path == "Opacity" && control is TrackBar bar)
        bar.TickFrequency = 5;
};
```

Origem: 3.5.

### 7.5 Passo de layout (antes das views)

Fica no núcleo, agnóstico, e devolve os retângulos de cada linha?

```csharp
var rows = Layout.Compute(inspector);   // sem nenhum controle de UI
// cada linha: PxRect Row, PxRect Label e PxRect Editor
view.Apply(rows);                       // a view só posiciona
```

Depende de 8.1 e 8.2. Origem: 3.4.

## 8. Primitivos e PixieLib

### 8.1 Precisão (antes das views)

Manter as variantes int e float, como o `System.Drawing`, ou um tipo só em double, como o WPF?

```csharp
// (a) hoje: int e float
PxPoint p = new(10, 20);
PxPointF f = new(10.5f, 20f);

// (b) um tipo só, em double
PxPoint d = new(10.5, 20.0);
```

Origem: seção 5, item 1.

### 8.2 Primitivos novos (antes das views)

Criar `PxRect`, `PxPadding` e `PxDock`?

```csharp
PxRect label = new(0, 23, 120, 23);     // x, y, largura, altura: o resultado do layout
PxPadding margin = new(4, 2, 4, 2);     // o Padding do WinForms, o Thickness do WPF
PxDock dock = PxDock.Top;               // no lugar do DockStyle nas opções
```

Origem: seção 5, item 1.

### 8.3 Cores

Ganham o prefixo ou continuam com o espaço de cor no nome?

```csharp
ArgbColor fill;  HslColor tone;       // hoje
PxColor fill;    PxHslColor tone;     // com o prefixo
```

Origem: seção 5, item 1.

### 8.4 CS0457

A conversão `ArgbColor → HslColor` está declarada nas duas structs. Em qual das duas ela fica?

```csharp
// ArgbColor.cs: public static implicit operator HslColor(ArgbColor color)
// HslColor.cs:  public static implicit operator HslColor(ArgbColor color)
HslColor hsl = argb;    // CS0457: conversão ambígua
```

Origem: 3.4.

### 8.5 Regras de conversão

Confirma as de 3.4?

```csharp
System.Drawing.Point sd = pxPoint;       // implícita (hoje só existe esse sentido)
PxPoint back = sd;                       // implícita (proposta: o outro sentido)
System.Windows.Point wp = pxPoint;       // implícita: o double comporta tudo
var ws = (System.Windows.Size)pxSize;    // explícita: o Size do WPF lança com negativo
PxPoint fromWpf = (PxPoint)wp;           // explícita: perde precisão
```

As conversões do WPF só voltam junto com o alvo Windows (7.1). Origem: 3.4.

### 8.6 PixieLib: precisão padrão

`float`, que repassa para o `System.Numerics`, ou `double`, como o `Vec2` do C++?

```csharp
PxVec2f a;   // float: o Add vira o Vector2.Add, com o mesmo vaddps (testado no JIT)
PxVec2d b;   // double: implementação própria, como o pxVec2 do C++
// qual das duas o inspector usa, e qual o nome sem sufixo, se ele existir?
```

Origem: 3.9.

### 8.7 PixieLib: segunda DLL

Quando o layout usar os primitivos, o inspector passa a depender da PixieLib. Aceita essa exceção
à regra de uma DLL?

```text
TuxHost → InteractiveEditor.dll → PixieLib.dll
```

Origem: 3.9.

### 8.8 PixieLib: onde e como

Confirma as propostas de 3.9?

```text
Sakamoto0110/PixieLib
├── cpp/      existe, de 2023, com o pxVec2 em double
└── dotnet/   proposta: a PixieLib em C#, com o gerador de precisões
```

```csharp
PxPoint q = p + s;          // ponto + tamanho → ponto
PxVec2 d = q - p;           // ponto − ponto → vetor
PxVec2 v = p;               // implícita para PxVec2: a matemática vem dele
PxPoint back = (PxPoint)v;  // explícita de volta
```

Origem: 3.9.

### 8.9 Sufixo de precisão

Qual fica?

```csharp
PxPointF a;   // hoje: o F do System.Drawing
PxPointf b;   // o sufixo do gerador, como em PxVec2f, PxVec2d e PxVec2i
```

Origem: 3.9.

## 9. O que eu posso aplicar assim que você responder

### 9.1 Ligar que lança, `Unbind()` e religar, ainda sem o multi-bind

```csharp
inspector.bind(foo);
inspector.bind(bar);    // passa a lançar: já há um objeto ligado
inspector.Unbind();     // e a troca fica explícita
inspector.bind(bar);
```

Posso?

### 9.2 A regra do objeto do grupo

```csharp
inspector["Moo"].SetValue(new Moo());   // passa a lançar
inspector["Pos.X"].SetValue(5);         // a struct continua voltando ao dono, por dentro
```

Depende de 3.1.

### 9.3 O inspector guardando a raiz

```csharp
inspector.Label = "x";                  // deixa de compilar
```

Depende de 1.1.

### 9.4 Sem `IEnumerable`, com nome nos percursos

```csharp
foreach (var node in inspector)         // deixa de compilar
foreach (var node in inspector.Rows)    // no lugar dele
```

Depende de 1.3.
