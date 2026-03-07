using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class RunResults
	{
		public string RunDescriptor { get; set; }
		public List<Population> Generations { get; set; }
		public List<Genome> AllGeneratedGenomes { get; set; }

		public RunResults(in List<Population> generations, in List<Genome> genomes, string descriptor)
		{
			Generations = generations;
			AllGeneratedGenomes = genomes;
			RunDescriptor = descriptor;
		}
	}
}
