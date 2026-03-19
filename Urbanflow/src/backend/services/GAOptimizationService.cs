using System;
using System.Collections.Generic;
using System.Text;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.test_automater;

namespace Urbanflow.src.backend.services
{
	public class GAOptimizationService
	{
		public NetworkInformation NetworkInformation {  get; }
		public OptimizationSettings OptimizationSettings { get; }
		public GAStatistics StatisticsCollector { get; }

		public GAOptimizationService(in NetworkInformation networkInformation, in OptimizationSettings optimizationSettings)
		{
			NetworkInformation = networkInformation;
			OptimizationSettings = optimizationSettings;
		}

		public GAOptimizationService(in NetworkInformation networkInformation, in OptimizationSettings settings, in GAStatistics statisticsCollector)
		{
			NetworkInformation = networkInformation;
			OptimizationSettings = settings;
			StatisticsCollector = statisticsCollector;
		}

		public Result<RunResults> RunGeneticAlgorithm(string Descriptor)
		{
			List<Population> Generations = new List<Population>();
			List<Genome> AllGeneratedGenomes = new List<Genome>();

			string[] steps = { "route", "time" };

			var currentPopulation = new Population(1, 1); // first Population
			var result = currentPopulation.PopulateByIntializingGenomes(OptimizationSettings, NetworkInformation);
			if (result.IsFailure)
			{
				return Result<RunResults>.Failure(result.Error);
			}
			AllGeneratedGenomes.AddRange(result.Value);

			int iteration = OptimizationSettings.IterationNumber;

			foreach (var step in steps) {
				for (int i = 0; i < OptimizationSettings.IterationNumber; i++)
				{
					result = currentPopulation.PopulateByCreatingNewGenomes(OptimizationSettings, NetworkInformation, step);
					if (result.IsFailure)
					{
						return Result<RunResults>.Failure(result.Error);
					}
					AllGeneratedGenomes.AddRange(result.Value);
					Generations.Add(currentPopulation);

					var newPopulation_result = currentPopulation.ExtractNextPopulation(OptimizationSettings);
					if (newPopulation_result.IsFailure)
					{
						return Result<RunResults>.Failure(newPopulation_result.Error);
					}
					currentPopulation = newPopulation_result.Value;
				}
			}
			Generations.Add(currentPopulation);

			RunResults runResults = new RunResults(Generations, AllGeneratedGenomes, Descriptor);
			return Result<RunResults>.Success(runResults);
		}
	}
}
