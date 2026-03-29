using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.ga
{
	// hold a pupolation of genomes with their children
	public class Population
	{
		public int GenerationID { get; set; }
		public List<Genome> Genomes { get; set; } = [];
		

		//helper values
		private int GenomeCounter { get; set; } = 0;
		public List<Genome> NewGenomes { get; set; } = [];

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
					Genomes.Add(
						new Genome(
							GenomeCounter++,
							GenerationID,
							settings.UserOptimizationParameters,
							network));
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

			Genome best = Genomes[0];
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

			return Result<Genome>.Success(best);
		}

		public Result<Genome> PopulateByCreatingNewGenomesNewWay(in Population previousPopulation, in OptimizationSettings settings, in NetworkInformation network, string step = "")
		{
			var GenomeList = previousPopulation.Genomes;
			if (GenomeList == null || GenomeList.Count == 0)
			{
				return Result<Genome>.Failure($"Previous population is empty, can't create new genomes. (GenerationID: {GenerationID})");
			}
			var sortedGenomes = GenomeList.OrderBy(g => g.FitnessValue).ToList();

			for (int i = 1; i <= settings.PopulationSize; i++)
			{
				CreateNewGenomeControlledMutation(sortedGenomes, settings, network, step);
			}

			Genomes.AddRange(NewGenomes);
			Genome bestGenome = Genomes[0];
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

			return Result<Genome>.Success(bestGenome);
		}		

		public Result<Population> ExtractNextPopulationNewWay(in OptimizationSettings settings)
		{
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<Population>.Failure(
					$"Current population empty, can't extract next population (GenerationID: {GenerationID})");
			}

			int takeCount = (int)((double)settings.PopulationSize * 0.35);

			// Only take what you actually need (no Chunk, no extra arrays)
			var bestGenomes = Genomes
				.OrderBy(g => g.FitnessValue)
				.Take(takeCount)
				.ToArray();

			var nextPopulation = new Population(
				GenerationID + 1,
				GenomeCounter,
				bestGenomes);

			return Result<Population>.Success(nextPopulation);
		}

		internal Result<Genome> PopulateByCreatingNewGenomesOldWay(in Population previousPopulation, in OptimizationSettings settings, in NetworkInformation network, string step)
		{
			var GenomeList = previousPopulation.Genomes;
			if (GenomeList == null || GenomeList.Count == 0)
			{
				return Result<Genome>.Failure($"Previous population is empty, can't create new genomes. (GenerationID: {GenerationID})");
			}

			var sortedGenomes = GenomeList.OrderBy(g => g.FitnessValue).ToList();

			for (int i = Genomes.Count; i <= settings.PopulationSize; i++)
			{
				CreateNewGenome(sortedGenomes, settings, network, step);
			}

			Genomes.AddRange(NewGenomes);
			Genome bestGenome = Genomes[0];
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
			return Result<Genome>.Success(bestGenome);
		}

		internal Result<Population> ExtractNextPopulationOldWay(in OptimizationSettings settings)
		{
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<Population>.Failure(
					$"Current population empty, can't extract next population (GenerationID: {GenerationID})");
			}

			int takeCount = settings.PopulationSize / 10;

			// Only take what you actually need, then materialize once
			var bestGenomes = Genomes
				.OrderBy(g => g.FitnessValue)
				.Take(takeCount)
				.ToArray();

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
				: random.Next(2)==1;

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

			NewGenomes.Add(g);
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

			NewGenomes.Add(g);
		}

		public List<(int, int, double)> GatherFitnessValues()
		{
			var fitnessValueList = new List<(int, int, double)>(Genomes.Count);

			foreach (var genome in Genomes)
			{
				fitnessValueList.Add((GenerationID, genome.GenomeID, genome.FitnessValue));
			}

			return fitnessValueList;
		}
	}
}
