using Checkers.Core;
using Checkers.Genetic;

BoardSolverMemoryAllocator.DisablePreAllocation();

var simulation = new Simulation();

var simulations = 100;
var generation = GenerationStorage.LoadLastOrCreateNew();

var shouldStop = false;
Console.CancelKeyPress += (_, args) =>
{
    if (shouldStop)
    {
        args.Cancel = false;
        return;
    }

    Console.WriteLine("Waiting for current simulation to finish...");
    shouldStop = true;

    args.Cancel = true;
};

for (var i = 0; i < simulations; i++)
{
    Console.WriteLine($"Simulating generation {generation.Id}...");
    generation = simulation.NextGeneration(generation);
    GenerationStorage.SaveGeneration(generation);

    if (shouldStop)
    {
        Console.WriteLine("Stopping simulation");
        break;
    }
}