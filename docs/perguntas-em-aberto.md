# Perguntas em aberto

Estado de 9 de outubro de 2026. As respondidas até 02/10 saíram daqui e estão na seção 0 de
`notas-modernizacao.md`, citadas com um `P` na frente do número (`P2.2`). A explicação da 6.2 e da
6.3, que você pediu, ficou na seção 3.12 das notas, e as escolhas da view WinForms (7.7 a 7.13),
respondidas em 02/10 com as sugestões, estão na seção 0 (Views) e na 3.5.

Em aberto, as quatro de baixo: as escolhas que eu fiz em 09/10 ao juntar a branch das views novas
(`claude/vibrant-fermi-smwjw5`) com esta, quando você pediu ("pode fazer isso?"). O código já segue
a sugestão de cada uma; se você escolher outra coisa, eu mudo.

Os números antigos continuam valendo, e os novos seguem a numeração de cada seção, sem reaproveitar
número. Os nomes que ainda não existem no código são só ilustração, e a sugestão, quando há, vem no
fim de cada pergunta.

Dá para responder pelo número, como antes: "5.9: sim" ou "6.7: b".

## 7. Views

**7.20 Um projeto por framework.** O núcleo voltou a ter só `net10.0`, e cada framework tem o seu
projeto: `InteractiveEditor.WinForms`, `.Wpf`, `.Avalonia` e `.ImGui`. É o desenho da branch nova,
e substitui os dois alvos da P7.1. Quem usa uma view recebe três DLLs: o núcleo, a do framework (a
view) e a `PixieLib` (os primitivos). A outra saída seria voltar aos dois alvos, com o WinForms e o
WPF na DLL do núcleo, e o Avalonia e o ImGui como projetos à parte. Sugestão: ficar com um projeto
por framework. O núcleo roda em qualquer sistema, ninguém carrega um framework que não usa (some o
aviso MSB3277 de quem usa só o WinForms, 3.7), e as quatro views ficam do mesmo jeito.

**7.21 O que as views usam de interno no núcleo.** As `ViewRules` (o editor de cada linha, quem faz
scrubbing, quem mostra mistos) e a `CultureInUse` das opções são `internal`, e os quatro projetos
as veem pelo `InternalsVisibleTo`, que só vale para esses nomes: uma view de outra pessoa não as vê,
e o `TerminalHost`, que só usa a API pública, refaz a cultura por conta própria. A outra saída é
deixar as duas públicas. Sugestão: tornar pública a `CultureInUse`, que qualquer um que mostre
valores precisa, e deixar as `ViewRules` internas até aparecer uma view de fora, para não fixar uma
API que ainda pode mudar.

**7.22 O que as views novas fazem diferente da WinForms.** (a) Nenhuma usa o passo de layout: o
Avalonia empilha as linhas nos painéis dele, e o ImGui desenha uma tabela a cada quadro. (b)
Nenhuma tem as válvulas (P7.4). (c) O Avalonia não faz scrubbing. (d) O `(?)` não abre a janela:
no Avalonia, o `Help` vai no tooltip do rótulo, sem o `(?)`; no ImGui, o `(?)` mostra a ajuda num
tooltip. Sugestão: deixar (a) como está, porque os dois frameworks fazem o próprio layout; pôr no
Avalonia as válvulas, o scrubbing e o `(?)` com a janela, um commit para cada; e deixar o ImGui sem
válvulas, porque ele não tem controles para entregar, e com o `(?)` no tooltip, que é o costume
dele.

**7.23 Os filhos de um nó na API pública.** A API pública não dá os filhos que uma view mostra
embaixo de um nó: a enumeração entrega a árvore inteira, e o `Rows`, todas as linhas abaixo. O
`ShownChildren`, que dá exatamente isso (sem os ignorados e os escondidos, pelo `Order`), é interno,
e o `TerminalHost` filtra o `Rows` pelo `Parent` para chegar no mesmo. Sugestão: tornar público o
`ShownChildren`, para nenhum host ter de refazer essa regra.
