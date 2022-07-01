using System.Diagnostics;
using Checkers.Core;

namespace Checkers.Genetic;

public class Simulation
{
    private const int NormalMaxPercentDeviation = 15;
    private const int NormalMaxFlatDeviation = 20;

    private const int ExtremeMaxPercentDeviation = 100;
    private const int ExtremeMaxFlatDeviation = 500;

    private const float ExtremeMutationChance = 0.075f;

    private const int MatchesPerGenome = 4;

    private const int DrawScore = 0;
    private const int DefeatScore = -100;
    private const int VictoryScore = 100;
    private const int VictoryBonusPerTurnBeforeLimit = 5;

    private readonly Random _random = new(234921871);

    private Action<SolverConfig>? _configurator;

    private int _currentReadyCount;
    private int _currentMatchCount;

    private void Shuffle<T>(IList<T> array)
    {
        var n = array.Count;
        while (n > 1)
        {
            var k = _random.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    public Generation NextGeneration(Generation generation)
    {
        _configurator = config =>
        {
            config.MaxSearchDepth = generation.GenerationRules.MaxSearchDepth;
            config.MaxEvaluationTime = generation.GenerationRules.MaxSearchTime;
        };

        _currentMatchCount = MatchesPerGenome * generation.Genomes.Count / 2;
        _currentReadyCount = 0;

        var (leftCompetitors, rightCompetitors) = PrepareCompetitors(generation.Genomes);

        for (var gameIndex = 0; gameIndex < MatchesPerGenome; gameIndex++)
        {
            SimulateAllPairs(leftCompetitors, rightCompetitors);

            rightCompetitors.Add(rightCompetitors[0]);
            rightCompetitors.RemoveAt(0);
        }

        Console.WriteLine();

        var competitors = leftCompetitors.Concat(rightCompetitors).ToList();
        var averagePlayTime = competitors.Average(comp => comp.PlayTime);
        var drawCount = competitors.Sum(comp => comp.Draws);
        var gameCount = MatchesPerGenome * competitors.Count;

        var survivors = EliminateWorstCompetitors(competitors);

        var nextGenerationGenomes = survivors.Select(comp => comp.Genome).ToArray();
        var children = Reproduce(nextGenerationGenomes);

        Console.WriteLine("Simulation completed. Average play time: {0:F2}. {1} draws out of {2} games.",
            averagePlayTime, drawCount, gameCount);

        return new Generation(generation.Id + 1, nextGenerationGenomes.Concat(children), generation.GenerationRules);
    }

    private static IEnumerable<Competitor> EliminateWorstCompetitors(IEnumerable<Competitor> competitors)
    {
        var orderedCompetitors = competitors.OrderByDescending(comp => comp.Score).ToList();
        return orderedCompetitors.GetRange(0, orderedCompetitors.Count / 2);
    }

    private (List<Competitor>, List<Competitor>) PrepareCompetitors(IReadOnlyCollection<Genome> genomes)
    {
        var competitorCount = genomes.Count;
        var competitors = genomes.Select(genome => new Competitor(genome)).ToArray();
        Shuffle(competitors);

        var leftCompetitors = competitors.Take(competitorCount / 2).ToList();
        var rightCompetitors = competitors.Skip(competitorCount / 2).ToList();
        return (leftCompetitors, rightCompetitors);
    }

    private IEnumerable<Genome> Reproduce(IList<Genome> parents)
    {
        IEnumerable<Genome> GenerateChildren()
        {
            var reproducingPairs = PairRandomly(parents);
            return reproducingPairs
                .Select(p => GenomeFactory.CombineGenomes(p.LeftGenome, p.RightGenome))
                .Select(child =>
                {
                    var extremeMutation = _random.NextSingle() < ExtremeMutationChance;
                    if (extremeMutation)
                    {
                        GenomeFactory.MutateGenome(child, ExtremeMaxPercentDeviation, ExtremeMaxFlatDeviation);
                    }
                    else
                    {
                        GenomeFactory.MutateGenome(child, NormalMaxPercentDeviation, NormalMaxFlatDeviation);
                    }

                    return child;
                });
        }

        for (var i = 0; i < 2; i++)
        {
            foreach (var child in GenerateChildren())
            {
                yield return child;
            }
        }
    }

    private IEnumerable<GenomePair> PairRandomly(IList<Genome> genomes)
    {
        Shuffle(genomes);

        for (var i = 0; i < genomes.Count; i += 2)
        {
            var whiteSolver = genomes[i];
            var blackSolver = genomes[i + 1];
            yield return new GenomePair
            {
                LeftGenome = whiteSolver,
                RightGenome = blackSolver
            };
        }
    }

    private void SimulateAllPairs(IEnumerable<Competitor> leftCompetitors, IEnumerable<Competitor> rightCompetitors)
    {
        var pairs = leftCompetitors.Zip(rightCompetitors)
            .Select(pair => new CompetingPair
            {
                LeftCompetitor = pair.First,
                RightCompetitor = pair.Second
            }).ToArray();

        var dummy = new object();

        Parallel.ForEach(pairs, pair =>
        {
            SimulatePair(pair);
            lock (dummy)
            {
                _currentReadyCount++;
                Console.Write("\rPlayed {0} matches out of {1}.", _currentReadyCount, _currentMatchCount);
            }
        });
    }

    private void SimulatePair(in CompetingPair pair)
    {
        var board = new Board();
        var whiteAi = CreateAi(board, pair.LeftCompetitor.Genome);
        var blackAi = CreateAi(board, pair.RightCompetitor.Genome);

        var startTime = Stopwatch.StartNew();
        var (result, turns) = PlayFullGame(board, whiteAi, blackAi);
        var passedTime = startTime.ElapsedMilliseconds / 1000f;

        switch (result)
        {
            case GameEndState.Draw:
                pair.LeftCompetitor.Draws++;
                pair.RightCompetitor.Draws++;
                pair.LeftCompetitor.Score += DrawScore;
                pair.RightCompetitor.Score += DrawScore;
                break;
            case GameEndState.WhiteWin:
                pair.LeftCompetitor.Score += VictoryScore;
                pair.LeftCompetitor.Score += (Board.MaxTurns - turns) * VictoryBonusPerTurnBeforeLimit;
                pair.RightCompetitor.Score += DefeatScore;
                break;
            case GameEndState.BlackWin:
                pair.LeftCompetitor.Score += DefeatScore;
                pair.RightCompetitor.Score += VictoryScore;
                pair.RightCompetitor.Score += (Board.MaxTurns - turns) * VictoryBonusPerTurnBeforeLimit;
                break;
        }

        pair.LeftCompetitor.PlayTime += passedTime;
        pair.RightCompetitor.PlayTime += passedTime;
    }

    private CheckersAi CreateAi(Board board, Genome genome)
    {
        var analyzer = new BoardHeuristicAnalyzer(genome.ToConfig());
        var solver = new BoardSolver(analyzer);
        if (_configurator is not null)
        {
            solver.Configure(_configurator);
        }

        return new CheckersAi(board, solver);
    }

    private static (GameEndState, int) PlayFullGame(Board board, CheckersAi whiteAi, CheckersAi blackAi)
    {
        GameEndState endState;
        while ((endState = board.GetGameEndState()) == GameEndState.None)
        {
            var player = board.CurrentTurn == PieceColor.White ? whiteAi : blackAi;
            var move = player.GetNextMove().Move;
            board.MakeMove(move);

            whiteAi.SelectMove(move);
            blackAi.SelectMove(move);
        }

        return (endState, board.TurnCount);
    }

    public Genome[] FindBestGenomes(Generation generation, int amount)
    {
        _currentReadyCount = 0;

        Genome[] PlayEliminationMatches(IReadOnlyCollection<Genome> genomes)
        {
            var (left, right) = PrepareCompetitors(genomes);
            SimulateAllPairs(left, right);
            return left.Zip(right)
                .Select(pair =>
                {
                    if (pair.First.Score > pair.Second.Score)
                    {
                        return pair.First;
                    }

                    if (pair.First.Score < pair.Second.Score)
                    {
                        return pair.Second;
                    }

                    return _random.Next() % 2 == 0 ? pair.First : pair.Second;
                })
                .Select(comp => comp.Genome)
                .ToArray();
        }

        var participants = generation.Genomes.ToArray();

        var log2 = MathF.Log2(participants.Length);
        var nearestPowerOfTwo = (int)MathF.Pow(2, (int)log2);
        if (nearestPowerOfTwo != participants.Length)
        {
            Shuffle(participants);
            participants = participants.Take(nearestPowerOfTwo).ToArray();
        }

        _currentMatchCount = participants.Length - 1;

        while (participants.Length / 2 >= amount)
        {
            participants = PlayEliminationMatches(participants);
        }

        return participants.Take(amount).ToArray();
    }

    private struct GenomePair
    {
        public Genome LeftGenome { get; init; }
        public Genome RightGenome { get; init; }
    }

    private readonly struct CompetingPair
    {
        public Competitor LeftCompetitor { get; init; }
        public Competitor RightCompetitor { get; init; }
    }

    private class Competitor
    {
        public Competitor(Genome genome)
        {
            Genome = genome;
        }

        public readonly Genome Genome;

        public int Score { get; set; }
        public int Draws { get; set; }
        public float PlayTime { get; set; }
    }
}