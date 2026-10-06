using System.Numerics;

namespace Conde.Precisao;

/// <summary>
/// O FORMATO em si: o que cabe, o que nao cabe, e o que acontece entre dois
/// numeros vizinhos.
///
/// Quase toda surpresa com numero de precisao dupla vem de uma frase so: eles sao
/// um conjunto FINITO de racionais, espalhados de um jeito desigual. Entre 1 e 2
/// ha dois elevado a cinquenta e dois deles; entre 2^52 e 2^53 ha um por inteiro;
/// acima disso, faltam inteiros.
/// </summary>
public static class Formato
{
    /// <summary>
    /// O ESPACAMENTO: a distancia ate o proximo numero representavel.
    ///
    /// Ele dobra a cada potencia de dois. Perto de 1 ele e 2,2e-16; perto de um
    /// bilhao ele e 1,2e-7; perto de 2^53 ele e 1, e e por isso que somar 1 a 2^53
    /// nao muda nada.
    /// </summary>
    public static double Espacamento(double valor)
    {
        if (valor == 0) return double.Epsilon;

        var bits = BitConverter.DoubleToInt64Bits(Math.Abs(valor));

        return BitConverter.Int64BitsToDouble(bits + 1) - Math.Abs(valor);
    }

    /// <summary>O proximo numero representavel depois deste.</summary>
    public static double Proximo(double valor)
    {
        if (double.IsNaN(valor) || double.IsPositiveInfinity(valor)) return valor;
        if (valor == 0) return double.Epsilon;

        var bits = BitConverter.DoubleToInt64Bits(valor);

        return BitConverter.Int64BitsToDouble(valor > 0 ? bits + 1 : bits - 1);
    }

    /// <summary>
    /// Quantos numeros representaveis existem ENTRE dois valores.
    ///
    /// Esta e a medida honesta de "quao perto" dois numeros de ponto flutuante
    /// estao: a diferenca absoluta nao diz nada sem a escala, e a contagem de
    /// passos diz tudo.
    /// </summary>
    public static long PassosEntre(double menor, double maior)
    {
        var a = BitConverter.DoubleToInt64Bits(menor);
        var b = BitConverter.DoubleToInt64Bits(maior);

        if (a < 0) a = long.MinValue - a;
        if (b < 0) b = long.MinValue - b;

        return Math.Abs(b - a);
    }

    /// <summary>
    /// O maior inteiro a partir do qual FALTAM inteiros: dois elevado a cinquenta e
    /// tres.
    ///
    /// Acima dele, 2^53 + 1 nao existe como numero de precisao dupla, e tentar
    /// escrever ele devolve 2^53. E e por isso que identificador grande nunca deve
    /// ir para um numero com virgula.
    /// </summary>
    public static readonly double LimiteDosInteiros = Math.Pow(2, 53);

    /// <summary>
    /// O numero EXATO que o computador guarda quando se escreve um decimal.
    ///
    /// E este o metodo que encerra toda discussao sobre "0,1 mais 0,2 nao da 0,3":
    /// ele imprime o valor exato, com todas as casas, e da para ver de onde a
    /// diferenca saiu.
    /// </summary>
    public static string ValorExato(double valor, int casas = 60) => Exato.De(valor).Decimal(casas);

    /// <summary>
    /// A fracao decimal que NAO tem representacao exata em binario.
    ///
    /// Um decimal so cabe exatamente em binario se o denominador dele, depois de
    /// simplificado, for uma potencia de dois. Um decimo e um sobre dez, e dez tem
    /// um fator cinco, entao nao cabe: a expansao binaria dele e infinita e
    /// periodica, do mesmo jeito que um terco em decimal.
    /// </summary>
    public static bool CabeExatamente(long numerador, long denominador)
    {
        if (denominador == 0) throw new DivideByZeroException();

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerador), BigInteger.Abs(denominador));
        var reduzido = BigInteger.Abs(denominador) / (divisor.IsZero ? BigInteger.One : divisor);

        while (reduzido.IsEven) reduzido >>= 1;

        return reduzido.IsOne;
    }

    /// <summary>
    /// A SOMA de dois numeros de precisao dupla e EXATA ou arredondada, e da para
    /// saber qual.
    ///
    /// Esta funcao devolve a parte que o arredondamento jogou fora, exatamente. E o
    /// mesmo valor que a compensacao de Kahan guarda, e aqui ele e calculado com a
    /// fracao exata em vez de com o truque algebrico.
    /// </summary>
    public static Exato PerdidoNaSoma(double a, double b) =>
        Exato.De(a) + Exato.De(b) - Exato.De(a + b);

    /// <summary>
    /// A SUBTRACAO de dois numeros PROXIMOS e exata: nada se perde.
    ///
    /// Este e o resultado que mais contraria a intuicao de quem ouviu falar em
    /// cancelamento catastrofico. O teorema de Sterbenz diz que, se um numero esta
    /// entre a metade e o dobro do outro, a diferenca cabe no formato sem
    /// arredondar NADA.
    ///
    /// O cancelamento nao perde digitos: ele REVELA os digitos que ja estavam
    /// errados.
    /// </summary>
    public static bool SubtracaoEhExata(double a, double b) => PerdidoNaSoma(a, -b).EhZero;

    /// <summary>
    /// A condicao de Sterbenz: um esta entre a metade e o dobro do outro.
    /// </summary>
    public static bool ValeSterbenz(double a, double b) =>
        a != 0 && b != 0 && Math.Sign(a) == Math.Sign(b) && b / 2 <= a && a <= 2 * b;

    /// <summary>
    /// A ASSOCIATIVIDADE da soma, que o ponto flutuante NAO tem.
    ///
    /// (a + b) + c e a + (b + c) sao a mesma coisa na matematica e numeros
    /// diferentes aqui. E por isso que um compilador nao pode reordenar somas de
    /// ponto flutuante sem permissao, e por isso que somar um vetor em paralelo
    /// muda a resposta.
    /// </summary>
    public static bool SomaEhAssociativa(double a, double b, double c) => (a + b) + c == a + (b + c);

    /// <summary>
    /// Procura um trio que quebra a associatividade, para o teste nao ficar
    /// torcendo.
    /// </summary>
    public static (double A, double B, double C)? TrioQueQuebraAAssociatividade(Sorteio sorteio, int tentativas = 10_000)
    {
        ArgumentNullException.ThrowIfNull(sorteio);

        for (var i = 0; i < tentativas; i++)
        {
            var a = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-10, 10));
            var b = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-10, 10));
            var c = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-10, 10));

            if (!SomaEhAssociativa(a, b, c)) return (a, b, c);
        }

        return null;
    }
}
