using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Urbanflow.src.backend.models.util;
using yWorks.Layout.Graph;

namespace Urbanflow.src.backend.models.ga
{
	public class Genome
	{
		public int GenomeID { get;  }
		public int GenerationID { get; } //genome got created in the N-th generation
		public List<GenomeRoute> MutableRoutes { get; } = [];
		public double FitnessValue { get; private set; }

		//helper values
		public List<int> Parents { get; } = [];
		public List<GenomeRoute> AllRoutes { get; private set; }


		//initialization
		public Genome(int id, int generation, in OptimizationParameters parameters, in NetworkInformation network)
		{
			GenomeID = id;
			GenerationID = generation;
			int neededRoutes = parameters.Genome_RouteCount - network.StaticRoutes.Count;

			int i = 0;
			int failures = 0;
			Result<GenomeRoute> routeInitializationResult;
			while (i < neededRoutes && failures<parameters.Genome_RouteCount*2) {
				routeInitializationResult = GAUtil.PerformRouteInitialization(network, parameters);
				if (routeInitializationResult.IsSuccess)
				{
					MutableRoutes.Add(routeInitializationResult.Value);
					i++;				
				}
				else
				{
					failures++;
				}				
			}
			if(MutableRoutes.Count < neededRoutes)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at creating new routes multiple times, not enough routes got created");
			}
			var fitnessResult = CalculateFitnessValue(parameters, network);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}

		
		//Crossing
		public Genome(int id, int generation, Genome parent1, Genome parent2, in OptimizationParameters parameters, in NetworkInformation network, string step)
		{
			GenomeID = id;
			GenerationID = generation;
			Parents.Add(parent1.GenomeID);
			Parents.Add(parent2.GenomeID);
			switch (step)
			{
				case "route":
					for (int i = 0; i < parameters.Genome_RouteCount; i++)
					{
						var crossoverResult = GAUtil.PerformCrossover(parent1.MutableRoutes[i], parent2.MutableRoutes[i], network, parameters);
						int trycount = 0;
						while (crossoverResult.IsFailure && trycount < 4)
						{
							crossoverResult = GAUtil.PerformCrossover(parent1.MutableRoutes[i], parent2.MutableRoutes[i], network, parameters);
							trycount++;
						}
						if (crossoverResult.IsFailure)
						{
							throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) route crossing failed when performing crossover, the error remained after several tries: {crossoverResult.Error}");
						}
						MutableRoutes.Add(crossoverResult.Value);
					}
					break;
				case "time":
					for (int i = 0; i < parameters.Genome_RouteCount; i++)
					{
						int s1Child = Random.Shared.Next(2) == 0 ? parent1.MutableRoutes[i].OnStartTime : parent2.MutableRoutes[i].OnStartTime;
						int s2Child = Random.Shared.Next(2) == 0 ? parent1.MutableRoutes[i].BackStartTime : parent2.MutableRoutes[i].BackStartTime;
						int fChild = Random.Shared.Next(2) == 0 ? parent1.MutableRoutes[i].Headway : parent2.MutableRoutes[i].Headway;
						MutableRoutes.Add(new GenomeRoute(parent1.MutableRoutes[i].OnRoute, s1Child, parent1.MutableRoutes[i].BackRoute, s2Child, fChild));
					}
					break;
				default: break;
			}
			var fitnessResult = CalculateFitnessValue(parameters, network);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}


		//Mutation
		public Genome(int id, int generation, Genome parent, in OptimizationParameters parameters, in NetworkInformation network , string step)
		{
			GenomeID = id;
			GenerationID = generation;
			Parents.Add(parent.GenomeID);
			switch (step)
			{
				case "route": 
					for (int i = 0; i < parameters.Genome_RouteCount; i++)
					{
						var mutationResult = GAUtil.PerformRouteMutation(parent.MutableRoutes[i], network, parameters);
						int trycount = 0;
						while (mutationResult.IsFailure)
						{
							mutationResult = GAUtil.PerformRouteMutation(parent.MutableRoutes[i], network, parameters);
							trycount++;
						}
						if (mutationResult.IsFailure)
						{
							throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) route crossing failed when performing crossover, the error remained after several tries: {mutationResult.Error}");
						}
						MutableRoutes.Add(mutationResult.Value);
					}
					break;
					
				case "time":
					for (int i = 0;	i< parameters.Genome_RouteCount; i++)
					{
						int uS = Random.Shared.Next(-30, 31);
						int uF = Random.Shared.Next(5, 31);

						int newS1 = (parent.MutableRoutes[i].OnStartTime + uS + 60) % 60;
						int newS2 = (parent.MutableRoutes[i].BackStartTime + uS + 60) % 60;
						int newF = Math.Clamp(parent.MutableRoutes[i].Headway + (Random.Shared.Next(2) == 0 ? uF : -uF), 5, 60);

						MutableRoutes.Add(new GenomeRoute(parent.MutableRoutes[i].OnRoute, newS1, parent.MutableRoutes[i].BackRoute, newS2, newF));
					}
					
					break;
				default: break;
			}
			var fitnessResult = CalculateFitnessValue(parameters, network);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}


		//Fitness function
		public Result<double> CalculateFitnessValue(in OptimizationParameters parameters, in NetworkInformation network, string step = "") 
		{
			double fitnessValue = 0;

			//statikus járatok összefésülése a módosíthatókkal
			AllRoutes = [.. MutableRoutes, .. network.StaticRoutes];
			if (AllRoutes == null || AllRoutes.Count == 0)
			{
				return Result<double>.Failure("No routes to work with.");
			}

			Result<double> result;
			switch (step)
			{
				case "route":
					////Soft Constraint: Terminal distribution between routes
					//result = CalculateSoftConstraint_Route_Terminal(network);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					////Soft Constraint: Degree distribution betweeen stops
					//result = CalculateSoftConstraint_Route_Hub(network);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					////Soft Constraint: Route distribution between districts
					//result = CalculateSoftConstraint_Route_District(network);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					////Soft Constraint: Routes are the required length
					//result = CalculateSoftConstraint_Route_Length(parameters);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;					

					////Hard Constraint: Are all stops included in the network
					//result = CalculateHardConstraint_Route_Coverage(network);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					////Hard Constraint: Are the first and last stops terminals
					//result = CalculateHardConstraint_Route_Terminal(network);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					////Hard Constraint: Are the routes loop free
					//result = CalculateHardConstraint_Route_Loop();
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					////Hard Constraint: Transfer count over allowed treshold
					//result = CalculateHardConstraint_Route_Transfer(parameters, network);
					//if (result.IsFailure) return result;
					//fitnessValue += result.Value;

					//All of the above included in one:
					result = CalculateRouteFitness(parameters, network);
					if (result.IsFailure) return result;
					fitnessValue += result.Value;

					//Soft Constraint: Avarage travel time is optimal
					result = CalculateSoftConstraint_Route_Traveltime(parameters, network);
					if (result.IsFailure) return result;
					fitnessValue += result.Value;

					//Hard Constraint: Redudancy of routes over allowed treshold
					result = CalculateHardConstraint_Route_Redundancy(parameters);
					if (result.IsFailure) return result;
					fitnessValue += result.Value;
					break;
				case "time":

					//Soft Constraint: Optimize the waiting time at changes
					result = CalculateSoftConstraint_Time_Wait(parameters, network);
					if (result.IsFailure) return result;
					fitnessValue += result.Value;

					//Soft Constraint: Optimize the total time the travel takes, including waiting for changes
					result = CalculateSoftConstraint_Time_TotalTravel(parameters, network);
					if (result.IsFailure) return result;
					fitnessValue += result.Value;

					//Hard Constraint: The busses needed to aperate at the same time are below fleet size
					result = CalculateHardConstraint_Time_Fleet(parameters, network);
					if (result.IsFailure) return result;
					fitnessValue += result.Value;

					break;
			}
			return Result<double>.Success(fitnessValue);
		}


		//helper methods
		private Result<double> CalculateHardConstraint_Time_Fleet(OptimizationParameters parameters, NetworkInformation network)
		{
			//Hard Constraint: The busses needed to aperate at the same time are below fleet size
			throw new NotImplementedException();
		}

		private Result<double> CalculateSoftConstraint_Time_TotalTravel(OptimizationParameters parameters, NetworkInformation network)
		{
			//Soft Constraint: Optimize the total time the travel takes, including waiting for changes
			throw new NotImplementedException();
		}

		private Result<double> CalculateSoftConstraint_Time_Wait(OptimizationParameters parameters, NetworkInformation network)
		{
			//Soft Constraint: Optimize the waiting time at changes
			throw new NotImplementedException();
		}

		private Result<double> CalculateHardConstraint_Route_Transfer(OptimizationParameters parameters, NetworkInformation network)
		{
			//Hard Constraint: Transfer count over allowed treshold
			throw new NotImplementedException();
		}

		private Result<double> CalculateSoftConstraint_Route_Traveltime(in OptimizationParameters parameters, in NetworkInformation network)
		{
			//Soft Constraint: Avarage travel time is optimal
			throw new NotImplementedException();
		}

		private Result<double> CalculateHardConstraint_Route_Redundancy(in OptimizationParameters parameters)
		{
			int redundantPairCount = 0;
			var routes = AllRoutes;

			for (int i = 0; i < routes.Count; i++)
			{
				var routeA = routes[i].OnRoute;

				for (int j = i + 1; j < routes.Count; j++)
				{
					var routeB = routes[j].OnRoute;

					int matchingStopCount = 0;

					foreach (var stop in routeA)
						if (routeB.Contains(stop))
							matchingStopCount++;

					double percentage = (double)(matchingStopCount * 2) / (routeA.Count + routeB.Count);

					if (percentage > parameters.Fitness_RedundancyPercentParameter)
						redundantPairCount++;
				}
			}

			return Result<double>.Success(redundantPairCount * 10);
		}

		private Result<double> CalculateHardConstraint_Route_Loop()
		{
			//Hard Constraint: Are the routes loop free
			var loopCount = 0;
			foreach (var route in AllRoutes)
			{
				var visited = new HashSet<Guid>();

				foreach (var stop in route.OnRoute)
				{
					if (!visited.Add(stop))
						loopCount++;
				}

				visited.Clear();

				foreach (var stop in route.BackRoute)
				{
					if (!visited.Add(stop))
						loopCount++;
				}
			}
			return Result<double>.Success(loopCount * 100);
		}

		private Result<double> CalculateHardConstraint_Route_Terminal(in NetworkInformation network)
		{
			int mistakePoints = 0;
			var terminals = network.Terminals;

			foreach (var route in AllRoutes)
			{
				var onRoute = route.OnRoute;
				var backRoute = route.BackRoute;

				var fromOnRoute = onRoute[0];
				var toOnRoute = onRoute[^1];
				var fromBackRoute = backRoute[0];
				var toBackRoute = backRoute[^1];

				if (fromBackRoute != toOnRoute) mistakePoints++;
				if (fromOnRoute != toBackRoute) mistakePoints++;

				if (!terminals.Contains(fromOnRoute)) mistakePoints++;
				if (!terminals.Contains(toOnRoute)) mistakePoints++;
				if (!terminals.Contains(fromBackRoute)) mistakePoints++;
				if (!terminals.Contains(toBackRoute)) mistakePoints++;
			}

			return Result<double>.Success(mistakePoints * 1000.0);
		}

		private Result<double> CalculateHardConstraint_Route_Coverage(in NetworkInformation network)
		{
			var unmetStops = new HashSet<Guid>(network.AllStops);

			foreach (var route in AllRoutes)
			{
				foreach (var stop in route.OnRoute)
					unmetStops.Remove(stop);

				foreach (var stop in route.BackRoute)
					unmetStops.Remove(stop);
			}

			return Result<double>.Success(unmetStops.Count * 1000.0);
		}

		private Result<double> CalculateSoftConstraint_Route_Length(in OptimizationParameters parameters)
		{
			double lengthDeviations = 0;
			double invTargetLength = 1.0 / parameters.Fitness_RouteLengthParameter;

			var routes = AllRoutes;

			foreach (var route in routes)
			{
				double onDeviation = Math.Abs(route.OnRoute.Count * invTargetLength - 1);
				if (onDeviation > 0.2)
					lengthDeviations += onDeviation;

				double backDeviation = Math.Abs(route.BackRoute.Count * invTargetLength - 1);
				if (backDeviation > 0.2)
					lengthDeviations += backDeviation;
			}

			if (lengthDeviations == 0)
				return Result<double>.Success(0);

			return Result<double>.Success(lengthDeviations / routes.Count * 2);
		}

		private Result<double> CalculateSoftConstraint_Route_District(in NetworkInformation network)
		{
			double districtConnections = 0;

			var districts = network.Districts;
			var routes = AllRoutes;

			for (int i = 0; i < districts.Count; i++)
			{
				var district1 = districts[i];

				for (int j = i + 1; j < districts.Count; j++)
				{
					var district2 = districts[j];

					foreach (var route in routes)
					{
						bool onRouteConnect =
							route.OnRoute.Any(district1.Contains) &&
							route.OnRoute.Any(district2.Contains);

						bool backRouteConnect =
							route.BackRoute.Any(district1.Contains) &&
							route.BackRoute.Any(district2.Contains);

						if (onRouteConnect || backRouteConnect)
							districtConnections++;
					}
				}
			}

			double allConnections = Factorial(districts.Count);

			return Result<double>.Success(districtConnections / allConnections);
		}

		private Result<double> CalculateSoftConstraint_Route_Hub(in NetworkInformation network)
		{
			Dictionary<Guid, int> hubDegrees = [];
			foreach(var hubId in network.Hubs)
			{
				hubDegrees[hubId] = 0;
				foreach(var route in AllRoutes)
				{
					if(route.OnRoute.Contains(hubId) || route.BackRoute.Contains(hubId))
					{
						hubDegrees[hubId]++;
					}
				}
			}

			// fokszámok növekvő rendezése
			var sorted = hubDegrees.OrderBy(h => h.Value).ToList();

			double sumFi = sorted.Sum(s => s.Value);
			if (sumFi == 0) return Result<Double>.Failure("Couldn't calculate hub degrees");

			double weightedSum = 0.0;

			for (int i = 0; i < network.Hubs.Count; i++)
			{
				weightedSum += i * sorted[i].Value;
			}

			double gini =
				(2.0 * weightedSum) / (network.Hubs.Count * sumFi)
				- (double)(network.Hubs.Count + 1) / network.Hubs.Count;

			return Result<double>.Success(gini);
		}

		private Result<double> CalculateSoftConstraint_Route_Terminal(in NetworkInformation network)
		{
			//melyik megálló hányszor van érintve
			Dictionary<Guid, int> terminalUsage = [];
			foreach (var route in AllRoutes) {
				terminalUsage[route.OnRoute[0]]++;
				terminalUsage[route.OnRoute[^1]]++;
			}

			if (network.Terminals.Count <= 0) {
				return Result<double>.Failure("There is no terminals defined for the network");
			}
			
			//ideális átlagos terhelése:
			double tAvg = 2.0 * AllRoutes.Count / network.Terminals.Count;

			// szórás számítása
			double sumSquares = 0.0;
			foreach (var tv in terminalUsage)
			{
				sumSquares += Math.Pow(tv.Value - tAvg, 2);
			}
			double sigma = Math.Sqrt(sumSquares / network.Terminals.Count);

			// relatív szórás
			double relativeDeviation = sigma / tAvg;

			return Result<double>.Success(Math.Min(1.0, relativeDeviation));
		}


		private static double Factorial(int n)
		{
			if (n < 0)
				throw new ArgumentException("n must be non-negative");

			double result = 1.0;

			for (int i = 2; i <= n; i++)
				result *= i;

			return result;
		}


		//Soft Constraint: Terminal distribution between routes
		//Soft Constraint: Degree distribution betweeen stops
		//Soft Constraint: Route distribution between districts
		//Soft Constraint: Routes are the required length
		//Hard Constraint: Are all stops included in the network
		//Hard Constraint: Are the first and last stops terminals
		//Hard Constraint: Are the routes loop free
		//Hard Constraint: Redudancy of routes over allowed treshold

		private Result<double> CalculateRouteFitness(in OptimizationParameters parameters, in NetworkInformation network)
		{
			var routes = AllRoutes;
			int routeCount = routes.Count;

			var terminals = network.Terminals;
			var hubs = network.Hubs;

			var unmetStops = new HashSet<Guid>(network.AllStops);
			var visited = new HashSet<Guid>();

			Dictionary<Guid, int> hubDegrees = [];
			Dictionary<Guid, int> terminalUsage = [];

			foreach (var h in hubs) hubDegrees[h] = 0;

			int loopCount = 0;
			int terminalMistakes = 0;
			double lengthDeviation = 0;

			double invTargetLength = 1.0 / parameters.Fitness_RouteLengthParameter;

			// store route stop sets for redundancy later
			var routeStopSets = new List<HashSet<Guid>>(routeCount);

			foreach (var route in routes)
			{
				var onRoute = route.OnRoute;
				var backRoute = route.BackRoute;

				var routeStops = new HashSet<Guid>();
				routeStopSets.Add(routeStops);

				visited.Clear();

				foreach (var stop in onRoute)
				{
					if (!visited.Add(stop)) loopCount++;
					unmetStops.Remove(stop);
					routeStops.Add(stop);

					if (hubDegrees.TryGetValue(stop, out int value)) hubDegrees[stop] = ++value;
				}

				visited.Clear();

				foreach (var stop in backRoute)
				{
					if (!visited.Add(stop)) loopCount++;
					unmetStops.Remove(stop);
					routeStops.Add(stop);

					if (hubDegrees.TryGetValue(stop, out int value)) hubDegrees[stop] = ++value;
				}

				// terminal constraint
				var fromOn = onRoute[0];
				var toOn = onRoute[^1];
				var fromBack = backRoute[0];
				var toBack = backRoute[^1];

				if (fromBack != toOn) terminalMistakes++;
				if (fromOn != toBack) terminalMistakes++;

				if (!terminals.Contains(fromOn)) terminalMistakes++;
				if (!terminals.Contains(toOn)) terminalMistakes++;
				if (!terminals.Contains(fromBack)) terminalMistakes++;
				if (!terminals.Contains(toBack)) terminalMistakes++;

				if (!terminalUsage.ContainsKey(fromOn)) terminalUsage[fromOn] = 0;
				if (!terminalUsage.ContainsKey(toOn)) terminalUsage[toOn] = 0;

				terminalUsage[fromOn]++;
				terminalUsage[toOn]++;

				// length deviation
				double onDev = Math.Abs(onRoute.Count * invTargetLength - 1);
				if (onDev > 0.2) lengthDeviation += onDev;

				double backDev = Math.Abs(backRoute.Count * invTargetLength - 1);
				if (backDev > 0.2) lengthDeviation += backDev;
			}

			// redundancy check
			int redundantPairs = 0;

			for (int i = 0; i < routeStopSets.Count; i++)
			{
				var setA = routeStopSets[i];

				for (int j = i + 1; j < routeStopSets.Count; j++)
				{
					var setB = routeStopSets[j];

					int match = 0;
					foreach (var stop in setA)
						if (setB.Contains(stop))
							match++;

					double perc = (double)(match * 2) / (setA.Count + setB.Count);

					if (perc > parameters.Fitness_RedundancyPercentParameter/100)
						redundantPairs++;
				}
			}

			// hub gini
			var sorted = hubDegrees.Values.OrderBy(v => v).ToList();

			double sumFi = sorted.Sum();
			double weightedSum = 0;

			for (int i = 0; i < sorted.Count; i++)
				weightedSum += i * sorted[i];

			double hubGini = sumFi == 0
				? 0
				: (2.0 * weightedSum) / (sorted.Count * sumFi)
				  - (double)(sorted.Count + 1) / sorted.Count;

			// terminal load balance
			double tAvg = 2.0 * routeCount / terminals.Count;

			double sumSquares = 0;
			foreach (var tv in terminalUsage.Values)
				sumSquares += Math.Pow(tv - tAvg, 2);

			double sigma = Math.Sqrt(sumSquares / terminals.Count);
			double terminalDeviation = Math.Min(1.0, sigma / tAvg);

			// district connection
			double districtConnections = 0;
			var districts = network.Districts;

			for (int i = 0; i < districts.Count; i++)
			{
				var d1 = districts[i];

				for (int j = i + 1; j < districts.Count; j++)
				{
					var d2 = districts[j];

					foreach (var route in routes)
					{
						bool on =
							route.OnRoute.Any(d1.Contains) &&
							route.OnRoute.Any(d2.Contains);

						bool back =
							route.BackRoute.Any(d1.Contains) &&
							route.BackRoute.Any(d2.Contains);

						if (on || back)
							districtConnections++;
					}
				}
			}

			double allConnections = Factorial(districts.Count);

			double score =
				  redundantPairs * 10
				+ loopCount * 100
				+ terminalMistakes * 1000
				+ unmetStops.Count * 1000
				+ (lengthDeviation == 0 ? 0 : lengthDeviation / routeCount * 2)
				+ districtConnections / allConnections
				+ hubGini
				+ terminalDeviation;

			return Result<double>.Success(score);
		}
	}
}
