using Conde.Precisao;

var pedido = args.Length > 0 ? args[0] : "tudo";
var tudo = pedido == "tudo";

if (tudo || pedido == "juiz") Juiz();
if (tudo || pedido == "soma") Somas();
if (tudo || pedido == "cancelamento") Cancelamentos();
if (tudo || pedido == "dinheiro") Dinheiros();

static void Juiz()
{
    Console.WriteLine();
    Console.WriteLine("  O QUE O COMPUTADOR REALMENTE GUARDA");
    Console.WriteLine();

    foreach (var (escrito, exato) in Medidas.Decimais())
        Console.WriteLine($"    {escrito,-12} {exato}");

    Console.WriteLine();
    Console.WriteLine($"    0.1 + 0.2 == 0.3 ?  {(0.1 + 0.2 == 0.3 ? "sim" : "nao")}");
    Console.WriteLine();

    Console.WriteLine("  O ESPACAMENTO ENTRE NUMEROS VIZINHOS");
    Console.WriteLine();
    Console.WriteLine("  perto de            espacamento   inteiros que faltam entre dois vizinhos");

    foreach (var l in Medidas.Espacamentos())
        Console.WriteLine($"  {l.Perto,-20:E2} {l.Espacamento,11:E2} {l.InteirosQueFaltam,38:N0}");

    Console.WriteLine();
    Console.WriteLine($"    acima de 2^53 = {Formato.LimiteDosInteiros:N0}, faltam inteiros");
    Console.WriteLine($"    2^53 + 1 == 2^53 ?  {(Formato.LimiteDosInteiros + 1 == Formato.LimiteDosInteiros ? "sim" : "nao")}");
    Console.WriteLine();
}

static void Somas()
{
    Console.WriteLine();
    Console.WriteLine("  AS QUATRO SOMAS CONTRA A SOMA EXATA (digitos decimais corretos)");
    Console.WriteLine();
    Console.WriteLine("  lista                          jeito         digitos corretos");

    var anterior = "";
    foreach (var l in Medidas.Somas())
    {
        if (l.Lista != anterior) { Console.WriteLine(); anterior = l.Lista; }

        Console.WriteLine($"  {l.Lista,-30} {l.Jeito,-13} {l.DigitosCorretos,16:F1}");
    }

    Console.WriteLine();
    Console.WriteLine("  O ERRO CRESCENDO COM O NUMERO DE PARCELAS (escalas variadas)");
    Console.WriteLine();
    Console.WriteLine("  parcelas      ingenua   em arvore   Kahan");

    foreach (var l in Medidas.Crescimentos())
        Console.WriteLine($"  {l.Parcelas,9:N0} {l.Ingenua,11:F1} {l.EmArvore,11:F1} {l.Kahan,7:F1}");

    Console.WriteLine();
}

static void Cancelamentos()
{
    Console.WriteLine();
    Console.WriteLine("  A FORMULA DE BHASKARA, DO JEITO DO LIVRO E REESCRITA");
    Console.WriteLine("  (raizes exatas: 10^k e 10^-k)");
    Console.WriteLine();
    Console.WriteLine("  raiz grande      do livro   reescrita   a raiz pequena, exata");

    foreach (var l in Medidas.Bhaskaras())
        Console.WriteLine($"  {l.C,-16:E1} {l.DoLivro,8:F1} {l.Estavel,11:F1}   {l.Certo}");

    Console.WriteLine();
    Console.WriteLine("  A VARIANCIA PELAS DUAS FORMULAS");
    Console.WriteLine();
    Console.WriteLine("  centro         uma passada   Welford   deu negativa");

    foreach (var l in Medidas.Variancias())
        Console.WriteLine($"  {l.Centro,-14:E1} {l.UmaPassada,11:F1} {l.Welford,9:F1} {(l.DeuNegativa ? "SIM" : "nao"),14}");

    Console.WriteLine();
}

static void Dinheiros()
{
    Console.WriteLine();
    Console.WriteLine("  O SALDO DE UM EXTRATO, EM TRES REPRESENTACOES");
    Console.WriteLine();
    Console.WriteLine("  lancamentos   diferenca do ponto flutuante (centavos)   decimal acertou");

    foreach (var l in Medidas.Dinheiros())
        Console.WriteLine($"  {l.Lancamentos,11:N0} {l.DiferencaEmCentavos,40} {(l.DecimalAcertou ? "sim" : "NAO"),17}");

    Console.WriteLine();
    Console.WriteLine("  O RATEIO: DIVIDIR SEM PERDER NEM CRIAR CENTAVO");
    Console.WriteLine();
    Console.WriteLine("  total (centavos)   pessoas   sobra com double   sobra com centavos");

    foreach (var l in Medidas.Rateios())
        Console.WriteLine($"  {l.Total,16:N0} {l.Pessoas,9} {l.SobraComDouble,18:N0} {l.SobraComCentavos,20:N0}");

    Console.WriteLine();
}
