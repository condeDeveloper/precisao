using System.Numerics;

namespace Conde.Precisao;

/// <summary>
/// DINHEIRO: o caso em que o erro de arredondamento tem dono e tem valor.
///
/// A regra que todo mundo repete e "nunca use ponto flutuante para dinheiro", e
/// ela esta certa. O que quase nunca aparece e QUANTO custa desobedecer, e com
/// que cara o erro aparece: ele nao vem como uma excecao, ele vem como um
/// centavo que nao fecha no fim do mes.
///
/// Aqui as tres formas sao comparadas contra a conta exata: ponto flutuante,
/// decimal do dotnet e centavos inteiros.
/// </summary>
public static class Dinheiro
{
    public sealed record Lancamento(long Centavos)
    {
        public double ComoDouble => Centavos / 100.0;

        public decimal ComoDecimal => Centavos / 100m;
    }

    /// <summary>
    /// O extrato SORTEADO: valores em centavos, positivos e negativos.
    ///
    /// Em centavos inteiros, porque e assim que o dinheiro existe de verdade: nao
    /// existe meio centavo numa conta bancaria.
    /// </summary>
    public static List<Lancamento> Extrato(int quantos, ulong semente = 7, int maximoEmReais = 10_000)
    {
        var sorteio = new Sorteio(semente);
        var lancamentos = new List<Lancamento>();

        for (var i = 0; i < quantos; i++)
        {
            var sinal = sorteio.Fracao() < 0.5 ? -1 : 1;
            lancamentos.Add(new Lancamento(sinal * (long)sorteio.Entre(1, maximoEmReais * 100)));
        }

        return lancamentos;
    }

    /// <summary>O saldo somando com ponto flutuante.</summary>
    public static double SaldoComDouble(IReadOnlyList<Lancamento> extrato)
    {
        ArgumentNullException.ThrowIfNull(extrato);

        var saldo = 0.0;
        foreach (var l in extrato) saldo += l.ComoDouble;

        return saldo;
    }

    /// <summary>O saldo somando com o decimal do dotnet.</summary>
    public static decimal SaldoComDecimal(IReadOnlyList<Lancamento> extrato)
    {
        ArgumentNullException.ThrowIfNull(extrato);

        var saldo = 0m;
        foreach (var l in extrato) saldo += l.ComoDecimal;

        return saldo;
    }

    /// <summary>O saldo em CENTAVOS INTEIROS, que e exato por construcao.</summary>
    public static long SaldoEmCentavos(IReadOnlyList<Lancamento> extrato)
    {
        ArgumentNullException.ThrowIfNull(extrato);

        long saldo = 0;
        foreach (var l in extrato) saldo += l.Centavos;

        return saldo;
    }

    /// <summary>
    /// A diferenca entre o saldo com ponto flutuante e o certo, EM CENTAVOS.
    ///
    /// E esta a unidade que importa: dizer que o erro relativo e 1e-16 nao fecha
    /// caixa nenhum.
    /// </summary>
    public static Exato DiferencaEmCentavos(IReadOnlyList<Lancamento> extrato)
    {
        var comDouble = Exato.De(SaldoComDouble(extrato));
        var certo = Exato.De(SaldoEmCentavos(extrato), 100);

        return (comDouble - certo) * Exato.De(100);
    }

    /// <summary>
    /// O RATEIO: dividir um valor entre n pessoas sem perder nem criar centavo.
    ///
    /// E o problema de dinheiro mais comum que existe e o que mais aparece errado.
    /// Dividir cem reais por tres da 33,33 para cada um, e tres vezes 33,33 e
    /// 99,99: falta um centavo, e ele tem que ir para alguem.
    ///
    /// A regra do resto distribui os centavos que sobram, um para cada um dos
    /// primeiros, e o total fecha SEMPRE.
    /// </summary>
    public static long[] RatearEmCentavos(long total, int pessoas)
    {
        if (pessoas <= 0) throw new ArgumentOutOfRangeException(nameof(pessoas));

        var baseDaParte = total / pessoas;
        var sobra = total - baseDaParte * pessoas;

        var partes = new long[pessoas];
        for (var i = 0; i < pessoas; i++) partes[i] = baseDaParte;

        var passo = sobra >= 0 ? 1 : -1;
        for (var i = 0; i < Math.Abs(sobra); i++) partes[i] += passo;

        return partes;
    }

    /// <summary>
    /// O MESMO rateio feito com ponto flutuante e arredondamento, que e o jeito que
    /// aparece na planilha.
    ///
    /// Ele perde ou cria centavo, e a medida conta quantas vezes.
    /// </summary>
    public static long[] RatearComDouble(long total, int pessoas)
    {
        if (pessoas <= 0) throw new ArgumentOutOfRangeException(nameof(pessoas));

        var parte = total / 100.0 / pessoas;
        var partes = new long[pessoas];

        for (var i = 0; i < pessoas; i++) partes[i] = (long)Math.Round(parte * 100, MidpointRounding.AwayFromZero);

        return partes;
    }

    /// <summary>
    /// Quantos CENTAVOS o rateio criou ou perdeu.
    /// </summary>
    public static long SobraOuFalta(long[] partes, long total)
    {
        ArgumentNullException.ThrowIfNull(partes);

        long soma = 0;
        foreach (var p in partes) soma += p;

        return soma - total;
    }

    /// <summary>
    /// O JUROS COMPOSTOS, que e onde o erro de arredondamento vira juros tambem.
    ///
    /// Aplicar uma taxa n vezes com ponto flutuante acumula erro a cada mes. Com a
    /// conta exata, o resultado e uma fracao e nao tem erro nenhum.
    /// </summary>
    public static double JurosComDouble(double capital, double taxa, int meses)
    {
        var valor = capital;
        for (var i = 0; i < meses; i++) valor *= 1 + taxa;

        return valor;
    }

    /// <summary>A mesma conta com fracoes exatas.</summary>
    public static Exato JurosExatos(long capitalEmCentavos, long taxaPorCento, int meses)
    {
        var valor = Exato.De(capitalEmCentavos, 100);
        var fator = Exato.Um + Exato.De(taxaPorCento, 100);

        for (var i = 0; i < meses; i++) valor *= fator;

        return valor;
    }

    /// <summary>
    /// O valor arredondado para centavos, como ele apareceria no extrato.
    /// </summary>
    public static BigInteger ArredondarParaCentavos(Exato valor)
    {
        var emCentavos = valor * Exato.De(100);
        var dobro = emCentavos.Numerador * 2;
        var inteiro = BigInteger.Divide(dobro + emCentavos.Denominador * (dobro.Sign < 0 ? -1 : 1), emCentavos.Denominador * 2);

        return inteiro;
    }
}
