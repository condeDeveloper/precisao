using System.Numerics;
using Conde.Precisao;
using Xunit;

namespace Conde.Precisao.Testes;

public class CancelamentoTestes
{
    /// <summary>
    /// A FORMULA DE BHASKARA do jeito do livro perde quase todos os digitos da
    /// raiz pequena, e a reescrita acerta dezesseis.
    ///
    /// O polinomio e x^2 - (10^k + 10^-k) x + 1, cujas raizes sao exatamente 10^k e
    /// 10^-k. Nada de patologico: um polinomio com coeficientes comuns.
    /// </summary>
    [Fact]
    public void ABhaskaraDoLivroPerdeAsRaizesPequenas()
    {
        foreach (var k in new[] { 5, 7, 8 })
        {
            var grande = Math.Pow(10, k);
            var b = -(grande + 1 / grande);
            var certo = Exato.De(1, (long)grande);

            var doLivro = Exato.DigitosCorretos(Cancelamento.BhaskaraDoLivro(1, b, 1).Menor, certo);
            var estavel = Exato.DigitosCorretos(Cancelamento.BhaskaraEstavel(1, b, 1).Menor, certo);

            Assert.True(doLivro < 10, $"k={k}: o livro acertou {doLivro:F1} digitos");
            Assert.True(estavel > 15, $"k={k}: a reescrita acertou so {estavel:F1} digitos");
        }
    }

    /// <summary>
    /// E as duas acertam a raiz GRANDE: o problema nao e a formula inteira, e uma
    /// das duas raizes.
    /// </summary>
    [Fact]
    public void AsDuasAcertamARaizGrande()
    {
        foreach (var k in new[] { 5, 7, 8 })
        {
            var grande = Math.Pow(10, k);
            var b = -(grande + 1 / grande);
            var certo = Exato.De((long)grande);

            Assert.True(Exato.DigitosCorretos(Cancelamento.BhaskaraDoLivro(1, b, 1).Maior, certo) > 14);
            Assert.True(Exato.DigitosCorretos(Cancelamento.BhaskaraEstavel(1, b, 1).Maior, certo) > 14);
        }
    }

    /// <summary>
    /// As duas formulas dao as MESMAS raizes em polinomios bem comportados: a
    /// reescrita nao e outro algoritmo, e a mesma formula.
    /// </summary>
    [Fact]
    public void AsDuasConcordamEmPolinomiosBemComportados()
    {
        var sorteio = new Sorteio(5);

        for (var caso = 0; caso < 2_000; caso++)
        {
            var r1 = sorteio.Entre(1, 100);
            var r2 = sorteio.Entre(1, 100);

            // (x - r1)(x - r2)
            double a = 1, b = -(r1 + r2), c = (double)r1 * r2;

            var doLivro = Cancelamento.BhaskaraDoLivro(a, b, c);
            var estavel = Cancelamento.BhaskaraEstavel(a, b, c);

            Assert.Equal(doLivro.Maior, estavel.Maior, 9);
            Assert.Equal(doLivro.Menor, estavel.Menor, 9);
        }
    }

    /// <summary>
    /// E as raizes EXATAS, quando racionais, conferem as duas: o juiz nao e uma das
    /// implementacoes.
    /// </summary>
    [Fact]
    public void AsRaizesExatasConferemAsDuas()
    {
        var sorteio = new Sorteio(7);

        for (var caso = 0; caso < 1_000; caso++)
        {
            long r1 = sorteio.Entre(1, 1000);
            long r2 = sorteio.Entre(1, 1000);

            var exatas = Cancelamento.RaizesExatas(1, -(r1 + r2), r1 * r2);

            Assert.NotNull(exatas);
            Assert.Equal(Exato.De(Math.Max(r1, r2)), exatas!.Value.Maior);
            Assert.Equal(Exato.De(Math.Min(r1, r2)), exatas.Value.Menor);
        }
    }

    [Fact]
    public void ARaizInteiraEhARaizInteira()
    {
        for (var n = 0; n < 2_000; n++)
        {
            var raiz = Cancelamento.RaizInteira(n);

            Assert.True(raiz * raiz <= n);
            Assert.True((raiz + 1) * (raiz + 1) > n);
        }

        var grande = BigInteger.Pow(10, 40);
        Assert.Equal(BigInteger.Pow(10, 20), Cancelamento.RaizInteira(grande));
    }

    /// <summary>
    /// A VARIANCIA pela formula de uma passada chega a dar NEGATIVA, que e
    /// matematicamente impossivel: variancia e media de quadrados.
    /// </summary>
    [Fact]
    public void AVarianciaDeUmaPassadaChegaADarNegativa()
    {
        var numeros = Cancelamento.GrandesComPoucaVariacao(10_000, 1e12, 1.0);

        Assert.True(Cancelamento.VarianciaDeUmaPassada(numeros) < 0);
        Assert.True(Cancelamento.VarianciaDeWelford(numeros) > 0);
        Assert.True(Cancelamento.VarianciaExata(numeros).Sinal > 0);
    }

    /// <summary>
    /// E WELFORD acerta onde ela erra, com o mesmo numero de passadas e as mesmas
    /// quatro operacoes.
    /// </summary>
    [Fact]
    public void WelfordAcertaOndeAFormulaDeUmaPassadaErra()
    {
        foreach (var centro in new[] { 1e6, 1e9, 1e12 })
        {
            var numeros = Cancelamento.GrandesComPoucaVariacao(10_000, centro, 1.0);
            var certo = Cancelamento.VarianciaExata(numeros);

            var umaPassada = Exato.DigitosCorretos(Cancelamento.VarianciaDeUmaPassada(numeros), certo);
            var welford = Exato.DigitosCorretos(Cancelamento.VarianciaDeWelford(numeros), certo);

            Assert.True(welford > umaPassada + 3, $"centro {centro}: uma passada {umaPassada:F1}, Welford {welford:F1}");
        }
    }

    /// <summary>
    /// Com numeros PEQUENOS as duas acertam, e e por isso que o problema passa
    /// despercebido em teste.
    /// </summary>
    [Fact]
    public void ComNumerosPequenosAsDuasAcertam()
    {
        var numeros = Cancelamento.GrandesComPoucaVariacao(10_000, 1.0, 1.0);
        var certo = Cancelamento.VarianciaExata(numeros);

        Assert.True(Exato.DigitosCorretos(Cancelamento.VarianciaDeUmaPassada(numeros), certo) > 12);
        Assert.True(Exato.DigitosCorretos(Cancelamento.VarianciaDeWelford(numeros), certo) > 12);
    }

    /// <summary>
    /// A DIFERENCA DE QUADRADOS: a - b antes de multiplicar nunca cancela, e a*a -
    /// b*b cancela.
    /// </summary>
    [Fact]
    public void ADiferencaDeQuadradosReescritaNaoCancela()
    {
        var sorteio = new Sorteio(11);
        var piorIngenua = 17.0;
        var piorEstavel = 17.0;

        for (var caso = 0; caso < 5_000; caso++)
        {
            var a = 1 + sorteio.Fracao() * 1e8;
            var b = a * (1 - sorteio.Fracao() * 1e-12);

            var certo = Cancelamento.DiferencaDeQuadradosExata(a, b);
            if (certo.EhZero) continue;

            piorIngenua = Math.Min(piorIngenua, Exato.DigitosCorretos(Cancelamento.DiferencaDeQuadradosIngenua(a, b), certo));
            piorEstavel = Math.Min(piorEstavel, Exato.DigitosCorretos(Cancelamento.DiferencaDeQuadradosEstavel(a, b), certo));
        }

        Assert.True(piorIngenua < 8, $"a ingenua nunca ficou abaixo de {piorIngenua:F1} digitos");
        Assert.True(piorEstavel > 14, $"a estavel chegou a {piorEstavel:F1} digitos");
    }

    /// <summary>
    /// E as duas sao a MESMA conta: com numeros bem separados elas dao o mesmo
    /// numero.
    /// </summary>
    [Fact]
    public void AsDuasFormasDaDiferencaDeQuadradosConcordamQuandoNaoHaCancelamento()
    {
        var sorteio = new Sorteio(13);

        for (var caso = 0; caso < 2_000; caso++)
        {
            var a = sorteio.Entre(1, 1000);
            var b = sorteio.Entre(1, 1000);

            Assert.Equal(Cancelamento.DiferencaDeQuadradosIngenua(a, b), Cancelamento.DiferencaDeQuadradosEstavel(a, b));
        }
    }
}
