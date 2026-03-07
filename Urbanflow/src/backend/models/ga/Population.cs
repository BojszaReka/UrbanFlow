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
		public List<Genome> Genomes { get; set; }

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
			List<Genome> newGenomes = new List<Genome>();
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

			List<Genome> newGenomes = new List<Genome>();
			for (int i = 1; i <= settings.PopulationSize; i++)
			{
				// birthmode is random
				string birthMode = "";
				// choose on random: 1 or 2 genome
				Genome g;
				switch (birthMode)
				{
					case "crossing":
						// choose them somehow based on fitness value
						Genome parent_1 = null;
						Genome parent_2 = null;
						g = new Genome(GenomeCounter++, GenerationID + 1, parent_1, parent_2, settings.UserOptimizationParameters, step);
						break;
					case "mutation":
						Genome parent = null;
						g = new Genome(GenomeCounter++, GenerationID + 1, parent, settings.UserOptimizationParameters, step);
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
			Population nextPopulation = new Population(GenerationID + 1, GenomeCounter, newPopulationGenomes);
			return Result<Population>.Success(nextPopulation);
		}

		

	}
}
