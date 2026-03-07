using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class OptimizationSettings
	{
		public int PopulationSize { get; set; }
		public int IterationNumber { get; set; }
		public OptimizationParameters UserOptimizationParameters { get; set; }
	}
}
