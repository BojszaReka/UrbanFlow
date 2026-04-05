using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class OptimizationParameters
	{
		//Genome parameters
		public int Genome_HubNumberInRoute { get; set; }
		public int Genome_RouteCount { get; internal set; }
		public bool Genome_AllowOneWayRoutes { get; internal set; }


		// For fitness calculation
		public int Fitness_RedundancyPercentParameter { get; set; }
		public int Fitness_RouteLengthParameter { get; set; }
		public int Fitness_MaximalAllowedChangeParameter { get; set; }
		public int Fitness_FleetCapacityParameter { get; set; }
		public int Fitness_MaximumWaitingMinutesParameter { get; set; }
		public int Fitness_MinimalWaitingMinutesParameter { get; set; }
		public double Fitness_MaximumTravelTimeParameter { get; set; }
	}
}
