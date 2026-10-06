using Conde.Precisao;
using Xunit;

namespace Conde.Precisao.Testes;

public class SomaTestes
{
    /// <summary>
    /// Com numeros na MESMA ESCALA, as quatro somas acertam tudo. E por isso que
    /// medir com numeros sorteados entre zero e um nao descobre nada.
    /// </summary>
    [Fact]
    public void ComAMesmaEscalaAsQuatroAcertam()
    {
        var numeros = Listas.MesmaEscala(100_000);
        var certo = Soma.Exatamente(numeros);

        foreach (var jeito in Soma.Todos())
            Assert.True(Exato.DigitosCorretos(jeito.Somar(numeros), certo) > 14, jeito.Nome);
    }

    /// <summary>
    /// O ACHADO CLASSICO: um numero grande e muitos pequenos.
    ///
    /// Cada parcela pequena, sozinha, desaparece no arredondamento do total, e o
    /// total delas nao e desprezivel. A soma ingenua perde cinco digitos; ordenar
    /// resolve inteiro.
    /// </summary>
    [Fact]
    public void UmGrandeEMuitosPequenosQuebraASomaIngenua()
    {
        var numeros = Listas.UmGrandeEMuitosPequenos(100_000);
        var certo = Soma.Exatamente(numeros);

        var ingenua = Exato.DigitosCorretos(Soma.Ingenua(numeros), certo);

        Assert.True(ingenua < 12, $"a ingenua acertou {ingenua:F1} digitos");
        Assert.True(Exato.DigitosCorretos(Soma.Ordenada(numeros), certo) > 15);
        Assert.True(Exato.DigitosCorretos(Soma.Kahan(numeros), certo) > 15);
        Assert.True(Exato.DigitosCorretos(Soma.EmArvore(numeros), certo) > 15);
    }

    /// <summary>
    /// E o caso extremo: somar 1 a 10^16 nao muda nada, cem mil vezes seguidas.
    /// </summary>
    [Fact]
    public void SomarUmAUmNumeroGrandeNaoMudaNada()
    {
        var total = 1e16;
        for (var i = 0; i < 100_000; i++) total += 1.0;

        Assert.Equal(1e16, total);

        // e a soma exata diz quanto se perdeu
        var numeros = Listas.UmGrandeEMuitosPequenos(100_000);
        Assert.Equal(Exato.De(1e16) + Exato.De(100_000), Soma.Exatamente(numeros));
    }

    /// <summary>
    /// O ERRO DA SOMA INGENUA cresce com o numero de parcelas, e o de Kahan nao.
    /// </summary>
    [Fact]
    public void OErroDaIngenuaCresceComAsParcelasEODeKahanNao()
    {
        var digitosIngenua = new List<double>();
        var digitosKahan = new List<double>();

        foreach (var parcelas in new[] { 1_000, 10_000, 100_000, 1_000_000 })
        {
            var numeros = Listas.EscalasVariadas(parcelas);
            var certo = Soma.Exatamente(numeros);

            digitosIngenua.Add(Exato.DigitosCorretos(Soma.Ingenua(numeros), certo));
            digitosKahan.Add(Exato.DigitosCorretos(Soma.Kahan(numeros), certo));
        }

        // a ingenua perde digitos conforme n cresce
        Assert.True(digitosIngenua[^1] < digitosIngenua[0] - 2,
            $"a ingenua foi de {digitosIngenua[0]:F1} para {digitosIngenua[^1]:F1}");

        // e o Kahan nao se mexe
        Assert.All(digitosKahan, d => Assert.True(d > 15.5, $"Kahan acertou so {d:F1}"));
    }

    /// <summary>
    /// O ACHADO QUE ME CORRIGIU: KAHAN NAO E SEMPRE A MELHOR.
    ///
    /// Numa lista com CANCELAMENTO (pares de numeros grandes com sinais opostos), a
    /// soma em arvore acerta tudo e a de Kahan acerta sete digitos e meio. Eu ia
    /// escrever que Kahan e a melhor das quatro.
    ///
    /// O motivo: a arvore soma os VIZINHOS primeiro, e os pares que se cancelam
    /// estao lado a lado, entao a cancelacao acontece exatamente (teorema de
    /// Sterbenz) e nada se perde. Kahan percorre a lista em ordem e passa por somas
    /// parciais enormes, compensando um arredondamento que a arvore nem produz.
    ///
    /// A compensacao conserta o acumulo do erro; ela nao conserta a ORDEM errada.
    /// </summary>
    [Fact]
    public void KahanNaoEhSempreAMelhor()
    {
        var numeros = Listas.ComCancelamento(50_000);
        var certo = Soma.Exatamente(numeros);

        var kahan = Exato.DigitosCorretos(Soma.Kahan(numeros), certo);
        var arvore = Exato.DigitosCorretos(Soma.EmArvore(numeros), certo);

        Assert.True(arvore > kahan + 5, $"arvore {arvore:F1}, Kahan {kahan:F1}");
        Assert.True(arvore > 15);
    }

    /// <summary>
    /// E ORDENAR, que e o conselho mais repetido sobre somas, tambem nao resolve o
    /// cancelamento: ele separa os positivos dos negativos pela magnitude e junta
    /// os que deveriam se anular.
    /// </summary>
    [Fact]
    public void OrdenarNaoResolveOCancelamento()
    {
        var numeros = Listas.ComCancelamento(50_000);
        var certo = Soma.Exatamente(numeros);

        var ordenada = Exato.DigitosCorretos(Soma.Ordenada(numeros), certo);

        Assert.True(ordenada < 12, $"a ordenada acertou {ordenada:F1} digitos");
    }

    /// <summary>
    /// A soma EXATA e a soma de verdade, e ela e conferida contra casos de resposta
    /// conhecida.
    /// </summary>
    [Fact]
    public void ASomaExataEhExata()
    {
        Assert.Equal(Exato.De(3), Soma.Exatamente([1.0, 2.0]));
        Assert.Equal(Exato.Zero, Soma.Exatamente([]));
        Assert.Equal(Exato.De(0.1) + Exato.De(0.2), Soma.Exatamente([0.1, 0.2]));

        // e ela NAO e o que a soma de ponto flutuante da
        Assert.NotEqual(Exato.De(0.1 + 0.2), Soma.Exatamente([0.1, 0.2]));
    }

    /// <summary>
    /// As quatro somam as MESMAS parcelas: nenhuma delas joga nada fora.
    ///
    /// Isso e conferido com uma lista em que todas acertam, para a igualdade ser
    /// sobre o que foi somado e nao sobre o erro.
    /// </summary>
    [Fact]
    public void AsQuatroSomamAsMesmasParcelas()
    {
        var sorteio = new Sorteio(5);

        for (var caso = 0; caso < 200; caso++)
        {
            var quantos = sorteio.Entre(1, 50);
            var numeros = new double[quantos];
            for (var i = 0; i < quantos; i++) numeros[i] = sorteio.Entre(-1000, 1000);

            var certo = Soma.Exatamente(numeros).ParaDouble();

            foreach (var jeito in Soma.Todos()) Assert.Equal(certo, jeito.Somar(numeros));
        }
    }

    [Fact]
    public void AListaVaziaSomaZero()
    {
        foreach (var jeito in Soma.Todos()) Assert.Equal(0.0, jeito.Somar([]));
    }

    [Fact]
    public void AListaDeUmElementoDevolveOElemento()
    {
        foreach (var jeito in Soma.Todos()) Assert.Equal(3.5, jeito.Somar([3.5]));
    }
}
