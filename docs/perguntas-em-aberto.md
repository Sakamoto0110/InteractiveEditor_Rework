# Perguntas em aberto

Estado de 27 de setembro de 2026, depois das respostas do mesmo dia. As respondidas saíram daqui e
estão na seção 0 de `notas-modernizacao.md`, citadas com um `P` na frente do número (`P2.2`). O que
ficou: as que não foram respondidas, as que ficaram para o final e os desdobramentos das respostas.

Os números antigos continuam valendo, e os novos seguem a numeração de cada seção, sem reaproveitar
número. A seção 0 é nova: ela é da premissa de erros. Os nomes que ainda não existem no código são
só ilustração, e a sugestão, quando há, vem no fim de cada pergunta.

Dá para responder pelo número, como antes: "3.2: sim" ou "1.13: ok".

---

## 0. A premissa: o inspector não cai

### 0.1 Severidade

A premissa fala em fallback automático, em semiautomático e em só as falhas graves subirem. Uma
escala possível, da mais leve para a mais grave:

| Nível | O que aconteceu | Exemplo |
|---|---|---|
| Recuperado | fallback automático, sem ambiguidade | um getter lança numa leitura da view: a linha mostra o erro, e a leitura seguinte tenta de novo |
| Contornado | fallback semiautomático: não caiu, mas pode não ser o certo | `[InspectorRange(10, 1)]`: a faixa é ignorada, e quem assina o evento pode pôr outra |
| Crítico | sem resolução: o nó que falhou sai, e o resto continua | o tipo de um membro não carrega (assembly ausente) |
| Fatal | o inspector não tem como continuar | o próprio tipo da raiz não pode ser lido |

Com isso, a contagem do `OnCreated` sai direto: todos os erros, os contornados que ninguém tratou
(o fallback valeu) e os críticos.

Sugestão: essa escala, e só o fatal sobe para quem chamou. Na resposta, o crítico também subia, mas
o exemplo do `CriticalErrorCount` dizia que ele não derruba nada.

### 0.2 Onde passa a linha entre lançar e avisar

As respostas pediram exceção para o uso errado da API, e a premissa pede evento para os pontos
fracos. A linha que eu tirei disso:

```csharp
inspector.Bind(foo);
inspector.Bind(bar);          // uso errado: lança, e o foo continua ligado
inspector["Moo.Nope"];        // uso errado: KeyNotFoundException, como hoje
inspector["Moo"].SetValue(m); // uso errado: lança (grupo)

class Boo { [InspectorRange(10, 1)] public int Opacity { get; set; } }
Inspector.Create<Boo>();      // hoje lança. Uso errado ou ponto fraco da descoberta?
```

O atributo inválido é o caso de fronteira: foi quem escreveu a classe que errou, mas o erro aparece
dentro da descoberta.

Sugestão: a linha como acima, e o atributo inválido como ponto fraco (evento, fallback e o
`Create` segue).

## 1. O Inspector

### 1.6 Campo só de exibição (o resto da 1.6)

O botão entra e o cabeçalho não. Faltou o campo só de exibição. Um membro somente leitura já cobre o
`UID` e o `Name` do OverlayApplication; o caso que sobra é um valor sem membro, como o `Layer`, que
vinha da árvore de camadas.

```csharp
component.AddButton("LayerUp", "▲", () => tree.OnLayerUp());   // entra
component.AddDisplay("Layer", () => tree.SelectedIndex);        // entra?
```

Sugestão: entra, com um getter.

### 1.9 Os eventos da criação

A criação, a descoberta e as falhas dela acontecem dentro do `Create`. Quando o `Create` devolve o
inspector, esses eventos já passaram, e ninguém conseguiu assinar.

```csharp
var inspector = Inspector.Create<Foo>();       // a descoberta roda aqui dentro
inspector.OnDiscoveryFailure += ...;           // tarde demais
```

- (a) eventos estáticos, que valem para todos os inspectors: assina antes do `Create`;
- (b) criação em dois passos: criar, assinar e só então descobrir (`inspector.Build()`);
- (c) o `Create` guarda o resultado no inspector (as contagens e as falhas), para ler depois; aí
  ninguém age no meio da descoberta.

Sugestão: (a) para agir no meio do caminho e (c) para conferir depois.

### 1.10 `ValueChanged` nos dois sentidos

`ValueChanged` serve. Há só um motivo para pensar no nome: com o `INotifyPropertyChanged` (P2.6), o
nó também fica sabendo de mudanças que vêm do objeto, e a view precisa separar as duas para não
entrar em laço (ela grava, o evento volta, ela atualiza o controle, o controle avisa que mudou...).

```csharp
node.ValueChanged += (s, e) =>
{
    if (e.Source == ValueSource.View)
        return;                   // a própria edição: nada a fazer
    UpdateControl(node);          // veio do objeto, de um Refresh() ou de um Force*
};
```

Sugestão: um evento só, com a origem nos args. E os nomes seguem a convenção do .NET: o evento se
chama `Created`, e `OnCreated` é o método que o dispara.

### 1.11 A trava das opções globais com vários inspectors

A trava fica enquanto houver pelo menos um inspector vivo. Um inspector que nunca recebe `Dispose`
segura a trava para sempre: o finalizador não serve, porque não tem hora para rodar.

```csharp
using var a = Inspector.Create<Foo>();
var b = Inspector.Create<Boo>();                     // sem using nem Dispose
a.Dispose();
GlobalOptions.RequireExpandableAttribute = true;     // lança: o b ainda está vivo
```

O TuxHost muda junto: hoje ele liga a flag com três inspectors vivos.

Sugestão: uma contagem de inspectors vivos, que solta a trava no último `Dispose`; a mensagem da
exceção diz quantos ainda estão vivos.

### 1.12 O modo pelo jeito de criar (a opinião que você pediu na 1.7)

Concordo: tipado infere o automático, e sem tipo infere o manual, porque sem tipo não há o que
descobrir. Eu só manteria o modo explícito para o caso tipado e manual, que é o do
OverlayApplication: editores de tipos conhecidos, montados campo a campo. Tipado, o `Add("X")`
confere na hora que o membro existe, e o editor sai do tipo dele; sem tipo, o membro só é procurado
pelo nome no bind, e um erro de digitação só aparece lá.

```csharp
var auto = Inspector.Create<ComponentPreset>();                       // automático
var manual = Inspector.Create<ComponentPreset>(TypeBinderMode.Manual);
manual.Add("X").Label = "PosX";           // confere já que ComponentPreset tem X
var untyped = Inspector.Create();         // manual, sem tipo
untyped.Add("X");                         // o X só é procurado no bind
```

Confirma?

### 1.13 O enum de controle do binder

Pelo que você descreveu, `Automatic` só vale junto com um sentido, e `Manual` exclui o resto. Dá
para o próprio enum não ter combinação inválida: automático é ter algum sentido ligado, e manual é
não ter nenhum.

```csharp
[Flags]
enum BinderControlMode
{
    Manual = 0,                                    // nada passa sozinho: só os Force*
    ViewToInstance = 1,
    InstanceToView = 2,
    Automatic = ViewToInstance | InstanceToView,   // os dois sentidos: o padrão
}
```

Sugestão: assim, sem precisar validar.

## 2. Binding

### 2.10 O `GetValue` com valores mistos

A 2.4 decidiu o que a linha mostra. Falta o que o nó devolve para quem chama.

```csharp
inspector.Bind(a);
inspector.AddBind(b);           // a.X == 1, b.X == 2

inspector["X"].GetValue();      // 1, o da primeira instância
inspector["X"].IsMixed;         // true: é o que a linha usa para o indicativo
inspector["X"].GetValues();     // { 1, 2 }, um por objeto
```

Sugestão: assim.

### 2.11 Cultura padrão e mecanismo

A cultura vai para as opções do inspector (P2.7). Falta o valor padrão e o jeito de converter.

```csharp
inspector.Options.Culture = CultureInfo.InvariantCulture;   // para quem quiser
// padrão: a cultura atual, como no original (Convert.ToDouble)?
// conversão: IParsable<T> quando o tipo implementa (números, DateTime, Guid...),
// e TypeConverter para o resto (enums, Color...)
```

Sugestão: a cultura atual como padrão, e os dois mecanismos, nessa ordem.

### 2.12 Quando a view grava (a sugestão que você pediu na 2.7.1)

Gravar a cada tecla briga com quem está digitando: `-`, `1,` e o campo vazio não convertem, e cada
um viraria uma falha na linha. O que eu sugiro:

- texto e número: grava no Enter e quando o controle perde o foco; o Esc volta ao valor do objeto;
- toggle e escolha: grava na hora, porque não há estado intermediário;
- slider e scrubbing: grava enquanto arrasta, como no original, para o objeto acompanhar;
- conversão que falha: a linha mostra a falha, o texto fica como foi digitado, e o objeto mantém o
  valor que tinha.

Confirma?

### 2.13 Sanitizadores: uma lista ou duas

A 2.9 decidiu a lista ordenada por campo. No original, texto e número se misturavam na mesma
cadeia, e uma regra de número recebia texto e não fazia nada. Numa lista só, uma regra de valor
antes de uma de texto não faria sentido.

```csharp
// (a) uma lista: a conversão acontece entre a última regra de texto e a primeira de valor
node.Sanitizers.Add(TextRule.Digits);
node.Sanitizers.Add(ValueRule.Min(0));

// (b) duas listas, cada uma na sua ordem: texto → conversão → valor
node.TextRules.Add(TextRule.Digits);
node.ValueRules.Add(ValueRule.Min(0));
```

Sugestão: (b), porque a ordem entre texto e valor fica garantida pela estrutura.

## 3. O objeto do grupo

### 3.2 Structs (explicada de novo)

Para saber se o objeto de um grupo foi trocado por fora, o grupo guarda o objeto que viu no bind e
depois compara a referência. Com class isso funciona, porque o objeto tem identidade. Com struct,
não: cada leitura devolve uma cópia nova, e não há referência para comparar.

```csharp
public Moo Details { get; set; }   // class
foo.Details = new Moo();           // outra referência: dá para saber que foi trocado

public PxPoint Pos { get; set; }   // struct
foo.Pos = new PxPoint(1, 2);       // troca por fora
inspector["Pos.X"].SetValue(5);    // edição pelo inspector: também regrava o Pos inteiro no dono
// nos dois casos, o inspector só vê que o valor de Pos mudou, e não separa um do outro
```

A pergunta: o grupo de uma struct fica fora da detecção, mostrando sempre o valor atual, sem ramo
comprometido? Gravar no grupo fica proibido do mesmo jeito (9.2): `inspector["Pos"].SetValue(...)`
lança, e a struct muda pelos filhos.

Sugestão: sim.

### 3.6 O raio da invalidação e o religar automático

Do seu comentário sobre a seção 8 do relatório, com o exemplo:

```csharp
class Foo
{
    public Moo Moo { get; set; } = new();
    public void Work() => Moo = new Moo();
}

inspector.Bind(foo);
foo.Work();                        // troca por fora
```

Um detalhe antes: nada lança no .NET quando isso acontece. Hoje a cadeia de pais simplesmente lê o
`Moo` novo; com a detecção, o grupo guarda o `Moo` que viu no bind, e ele continua vivo. É a
comparação entre os dois que acusa a troca, o que combina com a premissa: nenhuma exceção no
caminho.

O que falta decidir:

- **Raio**: (a) só o ramo trocado (o `Moo` e o que está abaixo dele), e o resto do `Foo` continua
  funcionando; ou (b) o dono inteiro, como no seu exemplo: o `Foo` todo, que no topo é o inspector
  inteiro.
- **Reação**: (a) desativa o ramo, e ele fica assim até alguém religar (a 3.4); (b) religa sozinho
  a partir da raiz; (c) desativa e avisa, e quem assina o evento pode aceitar o objeto novo na
  hora, que é o fallback semiautomático da premissa.

Sugestão: raio (a), porque o `Foo` continua sendo o mesmo objeto e os outros membros dele seguem
certos; reação (c). A sua ideia de separar, no nó, as opções do estado (comprometido, erros) em
duas regiões do arquivo fica anotada para quando esse estado existir.

## 4. ReadOnly

### 4.5 Quais membros a reflection esconde

Do seu comentário sobre a seção 3.6 do relatório: o setter privado some, e o `[InspectorReadOnly]`
traz o membro de volta. Há casos parecidos:

```csharp
public string Secret { get; private set; }    // setter privado: some (decidido)
public string Code { get; protected set; }    // protected ou internal: some também?
public string Key { get; init; }              // init: só no construtor
public readonly int Seed;                     // campo readonly
public Guid Id { get; }                       // só getter, auto-propriedade
public int Area => W * H;                     // só getter, calculada: aparece (P5.3)
```

Com isso, o `Secret` do `Boo` sai da saída do TuxHost.

Sugestão: some todo setter não público (private, protected, internal), que é o caso em que a
classe escolheu esconder a escrita; `init`, `readonly` e só getter continuam aparecendo, somente
leitura.

### 4.6 O `ReadOnly` herdado e a camada manual

Para o `ReadOnly` passar para os filhos (P4.1, P4.2) sem um segundo valor, o nó pode consultar os
pais na hora da leitura: o `ReadOnly` dele é o próprio ou o de algum pai. Assim, uma mudança na
camada manual vale na hora para o ramo todo.

```csharp
[InspectorReadOnly] public Moo Details { get; set; }

inspector["Details.MooX"].ReadOnly;           // true, pelo pai
inspector["Details.MooX"].ReadOnly = false;   // não reabre enquanto o Details for somente leitura
inspector["Details"].ReadOnly = false;        // aí o ramo inteiro reabre
```

A outra saída é copiar para os filhos no `Create`: a camada manual consegue reabrir um filho, mas
uma mudança no pai depois do `Create` não chega aos filhos.

Sugestão: a consulta na hora da leitura. É o que faz o "se tentar forçar, lança" da 4.1 valer
sempre.

## 5. Descoberta

### 5.7 O nome composto e o ponto do caminho

Na 5.4, o nome composto (`Derived.Value`) pode ser o padrão. Só que o ponto é o separador do
caminho: `inspector["Derived.Value"]` procuraria um filho `Derived` com um filho `Value`.

```csharp
class Base { public int Value { get; set; } }
class Derived : Base { public new string Value { get; set; } = ""; }

inspector["Value"];                  // hoje: acha o do Derived, que vem primeiro
inspector["Derived.Value"];          // KeyNotFoundException: o ponto separa o caminho
inspector["Value", typeof(Base)];    // uma saída: o escondido, pelo tipo que o declara
```

Sugestão: o nome composto só no rótulo (`Derived.Value` e `Base.Value`); no caminho, `Value`
continua sendo o do tipo derivado, como no C#, e o escondido é achado pelo tipo que o declara.

Uma dúvida sobre a sua resposta: no `"Base.Derived" - expandível`, o que seria expandível?

### 5.8 `Color` sem escolha explícita

A 5.5 pede que o comportamento de tipos como o `Color` seja escolhido explicitamente. Sem ninguém
escolher, o que aparece?

```csharp
public System.Drawing.Color Fill { get; set; }   // ninguém escolheu o editor
// (a) grupo, como hoje: R, G, B, A, IsKnownColor, IsEmpty... todos somente leitura
// (b) uma linha Display, só leitura, até alguém escolher
// (c) não aparece, e um evento avisa
```

Sugestão: (b), com um aviso de diagnóstico.

## 6. Opções e editores

### 6.6 O filtro por nome: por tipo ou por instância

A 6.4 põe o filtro na precedência dos atributos, o que o aplica no `Create`, por tipo. O caso do
OverlayApplication era por instância: um editor só para retângulo, elipse e texto, e cada objeto
escondia os campos que não usava.

```csharp
var component = Inspector.Create<ComponentPreset>();
component.Bind(rectangle);    // esconde Text, FontName e IsBold
component.Rebind(label);      // mostra de novo
```

Sugestão: por tipo, como na 6.4; o caso por instância vai junto com a visibilidade condicional
(6.2), que ficou para o final.

## 9. O que depende de você para aplicar

### 9.3 O inspector guardando a raiz (explicada de novo)

É a aplicação da 1.1, a composição, que você já decidiu. Hoje o `Inspector` herda de
`InspectorNode`, então tudo o que um nó tem aparece também no inspector, mesmo sem sentido na raiz:

```csharp
inspector.Label = "x";            // compila, e ninguém lê: a raiz não é um membro
inspector.Range = ...;            // idem
inspector.ReadOnly = true;        // idem
inspector.GetValue();             // devolve o objeto ligado
inspector.SetValue(outroFoo);     // com a 9.2, lança
```

Com a composição, o `Inspector` deixa de ser um nó: ele guarda a raiz num campo interno e expõe só
o que é dele (o indexador, a enumeração, as linhas, o bind e, depois, eventos e opções). As linhas
acima deixam de compilar, e as opções continuam nos nós: `inspector["x"].Label`.

Posso aplicar?

## Para o final

### 6.2 Visibilidade condicional e 6.3 Editores do `EditField()`

Você pediu que eu explique com calma: por que isso existia, como funcionava, se é necessário, a
importância e o estrago se sair. Para isso vou reler o código original
(`Sakamoto0110/InteractiveEditor`, branch `InspectorVariant0.7.1a`) e o OverlayApplication, que não
estão nesta sessão.
