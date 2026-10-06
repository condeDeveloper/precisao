# precisao

Ponto flutuante do zero em C# e .NET 8: quatro somas, cancelamento catastrófico,
variância, dinheiro e o formato por dentro. O juiz é a **fração exata** que todo
número de precisão dupla realmente é.

```
$ dotnet medidor.dll juiz

    0.1          0.1000000000000000055511151231257827021181583404541015625
    0.2          0.200000000000000011102230246251565404236316680908203125
    0.1 + 0.2    0.3000000000000000444089209850062616169452667236328125
    0.3          0.299999999999999988897769753748434595763683319091796875
    0.5          0.5

    0.1 + 0.2 == 0.3 ?  nao
```

Todo número de precisão dupla é exatamente um inteiro de 53 bits vezes dois
elevado a alguma coisa. Isso não é aproximação e não é metáfora: é o formato.
Converter cada número para a fração que ele é, contar com inteiros grandes e
comparar não dá "a resposta de referência": dá **a resposta**, e a distância até
ela é o erro, medido e não estimado.

As linhas acima não são uma aproximação do que o computador guarda. São o valor
exato dele, com todas as casas.

## As quatro somas somam os mesmos números

```
$ dotnet medidor.dll soma

  lista                          jeito         digitos corretos

  mesma escala                   ingenua                   17.0
  mesma escala                   ordenada                  17.0
  mesma escala                   em arvore                 17.0
  mesma escala                   Kahan                     17.0

  um grande e muitos pequenos    ingenua                   11.0
  um grande e muitos pequenos    ordenada                  17.0
  um grande e muitos pequenos    em arvore                 17.0
  um grande e muitos pequenos    Kahan                     17.0

  escalas variadas               ingenua                   13.3
  escalas variadas               ordenada                  15.5
  escalas variadas               em arvore                 16.8
  escalas variadas               Kahan                     16.8

  com cancelamento               ingenua                    6.9
  com cancelamento               ordenada                   8.4
  com cancelamento               em arvore                 17.0
  com cancelamento               Kahan                      7.5
```

O primeiro bloco é por que o problema passa despercebido: com números na mesma
escala, as quatro acertam tudo. **Medir com números sorteados entre zero e um não
descobre nada.**

E o erro da soma ingênua cresce com o número de parcelas, enquanto o dos outros
dois não:

```
  parcelas      ingenua   em arvore   Kahan
      1,000        15.4        16.1    16.1
  1,000,000        12.2        16.4    16.4
```

## O cancelamento não perde dígitos: ele revela os que já estavam errados

```
$ dotnet medidor.dll cancelamento

  raiz grande      do livro   reescrita   a raiz pequena, exata
  1.0E+003             10.6        16.7   0.001
  1.0E+005              6.5        16.1   0.00001
  1.0E+007              2.5        16.3   0.0000001
  1.0E+008              0.3        16.7   0.00000001
```

O polinômio é x² - (10^k + 10^-k)x + 1, cujas raízes são exatamente 10^k e 10^-k.
Nada de patológico: coeficientes comuns, e a fórmula de Bhaskara como está no
livro devolve **zero dígitos corretos** para uma das raízes.

A reescrita é de uma linha: calcular a raiz que não cancela pela fórmula normal e
tirar a outra do produto, que vale c/a. A subtração perigosa vira uma divisão.
Mesma fórmula, mesma precisão dupla, dezesseis dígitos.

E o teorema de Sterbenz diz o que quase ninguém espera: subtrair dois números
**próximos** é exato, sem arredondar nada. O cancelamento não perde dígitos. Ele
revela que os dois números já tinham erro, pequeno em relação a cada um deles e
enorme em relação à diferença.

## A variância negativa

```
  centro         uma passada   Welford   deu negativa
  1.0E+000              13.2      14.3            nao
  1.0E+006               1.8       9.6            nao
  1.0E+009               0.0       7.8            nao
  1.0E+012               0.0       5.6            SIM
```

"Média dos quadrados menos quadrado da média" está em metade dos livros de
estatística e em quase toda planilha. Com números grandes e variação pequena, que
é a forma de toda série de telemetria que existe, ela devolve variância
**negativa**, que é matematicamente impossível: variância é média de quadrados.

Welford, de 1962, é a mesma passada única com as mesmas quatro operações, sem o
termo que cancela.

## O que as medidas me corrigiram

**Kahan não é sempre a melhor, e eu ia escrever que era.** Numa lista com
cancelamento, a soma em árvore acerta dezessete dígitos e a de Kahan acerta sete e
meio. A árvore soma os **vizinhos** primeiro, os pares que se cancelam estão lado
a lado, a cancelação acontece exatamente (Sterbenz de novo) e nada se perde.
Kahan percorre a lista em ordem e passa por somas parciais enormes, compensando um
arredondamento que a árvore nem produz.

A compensação conserta o **acúmulo** do erro. Ela não conserta a **ordem** errada.

**Somar dinheiro com ponto flutuante quase não erra, e o conselho de nunca usar
ponto flutuante para dinheiro continua certo pelo outro motivo.** Com um milhão de
lançamentos de até dez mil reais, a soma com ponto flutuante erra três centésimos
de milésimo de centavo. O problema do dinheiro não está na soma:

```
  total (centavos)   pessoas   sobra com double   sobra com centavos
            10,000         3                 -1                    0
            10,000         7                  3                    0
            99,999        11                  2                    0
```

Está no **arredondamento da divisão**. Dividir cem reais por três dá 33,33 para
cada um, e três vezes 33,33 é 99,99: falta um centavo, e ele tem que ir para
alguém. Em mais da metade dos rateios de valores pequenos, a conta com ponto
flutuante cria ou perde centavo; em centavos inteiros, com a sobra distribuída de
um em um, ela fecha sempre.

**A minha conversão de fração exata para número com vírgula virava NaN em
silêncio.** O numerador e o denominador de uma fração exata passam fácil de 2^1024
(juros compostos de 360 meses dão uma fração com setecentos dígitos de cada lado),
e a conversão de cada um deles vira infinito. Infinito dividido por infinito é
NaN, e a medida de erro inteira virava NaN sem aviso nenhum. O conserto é dividir
já na escala, com inteiros grandes, e devolver o expoente por fora.

**O truque algébrico de Kahan só é exato quando a maior parcela vem primeiro.** Eu
tinha escrito o teste comparando `total - a - b` com a perda exata e ele falhava.
A condição se chama Fast2Sum, e ela é mais uma vez sobre **ordem**: até o conserto
do erro de arredondamento depende da ordem das parcelas.

**Eu ia escrever o contraexemplo da associatividade errado.** Com a = 10^16,
b = 1, c = -10^16, as duas ordens dão zero: `1 - 10^16` já arredonda para
`-10^16`. O contraexemplo que funciona é a = 1, b = 10^16, c = -10^16, e aí
`(1 + 10^16) - 10^16` dá zero e `1 + (10^16 - 10^16)` dá um.

## Por que o juiz é a fração exata

Comparar duas implementações de ponto flutuante diz qual das duas está diferente.
A fração exata diz qual está **certa**, e quanto a outra errou, em dígitos
decimais.

E ela é barata de confiar: a conversão de um número de precisão dupla para fração
é a leitura dos 64 bits, e ela é testada nos dois sentidos, em cinco mil números
sorteados ao longo de quatrocentas ordens de grandeza, incluindo os subnormais.

Nenhuma medida aqui usa relógio. Todas são dígitos corretos, centavos de diferença
ou contagem de casos. A integração contínua roda em três sistemas, e isso importa
mais aqui do que em qualquer outro lugar: a aritmética de ponto flutuante é
exatamente o que está sendo medido, e se ela diferisse entre sistemas, a tabela
seria três tabelas. O padrão promete que não, e esta é a conferência.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Exato.cs` | a fração exata de um número de precisão dupla: o juiz |
| `Soma.cs` | as quatro somas e as quatro listas que as separam |
| `Cancelamento.cs` | Bhaskara, variância e diferença de quadrados, nas duas formas |
| `Formato.cs` | espaçamento, Sterbenz, associatividade e o que cabe em binário |
| `Dinheiro.cs` | extrato, rateio e juros, em três representações |
| `Medidas.cs` | as tabelas que o medidor imprime e que os testes conferem |

## Como rodar

```
dotnet test testes/Precisao.Testes/Precisao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `juiz`, `soma`, `cancelamento`, `dinheiro` e `tudo`.

## Licença

MIT.
