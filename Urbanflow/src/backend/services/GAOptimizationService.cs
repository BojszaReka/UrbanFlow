using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.util;
using static Urbanflow.src.backend.models.util.Log;

namespace Urbanflow.src.backend.services
{
	public class GAOptimizationService(in NetworkInformation networkInformation, in OptimizationSettings optimizationSettings)
	{
		public NetworkInformation NetworkInformation { get; } = networkInformation;
		public OptimizationSettings OptimizationSettings { get; } = optimizationSettings;

		public Result<RunResults> RunGeneticAlgorithm(string Descriptor, bool doItOldWay = false)
		{
			List<Genome> BestGenomes = [];
			List<(int gen, (double best, double avg, double worst))> FitnessValuesPerGenerations = [];
			Result<Genome> result;

			int takeCount = (int)((double)OptimizationSettings.PopulationSize * 0.35);
			if (doItOldWay)
			{
				takeCount = OptimizationSettings.PopulationSize / 10;
			}
				

			var currentPopulation = new Population(1, 1); // first Population
			try
			{
				result = currentPopulation.PopulateByIntializingGenomes(OptimizationSettings, NetworkInformation);
				if (result.IsFailure)
				{
					return Result<RunResults>.Failure(result.Error);
				}
				BestGenomes.Add(result.Value);
				FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());
			}
			catch (Exception e)
			{
				return Result<RunResults>.Failure("Initializing genomes failed because: " + e.Message);
			}

			var newPopulation_result = currentPopulation.ExtractNextPopulation(takeCount);
			if (newPopulation_result.IsFailure)
			{
				return Result<RunResults>.Failure(newPopulation_result.Error);
			}
			Population previousPopulation = currentPopulation;
			currentPopulation = newPopulation_result.Value;

			foreach (string step in new List<string>(["route", "time"]))
			{
				for (int i = 0; i < OptimizationSettings.IterationNumber; i++)
				{
					result = currentPopulation.PopulateByCreatingNewGenomes(previousPopulation, OptimizationSettings, NetworkInformation, step, doItOldWay);
					if (result.IsFailure)
					{
						return Result<RunResults>.Failure(result.Error);
					}
					BestGenomes.Add(result.Value);
					FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());

					newPopulation_result = currentPopulation.ExtractNextPopulation(takeCount);
					if (newPopulation_result.IsFailure)
					{
						return Result<RunResults>.Failure(newPopulation_result.Error);
					}
					previousPopulation = currentPopulation;
					currentPopulation = newPopulation_result.Value;
				}
				if (step == "route")
				{
					var popResult = previousPopulation.ExtractNextPopulationForTimeOptimization(OptimizationSettings.PopulationSize, OptimizationSettings.UserOptimizationParameters, NetworkInformation);
					if(popResult.IsFailure)
						return Result<RunResults>.Failure(popResult.Error);
					previousPopulation = popResult.Value;

					popResult = previousPopulation.ExtractNextPopulation(takeCount);
					if (popResult.IsFailure)
						return Result<RunResults>.Failure(popResult.Error);
					currentPopulation = popResult.Value;
				}
			}

			RunResults runResults = new(BestGenomes, FitnessValuesPerGenerations, Descriptor);
			return Result<RunResults>.Success(runResults);
		}

		public Result<RunResults> RunGeneticAlgorithmWithLogging(string Descriptor, bool doItOldWay = false)
		{
			OptimizationLoggerService.Instance.Log($"Starting GA run: {Descriptor}");

			OptimizationLoggerService.Instance.Log($"Using the following settings: \n{OptimizationSettings.ToString()}");


			List<Genome> BestGenomes = [];
			List<(int gen, (double best, double avg, double worst))> FitnessValuesPerGenerations = [];
			Result<Genome> result;

			int takeCount = (int)((double)OptimizationSettings.PopulationSize * 0.35);
			if (doItOldWay)
			{
				takeCount = OptimizationSettings.PopulationSize / 10;
				OptimizationLoggerService.Instance.Log("Using legacy GA mode", LogLevel.Warning);
			}


			var currentPopulation = new Population(1, 1); // first Population
			try
			{
				OptimizationLoggerService.Instance.Log("Initializing first population...");
				result = currentPopulation.PopulateByIntializingGenomes(OptimizationSettings, NetworkInformation);
				if (result.IsFailure)
				{
					OptimizationLoggerService.Instance.Log($"Initialization failed: {result.Error}", LogLevel.Error);
					return Result<RunResults>.Failure(result.Error);
				}
				OptimizationLoggerService.Instance.Log("Initial population created", LogLevel.Success);
				BestGenomes.Add(result.Value);
				FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());
			}
			catch (Exception e)
			{
				OptimizationLoggerService.Instance.Log($"Exception during initialization: {e.Message}", LogLevel.Error);
				return Result<RunResults>.Failure("Initializing genomes failed because: " + e.Message);
			}

			var newPopulation_result = currentPopulation.ExtractNextPopulation(takeCount);
			if (newPopulation_result.IsFailure)
			{
				OptimizationLoggerService.Instance.Log($"Failed to extract next population: {newPopulation_result.Error}", LogLevel.Error);
				return Result<RunResults>.Failure(newPopulation_result.Error);
			}
			Population previousPopulation = currentPopulation;
			currentPopulation = newPopulation_result.Value;

			foreach (string step in new List<string>(["route", "time"]))
			{
				OptimizationLoggerService.Instance.Log($"Starting optimization step: {step}");

				for (int i = 0; i < OptimizationSettings.IterationNumber; i++)
				{
					OptimizationLoggerService.Instance.Log($"Generation {i + 1}/{OptimizationSettings.IterationNumber} ({step})");

					result = currentPopulation.PopulateByCreatingNewGenomes(previousPopulation, OptimizationSettings, NetworkInformation, step, doItOldWay);
					if (result.IsFailure)
					{
						OptimizationLoggerService.Instance.Log($"Generation failed: {result.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(result.Error);
					}
					BestGenomes.Add(result.Value);
					FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());

					newPopulation_result = currentPopulation.ExtractNextPopulation(takeCount);
					if (newPopulation_result.IsFailure)
					{
						OptimizationLoggerService.Instance.Log($"Population extraction failed: {newPopulation_result.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(newPopulation_result.Error);
					}
					previousPopulation = currentPopulation;
					currentPopulation = newPopulation_result.Value;
				}
				if (step == "route")
				{
					OptimizationLoggerService.Instance.Log("Switching to time optimization phase...");

					var popResult = previousPopulation.ExtractNextPopulationForTimeOptimization(OptimizationSettings.PopulationSize, OptimizationSettings.UserOptimizationParameters, NetworkInformation);
					if (popResult.IsFailure){
						OptimizationLoggerService.Instance.Log($"Time optimization prep failed: {popResult.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(popResult.Error); 
					}
					previousPopulation = popResult.Value;

					popResult = previousPopulation.ExtractNextPopulation(takeCount);
					if (popResult.IsFailure){
						OptimizationLoggerService.Instance.Log($"Population extraction failed: {popResult.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(popResult.Error); 
					}
					currentPopulation = popResult.Value;
				}
			}

			OptimizationLoggerService.Instance.Log("GA run completed successfully", LogLevel.Success);

			RunResults runResults = new(BestGenomes, FitnessValuesPerGenerations, Descriptor);
			return Result<RunResults>.Success(runResults);
		}
		public Result<RunResults> RunGeneticAlgorithmWithLogging(string Descriptor, CancellationToken token, bool doItOldWay = false)
		{
			OptimizationLoggerService.Instance.Log($"Starting GA run: {Descriptor}");

			OptimizationLoggerService.Instance.Log($"Using the following settings: \n{OptimizationSettings.ToString()}");


			List<Genome> BestGenomes = [];
			List<(int gen, (double best, double avg, double worst))> FitnessValuesPerGenerations = [];
			Result<Genome> result;

			int takeCount = OptimizationSettings.PopulationSize / 10;
			if (doItOldWay)
			{
				OptimizationLoggerService.Instance.Log("Using legacy GA mode", LogLevel.Warning);
			}


			var currentPopulation = new Population(1, 1);
			try
			{
				OptimizationLoggerService.Instance.Log("Initializing first population...");
				result = currentPopulation.PopulateByIntializingGenomes(OptimizationSettings, NetworkInformation);
				if (result.IsFailure)
				{
					OptimizationLoggerService.Instance.Log($"Initialization failed: {result.Error}", LogLevel.Error);
					return Result<RunResults>.Failure(result.Error);
				}
				OptimizationLoggerService.Instance.Log("Initial population created", LogLevel.Success);
				BestGenomes.Add(result.Value);
				FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());
			}
			catch (Exception e)
			{
				OptimizationLoggerService.Instance.Log($"Exception during initialization: {e.Message}", LogLevel.Error);
				return Result<RunResults>.Failure("Initializing genomes failed because: " + e.Message);
			}

			var newPopulation_result = currentPopulation.ExtractNextPopulation(takeCount);
			if (newPopulation_result.IsFailure)
			{
				OptimizationLoggerService.Instance.Log($"Failed to extract next population: {newPopulation_result.Error}", LogLevel.Error);
				return Result<RunResults>.Failure(newPopulation_result.Error);
			}
			Population previousPopulation = currentPopulation;
			currentPopulation = newPopulation_result.Value;

			foreach (string step in new List<string>(["route", "time"]))
			{
				OptimizationLoggerService.Instance.Log($"Starting optimization step: {step}");

				for (int i = 0; i < OptimizationSettings.IterationNumber && !token.IsCancellationRequested; i++)
				{

					OptimizationLoggerService.Instance.Log($"Generation {i + 1}/{OptimizationSettings.IterationNumber} ({step})");

					result = currentPopulation.PopulateByCreatingNewGenomes(previousPopulation, OptimizationSettings, NetworkInformation, step, doItOldWay);
					if (result.IsFailure)
					{
						OptimizationLoggerService.Instance.Log($"Generation failed: {result.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(result.Error);
					}
					BestGenomes.Add(result.Value);
					FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());

					newPopulation_result = currentPopulation.ExtractNextPopulation(takeCount);
					if (newPopulation_result.IsFailure)
					{
						OptimizationLoggerService.Instance.Log($"Population extraction failed: {newPopulation_result.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(newPopulation_result.Error);
					}
					previousPopulation = currentPopulation;
					currentPopulation = newPopulation_result.Value;
				}
				if (step == "route" && !token.IsCancellationRequested)
				{

					OptimizationLoggerService.Instance.Log("Switching to time optimization phase...");

					var popResult = previousPopulation.ExtractNextPopulationForTimeOptimization(OptimizationSettings.PopulationSize, OptimizationSettings.UserOptimizationParameters, NetworkInformation);
					if (popResult.IsFailure){
						OptimizationLoggerService.Instance.Log($"Time optimization prep failed: {popResult.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(popResult.Error); 
					}
					previousPopulation = popResult.Value;

					popResult = previousPopulation.ExtractNextPopulation(takeCount);
					if (popResult.IsFailure){
						OptimizationLoggerService.Instance.Log($"Population extraction failed: {popResult.Error}", LogLevel.Error);
						return Result<RunResults>.Failure(popResult.Error); 
					}
					currentPopulation = popResult.Value;
				}
			}

			if (token.IsCancellationRequested)
				return Result<RunResults>.Failure("Genetic Algorithm cancelled");

			OptimizationLoggerService.Instance.Log("GA run completed successfully", LogLevel.Success);

			RunResults runResults = new(BestGenomes, FitnessValuesPerGenerations, Descriptor);
			return Result<RunResults>.Success(runResults);
		}
	}
}
