# Perguntas em aberto

Estado de 2 de outubro de 2026, depois das respostas de 27/09, de 29/09, de 01/10 e de 02/10. As
respondidas saíram daqui e estão na seção 0 de `notas-modernizacao.md`, citadas com um `P` na frente
do número (`P2.2`), e a explicação da 6.2 e da 6.3, que você pediu, ficou na seção 3.12 das notas.

Ainda em 02/10 você aceitou seguir pelas views, em cinco cortes. O primeiro, os dois alvos e as
conversões dos primitivos (P7.1), entrou no commit `ccc3a5d`. O segundo é a view WinForms (P7.2 e
P7.3), e ao desenhá-la apareceram as escolhas abaixo, de 7.7 a 7.13. A 7.8 e a 7.13 mudam o núcleo
(um evento novo cada); as outras são de como a view se comporta. Nenhuma tem código ainda.

Os números antigos continuam valendo, e os novos seguem a numeração de cada seção, sem reaproveitar
número. Os nomes que ainda não existem no código são só ilustração, e a sugestão, quando há, vem no
fim de cada pergunta.

Dá para responder pelo número, como antes: "7.7: sim" ou "7.8: b".

---

## 7. Views

### 7.7 A forma da view WinForms

A P7.2 decidiu fábricas com nomes distintos, e a 3.5, que a view recebe o inspector e o observa, sem
herdar dele. Falta a forma:

```csharp
var view = inspector.CreateWinFormsView();   // um UserControl, só no alvo -windows
view.Dock = DockStyle.Fill;
form.Controls.Add(view);
```

- a classe: `WinFormsInspectorView`, um `UserControl` (o do WPF seria `WpfInspectorView`, também
  com nome distinto, para um arquivo que importa os dois não ter ambiguidade);
- a fábrica: (a) um método de extensão do `Inspector`, no código da view; ou (b) um método do
  próprio `Inspector`, num arquivo parcial `Inspector.Windows.cs`, como no commit `c537554`;
- o código: numa pasta `Views/WinForms`, que o alvo `net10.0` deixa de fora inteira, sem o sufixo
  `.Windows.cs` em cada arquivo;
- descartar a view tira as assinaturas dela e não mexe no inspector, que é de quem o criou.

Sugestão: a classe, a pasta e o descarte como acima, e a fábrica (a), porque o `Inspector` fica
igual nos dois alvos, e tudo o que é da view fica na pasta dela.

### 7.8 Uma opção mudada depois de a view montar

Hoje só a regra do `VisibleWhen` avisa quando muda (`VisibleChanged`). As outras opções são
propriedades sem aviso, então uma mudança feita depois de a view montar não chega a ela:

```csharp
// num handler do ValueChanged do tipo de efeito, como o OverlayApplication fazia (3.12)
effect["TargetColor"].Visible = false;

// hoje: o nó muda, mas nada avisa, e a view continua mostrando a linha
// (a) um evento no inspector quando muda uma opção que a view mostra (o rótulo, o editor, a
//     faixa, Visible, Collapsed, Ignored, Order, ReadOnly, Expandable, VisibleWhen, Choices, o
//     texto de um botão e as opções de layout do InspectorOptions), com o nó e qual opção; a view
//     refaz o layout ou só a linha
// (b) a view lê as opções quando monta, e quem muda uma depois chama view.Rebuild()
```

Sugestão: (a), pelo mesmo motivo da P6.2: mostrar de novo não pode depender de alguém lembrar. O
evento só sai quando o valor muda de fato, e as listas de regras (`TextRules` e `ValueRules`) ficam
de fora, porque a view não as mostra.

### 7.9 O controle de cada editor

A 3.5 já fala em `TextBox`, `ComboBox`, `TrackBar` e `CheckBox`. A proposta para todos:

| Editor | Controle | Observação |
|---|---|---|
| `Text` | `TextBox` | grava no Enter e ao perder o foco, e o Esc volta (P2.12) |
| `Number` | `TextBox` | como o texto; a conversão e a faixa já são do núcleo, e o `NumericUpDown` (em `decimal`, com limites próprios) duplicaria isso |
| `Toggle` | `CheckBox` | grava na hora |
| `Choice` | `ComboBox` só de escolha | a lista vem do `GetChoices()` cada vez que abre (P6.3) |
| `Slider` | `TrackBar` | posições pela faixa e pelo passo (o `TrackBar` só conta em int); sem faixa, vira um `Number` |
| `Color` | botão pintado com a cor | o clique abre o `ColorDialog` do sistema |
| `Button` | `Button` | com o `Text` do nó, chama o `Press()` |
| `Display` | `TextBox` só leitura | dá para selecionar e copiar |
| `Header` | só o rótulo, em negrito | o grupo de um tipo (P5.9) |
| `Separator` | uma linha horizontal | |
| `Selector` | `ComboBox` só de escolha | os itens, e a escolha vai para o `SelectedIndex` |
| `List` | `ListBox` e quatro botões | adicionar, remover, subir e descer, na linha a mais que o layout reserva |

Sugestão: a tabela como está.

### 7.10 Recolher um grupo

O layout já trata o grupo recolhido (o painel fica sem altura, e as linhas de baixo sobem), mas
nada na view diz como recolher:

- (a) uma seta antes do rótulo do grupo (▶ fechado, ▼ aberto), e o clique no rótulo ou na seta
  alterna o `Collapsed` do nó; o painel do grupo sem borda, só com o recuo
- (b) o mesmo, com uma borda em volta do painel

Sugestão: (a), que é como o original fazia (o `(?)` reaproveitado como seta, 1.1). A aparência você
confere no Windows no fim do corte.

### 7.11 A falha na linha

Uma conversão que falha deixa o texto como foi digitado (P2.12), e a falha fica no `Failure` do nó,
que a linha mostra (3.3). Como mostrar:

- (a) o fundo do editor fica vermelho claro, e a mensagem vai no tooltip dele
- (b) o `ErrorProvider` do WinForms, um ícone ao lado do editor com a mensagem

Sugestão: (a), porque fica dentro do retângulo do layout; o `ErrorProvider` desenha fora do
controle, e o editor ocupa a linha até a borda. A proteção das próprias views (um controle que falha
ao ser criado) é o corte 4, com a premissa de erros.

### 7.12 Avisos de outra thread

Um objeto pode disparar o `PropertyChanged` fora da thread da interface, e o WinForms lança quando
um controle é mexido de outra thread.

Sugestão: a view repassa o que recebe para a thread da interface (`BeginInvoke`), e o núcleo
continua sem saber de threads.

### 7.13 O inspector descartado com a view viva

O `Dispose` do inspector desliga os objetos e descarta a árvore, sem aviso. Uma view viva fica com
controles ligados a nós descartados:

- (a) um evento `Disposed` no inspector, e a view se esvazia ao recebê-lo
- (b) nada muda, e quem cria os dois descarta a view antes do inspector

Sugestão: (a), pela premissa (um descarte fora de ordem não pode derrubar a view) e pela P1.4
(eventos sem economia).
