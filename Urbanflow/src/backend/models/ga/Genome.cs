using BruTile.Extensions;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using GTFS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Windows.Media.Animation;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;
using yWorks.Layout.Graph;

namespace Urbanflow.src.backend.models.ga
{
	public class Genome : IComparable<Genome>
	{
		public int GenomeID { get;  }
		public int GenerationID { get; set; } //genome got created in the N-th generation
		public List<GenomeRoute> MutableRoutes { get; } = [];
		public double FitnessValue { get; private set; }

		//helper values
		public List<int> Parents { get; } = [];

		public double UnMetStopPercentage { get; private set; } = 0.0; 
		public List<Guid> UnMetStopList { get; private set; } = [];

		

		//initialization
		public Genome(int id, int generation, in OptimizationParameters parameters, in NetworkInformation network)
		{
			try
			{
				GenomeID = id;
				GenerationID = generation;
				int neededRoutes = parameters.Genome_RouteCount - network.StaticRoutes.Count;
				MutableRoutes.Capacity = Math.Max(MutableRoutes.Capacity, neededRoutes);

				if (neededRoutes <= 0)
				{
					FitnessValue = -1;
					return;
				}

				int created = 0;
				int targetFailures = parameters.Genome_RouteCount;
				int failures = 0;
				while (created < neededRoutes && failures < targetFailures)
				{
					var result = GAUtil.PerformRouteInitialization(network, parameters);

					if (result.IsSuccess)
					{
						result.Value.RouteIndex = created + 1;
						MutableRoutes.Add(result.Value);
						created++;
					}
					else
					{
						failures++;
					}
				}
				if (failures >= targetFailures)
				{
					throw new Exception("Genome initialization failed, because the failures exceeded the allowed threshold");
				}

				if (created < neededRoutes)
				{
					throw new Exception(
						$"Genome initialization failed: could not create enough routes. " +
						$"Created {created}/{neededRoutes}, failures {failures}");
				}
				var fitnessResult = CalculateFitnessValue(parameters, network, "route");
				if (fitnessResult.IsFailure)
				{
					throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
				}
				FitnessValue = fitnessResult.Value;
			} catch (Exception ex)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed, error: {ex.Message}");
			}
		}

		//Empty initialization
		public Genome(int id, int generation, in List<GenomeRoute> routes, in OptimizationParameters parameters, in NetworkInformation network, string step)
		{
			GenomeID = id;
			GenerationID = generation;
			MutableRoutes = routes;
			var fitnessResult = CalculateFitnessValue(parameters, network, step);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}

		//Crossing
		public Genome(int id, int generation, in Genome parent1, in Genome parent2, in OptimizationParameters parameters, in NetworkInformation network, string step)
		{
			GenomeID = id;
			GenerationID = generation;
			Parents.Add(parent1.GenomeID);
			Parents.Add(parent2.GenomeID);

			var p1Routes = parent1.MutableRoutes;
			var p2Routes = parent2.MutableRoutes;

			int routeCount = parameters.Genome_RouteCount;

			bool IsRouteCrossing = step == "route";

			if (IsRouteCrossing)
			{
				for (int i = 0; i < routeCount; i++)
				{
					var r1 = p1Routes[i];
					var r2 = p2Routes[i];

					Result<GenomeRoute> crossoverResult;
					int tryCount = 0;
					do
					{
						crossoverResult = GAUtil.PerformCrossover(r1, r2, network, parameters);
						tryCount++;
					}
					while (crossoverResult.IsFailure && tryCount < 4);

					if (crossoverResult.IsFailure)
					{
						throw new Exception(
							$"Genome (ID: {id}, Generation: {GenerationID}) crossover failed after retries: {crossoverResult.Error}");
					}
					crossoverResult.Value.RouteIndex = i + 1;
					MutableRoutes.Add(crossoverResult.Value);
				}
			}
			else
			{
				var random = Random.Shared;

				for (int i = 0; i < parameters.Genome_RouteCount; i++)
				{
					var r1 = p1Routes[i];
					var r2 = p2Routes[i];

					int s1Child = random.Next(2) == 0 ? r1.OnStartTime : r2.OnStartTime;
					int s2Child = random.Next(2) == 0 ? r1.BackStartTime : r2.BackStartTime;
					int fChild = random.Next(2) == 0 ? r1.Headway : r2.Headway;
					MutableRoutes.Add(new GenomeRoute(i+1, r1.OnRoute, s1Child, r1.BackRoute, s2Child, fChild, r1.OneWay));
				}
			}

			var fitnessResult = CalculateFitnessValue(parameters, network, step);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}


		//Mutation
		public Genome(int id, int generation, in Genome parent, in OptimizationParameters parameters, in NetworkInformation network , string step)
		{
			GenomeID = id;
			GenerationID = generation;
			Parents.Add(parent.GenomeID);

			var parentRoutes = parent.MutableRoutes;
			var unMetStops = parent.UnMetStopList;
			int routeCount = parameters.Genome_RouteCount;

			var random = Random.Shared;

			bool isRouteMutation = step == "route";

			if (isRouteMutation)
			{
				for (int i = 0; i < routeCount; i++)
				{
					var baseRoute = parentRoutes[i];

					Result<GenomeRoute> mutationResult;
					int tryCount = 0;

					do
					{
						mutationResult = GAUtil.PerformRouteMutation(
							baseRoute,
							unMetStops,
							network,
							parameters);

						tryCount++;
					}
					while (mutationResult.IsFailure && tryCount < 4);

					if (mutationResult.IsFailure)
					{
						throw new Exception(
							$"Genome (ID: {id}, Generation: {GenerationID}) mutation failed after retries: {mutationResult.Error}");
					}
					mutationResult.Value.RouteIndex = i + 1;
					MutableRoutes.Add(mutationResult.Value);
				}
			}
			else
			{
				for (int i = 0; i < parameters.Genome_RouteCount; i++)
				{
					var r = parentRoutes[i];

					int uS = random.Next(-15, 16);
					int uF = random.Next(1, 16);

					int newS1 = (r.OnStartTime + uS + 60) % 60;
					int newS2 = (r.BackStartTime + uS + 60) % 60;

					int newF = Math.Clamp(r.Headway + (random.Next(2) == 0 ? uF : -uF), 5, 60);
					if(newF == 0)
					{
						newF = 30;
					}

					MutableRoutes.Add(new GenomeRoute(i+1, r.OnRoute, newS1, r.BackRoute, newS2, newF, r.OneWay));
				}
			}
			var fitnessResult = CalculateFitnessValue(parameters, network, step);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}

		internal Genome FillTimePropertiesOfRoutes(in OptimizationParameters parameters, in NetworkInformation network)
		{
			foreach (var route in MutableRoutes)
			{
				route.RandomiseTimeValues();
			}

			var fitnessResult = CalculateFitnessValue(parameters, network, "time");
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {GenomeID}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;

			return this;
		}

		private Result<double> CalculateFitnessValue(in OptimizationParameters parameters, in NetworkInformation network, string step, bool withLogging = false)
		{
			using FitnessCalculator calculator = new();
			var result = calculator.CalculateFitnessValue(this, parameters, network, step, withLogging);
			UnMetStopList = calculator.UnMetStopList;
			UnMetStopPercentage = calculator.UnMetStopPercentage;
			return result;
		}

		public override string ToString()
		{
			return $"GenomeID={GenomeID}, Gen={GenerationID}, Fitness={FitnessValue:F4}, " +
				   $"UnmetStops={UnMetStopPercentage:P2}, Parents=[{string.Join(",", Parents)}]";
		}

		public int CompareTo(Genome? other)
		{
			if (other == null) return 1;
			int differencecounter = 0;

			if (other.MutableRoutes.Count != this.MutableRoutes.Count) differencecounter++;

			if (other.MutableRoutes.Count == this.MutableRoutes.Count)
			{
				List<GenomeRoute> otherGenomeRoutes = new List<GenomeRoute>(other.MutableRoutes);
				for (int i = 0; i < this.MutableRoutes.Count; i++)
				{
					var diff = -1;
					int j = -1;
					while (diff != 0 && j<otherGenomeRoutes.Count) {
						j++;
						diff = this.MutableRoutes[i].CompareTo(otherGenomeRoutes[j]);						
					}
					if(diff == 0)
					{
						otherGenomeRoutes.RemoveAt(j);
					}
					else
					{
						differencecounter++;
					}
				}
				if(otherGenomeRoutes.Count > 0)
				{
					differencecounter++;
				}
			}

			return differencecounter;
		}

		internal double EvaluateFitnessWithLogging(string step, OptimizationSettings settings, NetworkInformation networkInformation)
		{
			var fitnessResult = CalculateFitnessValue(settings.UserOptimizationParameters, networkInformation, step, true);
			if (fitnessResult.IsFailure)
			{
				OptimizationLoggerService.Instance.Log($"Fitness value calculation failed, error: {fitnessResult.Error}");
				//throw new Exception($"Fitness value calculation failed, error: {fitnessResult.Error}");
				return 0.0;
			}
			else
			{
				return fitnessResult.Value;
			}
			
		}
	}
}
