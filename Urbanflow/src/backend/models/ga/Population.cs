using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.backend.models.ga
{
	// hold a pupolation of genomes with their children
	public class Population
	{
		public int GenerationID { get; set; }
		public List<Genome> Genomes { get; set; } = [];


		//helper values
		private int GenomeCounter { get; set; } = 0;

		public Population(int generationId, int genomeCounter)
		{
			GenerationID = generationId;
			GenomeCounter = genomeCounter;
		}

		public Population(int generationId, int genomeCounter, in Genome[] genomes)
		{
			GenerationID = generationId;
			GenomeCounter = genomeCounter;
			Genomes.AddRange(genomes);
		}

		public Result<Genome> PopulateByIntializingGenomes(in OptimizationSettings settings, in NetworkInformation network)
		{
			int target = settings.PopulationSize;
			int failures = 0;

			for (int i = 0; i < target && failures < target; i++)
			{
				try
				{
					Genomes.Add(new Genome(GenomeCounter++, GenerationID, settings.UserOptimizationParameters, network));
				}
				catch
				{
					failures++;
				}
			}

			if (failures >= target)
			{
				return Result<Genome>.Failure($"Initializing genomes failed, failure threshold exceeded. Successfully initialized: {Genomes.Count}");
			}

			var best = Genomes[0];
			double bestFitness = best.FitnessValue;

			for (int i = 1; i < Genomes.Count; i++)
			{
				var g = Genomes[i];
				if (g.FitnessValue < bestFitness)
				{
					bestFitness = g.FitnessValue;
					best = g;
				}
			}
			best.GenerationID = GenerationID;
			OptimizationLoggerService.Instance.Log($"Current best genome: {best}");
			return Result<Genome>.Success(best);
		}

		public Result<Genome> PopulateByCreatingNewGenomes(in Population previousPopulation, in OptimizationSettings settings, in NetworkInformation network, string step, bool doItOldWay = false)
		{
			if (previousPopulation.Genomes == null || previousPopulation.Genomes.Count == 0)
			{
				return Result<Genome>.Failure($"Previous population is empty, can't create new genomes. (GenerationID: {GenerationID})");
			}
			var sortedGenomes = previousPopulation.Genomes.OrderBy(g => g.FitnessValue).ToList();

			if (doItOldWay)
			{
				for (int i = Genomes.Count; i <= settings.PopulationSize; i++)
				{
					CreateNewGenome(sortedGenomes, settings, network, step);
				}
			}
			else
			{
				for (int i = 1; i <= settings.PopulationSize; i++)
				{
					CreateNewGenomeControlledMutation(sortedGenomes, settings, network, step);
				}
			}

			var bestGenome = Genomes[0];
			double bestFitness = bestGenome.FitnessValue;

			for (int i = 1; i < Genomes.Count; i++)
			{
				var g = Genomes[i];
				if (g.FitnessValue < bestFitness)
				{
					bestFitness = g.FitnessValue;
					bestGenome = g;
				}
			}

			bestGenome.GenerationID = GenerationID;
			OptimizationLoggerService.Instance.Log($"Current best genome: {bestGenome}");
			return Result<Genome>.Success(bestGenome);
		}

		public Result<Population> ExtractNextPopulation(int takeCount)
		{
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<Population>.Failure(
					$"Current population empty, can't extract next population (GenerationID: {GenerationID})");
			}

			var bestGenomes = Genomes.OrderBy(g => g.FitnessValue).Take(takeCount).ToArray();

			var nextPopulation = new Population(
				GenerationID + 1,
				GenomeCounter,
				bestGenomes);

			return Result<Population>.Success(nextPopulation);
		}

		public void CreateNewGenomeControlledMutation(in List<Genome> sortedParentGenomes, in OptimizationSettings settings, in NetworkInformation network, string step = "")
		{
			var random = Random.Shared;

			var tournamentResult = GAUtil.TournamentSelect(sortedParentGenomes, 5, random);
			if (tournamentResult.IsFailure)
				throw new Exception($"TournamentSelect failed for parent_1: {tournamentResult.Error}");

			var parent1 = tournamentResult.Value;

			bool useCrossing = parent1.UnMetStopPercentage > 0.0
				? random.Next(5) > 1
				: random.Next(2) == 1;

			Genome g;

			if (useCrossing)
			{
				var tournamentResult2 = GAUtil.TournamentSelect(sortedParentGenomes, 5, random);
				if (tournamentResult2.IsFailure)
					throw new Exception($"TournamentSelect failed for parent_2: {tournamentResult2.Error}");

				var parent2 = tournamentResult2.Value;

				g = new Genome(
					GenomeCounter++,
					GenerationID + 1,
					parent1,
					parent2,
					settings.UserOptimizationParameters,
					network,
					step);
			}
			else
			{
				g = new Genome(
					GenomeCounter++,
					GenerationID + 1,
					parent1,
					settings.UserOptimizationParameters,
					network,
					step);
			}

			Genomes.Add(g);
		}

		public void CreateNewGenome(in List<Genome> sortedParentGenomes, in OptimizationSettings settings, in NetworkInformation network, string step = "")
		{
			var random = Random.Shared;

			var tournamentResult = GAUtil.TournamentSelect(sortedParentGenomes, 5, random);
			if (tournamentResult.IsFailure)
				throw new Exception($"TournamentSelect failed for parent_1: {tournamentResult.Error}");

			var parent1 = tournamentResult.Value;

			// 50/50 choice without allocations or strings
			bool useCrossing = random.Next(2) == 0;

			Genome g;

			if (useCrossing)
			{
				var tournamentResult2 = GAUtil.TournamentSelect(sortedParentGenomes, 5, random);
				if (tournamentResult2.IsFailure)
					throw new Exception($"TournamentSelect failed for parent_2: {tournamentResult2.Error}");

				var parent2 = tournamentResult2.Value;

				g = new Genome(
					GenomeCounter++,
					GenerationID + 1,
					parent1,
					parent2,
					settings.UserOptimizationParameters,
					network,
					step);
			}
			else
			{
				g = new Genome(
					GenomeCounter++,
					GenerationID + 1,
					parent1,
					settings.UserOptimizationParameters,
					network,
					step);
			}

			Genomes.Add(g);
		}

		public (int, (double, double, double)) GatherFitnessValues()
		{

			var sum = 0.0;
			double best = double.MaxValue;
			double worst = double.MinValue;

			foreach (var genome in Genomes)
			{
				sum += genome.FitnessValue;
				if (genome.FitnessValue > worst)
				{
					worst = genome.FitnessValue;
				}
				if (genome.FitnessValue < best)
				{
					best = genome.FitnessValue;
				}
			}

			var avg = sum / (double)Genomes.Count;
			OptimizationLoggerService.Instance.Log($"Fitness values for generation: {GenerationID}: best={best}, avarage={avg}, worst={worst}");
			return (GenerationID, (best, avg, worst));
		}

		internal void SetUpForTimeOptimization(in OptimizationParameters parameters, in NetworkInformation network)
		{
			foreach (var genome in Genomes)
			{
				genome.FillTimePropertiesOfRoutes(parameters, network);
			}
		}

		internal Result<Population> ExtractNextPopulationForTimeOptimization(int genomeCount, in OptimizationParameters parameters, in NetworkInformation network)
		{
			Genome bestGenome = null;
			double bestFitness = double.MaxValue;
			foreach (var genome in Genomes) { 
				if(genome.FitnessValue < bestFitness)
				{
					bestFitness = genome.FitnessValue;
					bestGenome = genome;
				}
			}

			if(bestGenome != null)
			{
				List<Genome> newGenomeList = [];
				for (int i = 0; i < genomeCount; i++)
				{
					newGenomeList.Add(bestGenome.FillTimePropertiesOfRoutes(parameters, network));
				}
				var nextPopulation = new Population(
					GenerationID + 1,
					GenomeCounter,
					[.. newGenomeList]);

				return Result<Population>.Success(nextPopulation);
			}

			return Result<Population>.Failure("Extract Next Population For Time Optimization failed, because no best genome was found");
		}
	}
}
