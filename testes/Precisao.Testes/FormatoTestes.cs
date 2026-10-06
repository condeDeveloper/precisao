using Conde.Precisao;
using Xunit;

namespace Conde.Precisao.Testes;

public class FormatoTestes
{
    /// <summary>
    /// O ESPACAMENTO dobra a cada potencia de dois, e e isso que explica tudo o
    /// mais.
    /// </summary>
    [Fact]
    public void OEspacamentoDobraACadaPotenciaDeDois()
    {
        for (var expoente = 0; expoente < 50; expoente++)
        {
            var valor = Math.Pow(2, expoente);

            Assert.Equal(2 * Formato.Espacamento(valor), Formato.Espacamento(2 * valor));
        }

        // perto de 1 ele e 2^-52
        Assert.Equal(Math.Pow(2, -52), Formato.Espacamento(1.0));
    }

    /// <summary>
    /// A partir de 2^53, FALTAM INTEIROS: o espacamento passa de um.
    ///
    /// E por isso que identificador grande nunca deve ir para um numero com
    /// virgula: o valor volta diferente e nada avisa.
    /// </summary>
    [Fact]
    public void APartirDeDoisElevadoACinquentaETresFaltamInteiros()
    {
        Assert.Equal(Formato.LimiteDosInteiros, Formato.LimiteDosInteiros + 1);
        Assert.NotEqual(Formato.LimiteDosInteiros, Formato.LimiteDosInteiros + 2);

        Assert.Equal(1.0, Formato.Espacamento(Formato.LimiteDosInteiros / 2));
        Assert.Equal(2.0, Formato.Espacamento(Formato.LimiteDosInteiros));

        // e abaixo dele todo inteiro cabe
        Assert.NotEqual(Formato.LimiteDosInteiros / 2, Formato.LimiteDosInteiros / 2 + 1);
    }

    [Fact]
    public void OProximoRepresentavelEhMesmoOProximo()
    {
        var sorteio = new Sorteio(3);

        for (var caso = 0; caso < 2_000; caso++)
        {
            var valor = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-50, 50));
            var proximo = Formato.Proximo(valor);

            Assert.True(proximo > valor);
            Assert.Equal(1, Formato.PassosEntre(valor, proximo));

            // e nao ha nada no meio: a media dos dois volta a ser um deles
            var meio = (valor + proximo) / 2;
            Assert.True(meio == valor || meio == proximo);
        }
    }

    /// <summary>
    /// Um decimal so CABE exatamente em binario se o denominador reduzido for uma
    /// potencia de dois.
    ///
    /// Um decimo nao cabe porque dez tem um fator cinco, do mesmo jeito que um
    /// terco nao cabe em decimal.
    /// </summary>
    [Fact]
    public void SoCabeQuemTemDenominadorPotenciaDeDois()
    {
        Assert.True(Formato.CabeExatamente(1, 2));
        Assert.True(Formato.CabeExatamente(3, 4));
        Assert.True(Formato.CabeExatamente(5, 1024));
        Assert.True(Formato.CabeExatamente(5, 10));

        Assert.False(Formato.CabeExatamente(1, 10));
        Assert.False(Formato.CabeExatamente(1, 3));
        Assert.False(Formato.CabeExatamente(2, 100));
    }

    /// <summary>
    /// O TEOREMA DE STERBENZ: subtrair dois numeros proximos e EXATO.
    ///
    /// Este e o resultado que mais contraria a intuicao de quem ouviu falar em
    /// cancelamento catastrofico. Se um numero esta entre a metade e o dobro do
    /// outro, a diferenca cabe no formato sem arredondar nada.
    ///
    /// O cancelamento nao perde digitos: ele REVELA os digitos que ja estavam
    /// errados.
    /// </summary>
    [Fact]
    public void SubtrairDoisNumerosProximosEhExato()
    {
        var sorteio = new Sorteio(5);
        var conferidos = 0;

        for (var caso = 0; caso < 20_000; caso++)
        {
            var a = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-20, 20));
            var b = a * (0.5 + sorteio.Fracao() * 1.5);

            if (!Formato.ValeSterbenz(a, b)) continue;

            conferidos++;
            Assert.True(Formato.SubtracaoEhExata(a, b), $"a={a}, b={b}");
        }

        Assert.True(conferidos > 5_000, $"so {conferidos} pares cairam na condicao");
    }

    /// <summary>
    /// E fora da condicao de Sterbenz a subtracao PODE arredondar, o que mostra que
    /// o teorema nao e vazio.
    /// </summary>
    [Fact]
    public void ForaDaCondicaoASubtracaoPodeArredondar()
    {
        var sorteio = new Sorteio(7);
        var arredondou = 0;

        for (var caso = 0; caso < 20_000; caso++)
        {
            var a = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-20, 20));
            var b = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-20, 20));

            if (Formato.ValeSterbenz(a, b)) continue;
            if (!Formato.SubtracaoEhExata(a, b)) arredondou++;
        }

        Assert.True(arredondou > 1_000, $"so {arredondou} subtracoes arredondaram");
    }

    /// <summary>
    /// O que a SOMA perdeu, exatamente. E o mesmo valor que a compensacao de Kahan
    /// guarda, calculado com a fracao exata em vez de com o truque algebrico.
    /// </summary>
    [Fact]
    public void OPerdidoNaSomaBateComOTruqueDeKahan()
    {
        var sorteio = new Sorteio(11);

        for (var caso = 0; caso < 5_000; caso++)
        {
            var a = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-10, 10));
            var b = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-10, 10));

            // o truque so e EXATO quando a maior parcela vem primeiro, que e a
            // condicao de Fast2Sum: a ordem importa ate no conserto do erro
            if (Math.Abs(a) < Math.Abs(b)) (a, b) = (b, a);

            var total = a + b;
            var peloTruque = b - (total - a);
            var exato = Formato.PerdidoNaSoma(a, b);

            Assert.Equal(exato, Exato.De(peloTruque));
        }
    }

    /// <summary>
    /// A SOMA NAO E ASSOCIATIVA, e achar um contraexemplo e facil.
    ///
    /// E por isso que um compilador nao pode reordenar somas de ponto flutuante sem
    /// permissao, e por isso que somar um vetor em paralelo muda a resposta.
    /// </summary>
    [Fact]
    public void ASomaNaoEhAssociativa()
    {
        // (1 + 10^16) - 10^16 da zero, porque o 1 some no arredondamento.
        // 1 + (10^16 - 10^16) da um, porque os grandes se cancelam antes.
        Assert.False(Formato.SomaEhAssociativa(1.0, 1e16, -1e16));
        Assert.Equal(0.0, (1.0 + 1e16) + -1e16);
        Assert.Equal(1.0, 1.0 + (1e16 + -1e16));

        var trio = Formato.TrioQueQuebraAAssociatividade(new Sorteio(13));

        Assert.NotNull(trio);
        Assert.NotEqual((trio!.Value.A + trio.Value.B) + trio.Value.C, trio.Value.A + (trio.Value.B + trio.Value.C));
    }

    /// <summary>
    /// E com numeros na MESMA escala ela quase sempre e associativa, que e por que
    /// o problema passa despercebido.
    /// </summary>
    [Fact]
    public void ComNumerosNaMesmaEscalaElaQuaseSempreEhAssociativa()
    {
        var sorteio = new Sorteio(17);
        var quebrou = 0;

        for (var caso = 0; caso < 10_000; caso++)
        {
            var a = sorteio.Fracao();
            var b = sorteio.Fracao();
            var c = sorteio.Fracao();

            if (!Formato.SomaEhAssociativa(a, b, c)) quebrou++;
        }

        Assert.True(quebrou < 3_000, $"quebrou em {quebrou} de 10.000");
    }

    [Fact]
    public void OValorExatoImprimeTudo()
    {
        Assert.Equal("0.5", Formato.ValorExato(0.5));
        Assert.StartsWith("0.1000000000000000055511151231257827", Formato.ValorExato(0.1), StringComparison.Ordinal);
        Assert.Equal("1", Formato.ValorExato(1.0));
    }
}
