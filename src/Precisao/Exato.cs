using System.Numerics;

namespace Conde.Precisao;

/// <summary>
/// O SORTEIO, escrito aqui em vez de vir da biblioteca.
///
/// A documentacao do sorteio de biblioteca nao promete a mesma sequencia entre
/// versoes nem entre sistemas, e a integracao continua roda em tres. Uma medida
/// de "quantos digitos a soma ingenua perdeu" com numeros diferentes em cada
/// sistema nao e uma medida: sao tres medidas que ninguem pode comparar.
/// </summary>
public sealed class Sorteio
{
    private ulong estado;

    public Sorteio(ulong semente) => estado = semente == 0 ? 1 : semente;

    public uint Proximo()
    {
        estado = estado * 6364136223846793005UL + 1442695040888963407UL;
        return (uint)(estado >> 32);
    }

    public int Ate(int limite) => (int)(Proximo() % (uint)limite);

    public int Entre(int menor, int maior) => menor + Ate(maior - menor + 1);

    public double Fracao() => Proximo() / 4294967296.0;
}

/// <summary>
/// O JUIZ deste repositorio: a FRACAO EXATA.
///
/// Todo numero de precisao dupla e exatamente uma fracao de denominador potencia
/// de dois: ele e um inteiro de 53 bits vezes dois elevado a alguma coisa. Isso
/// nao e aproximacao e nao e metafora, e o formato.
///
/// Dai sai o juiz: converter cada numero para a fracao exata que ele e, fazer as
/// contas com inteiros grandes, e comparar. O resultado nao e "a resposta de
/// referencia": e a resposta, e a distancia ate ela e o erro, medido e nao
/// estimado.
///
/// E ele mostra uma coisa que surpreende quase todo mundo: 0,1 nao e 0,1. O
/// numero que o computador guarda quando se escreve 0.1 e
/// 0,1000000000000000055511151231257827021181583404541015625, e esse e o valor
/// EXATO dele, nao uma aproximacao do que ele guarda.
/// </summary>
public readonly struct Exato : IEquatable<Exato>
{
    public BigInteger Numerador { get; }

    public BigInteger Denominador { get; }

    private Exato(BigInteger numerador, BigInteger denominador)
    {
        if (denominador.IsZero) throw new DivideByZeroException("fracao com denominador zero");

        if (denominador.Sign < 0)
        {
            numerador = -numerador;
            denominador = -denominador;
        }

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerador), denominador);
        if (!divisor.IsOne && !divisor.IsZero)
        {
            numerador /= divisor;
            denominador /= divisor;
        }

        Numerador = numerador;
        Denominador = denominador.IsZero ? BigInteger.One : denominador;
    }

    public static readonly Exato Zero = new(BigInteger.Zero, BigInteger.One);

    public static readonly Exato Um = new(BigInteger.One, BigInteger.One);

    public static Exato De(BigInteger numerador, BigInteger denominador) => new(numerador, denominador);

    public static Exato De(long inteiro) => new(inteiro, BigInteger.One);

    /// <summary>
    /// A FRACAO EXATA que um numero de precisao dupla realmente e.
    ///
    /// Os 64 bits sao um sinal, 11 de expoente e 52 de mantissa. O numero normal e
    /// (1 + mantissa/2^52) vezes 2 elevado a (expoente menos 1023); o subnormal,
    /// que acontece quando o expoente e zero, nao tem o 1 da frente.
    ///
    /// Esta conversao e o coracao do repositorio: ela e exata, ela e curta, e ela
    /// transforma toda discussao sobre erro de arredondamento numa conta com
    /// inteiros.
    /// </summary>
    public static Exato De(double valor)
    {
        if (double.IsNaN(valor) || double.IsInfinity(valor))
            throw new ArgumentException("nao e um numero finito", nameof(valor));

        var bits = BitConverter.DoubleToInt64Bits(valor);
        var negativo = bits < 0;
        var expoente = (int)((bits >> 52) & 0x7FF);
        var mantissa = bits & 0xFFFFFFFFFFFFFL;

        if (expoente == 0) expoente++;
        else mantissa |= 1L << 52;

        // o valor e mantissa vezes 2 elevado a (expoente - 1075)
        expoente -= 1075;

        var numerador = (BigInteger)mantissa;
        var denominador = BigInteger.One;

        if (expoente > 0) numerador <<= expoente;
        else denominador <<= -expoente;

        return new Exato(negativo ? -numerador : numerador, denominador);
    }

    public static Exato operator +(Exato a, Exato b) =>
        new(a.Numerador * b.Denominador + b.Numerador * a.Denominador, a.Denominador * b.Denominador);

    public static Exato operator -(Exato a, Exato b) =>
        new(a.Numerador * b.Denominador - b.Numerador * a.Denominador, a.Denominador * b.Denominador);

    public static Exato operator *(Exato a, Exato b) =>
        new(a.Numerador * b.Numerador, a.Denominador * b.Denominador);

    public static Exato operator /(Exato a, Exato b) =>
        new(a.Numerador * b.Denominador, a.Denominador * b.Numerador);

    public static Exato operator -(Exato a) => new(-a.Numerador, a.Denominador);

    public Exato Abs() => new(BigInteger.Abs(Numerador), Denominador);

    public int Sinal => Numerador.Sign;

    public bool EhZero => Numerador.IsZero;

    public static bool operator <(Exato a, Exato b) => (a - b).Sinal < 0;

    public static bool operator >(Exato a, Exato b) => (a - b).Sinal > 0;

    public static bool operator <=(Exato a, Exato b) => (a - b).Sinal <= 0;

    public static bool operator >=(Exato a, Exato b) => (a - b).Sinal >= 0;

    public bool Equals(Exato outro) => Numerador == outro.Numerador && Denominador == outro.Denominador;

    public override bool Equals(object? obj) => obj is Exato outro && Equals(outro);

    public override int GetHashCode() => HashCode.Combine(Numerador, Denominador);

    public static bool operator ==(Exato a, Exato b) => a.Equals(b);

    public static bool operator !=(Exato a, Exato b) => !a.Equals(b);

    /// <summary>
    /// O valor como numero de precisao dupla, para a conta dar a volta.
    ///
    /// Ele perde precisao, e e esse o ponto: medir quanto ele perde e o
    /// repositorio inteiro.
    /// </summary>
    public double ParaDouble()
    {
        if (Numerador.IsZero) return 0;

        // A divisao direta nao serve: o numerador e o denominador de uma fracao
        // exata passam facilmente de 2^1024, e a conversao de cada um deles para
        // numero com virgula vira infinito. Infinito dividido por infinito e NaN,
        // e a medida de erro inteira vira NaN sem nenhum aviso.
        //
        // O jeito certo e dividir os dois ja na escala, guardando o expoente de
        // fora: desloca o numerador ate o quociente ter uns 64 bits, divide com
        // inteiros grandes, e devolve a escala com ScaleB, que sabe produzir
        // subnormal em vez de zero.
        var sinal = Numerador.Sign;
        var numerador = BigInteger.Abs(Numerador);

        var expoente = (int)(numerador.GetBitLength() - Denominador.GetBitLength());
        var desloca = 64 - expoente;

        var escalado = desloca >= 0
            ? (numerador << desloca) / Denominador
            : numerador / (Denominador << -desloca);

        return sinal * Math.ScaleB((double)escalado, expoente - 64);
    }

    /// <summary>
    /// O ERRO RELATIVO de uma aproximacao, exato: a diferenca dividida pelo valor
    /// certo.
    ///
    /// Erro relativo e a medida certa quase sempre, porque errar um centesimo num
    /// numero perto de um e grave e errar um centesimo num numero perto de um
    /// bilhao nao e nada.
    /// </summary>
    public static Exato ErroRelativo(double aproximado, Exato certo)
    {
        if (certo.EhZero) return De(aproximado).Abs();

        return ((De(aproximado) - certo) / certo).Abs();
    }

    /// <summary>
    /// Quantos DIGITOS DECIMAIS CORRETOS a aproximacao tem.
    ///
    /// E a leitura humana do erro relativo: um erro de 1e-15 sao quinze digitos
    /// certos, e a precisao dupla tem uns dezesseis para dar. Zero digitos certos
    /// quer dizer que nem a primeira casa bate.
    /// </summary>
    public static double DigitosCorretos(double aproximado, Exato certo)
    {
        var erro = ErroRelativo(aproximado, certo);
        if (erro.EhZero) return 17;

        var valor = erro.ParaDouble();

        return valor <= 0 ? 17 : Math.Max(0, Math.Min(17, -Math.Log10(valor)));
    }

    public override string ToString() => Denominador.IsOne ? Numerador.ToString() : $"{Numerador}/{Denominador}";

    /// <summary>
    /// A expansao decimal EXATA, ate o numero de casas pedido.
    ///
    /// E ela que mostra que 0,1 nao e 0,1: a expansao do numero que o computador
    /// guarda tem cinquenta e cinco casas e nao termina em zeros.
    /// </summary>
    public string Decimal(int casas)
    {
        var sinal = Sinal < 0 ? "-" : "";
        var numerador = BigInteger.Abs(Numerador);

        var inteiro = numerador / Denominador;
        var resto = numerador % Denominador;

        if (casas <= 0 || resto.IsZero) return sinal + inteiro;

        var fracao = new System.Text.StringBuilder();

        for (var i = 0; i < casas && !resto.IsZero; i++)
        {
            resto *= 10;
            fracao.Append((char)('0' + (int)(resto / Denominador)));
            resto %= Denominador;
        }

        return $"{sinal}{inteiro}.{fracao}";
    }
}
