using System;
using System.Collections.Generic;
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

		public Population(int generationId, int genomeCounter, Genome[] genomes)
		{
			GenerationID = generationId;
			GenomeCounter = genomeCounter;
			Genomes.AddRange(genomes);
		}

		public Result<List<Genome>> PopulateByIntializingGenomes(in OptimizationSettings settings, in NetworkInformation network)
		{
			List<Genome> newGenomes = [];
			for (int i = 0; i < settings.PopulationSize; i++)
			{
				var g = new Genome(GenomeCounter++, GenerationID, settings.UserOptimizationParameters, network);
				Genomes.Add(g);
				newGenomes.Add(g);
			}
			return Result<List<Genome>>.Success(newGenomes);
		}

		public Result<List<Genome>> PopulateByCreatingNewGenomes(in OptimizationSettings settings, in NetworkInformation network, string step = "")
		{			
			if (Genomes == null || Genomes.Count == 0)
			{
				return Result<List<Genome>>.Failure($"Current population empty, can't create new genomes. (GenerationID: {GenerationID})");
			}

			string[] birthmodes = ["crossing", "mutation"];
			List<Genome> newGenomes = [];
			var sortedGenomes = Genomes.OrderBy(g => g.FitnessValue).ToList();
			var random = new Random();

			for (int i = 1; i <= settings.PopulationSize; i++)
			{
				string birthMode = birthmodes[random.Next(0, 1)];
				Genome g;

				var tournamentResult = GAUtil.TournamentSelect(sortedGenomes, 3, random);
				if (tournamentResult.IsFailure)
				{
					return Result<List<Genome>>.Failure($"Process of populating when creating new genomes failed at crossing when tried TournamentSelect for parent_1, iteration: {i}, error: {tournamentResult.Error}");
				}

				switch (birthMode)
				{
					case "crossing":
						Genome parent_1 = tournamentResult.Value;
						tournamentResult = GAUtil.TournamentSelect(sortedGenomes, 3, random);
						if (tournamentResult.IsFailure)
						{
							return Result<List<Genome>>.Failure($"Process of populating when creating new genomes failed at crossing when tried TournamentSelect for parent_2, iteration: {i}, error: {tournamentResult.Error}");
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

			return Result<List<Genome>>.Success(newGenomes);
		}

		public Result<Population> ExtractNextPopulation(in OptimizationSettings settings)
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
		
	}
}
