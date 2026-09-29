# Perguntas em aberto

Estado de 29 de setembro de 2026, depois das respostas de 27/09 e de 29/09. As respondidas saíram
daqui e estão na seção 0 de `notas-modernizacao.md`, citadas com um `P` na frente do número
(`P2.2`). Na última rodada de 29/09, as sugestões das que sobravam (1.14, 5.9, 5.10, 6.7 e 7.5)
foram aceitas. Ao aplicar a 5.10 (o seletor e o editor de lista) e a 7.5 (o passo de layout),
sobraram cinco escolhas que são suas: 4.7, 5.11, 5.12, 5.13 e 7.6. Na 5.12, na 5.13 e na 7.6, o
código já está como a sugestão; na 4.7 e na 5.11, ele está como o "hoje" do exemplo, e a sugestão
é mudar. As duas que você deixou para o final continuam no fim.

Os números antigos continuam valendo, e os novos seguem a numeração de cada seção, sem reaproveitar
número. Os nomes que ainda não existem no código são só ilustração, e a sugestão, quando há, vem no
fim de cada pergunta.

Dá para responder pelo número, como antes: "5.9: sim" ou "6.7: b".

---

## 4. Somente leitura

### 4.7 Coleção só com getter

A P4.1 deixa os filhos de um objeto aninhado somente leitura quando ele é, e um membro só com
getter é somente leitura. Aplicada a uma coleção (commit `eb58a3e`), ela trava o jeito mais comum
de declarar uma:

```csharp
public List<Moo> Items { get; } = [];

// hoje: Items fica somente leitura, e com ela os campos dos itens (Items.Item.MooX) e o editor de
// lista (AddItem lança); inspector["Items"].ReadOnly = false abre tudo
// (a) fica assim, como no objeto aninhado
// (b) numa coleção, o getter sozinho só impede trocar a coleção inteira; os itens e as operações
//     da lista continuam editáveis, e o [InspectorReadOnly] continua travando tudo
```

Sugestão: (b), porque a coleção só com getter é o padrão do .NET (a regra de análise CA2227 pede
isso), e o que o getter protege ali é a referência, não o conteúdo. O objeto aninhado fica como a
P4.1 decidiu.

## 5. Descoberta

### 5.11 As opções dos itens de uma coleção

Os atributos ficam no membro, e o item de uma coleção não tem membro. Hoje (commit `eb58a3e`), os
atributos do membro da coleção ficam no nó dela, e a linha do item só recebe o que o tipo do item
diz:

```csharp
[InspectorRange(0, 255)]
public List<int> Levels { get; set; } = [10, 20];

// hoje: inspector["Levels"].Range é (0, 255), e inspector["Levels.Item"].Range é null
// (a) a faixa e o scrubbing do membro passam para a linha do item, que é onde há número
// (b) ficam no nó da coleção, e o item recebe as opções à mão:
//     inspector["Levels.Item"].Range = new NumericRange(0, 255)
```

Sugestão: (a), porque a coleção não tem valor para limitar, e no editor de lista a linha do item
serve de modelo para a linha de cada item. O `[InspectorEditor]` continua escolhendo o editor da
coleção (seletor ou lista), e o rótulo, a dica e a ordem continuam na linha da coleção.

### 5.12 A linha do item escolhido

Ao aplicar a 5.10, o item escolhido ganhou uma linha própria, logo abaixo do seletor, com os campos
dele embaixo:

```text
Items          seletor              inspector["Items"]
└─ Item        o item escolhido     inspector["Items.Item"]
   ├─ MooX                          inspector["Items.Item.MooX"]
   └─ MooY
Numbers        seletor              (um List<int>)
└─ Item        número               inspector["Numbers.Item"]
```

- (a) fica assim, uma regra só para toda coleção
- (b) os campos direto embaixo do seletor (`Items.MooX`), e a linha do item só para um item sem
  campos, como o número

Sugestão: (a), porque o item sem campos, o de tipo com mais de um editor (fica fechado, como um
membro) e o struct saem da mesma regra, sem caso especial. O custo é uma linha a mais, a do item,
quando ele tem campos. `Item` é o nome que o C# dá ao indexador.

### 5.13 O editor de lista e o controle do binder

Sem `ViewToInstance`, o `SetValue` guarda o valor no nó até o `Apply()`. As operações do editor de
lista (commit `5395dea`) não são um valor:

```csharp
inspector.Options.BinderControl = BinderControlMode.InstanceToView;
((CollectionNode)inspector["Items"]).AddItem();

// hoje: grava na hora, como o botão
// (a) fica assim
// (b) lança, pedindo para ligar o ViewToInstance
// (c) a coleção inteira fica guardada como valor pendente até o Apply()
```

Sugestão: (a). A (b) deixa o editor de lista sem uso no modo manual, e a (c) pede um valor
pendente para a coleção inteira, com as operações refeitas no `Apply()`. A edição de um item
continua seguindo o controle do binder, pela linha do item.

## 7. Views

### 7.6 O passo de layout: a coluna dos editores e os valores padrão

O passo de layout entrou no commit `166ec4a` (P7.5), com duas escolhas minhas. A primeira é a
coluna dos editores:

```text
A            [ editor            ]      rótulo com 120 (LabelWidth)
▾ Box
    X        [ editor            ]      rótulo com 104 (120 menos um recuo)
    ▾ Down
        Z    [ editor            ]      rótulo com 88
```

- (a) os editores numa coluna só: o rótulo de cada nível perde o recuo, e o editor tem a mesma
  largura em qualquer profundidade
- (b) o rótulo sempre com 120, e o editor andando para a direita a cada nível

A segunda são os valores padrão: `RowHeight` 23 (o `FieldHeight` do OverlayApplication),
`RowSpacing` 2, `Indent` 16, `LabelWidth` 120, `LabelSpacing` 4, `Padding` 4 e `ListRows` 5 (as
linhas de item que o editor de lista mostra antes de rolar).

Sugestão: (a), como o PropertyGrid do WinForms, que alinha os valores numa coluna, e os valores
padrão como estão, como ponto de partida; cada inspector muda os seus pelo `inspector.Options`.

---

## Para o final

### 6.2 Visibilidade condicional e 6.3 Editores do `EditField()`

Você pediu que eu explique com calma: por que isso existia, como funcionava, se é necessário, a
importância e o estrago se sair. Isso vem depois da parte de código (29/09). Para explicar, vou
reler o código original (`Sakamoto0110/InteractiveEditor`, branch `InspectorVariant0.7.1a`) e o
OverlayApplication, que ainda não estão nesta sessão.
