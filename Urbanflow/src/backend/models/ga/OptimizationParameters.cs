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
		public int Genome_OneWayRoutePercentageTreshold { get; internal set; }


		// For fitness calculation
		public int Fitness_RedundancyPercentParameter { get; set; }
		public int Fitness_RouteLengthParameter { get; set; }
		public int Fitness_MinimumRouteLengthParameter { get; set; }
		public int Fitness_MaximalAllowedChangeParameter { get; set; }
		public int Fitness_FleetCapacityParameter { get; set; }
		public int Fitness_MaximumWaitingMinutesParameter { get; set; }
		public int Fitness_MinimalWaitingMinutesParameter { get; set; }
		public double Fitness_MaximumTravelTimeParameter { get; set; }

		public override string ToString()
		{
			return $"Genome Parameters:\n" +
				   $"- HubNumberInRoute: {Genome_HubNumberInRoute}\n" +
				   $"- RouteCount: {Genome_RouteCount}\n" +
				   $"- AllowOneWayRoutes: {Genome_AllowOneWayRoutes}\n" +
				   $"- OneWayRoutePercentageTreshold: {Genome_OneWayRoutePercentageTreshold}\n\n" +
				   $"Fitness Parameters:\n" +
				   $"- RedundancyPercent: {Fitness_RedundancyPercentParameter}\n" +
				   $"- RouteLength: {Fitness_RouteLengthParameter}\n" +
				   $"- MinimumRouteLength: {Fitness_MinimumRouteLengthParameter}\n" +
				   $"- MaxAllowedChange: {Fitness_MaximalAllowedChangeParameter}\n" +
				   $"- FleetCapacity: {Fitness_FleetCapacityParameter}\n" +
				   $"- MaxWaitingMinutes: {Fitness_MaximumWaitingMinutesParameter}\n" +
				   $"- MinWaitingMinutes: {Fitness_MinimalWaitingMinutesParameter}\n" +
				   $"- MaxTravelTime: {Fitness_MaximumTravelTimeParameter}";
		}
	}
}
