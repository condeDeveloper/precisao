using System.Numerics;
using Conde.Precisao;
using Xunit;

namespace Conde.Precisao.Testes;

/// <summary>
/// O juiz tambem precisa de juiz.
///
/// Todo numero deste repositorio e "a distancia ate a fracao exata". Se a
/// conversao de um numero de precisao dupla para fracao tivesse um erro, todas as
/// medidas estariam contra uma referencia errada e os numeros pareceriam
/// plausiveis. Por isso ela e conferida contra valores de resposta conhecida,
/// contra a volta e contra as propriedades da aritmetica.
/// </summary>
public class ExatoTestes
{
    [Fact]
    public void OSorteioEhDeterminista()
    {
        var a = new Sorteio(7);
        var b = new Sorteio(7);

        for (var i = 0; i < 1000; i++) Assert.Equal(a.Proximo(), b.Proximo());
    }

    /// <summary>
    /// A fracao precisa cobrir os DOIS LADOS do meio: um deslocamento de 33 em vez
    /// de 32 deixaria o gerador com 31 bits e metade das escolhas sorteadas
    /// deixaria de acontecer, com os numeros continuando plausiveis.
    /// </summary>
    [Fact]
    public void AFracaoDoSorteioCobreOsDoisLadosDoMeio()
    {
        var sorteio = new Sorteio(7);
        var acima = 0;
        var maior = 0.0;

        for (var i = 0; i < 10_000; i++)
        {
            var f = sorteio.Fracao();

            Assert.InRange(f, 0.0, 1.0);
            if (f > 0.5) acima++;
            if (f > maior) maior = f;
        }

        Assert.InRange(acima, 4_700, 5_300);
        Assert.True(maior > 0.999, $"o maior sorteio foi {maior}");
    }

    /// <summary>
    /// Os valores que CABEM exatamente viram a fracao obvia.
    /// </summary>
    [Fact]
    public void OsValoresQueCabemViramAFracaoObvia()
    {
        Assert.Equal(Exato.De(0), Exato.De(0.0));
        Assert.Equal(Exato.De(1), Exato.De(1.0));
        Assert.Equal(Exato.De(-7), Exato.De(-7.0));
        Assert.Equal(Exato.De(1, 2), Exato.De(0.5));
        Assert.Equal(Exato.De(3, 4), Exato.De(0.75));
        Assert.Equal(Exato.De(1, 1024), Exato.De(1.0 / 1024));
    }

    /// <summary>
    /// O ACHADO QUE ENCERRA A DISCUSSAO: 0,1 nao e 0,1.
    ///
    /// O numero guardado e maior que um decimo, e a expansao decimal exata dele tem
    /// cinquenta e cinco casas. Isso nao e uma aproximacao do que o computador
    /// guarda: e o valor dele.
    /// </summary>
    [Fact]
    public void UmDecimoNaoEhUmDecimo()
    {
        var guardado = Exato.De(0.1);
        var umDecimo = Exato.De(1, 10);

        Assert.NotEqual(umDecimo, guardado);
        Assert.True(guardado > umDecimo);

        Assert.StartsWith("0.1000000000000000055511151231257827", guardado.Decimal(60), StringComparison.Ordinal);
        Assert.Equal(0.1, guardado.ParaDouble());
    }

    /// <summary>
    /// E 0,1 mais 0,2 nao e 0,3, porque os tres sao outros numeros e a soma deles
    /// ainda e arredondada.
    /// </summary>
    [Fact]
    public void ZeroUmMaisZeroDoisNaoEhZeroTres()
    {
        Assert.NotEqual(0.3, 0.1 + 0.2);
        Assert.NotEqual(Exato.De(0.3), Exato.De(0.1) + Exato.De(0.2));

        // e a diferenca e de um passo so: eles sao numeros vizinhos
        Assert.Equal(1, Formato.PassosEntre(0.3, 0.1 + 0.2));
    }

    /// <summary>
    /// A conversao e EXATA nos dois sentidos para todo numero representavel: a
    /// fracao volta a ser o mesmo numero.
    /// </summary>
    [Fact]
    public void AVoltaDaOMesmoNumero()
    {
        var sorteio = new Sorteio(5);

        for (var caso = 0; caso < 5_000; caso++)
        {
            var valor = (sorteio.Fracao() - 0.5) * Math.Pow(10, sorteio.Entre(-200, 200));

            Assert.Equal(valor, Exato.De(valor).ParaDouble());
        }
    }

    /// <summary>
    /// Os SUBNORMAIS, que sao os numeros menores que o menor normal, tambem sao
    /// convertidos certo: eles nao tem o bit implicito da frente.
    /// </summary>
    [Fact]
    public void OsSubnormaisSaoConvertidosCerto()
    {
        Assert.Equal(double.Epsilon, Exato.De(double.Epsilon).ParaDouble());
        Assert.Equal(Exato.De(1, BigInteger.Pow(2, 1074)), Exato.De(double.Epsilon));

        var subnormal = double.Epsilon * 7;
        Assert.Equal(subnormal, Exato.De(subnormal).ParaDouble());
    }

    [Fact]
    public void NumerosQueNaoSaoFinitosSaoRecusados()
    {
        Assert.Throws<ArgumentException>(() => Exato.De(double.NaN));
        Assert.Throws<ArgumentException>(() => Exato.De(double.PositiveInfinity));
        Assert.Throws<DivideByZeroException>(() => Exato.De(1, 0));
    }

    /// <summary>
    /// A aritmetica exata obedece as regras que a de ponto flutuante nao obedece:
    /// associatividade, distributividade e comutatividade.
    /// </summary>
    [Fact]
    public void AAritmeticaExataObedeceAsRegras()
    {
        var sorteio = new Sorteio(9);

        for (var caso = 0; caso < 2_000; caso++)
        {
            var a = Exato.De(sorteio.Entre(-1000, 1000), sorteio.Entre(1, 1000));
            var b = Exato.De(sorteio.Entre(-1000, 1000), sorteio.Entre(1, 1000));
            var c = Exato.De(sorteio.Entre(-1000, 1000), sorteio.Entre(1, 1000));

            Assert.Equal((a + b) + c, a + (b + c));
            Assert.Equal(a * (b + c), a * b + a * c);
            Assert.Equal(a * b, b * a);
            Assert.Equal(a - b, -(b - a));
        }
    }

    [Fact]
    public void AFracaoEhSempreSimplificada()
    {
        Assert.Equal(Exato.De(1, 2), Exato.De(5, 10));
        Assert.Equal(Exato.De(1, 2), Exato.De(-5, -10));
        Assert.Equal(Exato.De(-1, 2), Exato.De(5, -10));
        Assert.Equal(BigInteger.One, Exato.De(4, 8).Numerador);
        Assert.Equal(2, Exato.De(4, 8).Denominador);
    }

    /// <summary>
    /// Os DIGITOS CORRETOS sao a leitura humana do erro relativo, e eles batem com
    /// casos de resposta conhecida.
    /// </summary>
    [Fact]
    public void OsDigitosCorretosBatemComCasosConhecidos()
    {
        var um = Exato.Um;

        Assert.Equal(17, Exato.DigitosCorretos(1.0, um));
        Assert.Equal(1, Exato.DigitosCorretos(1.1, um), 1);
        Assert.Equal(2, Exato.DigitosCorretos(1.01, um), 1);
        Assert.Equal(0, Exato.DigitosCorretos(2.0, um), 1);
        Assert.Equal(0, Exato.DigitosCorretos(0.0, um), 1);
    }

    [Fact]
    public void AExpansaoDecimalBateComOsCasosConhecidos()
    {
        Assert.Equal("0.5", Exato.De(1, 2).Decimal(10));
        Assert.Equal("0.333333", Exato.De(1, 3).Decimal(6));
        Assert.Equal("-0.25", Exato.De(-1, 4).Decimal(10));
        Assert.Equal("7", Exato.De(7).Decimal(10));
        Assert.Equal("3", Exato.De(7, 2).Decimal(0));
    }

    [Fact]
    public void AComparacaoOrdenaCerto()
    {
        Assert.True(Exato.De(1, 3) < Exato.De(1, 2));
        Assert.True(Exato.De(-1, 2) < Exato.De(1, 3));
        Assert.True(Exato.De(2, 4) <= Exato.De(1, 2));
        Assert.True(Exato.De(2, 4) >= Exato.De(1, 2));
        Assert.True(Exato.Zero.EhZero);
        Assert.Equal(-1, Exato.De(-5, 2).Sinal);
    }
}
