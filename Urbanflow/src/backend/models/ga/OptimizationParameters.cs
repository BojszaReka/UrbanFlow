using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class OptimizationParameters
	{
		//Genome parameters
		public int Genome_HubNumberInRoute { get; set; }


		// For fitness calculation
		public int Fitness_RedundancyPercentParameter { get; set; }
		public int Fitness_RouteLengthParameter { get; set; }
		public int Fitness_MaximalAllowedChangeParameter { get; set; }
		public int Fitness_FleetCapacityParameter { get; set; }
		public int Fitness_PreferedWaitingMinutesParameter { get; set; }
		public int Genome_RouteCount { get; internal set; }
	}
}
