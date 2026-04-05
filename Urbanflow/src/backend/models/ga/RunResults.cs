using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class RunResults(in List<Genome> genomes, in List<(int, (double, double, double))> fitnessValuesPerGenerations, string descriptor)
	{
		public string RunDescriptor { get; } = descriptor;
		public List<Genome> BestGeneratedGenomes { get; } = genomes;
		public List<(int, (double, double, double))> FitnessValuesPerGenerations { get; } = fitnessValuesPerGenerations; //min, avg, max
	}
}
