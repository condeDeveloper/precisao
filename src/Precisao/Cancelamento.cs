using System.Numerics;

namespace Conde.Precisao;

/// <summary>
/// O CANCELAMENTO CATASTROFICO: subtrair dois numeros parecidos.
///
/// Este e o mecanismo que mais gente conhece pelo nome e menos gente reconhece no
/// codigo. A subtracao em si e EXATA: quando dois numeros de precisao dupla sao
/// proximos, a diferenca deles cabe no formato sem arredondar nada. O problema
/// nao e a subtracao, e o que ela revela.
///
/// Cada um dos dois numeros ja tinha um erro, pequeno em relacao a ELE. Depois da
/// subtracao, o resultado e pequeno e os erros ficaram do mesmo tamanho. Nenhum
/// digito foi perdido na subtracao; os digitos significativos foram embora porque
/// a resposta e minuscula e o erro, nao.
///
/// E por isso que a saida nao e "usar mais precisao": e REESCREVER a formula para
/// a subtracao nao acontecer.
/// </summary>
public static class Cancelamento
{
    /// <summary>
    /// A FORMULA DE BHASKARA escrita do jeito do livro.
    ///
    /// Quando b ao quadrado e muito maior que 4ac, a raiz quadrada fica quase
    /// igual a |b|, e uma das duas raizes e calculada como a diferenca de dois
    /// numeros quase iguais. Os digitos vao embora.
    /// </summary>
    public static (double Maior, double Menor) BhaskaraDoLivro(double a, double b, double c)
    {
        var raiz = Math.Sqrt(b * b - 4 * a * c);

        return ((-b + raiz) / (2 * a), (-b - raiz) / (2 * a));
    }

    /// <summary>
    /// A MESMA formula, reescrita para a subtracao nao acontecer.
    ///
    /// O truque e de uma linha: calcular a raiz que NAO cancela pela formula
    /// normal, e tirar a outra do produto das raizes, que vale c/a. A soma
    /// perigosa vira uma divisao, e a divisao nao cancela nada.
    ///
    /// Sao as mesmas raizes, do mesmo polinomio, com a mesma precisao dupla.
    /// </summary>
    public static (double Maior, double Menor) BhaskaraEstavel(double a, double b, double c)
    {
        var raiz = Math.Sqrt(b * b - 4 * a * c);

        // escolhe o sinal que SOMA em vez de subtrair
        var q = -(b + Math.Sign(b) * raiz) / 2;

        var uma = q / a;
        var outra = q == 0 ? 0 : c / q;

        return uma >= outra ? (uma, outra) : (outra, uma);
    }

    /// <summary>As raizes EXATAS do polinomio, quando elas sao racionais.</summary>
    public static (Exato Maior, Exato Menor)? RaizesExatas(long a, long b, long c)
    {
        var delta = (BigInteger)b * b - 4 * (BigInteger)a * c;
        if (delta.Sign < 0) return null;

        var raiz = RaizInteira(delta);
        if (raiz * raiz != delta) return null;

        var uma = Exato.De(-b + raiz, 2 * (BigInteger)a);
        var outra = Exato.De(-b - raiz, 2 * (BigInteger)a);

        return uma >= outra ? (uma, outra) : (outra, uma);
    }

    /// <summary>A raiz quadrada inteira, por busca binaria com inteiros grandes.</summary>
    public static BigInteger RaizInteira(BigInteger valor)
    {
        if (valor.Sign < 0) throw new ArgumentOutOfRangeException(nameof(valor));
        if (valor.IsZero) return BigInteger.Zero;

        var baixo = BigInteger.One;
        var alto = valor;

        while (baixo < alto)
        {
            var meio = (baixo + alto + 1) / 2;
            if (meio * meio <= valor) baixo = meio; else alto = meio - 1;
        }

        return baixo;
    }

    /// <summary>
    /// A VARIANCIA pela formula "media dos quadrados menos quadrado da media".
    ///
    /// E a formula que cabe numa passada so e que esta em metade dos livros de
    /// estatistica e em quase toda planilha. Quando os numeros sao grandes e a
    /// variancia e pequena, os dois termos ficam quase iguais, e a subtracao nao
    /// deixa nada.
    ///
    /// Ela chega a devolver variancia NEGATIVA, que e matematicamente impossivel:
    /// variancia e media de quadrados.
    /// </summary>
    public static double VarianciaDeUmaPassada(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);
        if (numeros.Count == 0) return 0;

        var soma = 0.0;
        var somaDosQuadrados = 0.0;

        foreach (var x in numeros)
        {
            soma += x;
            somaDosQuadrados += x * x;
        }

        var media = soma / numeros.Count;

        return somaDosQuadrados / numeros.Count - media * media;
    }

    /// <summary>
    /// A VARIANCIA de Welford, de 1962: uma passada so, sem a subtracao.
    ///
    /// Ela atualiza a media e a soma dos desvios ao quadrado a cada numero novo. E
    /// uma passada igual a outra, com as mesmas quatro operacoes, e sem o termo
    /// que cancela.
    /// </summary>
    public static double VarianciaDeWelford(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);
        if (numeros.Count == 0) return 0;

        var media = 0.0;
        var acumulado = 0.0;
        var quantos = 0;

        foreach (var x in numeros)
        {
            quantos++;

            var desvio = x - media;
            media += desvio / quantos;
            acumulado += desvio * (x - media);
        }

        return acumulado / quantos;
    }

    /// <summary>A variancia EXATA, que e o juiz.</summary>
    public static Exato VarianciaExata(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);
        if (numeros.Count == 0) return Exato.Zero;

        var soma = Exato.Zero;
        foreach (var x in numeros) soma += Exato.De(x);

        var media = soma / Exato.De(numeros.Count);
        var acumulado = Exato.Zero;

        foreach (var x in numeros)
        {
            var desvio = Exato.De(x) - media;
            acumulado += desvio * desvio;
        }

        return acumulado / Exato.De(numeros.Count);
    }

    /// <summary>
    /// Numeros grandes com variancia pequena: o caso em que a formula de uma
    /// passada desmonta.
    ///
    /// Sao medidas em torno de um valor grande, com ruido pequeno, que e a forma de
    /// quase toda serie de telemetria que existe.
    /// </summary>
    public static double[] GrandesComPoucaVariacao(int quantos, double centro, double ruido, ulong semente = 5)
    {
        var sorteio = new Sorteio(semente);
        var numeros = new double[quantos];

        for (var i = 0; i < quantos; i++) numeros[i] = centro + (sorteio.Fracao() - 0.5) * ruido;

        return numeros;
    }

    /// <summary>
    /// A DIFERENCA DE QUADRADOS escrita de dois jeitos.
    ///
    /// a*a - b*b cancela quando a e b sao proximos; (a+b)*(a-b) nao cancela nunca,
    /// porque a subtracao acontece ANTES de a escala crescer.
    /// </summary>
    public static double DiferencaDeQuadradosIngenua(double a, double b) => a * a - b * b;

    public static double DiferencaDeQuadradosEstavel(double a, double b) => (a + b) * (a - b);

    public static Exato DiferencaDeQuadradosExata(double a, double b) =>
        Exato.De(a) * Exato.De(a) - Exato.De(b) * Exato.De(b);
}
