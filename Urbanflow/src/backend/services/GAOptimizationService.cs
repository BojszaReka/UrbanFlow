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

		public Result<RunResults> RunGeneticAlgorithmNewWaySelection(string Descriptor)
		{
			//List<Population> Generations = [];
			List<Genome> BestGenomes = [];
			List<(int, int, double)> FitnessValuesPerGenerations = [];

			//string[] steps = ["route", "time"];
			string[] steps = ["route"];

			Result<Genome> result;
			var currentPopulation = new Population(1, 1); // first Population
			try
			{
				result = currentPopulation.PopulateByIntializingGenomes(OptimizationSettings, NetworkInformation);
				if (result.IsFailure)
				{
					return Result<RunResults>.Failure(result.Error);
				}
				FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());
			}
			catch (Exception e) {
				throw new Exception("Initializing genomes failed because: "+e.Message);
			}

			//generate the new population with the best 50% of the previous
			var newPopulation_result = currentPopulation.ExtractNextPopulationNewWay(OptimizationSettings);
			if (newPopulation_result.IsFailure)
			{
				return Result<RunResults>.Failure(newPopulation_result.Error);
			}
			Population previousPopulation = currentPopulation;
			currentPopulation = newPopulation_result.Value;

			foreach (var step in steps) {
				for (int i = 0; i < OptimizationSettings.IterationNumber; i++)
				{
					result = currentPopulation.PopulateByCreatingNewGenomesNewWay(previousPopulation, OptimizationSettings, NetworkInformation, step);
					if (result.IsFailure)
					{
						return Result<RunResults>.Failure(result.Error);
					}
					BestGenomes.AddRange(result.Value);
					//Generations.Add(currentPopulation);
					FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());

					newPopulation_result = currentPopulation.ExtractNextPopulationNewWay(OptimizationSettings);
					if (newPopulation_result.IsFailure)
					{
						return Result<RunResults>.Failure(newPopulation_result.Error);
					}
					previousPopulation = currentPopulation;
					currentPopulation = newPopulation_result.Value;
				}
				FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());
			}
			//Generations.Add(currentPopulation);

			RunResults runResults = new(BestGenomes, FitnessValuesPerGenerations, Descriptor);
			return Result<RunResults>.Success(runResults);
		}

		public Result<RunResults> RunGeneticAlgorithmOldWaySelection(string Descriptor)
		{
			//List<Population> Generations = [];
			List<Genome> BestGenomes = [];
			List<(int, int, double)> FitnessValuesPerGenerations = [];

			//string[] steps = ["route", "time"];
			string[] steps = ["route"];

			//create the first population
			var currentPopulation = new Population(1, 1); 
			//fill the population with new genomes
			var result = currentPopulation.PopulateByIntializingGenomes(OptimizationSettings, NetworkInformation);
			if (result.IsFailure)
			{
				return Result<RunResults>.Failure(result.Error);
			}
			//gather fitness values of genomes
			FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());

			//generate the new population with the best 10% of the previous
			var newPopulation_result = currentPopulation.ExtractNextPopulationOldWay(OptimizationSettings);
			if (newPopulation_result.IsFailure)
			{
				return Result<RunResults>.Failure(newPopulation_result.Error);
			}
			Population previousPopulation = currentPopulation;
			currentPopulation = newPopulation_result.Value;

			foreach (var step in steps)
			{
				for (int i = 0; i < OptimizationSettings.IterationNumber; i++)
				{
					//populate the rest of the population with the children of the previous population
					result = currentPopulation.PopulateByCreatingNewGenomesOldWay(previousPopulation, OptimizationSettings, NetworkInformation, step);
					if (result.IsFailure)
					{
						return Result<RunResults>.Failure(result.Error);
					}
					BestGenomes.AddRange(result.Value);
					//Generations.Add(currentPopulation);
					FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());

					//generate the new population with the best 10% of the previous
					newPopulation_result = currentPopulation.ExtractNextPopulationOldWay(OptimizationSettings);
					if (newPopulation_result.IsFailure)
					{
						return Result<RunResults>.Failure(newPopulation_result.Error);
					}
					previousPopulation = currentPopulation;
					currentPopulation = newPopulation_result.Value;
				}
				FitnessValuesPerGenerations.AddRange(currentPopulation.GatherFitnessValues());
			}
			//Generations.Add(currentPopulation);

			RunResults runResults = new(BestGenomes, FitnessValuesPerGenerations, Descriptor);
			return Result<RunResults>.Success(runResults);
		}
	}
}
