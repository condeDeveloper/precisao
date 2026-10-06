using Conde.Precisao;
using Xunit;

namespace Conde.Precisao.Testes;

/// <summary>
/// Os testes das TABELAS que o medidor imprime.
///
/// Eles existem porque numero em README que ninguem confere envelhece errado em
/// silencio: alguem troca uma constante, a tabela muda e o texto continua dizendo
/// a frase antiga. Aqui o teste afirma sobre o mesmo objeto que vira linha
/// impressa.
/// </summary>
public class MedidasTestes
{
    [Fact]
    public void ATabelaDasSomasTemUmaLinhaPorParDeListaEJeito()
    {
        var linhas = Medidas.Somas(10_000);

        Assert.Equal(16, linhas.Count);
        Assert.Equal(4, linhas.Select(l => l.Lista).Distinct().Count());
        Assert.Equal(4, linhas.Select(l => l.Jeito).Distinct().Count());
    }

    /// <summary>
    /// A frase do texto: na mesma escala as quatro acertam, e e por isso que medir
    /// com numeros sorteados entre zero e um nao descobre nada.
    /// </summary>
    [Fact]
    public void NaMesmaEscalaAsQuatroAcertam()
    {
        foreach (var l in Medidas.Somas(10_000).Where(l => l.Lista == "mesma escala"))
            Assert.True(l.DigitosCorretos > 14, $"{l.Jeito} acertou {l.DigitosCorretos:F1}");
    }

    /// <summary>
    /// A frase do texto: com um grande e muitos pequenos, so a ingenua erra.
    /// </summary>
    [Fact]
    public void ComUmGrandeEMuitosPequenosSoAIngenuaErra()
    {
        var linhas = Medidas.Somas(100_000).Where(l => l.Lista == "um grande e muitos pequenos").ToList();

        Assert.True(linhas.Single(l => l.Jeito == "ingenua").DigitosCorretos < 12);

        foreach (var l in linhas.Where(l => l.Jeito != "ingenua"))
            Assert.True(l.DigitosCorretos > 15, $"{l.Jeito} acertou {l.DigitosCorretos:F1}");
    }

    /// <summary>
    /// A frase do texto: com CANCELAMENTO, a arvore ganha de Kahan e da ordenada.
    /// </summary>
    [Fact]
    public void ComCancelamentoAArvoreGanhaDeKahan()
    {
        var linhas = Medidas.Somas(100_000).Where(l => l.Lista == "com cancelamento").ToList();

        var arvore = linhas.Single(l => l.Jeito == "em arvore").DigitosCorretos;
        var kahan = linhas.Single(l => l.Jeito == "Kahan").DigitosCorretos;
        var ordenada = linhas.Single(l => l.Jeito == "ordenada").DigitosCorretos;

        Assert.True(arvore > kahan + 5, $"arvore {arvore:F1}, Kahan {kahan:F1}");
        Assert.True(arvore > ordenada + 5, $"arvore {arvore:F1}, ordenada {ordenada:F1}");
    }

    /// <summary>
    /// A frase do texto: o erro da ingenua cresce com as parcelas e o dos outros
    /// dois nao.
    /// </summary>
    [Fact]
    public void OErroDaIngenuaCresceEODosOutrosNao()
    {
        var linhas = Medidas.Crescimentos();

        Assert.Equal(4, linhas.Count);
        Assert.True(linhas[^1].Ingenua < linhas[0].Ingenua - 2,
            $"a ingenua foi de {linhas[0].Ingenua:F1} para {linhas[^1].Ingenua:F1}");

        Assert.All(linhas, l => Assert.True(l.Kahan > 15.5));
        Assert.All(linhas, l => Assert.True(l.EmArvore > 15.5));
    }

    /// <summary>
    /// A frase do texto: a Bhaskara do livro perde quase tudo e a reescrita acerta
    /// dezesseis.
    /// </summary>
    [Fact]
    public void ABhaskaraDoLivroPerdeQuaseTudo()
    {
        var linhas = Medidas.Bhaskaras();

        Assert.Equal(4, linhas.Count);

        foreach (var l in linhas) Assert.True(l.Estavel > 15, $"a reescrita acertou so {l.Estavel:F1}");

        // e o livro piora conforme a raiz grande cresce
        for (var i = 1; i < linhas.Count; i++) Assert.True(linhas[i].DoLivro < linhas[i - 1].DoLivro);

        Assert.True(linhas[^1].DoLivro < 1, $"o livro ainda acertou {linhas[^1].DoLivro:F1} digitos");
    }

    /// <summary>
    /// A frase do texto: a variancia de uma passada chega a dar NEGATIVA.
    /// </summary>
    [Fact]
    public void AVarianciaDeUmaPassadaChegaADarNegativa()
    {
        var linhas = Medidas.Variancias();

        Assert.Equal(4, linhas.Count);
        Assert.False(linhas[0].DeuNegativa);
        Assert.True(linhas[^1].DeuNegativa);

        foreach (var l in linhas) Assert.True(l.Welford >= l.UmaPassada, $"centro {l.Centro}");
    }

    /// <summary>
    /// A frase do texto: a soma do extrato com ponto flutuante quase nao erra, e o
    /// rateio erra sempre.
    /// </summary>
    [Fact]
    public void ASomaDoExtratoQuaseNaoErraEORateioErra()
    {
        foreach (var l in Medidas.Dinheiros())
        {
            Assert.True(l.DecimalAcertou);
            Assert.True(l.CentavosAcertou);
        }

        var rateios = Medidas.Rateios();

        Assert.Equal(4, rateios.Count);
        Assert.All(rateios, r => Assert.Equal(0, r.SobraComCentavos));
        Assert.Contains(rateios, r => r.SobraComDouble != 0);
    }

    /// <summary>
    /// A frase do texto: o espacamento dobra, e acima de 2^53 faltam inteiros.
    /// </summary>
    [Fact]
    public void OEspacamentoCresceEAcimaDeDoisAoCinquentaETresFaltamInteiros()
    {
        var linhas = Medidas.Espacamentos();

        Assert.Equal(6, linhas.Count);

        for (var i = 1; i < linhas.Count; i++) Assert.True(linhas[i].Espacamento > linhas[i - 1].Espacamento);

        Assert.Equal(0, linhas[0].InteirosQueFaltam);
        Assert.True(linhas[^1].InteirosQueFaltam > 100);
    }

    /// <summary>
    /// E a tabela dos decimais mostra o que o computador realmente guarda.
    /// </summary>
    [Fact]
    public void ATabelaDosDecimaisMostraOValorExato()
    {
        var linhas = Medidas.Decimais();

        Assert.Equal(5, linhas.Count);
        Assert.StartsWith("0.1000000000000000055511", linhas[0].Exato, StringComparison.Ordinal);
        Assert.Equal("0.5", linhas[^1].Exato);
        Assert.NotEqual(linhas[2].Exato, linhas[3].Exato);
    }
}
