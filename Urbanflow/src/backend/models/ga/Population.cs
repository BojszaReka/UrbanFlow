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
			List<Genome> newGenomes = [];
			for (int i = 0; i < settings.PopulationSize; i++)
			{
				var g = new Genome(GenomeCounter++, GenerationID, settings.UserOptimizationParameters, network);
				Genomes.Add(g);
				newGenomes.Add(g);
			}
			return Result<Genome>.Success(newGenomes.OrderBy(g => g.FitnessValue).First());
		}

		public Result<Genome> PopulateByCreatingNewGenomesNewWay(in OptimizationSettings settings, in NetworkInformation network, string step = "")
		{			
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<Genome>.Failure($"Current population empty, can't create new genomes. (GenerationID: {GenerationID})");
			}

			string[] birthmodes = ["crossing", "mutation"];
			List<Genome> newGenomes = [];
			var sortedGenomes = Genomes.OrderBy(g => g.FitnessValue).ToList();
			var random = new Random();
			double mutationptimizer = 0.25;
			double mutationIndex = 1;

			for (int i = 1; i <= settings.PopulationSize; i++)
			{
				var tournamentResult = GAUtil.TournamentSelect(sortedGenomes, 3, random);
				if (tournamentResult.IsFailure)
				{
					return Result<Genome>.Failure($"Process of populating when creating new genomes failed at crossing when tried TournamentSelect for parent_1, iteration: {i}, error: {tournamentResult.Error}");
				}

				int index = 0;
				if (0 < tournamentResult.Value.UnMetStopPercentage)
				{
					if (mutationIndex < 0)
					{
						mutationIndex = 1;
					}
					mutationIndex = mutationIndex - mutationptimizer;
					if (mutationIndex >= 0.5)
					{
						index = 1;
					}
				}
				else
				{
					index = Random.Shared.Next(0, 1);
				}
				string birthMode = birthmodes[random.Next(0, 1)];

				Genome g;

				switch (birthMode)
				{
					case "crossing":
						Genome parent_1 = tournamentResult.Value;
						tournamentResult = GAUtil.TournamentSelect(sortedGenomes, 3, random);
						if (tournamentResult.IsFailure)
						{
							return Result<Genome>.Failure($"Process of populating when creating new genomes failed at crossing when tried TournamentSelect for parent_2, iteration: {i}, error: {tournamentResult.Error}");
						}
						Genome parent_2 = tournamentResult.Value;
						g = new Genome(GenomeCounter++, GenerationID + 1, parent_1, parent_2, settings.UserOptimizationParameters, network, step);
						break;
					case "mutation":
						Genome parent = tournamentResult.Value;
						g = new Genome(GenomeCounter++, GenerationID + 1, parent, settings.UserOptimizationParameters, network, step);
						break;
					default:
						g = new Genome(GenomeCounter++, GenerationID + 1, settings.UserOptimizationParameters, network);
						break;
				}
				Genomes.Add(g);
				newGenomes.Add(g);
			}

			return Result<Genome>.Success(newGenomes.OrderBy(g => g.FitnessValue).First());
		}

		public Result<Population> ExtractNextPopulationNewWay(in OptimizationSettings settings)
		{
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<Population>.Failure($"Current population empty, can't extract next population (GenerationID: {GenerationID})");
			}
			IEnumerable<Genome[]> populationSplit = Genomes.OrderBy(g => g.FitnessValue).Chunk(settings.PopulationSize);
			var newPopulationGenomes = populationSplit.First();
			Population nextPopulation = new(GenerationID + 1, GenomeCounter, newPopulationGenomes);
			return Result<Population>.Success(nextPopulation);
		}

		internal Result<Genome> PopulateByCreatingNewGenomesOldWay(in Population previousPopulation, in OptimizationSettings settings, in NetworkInformation network, string step)
		{
			var GenomeList = previousPopulation.Genomes;
			if (GenomeList == null || GenomeList.Count == 0)
			{
				return Result<Genome>.Failure($"Current population empty, can't create new genomes. (GenerationID: {GenerationID})");
			}

			string[] birthmodes = ["crossing", "mutation"];
			List<Genome> newGenomes = [];
			var sortedGenomes = GenomeList.OrderBy(g => g.FitnessValue).ToList();
			var random = new Random();
			double mutationptimizer = 0.25;
			double mutationIndex = 1;

			for (int i = Genomes.Count; i <= settings.PopulationSize; i++)
			{
				var tournamentResult = GAUtil.TournamentSelect(sortedGenomes, 3, random);
				if (tournamentResult.IsFailure)
				{
					return Result<Genome>.Failure($"Process of populating when creating new genomes failed at crossing when tried TournamentSelect for parent_1, iteration: {i}, error: {tournamentResult.Error}");
				}

				int index = 0;
				if (0 < tournamentResult.Value.UnMetStopPercentage)
				{
					if (mutationIndex < 0)
					{
						mutationIndex = 1;
					}
					mutationIndex = mutationIndex - mutationptimizer;
					if (mutationIndex >= 0.5)
					{
						index = 1;
					}
				}
				else
				{
					index = Random.Shared.Next(0, 1);
				}
				string birthMode = birthmodes[random.Next(0, 1)];
				Genome g;

				switch (birthMode)
				{
					case "crossing":
						Genome parent_1 = tournamentResult.Value;
						tournamentResult = GAUtil.TournamentSelect(sortedGenomes, 3, random);
						if (tournamentResult.IsFailure)
						{
							return Result<Genome>.Failure($"Process of populating when creating new genomes failed at crossing when tried TournamentSelect for parent_2, iteration: {i}, error: {tournamentResult.Error}");
						}
						Genome parent_2 = tournamentResult.Value;
						g = new Genome(GenomeCounter++, GenerationID + 1, parent_1, parent_2, settings.UserOptimizationParameters, network, step);
						break;
					case "mutation":
						Genome parent = tournamentResult.Value;
						g = new Genome(GenomeCounter++, GenerationID + 1, parent, settings.UserOptimizationParameters, network, step);
						break;
					default:
						g = new Genome(GenomeCounter++, GenerationID + 1, settings.UserOptimizationParameters, network);
						break;
				}
				Genomes.Add(g);
				newGenomes.Add(g);
			}

			return Result<Genome>.Success(newGenomes.OrderBy(g => g.FitnessValue).First());
		}

		internal Result<Population> ExtractNextPopulationOldWay(in OptimizationSettings settings)
		{
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<Population>.Failure($"Current population empty, can't extract next population (GenerationID: {GenerationID})");
			}
			IEnumerable<Genome[]> populationSplit = Genomes.OrderBy(g => g.FitnessValue).Chunk(settings.PopulationSize / 10);
			var newPopulationGenomes = populationSplit.First();
			Population nextPopulation = new(GenerationID + 1, GenomeCounter, newPopulationGenomes);
			return Result<Population>.Success(nextPopulation);
		}

		public List<(int, int, double)> GatherFitnessValues()
		{
			List<(int, int, double)> fitnessValueList = [];
			foreach (Genome genome in Genomes)
			{
				fitnessValueList.Add((GenerationID, genome.GenomeID, genome.FitnessValue));
			}
			return fitnessValueList;
		}
	}
}
