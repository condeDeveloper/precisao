using Conde.Precisao;
using Xunit;

namespace Conde.Precisao.Testes;

public class DinheiroTestes
{
    /// <summary>
    /// O ACHADO QUE ME CORRIGIU: a SOMA de um extrato com ponto flutuante quase
    /// nao erra.
    ///
    /// Eu ia escrever que somar dinheiro com ponto flutuante perde centavos, porque
    /// e o que todo mundo diz. Com um milhao de lancamentos de ate dez mil reais, a
    /// diferenca e de TRES CENTESIMOS DE MILESIMO de centavo.
    ///
    /// O conselho esta certo e o motivo que costuma vir junto, nao: o problema do
    /// dinheiro em ponto flutuante nao esta na soma, esta no ARREDONDAMENTO da
    /// divisao, que o teste ao lado mede.
    /// </summary>
    [Fact]
    public void ASomaDeUmExtratoComPontoFlutuanteQuaseNaoErra()
    {
        foreach (var quantos in new[] { 1_000, 100_000, 1_000_000 })
        {
            var extrato = Dinheiro.Extrato(quantos);
            var diferenca = Dinheiro.DiferencaEmCentavos(extrato).Abs();

            Assert.True(diferenca < Exato.De(1, 100), $"{quantos} lancamentos: {diferenca.Decimal(10)} centavos");
        }
    }

    /// <summary>
    /// E o DECIMAL do dotnet acerta na virgula, sempre, porque ele guarda a parte
    /// decimal em base dez.
    /// </summary>
    [Fact]
    public void ODecimalAcertaSempre()
    {
        foreach (var quantos in new[] { 1_000, 10_000, 100_000 })
        {
            var extrato = Dinheiro.Extrato(quantos);

            Assert.Equal(Dinheiro.SaldoEmCentavos(extrato) / 100m, Dinheiro.SaldoComDecimal(extrato));
        }
    }

    /// <summary>
    /// O ACHADO DE VERDADE: o RATEIO com ponto flutuante cria ou perde centavo, e o
    /// rateio em centavos inteiros fecha sempre.
    ///
    /// Dividir cem reais por tres da 33,33 para cada um, e tres vezes 33,33 e
    /// 99,99: falta um centavo, e ele tem que ir para alguem.
    /// </summary>
    [Fact]
    public void ORateioComPontoFlutuanteCriaOuPerdeCentavo()
    {
        var errou = 0;
        var total = 0;

        for (long valor = 1; valor <= 2_000; valor++)
            for (var pessoas = 2; pessoas <= 13; pessoas++)
            {
                total++;

                Assert.Equal(0, Dinheiro.SobraOuFalta(Dinheiro.RatearEmCentavos(valor, pessoas), valor));
                if (Dinheiro.SobraOuFalta(Dinheiro.RatearComDouble(valor, pessoas), valor) != 0) errou++;
            }

        Assert.True((double)errou / total > 0.5, $"o rateio com double so errou em {errou} de {total}");
    }

    /// <summary>
    /// E o rateio em centavos distribui a sobra de um em um: ninguem recebe mais de
    /// um centavo a mais que o vizinho.
    /// </summary>
    [Fact]
    public void ORateioEmCentavosDistribuiASobraDeUmEmUm()
    {
        for (long total = -500; total <= 500; total++)
            for (var pessoas = 1; pessoas <= 9; pessoas++)
            {
                var partes = Dinheiro.RatearEmCentavos(total, pessoas);

                Assert.Equal(pessoas, partes.Length);
                Assert.Equal(0, Dinheiro.SobraOuFalta(partes, total));
                Assert.True(partes.Max() - partes.Min() <= 1, $"total {total}, {pessoas} pessoas");
            }
    }

    [Fact]
    public void RatearEntreZeroPessoasEhRecusado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Dinheiro.RatearEmCentavos(100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dinheiro.RatearComDouble(100, -1));
    }

    /// <summary>
    /// O JUROS COMPOSTOS: a conta com ponto flutuante acumula erro a cada mes, e a
    /// exata nao tem erro nenhum.
    ///
    /// Depois de trezentos e sessenta meses, a diferenca ainda e pequena em
    /// relacao ao valor, e ela ja e visivel em centavos.
    /// </summary>
    [Fact]
    public void OJurosCompostosAcumulamErro()
    {
        const long capital = 100_000_00;
        const long taxa = 1;

        foreach (var meses in new[] { 12, 120, 360 })
        {
            var comDouble = Dinheiro.JurosComDouble(capital / 100.0, taxa / 100.0, meses);
            var exato = Dinheiro.JurosExatos(capital, taxa, meses);

            var digitos = Exato.DigitosCorretos(comDouble, exato);

            Assert.True(digitos > 12, $"{meses} meses: so {digitos:F1} digitos");
            Assert.True(digitos < 17, $"{meses} meses: nenhum erro acumulou");
        }
    }

    /// <summary>
    /// E o juros EXATO e uma fracao sem erro nenhum: ele e a resposta, e o
    /// arredondamento para centavos acontece uma vez so, no fim.
    /// </summary>
    [Fact]
    public void OJurosExatoNaoTemErro()
    {
        // 100 reais a 10% por 2 meses: 100 * 1,1 * 1,1 = 121
        Assert.Equal(Exato.De(121), Dinheiro.JurosExatos(100_00, 10, 2));
        Assert.Equal(12100, Dinheiro.ArredondarParaCentavos(Dinheiro.JurosExatos(100_00, 10, 2)));

        // e com zero meses ele e o proprio capital
        Assert.Equal(Exato.De(1, 1) * Exato.De(100), Dinheiro.JurosExatos(100_00, 10, 0));
    }

    [Fact]
    public void OArredondamentoParaCentavosArredondaCerto()
    {
        Assert.Equal(100, Dinheiro.ArredondarParaCentavos(Exato.De(1)));
        Assert.Equal(33, Dinheiro.ArredondarParaCentavos(Exato.De(1, 3)));
        Assert.Equal(67, Dinheiro.ArredondarParaCentavos(Exato.De(2, 3)));
        Assert.Equal(-33, Dinheiro.ArredondarParaCentavos(Exato.De(-1, 3)));
        Assert.Equal(1, Dinheiro.ArredondarParaCentavos(Exato.De(1, 200)));
    }

    /// <summary>
    /// O extrato sorteado e DETERMINISTA, e os lancamentos sao inteiros em
    /// centavos: nao existe meio centavo numa conta bancaria.
    /// </summary>
    [Fact]
    public void OExtratoEhDeterministaEEmCentavosInteiros()
    {
        var um = Dinheiro.Extrato(1_000);
        var outro = Dinheiro.Extrato(1_000);

        Assert.Equal(um, outro);
        Assert.All(um, l => Assert.NotEqual(0, l.Centavos));
        Assert.Contains(um, l => l.Centavos < 0);
        Assert.Contains(um, l => l.Centavos > 0);
    }
}
