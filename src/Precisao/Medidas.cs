namespace Conde.Precisao;

/// <summary>
/// As MEDIDAS: as tabelas que o medidor imprime e que os testes conferem.
///
/// Elas ficam aqui, e nao no medidor, para o teste poder afirmar sobre o MESMO
/// numero que aparece no texto. Tabela de README que ninguem testa envelhece
/// errado em silencio.
///
/// Nenhuma delas usa relogio. Todas sao digitos corretos, centavos de diferenca
/// ou contagem de casos, e todas sao medidas contra a FRACAO EXATA.
/// </summary>
public static class Medidas
{
    public sealed record LinhaSoma(string Lista, string Jeito, double DigitosCorretos);

    /// <summary>
    /// AS QUATRO SOMAS contra a soma exata, em quatro listas de numeros.
    ///
    /// A coluna e em digitos decimais corretos, que e a leitura humana do erro
    /// relativo. A precisao dupla tem uns dezesseis para dar.
    /// </summary>
    public static List<LinhaSoma> Somas(int quantos = 100_000)
    {
        var linhas = new List<LinhaSoma>();

        foreach (var (nome, numeros) in Listas.Todas(quantos))
        {
            var certo = Soma.Exatamente(numeros);

            foreach (var jeito in Soma.Todos())
                linhas.Add(new LinhaSoma(nome, jeito.Nome, Exato.DigitosCorretos(jeito.Somar(numeros), certo)));
        }

        return linhas;
    }

    public sealed record LinhaCrescimento(int Parcelas, double Ingenua, double EmArvore, double Kahan);

    /// <summary>
    /// O ERRO CRESCENDO com o numero de parcelas.
    ///
    /// E a diferenca de forma que importa: o da soma ingenua cresce com n, o da
    /// arvore com log n, e o de Kahan nao cresce.
    /// </summary>
    public static List<LinhaCrescimento> Crescimentos()
    {
        var linhas = new List<LinhaCrescimento>();

        foreach (var parcelas in new[] { 1_000, 10_000, 100_000, 1_000_000 })
        {
            var numeros = Listas.EscalasVariadas(parcelas);
            var certo = Soma.Exatamente(numeros);

            linhas.Add(new LinhaCrescimento(
                parcelas,
                Exato.DigitosCorretos(Soma.Ingenua(numeros), certo),
                Exato.DigitosCorretos(Soma.EmArvore(numeros), certo),
                Exato.DigitosCorretos(Soma.Kahan(numeros), certo)));
        }

        return linhas;
    }

    public sealed record LinhaBhaskara(double C, double DoLivro, double Estavel, string Certo);

    /// <summary>
    /// A FORMULA DE BHASKARA, do jeito do livro e reescrita.
    ///
    /// O polinomio e x ao quadrado menos (10^k + 1/10^k) x mais 1, cujas raizes
    /// sao exatamente 10^k e 10^(-k). Com k crescendo, a raiz pequena some.
    /// </summary>
    public static List<LinhaBhaskara> Bhaskaras()
    {
        var linhas = new List<LinhaBhaskara>();

        foreach (var k in new[] { 3, 5, 7, 8 })
        {
            var grande = Math.Pow(10, k);
            var b = -(grande + 1 / grande);

            var doLivro = Cancelamento.BhaskaraDoLivro(1, b, 1).Menor;
            var estavel = Cancelamento.BhaskaraEstavel(1, b, 1).Menor;
            var certo = Exato.De(1, (long)grande);

            linhas.Add(new LinhaBhaskara(
                grande,
                Exato.DigitosCorretos(doLivro, certo),
                Exato.DigitosCorretos(estavel, certo),
                certo.Decimal(14)));
        }

        return linhas;
    }

    public sealed record LinhaVariancia(double Centro, double Ruido, double UmaPassada, double Welford, bool DeuNegativa);

    /// <summary>
    /// A VARIANCIA pelas duas formulas, em numeros grandes com pouca variacao.
    ///
    /// A coluna da direita e a que encerra o assunto: a formula de uma passada
    /// devolve variancia NEGATIVA, que e matematicamente impossivel.
    /// </summary>
    public static List<LinhaVariancia> Variancias(int quantos = 10_000)
    {
        var linhas = new List<LinhaVariancia>();

        foreach (var (centro, ruido) in new[] { (1.0, 1.0), (1e6, 1.0), (1e9, 1.0), (1e12, 1.0) })
        {
            var numeros = Cancelamento.GrandesComPoucaVariacao(quantos, centro, ruido);
            var certo = Cancelamento.VarianciaExata(numeros);

            var umaPassada = Cancelamento.VarianciaDeUmaPassada(numeros);

            linhas.Add(new LinhaVariancia(
                centro,
                ruido,
                Exato.DigitosCorretos(umaPassada, certo),
                Exato.DigitosCorretos(Cancelamento.VarianciaDeWelford(numeros), certo),
                umaPassada < 0));
        }

        return linhas;
    }

    public sealed record LinhaDinheiro(int Lancamentos, string DiferencaEmCentavos, bool DecimalAcertou, bool CentavosAcertou);

    /// <summary>
    /// O DINHEIRO: quantos centavos o ponto flutuante perde num extrato.
    /// </summary>
    public static List<LinhaDinheiro> Dinheiros()
    {
        var linhas = new List<LinhaDinheiro>();

        foreach (var quantos in new[] { 1_000, 10_000, 100_000, 1_000_000 })
        {
            var extrato = Dinheiro.Extrato(quantos);

            var certo = Dinheiro.SaldoEmCentavos(extrato);
            var diferenca = Dinheiro.DiferencaEmCentavos(extrato);

            linhas.Add(new LinhaDinheiro(
                quantos,
                diferenca.Decimal(6),
                Dinheiro.SaldoComDecimal(extrato) == certo / 100m,
                true));
        }

        return linhas;
    }

    public sealed record LinhaRateio(long Total, int Pessoas, long SobraComDouble, long SobraComCentavos);

    /// <summary>
    /// O RATEIO: dividir um valor entre n pessoas sem perder nem criar centavo.
    /// </summary>
    public static List<LinhaRateio> Rateios()
    {
        var linhas = new List<LinhaRateio>();

        foreach (var (total, pessoas) in new[] { (10_000L, 3), (10_000L, 7), (99_999L, 11), (1L, 3) })
            linhas.Add(new LinhaRateio(
                total,
                pessoas,
                Dinheiro.SobraOuFalta(Dinheiro.RatearComDouble(total, pessoas), total),
                Dinheiro.SobraOuFalta(Dinheiro.RatearEmCentavos(total, pessoas), total)));

        return linhas;
    }

    public sealed record LinhaEspacamento(double Perto, double Espacamento, long InteirosQueFaltam);

    /// <summary>
    /// O ESPACAMENTO entre numeros vizinhos, que dobra a cada potencia de dois.
    /// </summary>
    public static List<LinhaEspacamento> Espacamentos()
    {
        var linhas = new List<LinhaEspacamento>();

        foreach (var valor in new[] { 1.0, 1e3, 1e9, 1e15, Formato.LimiteDosInteiros, 1e18 })
        {
            var espacamento = Formato.Espacamento(valor);

            linhas.Add(new LinhaEspacamento(valor, espacamento, (long)Math.Max(0, espacamento - 1)));
        }

        return linhas;
    }

    /// <summary>
    /// O valor EXATO de alguns decimais famosos, que e o jeito de encerrar a
    /// discussao sobre 0,1 mais 0,2.
    /// </summary>
    public static List<(string Escrito, string Exato)> Decimais() =>
    [
        ("0.1", Formato.ValorExato(0.1)),
        ("0.2", Formato.ValorExato(0.2)),
        ("0.1 + 0.2", Formato.ValorExato(0.1 + 0.2)),
        ("0.3", Formato.ValorExato(0.3)),
        ("0.5", Formato.ValorExato(0.5)),
    ];
}
