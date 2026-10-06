namespace Conde.Precisao;

/// <summary>
/// As quatro SOMAS, da pior para a melhor, e a conta exata ao lado.
///
/// Somar uma lista de numeros e a operacao mais simples que existe e ela e o
/// exemplo mais claro de erro de arredondamento que existe, porque o erro se
/// ACUMULA: cada soma parcial e arredondada, e a proxima soma parte do valor
/// arredondado.
///
/// As quatro daqui somam exatamente os mesmos numeros e dao respostas diferentes.
/// </summary>
public static class Soma
{
    /// <summary>
    /// A soma INGENUA, da esquerda para a direita.
    ///
    /// E o que todo mundo escreve, e o erro dela cresce com o numero de parcelas:
    /// no pior caso, proporcional a n vezes a precisao da maquina.
    /// </summary>
    public static double Ingenua(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);

        var total = 0.0;
        foreach (var x in numeros) total += x;

        return total;
    }

    /// <summary>
    /// A soma ORDENADA pelo valor absoluto, do menor para o maior.
    ///
    /// E o conselho que aparece em todo lugar, e ele ajuda de verdade quando as
    /// parcelas tem sinais iguais: as pequenas se juntam entre si antes de
    /// encontrar as grandes, em vez de desaparecerem uma a uma dentro de um total
    /// enorme.
    ///
    /// E ele custa ordenar, e ele NAO ajuda quando ha cancelamento, que e uma das
    /// medidas daqui.
    /// </summary>
    public static double Ordenada(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);

        var copia = numeros.ToArray();
        Array.Sort(copia, (a, b) => Math.Abs(a).CompareTo(Math.Abs(b)));

        return Ingenua(copia);
    }

    /// <summary>
    /// A soma de KAHAN, de 1965: guardar o que se perdeu e devolver na proxima.
    ///
    /// Sao quatro linhas. A variavel de compensacao guarda a parte baixa que o
    /// arredondamento jogou fora, e a parcela seguinte entra ja descontada dela.
    ///
    /// O erro para de crescer com n: ele fica limitado por uma constante vezes a
    /// precisao da maquina, nao importa quantas parcelas sejam. E a diferenca
    /// entre perder digitos aos poucos e nao perder.
    ///
    /// Repare na linha do meio: ela parece algebra inutil. Num mundo exato,
    /// (total + y) - total menos y e zero. Em ponto flutuante ela e exatamente a
    /// parte que se perdeu, e e por isso que um compilador com otimizacao agressiva
    /// de ponto flutuante DESTROI este algoritmo simplificando a expressao.
    /// </summary>
    public static double Kahan(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);

        var total = 0.0;
        var perdido = 0.0;

        foreach (var x in numeros)
        {
            var corrigido = x - perdido;
            var novo = total + corrigido;

            perdido = novo - total - corrigido;
            total = novo;
        }

        return total;
    }

    /// <summary>
    /// A soma EM ARVORE: somar aos pares, depois os pares dos pares.
    ///
    /// Ela nao guarda nada e nao compensa nada: ela so muda a ORDEM das somas, de
    /// uma corrente de n elos para uma arvore de altura log n. O erro passa de
    /// proporcional a n para proporcional a log n, de graca, e ela e a que as
    /// bibliotecas de verdade usam, porque ela tambem paraleliza.
    /// </summary>
    public static double EmArvore(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);
        if (numeros.Count == 0) return 0;

        var atual = numeros.ToArray();

        while (atual.Length > 1)
        {
            var proximo = new double[(atual.Length + 1) / 2];

            for (var i = 0; i < proximo.Length; i++)
                proximo[i] = 2 * i + 1 < atual.Length ? atual[2 * i] + atual[2 * i + 1] : atual[2 * i];

            atual = proximo;
        }

        return atual[0];
    }

    /// <summary>A soma EXATA, que e o juiz.</summary>
    public static Exato Exatamente(IReadOnlyList<double> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);

        var total = Exato.Zero;
        foreach (var x in numeros) total += Exato.De(x);

        return total;
    }

    public sealed record Jeito(string Nome, Func<IReadOnlyList<double>, double> Somar);

    public static IEnumerable<Jeito> Todos()
    {
        yield return new Jeito("ingenua", Ingenua);
        yield return new Jeito("ordenada", Ordenada);
        yield return new Jeito("em arvore", EmArvore);
        yield return new Jeito("Kahan", Kahan);
    }
}

/// <summary>
/// As LISTAS que separam os metodos, e por que elas sao assim.
///
/// Uma lista de numeros sorteados entre zero e um nao separa nada: todos os
/// metodos acertam quinze digitos. O erro de arredondamento so aparece quando as
/// parcelas tem ESCALAS muito diferentes ou quando ha CANCELAMENTO, e as duas
/// coisas acontecem o tempo todo em conta de verdade.
/// </summary>
public static class Listas
{
    /// <summary>Numeros sorteados na mesma escala: o caso facil.</summary>
    public static double[] MesmaEscala(int quantos, ulong semente = 7)
    {
        var sorteio = new Sorteio(semente);
        var numeros = new double[quantos];

        for (var i = 0; i < quantos; i++) numeros[i] = sorteio.Fracao();

        return numeros;
    }

    /// <summary>
    /// Um numero GRANDE e muitos pequenos.
    ///
    /// E o caso classico: somar um milhao de centavos a um saldo de um bilhao. Cada
    /// centavo, sozinho, desaparece no arredondamento, e o total deles nao e
    /// desprezivel.
    /// </summary>
    public static double[] UmGrandeEMuitosPequenos(int quantos, double grande = 1e16, double pequeno = 1.0)
    {
        var numeros = new double[quantos + 1];
        numeros[0] = grande;

        for (var i = 1; i <= quantos; i++) numeros[i] = pequeno;

        return numeros;
    }

    /// <summary>
    /// Escalas SORTEADAS ao longo de muitas ordens de grandeza.
    ///
    /// E o caso que a ordenacao resolve e o ingenuo nao.
    /// </summary>
    public static double[] EscalasVariadas(int quantos, ulong semente = 11, int ordens = 12)
    {
        var sorteio = new Sorteio(semente);
        var numeros = new double[quantos];

        for (var i = 0; i < quantos; i++)
            numeros[i] = sorteio.Fracao() * Math.Pow(10, sorteio.Entre(-ordens, ordens));

        return numeros;
    }

    /// <summary>
    /// Numeros com SINAIS OPOSTOS e quase iguais, que e o cancelamento.
    ///
    /// Aqui a ordenacao NAO ajuda, e e a medida que mostra o limite do conselho
    /// mais repetido sobre somas.
    /// </summary>
    public static double[] ComCancelamento(int quantos, ulong semente = 13)
    {
        var sorteio = new Sorteio(semente);
        var numeros = new double[2 * quantos];

        for (var i = 0; i < quantos; i++)
        {
            var grande = sorteio.Fracao() * 1e12;

            numeros[2 * i] = grande;
            numeros[2 * i + 1] = -grande + sorteio.Fracao();
        }

        return numeros;
    }

    public static IEnumerable<(string Nome, double[] Numeros)> Todas(int quantos)
    {
        yield return ("mesma escala", MesmaEscala(quantos));
        yield return ("um grande e muitos pequenos", UmGrandeEMuitosPequenos(quantos));
        yield return ("escalas variadas", EscalasVariadas(quantos));
        yield return ("com cancelamento", ComCancelamento(quantos / 2));
    }
}
