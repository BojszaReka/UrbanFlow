using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Text;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.test_automater;

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
	}
}
