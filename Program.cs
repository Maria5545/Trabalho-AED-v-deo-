#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// Representa uma transação financeira.
/// </summary>
class Transacao
{
    public int Id { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; }

    public override string ToString()
        => $"ID:{Id:D4} | R${Valor:F2} | {Data:dd/MM/yyyy} | {Status}";
}

/// <summary>
/// Algoritmos de ordenação adaptados para Transacao[] (chave: Valor, ordem crescente).
/// </summary>
static class Ordenacao
{
    // ---------------------------------------------------------------
    // INSERTION SORT  -  O(n²) médio/pior, O(n) melhor, estável, in-place
    // ---------------------------------------------------------------
    public static void InsertionSort(Transacao[] a)
    {
        for (int i = 1; i < a.Length; i++)
        {
            Transacao chave = a[i];
            int j = i - 1;

            while (j >= 0 && a[j].Valor > chave.Valor)
            {
                a[j + 1] = a[j];
                j--;
            }
            a[j + 1] = chave;
        }
    }

    // ---------------------------------------------------------------
    // MERGE SORT  -  O(n log n) em todos os casos, estável, O(n) de memória extra
    // ---------------------------------------------------------------
    public static void MergeSort(Transacao[] a)
    {
        if (a.Length < 2) return;
        Transacao[] aux = new Transacao[a.Length];
        MergeSort(a, aux, 0, a.Length - 1);
    }

    private static void MergeSort(Transacao[] a, Transacao[] aux, int esq, int dir)
    {
        if (esq >= dir) return;

        int meio = esq + (dir - esq) / 2;
        MergeSort(a, aux, esq, meio);
        MergeSort(a, aux, meio + 1, dir);
        Intercalar(a, aux, esq, meio, dir);
    }

    private static void Intercalar(Transacao[] a, Transacao[] aux, int esq, int meio, int dir)
    {
        // Já está ordenado? Evita trabalho desnecessário.
        if (a[meio].Valor <= a[meio + 1].Valor) return;

        Array.Copy(a, esq, aux, esq, dir - esq + 1);

        int i = esq;        // início da metade esquerda
        int j = meio + 1;   // início da metade direita
        int k = esq;        // posição de escrita

        while (i <= meio && j <= dir)
        {
            // <= garante a estabilidade (empate: esquerda primeiro)
            if (aux[i].Valor <= aux[j].Valor) a[k++] = aux[i++];
            else a[k++] = aux[j++];
        }
        while (i <= meio) a[k++] = aux[i++];
        while (j <= dir) a[k++] = aux[j++];
    }

    // ---------------------------------------------------------------
    // QUICK SORT  -  O(n log n) médio, O(n²) pior (raro), não estável, in-place
    // Pivô: mediana de três. Partição estilo Hoare (lida bem com valores repetidos).
    // Recursão só na menor partição => profundidade máxima O(log n).
    // ---------------------------------------------------------------
    public static void QuickSort(Transacao[] a)
    {
        if (a.Length < 2) return;
        QuickSort(a, 0, a.Length - 1);
    }

    private static void QuickSort(Transacao[] a, int esq, int dir)
    {
        while (esq < dir)
        {
            decimal pivo = MedianaDeTres(a, esq, dir);
            int i = esq, j = dir;

            while (i <= j)
            {
                while (a[i].Valor < pivo) i++;
                while (a[j].Valor > pivo) j--;

                if (i <= j)
                {
                    Troca(a, i, j);
                    i++;
                    j--;
                }
            }

            // Partições: [esq..j] e [i..dir]. Recursão na menor, laço na maior.
            if (j - esq < dir - i)
            {
                QuickSort(a, esq, j);
                esq = i;
            }
            else
            {
                QuickSort(a, i, dir);
                dir = j;
            }
        }
    }

    private static decimal MedianaDeTres(Transacao[] a, int esq, int dir)
    {
        decimal x = a[esq].Valor;
        decimal y = a[esq + (dir - esq) / 2].Valor;
        decimal z = a[dir].Valor;

        if (x > y) (x, y) = (y, x);
        if (y > z) (y, z) = (z, y);
        if (x > y) (x, y) = (y, x);
        return y;
    }

    private static void Troca(Transacao[] a, int i, int j)
    {
        Transacao tmp = a[i];
        a[i] = a[j];
        a[j] = tmp;
    }
}

class Program
{
    // Insertion Sort é O(n²): acima deste n ele levaria horas/dias.
    // Ajuste se quiser testar valores maiores (ex.: 1_000_000 leva muito tempo).
    const int LIMITE_INSERTION = 100_000;

    static readonly string[] Statuses = { "Aprovada", "Pendente", "Recusada", "Estornada" };

    static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        int[] tamanhos = { 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000 };

        Console.WriteLine("=== ORDENAÇÃO DE TRANSAÇÕES: Insertion x Merge x Quick ===\n");

        var resultados = new List<string>();
        resultados.Add($"{"n",15} | {"Insertion (ms)",16} | {"Merge (ms)",14} | {"Quick (ms)",14}");
        resultados.Add(new string('-', 68));

        foreach (int n in tamanhos)
        {
            Console.WriteLine($">>> Gerando dataset com n = {n:N0} ...");

            Transacao[] original;
            try
            {
                original = GerarDataset(n, seed: 42);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine($"    Memória insuficiente para n = {n:N0}. Pulando.\n");
                resultados.Add($"{n,15:N0} | {"sem memória",16} | {"sem memória",14} | {"sem memória",14}");
                continue;
            }

            string tIns = "pulado (O(n²))";
            string tMerge, tQuick;
            Transacao[] resultadoParaTop10;

            try
            {
                // --- Insertion Sort ---
                if (n <= LIMITE_INSERTION)
                {
                    Transacao[] copia = (Transacao[])original.Clone();
                    double ms = Medir(() => Ordenacao.InsertionSort(copia));
                    tIns = Formatar(ms, EstaOrdenado(copia));
                }

                // --- Merge Sort ---
                Transacao[] copiaMerge = (Transacao[])original.Clone();
                double msMerge = Medir(() => Ordenacao.MergeSort(copiaMerge));
                tMerge = Formatar(msMerge, EstaOrdenado(copiaMerge));

                // --- Quick Sort ---
                Transacao[] copiaQuick = (Transacao[])original.Clone();
                double msQuick = Medir(() => Ordenacao.QuickSort(copiaQuick));
                tQuick = Formatar(msQuick, EstaOrdenado(copiaQuick));

                resultadoParaTop10 = copiaQuick;
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine($"    Memória insuficiente durante a ordenação de n = {n:N0}.\n");
                resultados.Add($"{n,15:N0} | {"-",16} | {"sem memória",14} | {"sem memória",14}");
                original = null;
                GC.Collect();
                continue;
            }

            resultados.Add($"{n,15:N0} | {tIns,16} | {tMerge,14} | {tQuick,14}");

            Console.WriteLine($"    Tempos (ms) -> Insertion: {tIns} | Merge: {tMerge} | Quick: {tQuick}");

            ExibirTop10(resultadoParaTop10);

            // Libera memória antes do próximo tamanho
            original = null;
            resultadoParaTop10 = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine("\n=== RESUMO DOS TEMPOS DE EXECUÇÃO ===");
        foreach (string linha in resultados)
            Console.WriteLine(linha);

        Console.WriteLine("\n(✓ = array conferido e ordenado corretamente)");
    }

    /// <summary>Gera n transações aleatórias (seed fixa => resultados reproduzíveis).</summary>
    static Transacao[] GerarDataset(int n, int seed)
    {
        var rnd = new Random(seed);
        var dados = new Transacao[n];
        DateTime hoje = DateTime.Today;

        for (int i = 0; i < n; i++)
        {
            dados[i] = new Transacao
            {
                Id = i + 1,
                Valor = rnd.Next(1, 100_000_000) / 100m,       // R$ 0,01 a R$ 999.999,99
                Data = hoje.AddDays(-rnd.Next(0, 365)),
                Status = Statuses[rnd.Next(Statuses.Length)]   // reaproveita as strings (economiza memória)
            };
        }
        return dados;
    }

    static double Medir(Action acao)
    {
        Stopwatch sw = Stopwatch.StartNew();
        acao();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    static string Formatar(double ms, bool ok) => $"{ms:N2} {(ok ? "✓" : "✗")}";

    static bool EstaOrdenado(Transacao[] a)
    {
        for (int i = 1; i < a.Length; i++)
            if (a[i - 1].Valor > a[i].Valor) return false;
        return true;
    }

    /// <summary>Após ordenar em ordem crescente, as 10 últimas posições são as maiores.</summary>
    static void ExibirTop10(Transacao[] ordenado)
    {
        int qtd = Math.Min(10, ordenado.Length);
        Console.WriteLine($"    Top {qtd} maiores transações:");

        for (int i = 0; i < qtd; i++)
        {
            Transacao t = ordenado[ordenado.Length - 1 - i];
            Console.WriteLine($"    {i + 1,2}. {t}");
        }
        Console.WriteLine();
    }
}