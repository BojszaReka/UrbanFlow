using GTFS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Media.Animation;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;
using yWorks.Layout.Graph;

namespace Urbanflow.src.backend.models.ga
{
	public class Genome
	{
		public int GenomeID { get;  }
		public int GenerationID { get; set; } //genome got created in the N-th generation
		public List<GenomeRoute> MutableRoutes { get; } = [];
		public double FitnessValue { get; private set; }

		//helper values
		public List<int> Parents { get; } = [];
		public List<GenomeRoute> AllRoutes { get; private set; }

		public double UnMetStopPercentage { get; private set; } = 0.0; 
		public List<Guid> UnMetStopList { get; private set; } = [];


		//initialization
		public Genome(int id, int generation, in OptimizationParameters parameters, in NetworkInformation network)
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
					MutableRoutes.Add(new GenomeRoute(r1.OnRoute, s1Child, r1.BackRoute, s2Child, fChild));
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

					MutableRoutes.Add(mutationResult.Value);
				}
			}
			else
			{
				for (int i = 0; i < parameters.Genome_RouteCount; i++)
				{
					var r = parentRoutes[i];

					int uS = random.Next(-30, 31);
					int uF = random.Next(5, 31);

					int newS1 = (r.OnStartTime + uS + 60) % 60;
					int newS2 = (r.BackStartTime + uS + 60) % 60;

					int newF = Math.Clamp(r.Headway + (random.Next(2) == 0 ? uF : -uF), 5, 60);

					MutableRoutes.Add(new GenomeRoute(r.OnRoute, newS1, r.BackRoute, newS2, newF));
				}
			}
			var fitnessResult = CalculateFitnessValue(parameters, network, step);
			if (fitnessResult.IsFailure)
			{
				throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) initialization failed at calculating fitness value, error: {fitnessResult.Error}");
			}
			FitnessValue = fitnessResult.Value;
		}


		//Fitness function
		public Result<double> CalculateFitnessValue(in OptimizationParameters parameters, in NetworkInformation network, string step = "") 
		{
			try
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
						result = CalculateRouteFitness(parameters, network);
						if (result.IsFailure) return result;
						fitnessValue += result.Value;

						//Hard Constraint: Transfer count over allowed treshold
						result = CalculateHardConstraint_Route_Transfer(parameters, network);
						if (result.IsFailure) return result;
						fitnessValue += result.Value;

						//Soft Constraint: Avarage travel time is optimal
						result = CalculateSoftConstraint_Route_Traveltime(network);
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

				AllRoutes = [];

				return Result<double>.Success(fitnessValue);
			}catch(Exception ex)
			{
				throw new Exception("Fitness value calculation failed: "+ex.Message);
			}
		}


		//helper methods
		private Result<double> CalculateHardConstraint_Time_Fleet(in OptimizationParameters parameters, in NetworkInformation network)
		{
			//Hard Constraint: The busses needed to aperate at the same time are below fleet size
			throw new NotImplementedException();
		}

		private Result<double> CalculateSoftConstraint_Time_TotalTravel(in OptimizationParameters parameters, in NetworkInformation network)
		{
			//Soft Constraint: Optimize the total time the travel takes, including waiting for changes
			throw new NotImplementedException();
		}

		private Result<double> CalculateSoftConstraint_Time_Wait(in OptimizationParameters parameters, in NetworkInformation network)
		{
			//Soft Constraint: Optimize the waiting time at changes
			throw new NotImplementedException();
		}

		private Result<double> CalculateHardConstraint_Route_Transfer(in OptimizationParameters parameters, in NetworkInformation network)
		{
			//Hard Constraint: Transfer count over allowed treshold
			var routes = AllRoutes;

			var routeStops = new List<HashSet<Guid>>(routes.Count);
			for (int i = 0; i < routes.Count; i++)
			{
				var stopSet = new HashSet<Guid>();

				if (routes[i].OnRoute != null)
				{
					foreach (var stop in routes[i].OnRoute)
						stopSet.Add(stop);
				}

				if (routes[i].BackRoute != null)
				{
					foreach (var stop in routes[i].BackRoute)
						stopSet.Add(stop);
				}

				routeStops.Add(stopSet);
			}

			var stopToRoutes = new Dictionary<Guid, List<int>>();
			for (int routeIndex = 0; routeIndex < routeStops.Count; routeIndex++)
			{
				foreach (var stop in routeStops[routeIndex])
				{
					if (!stopToRoutes.TryGetValue(stop, out var list))
					{
						list = new List<int>();
						stopToRoutes[stop] = list;
					}

					list.Add(routeIndex);
				}
			}

			var adjacency = new HashSet<int>[routes.Count];
			for (int i = 0; i < adjacency.Length; i++)
				adjacency[i] = new HashSet<int>();

			foreach (var routeIndices in stopToRoutes.Values)
			{
				for (int i = 0; i < routeIndices.Count; i++)
				{
					for (int j = i + 1; j < routeIndices.Count; j++)
					{
						int a = routeIndices[i];
						int b = routeIndices[j];

						if (a == b)
							continue;

						adjacency[a].Add(b);
						adjacency[b].Add(a);
					}
				}
			}

			var distance = new int[routes.Count];
			var queue = new Queue<int>(routes.Count);

			int maxShortestPathEdges = 0;
			bool disconnected = false;

			for (int start = 0; start < routes.Count; start++)
			{
				Array.Fill(distance, -1);
				queue.Clear();

				distance[start] = 0;
				queue.Enqueue(start);

				while (queue.Count > 0)
				{
					int current = queue.Dequeue();
					int nextDistance = distance[current] + 1;

					foreach (var neighbor in adjacency[current])
					{
						if (distance[neighbor] != -1)
							continue;

						distance[neighbor] = nextDistance;
						queue.Enqueue(neighbor);
					}
				}

				for (int i = 0; i < distance.Length; i++)
				{
					if (distance[i] == -1)
					{
						disconnected = true;
						break;
					}

					if (distance[i] > maxShortestPathEdges)
						maxShortestPathEdges = distance[i];
				}

				if (disconnected)
					break;
			}

			// max transfers = diameter in edges
			// Hard constraint: 100 if over allowed threshold, otherwise 0
			int changeCount = parameters.Fitness_MaximalAllowedChangeParameter;
			double penalty = disconnected ||
							 maxShortestPathEdges > changeCount
				? 100 * (maxShortestPathEdges-changeCount)
				: 0d;

			return Result<double>.Success(penalty);
		}

		private Result<double> CalculateSoftConstraint_Route_Traveltime(in NetworkInformation network)
		{
			var routes = AllRoutes;
			double alpha = 0.5;
			double beta = 0.5;

			var stopToDistrict = new Dictionary<Guid, Guid>();

			foreach (var (id, list) in network.Districts)
			{
				foreach (var stop in list)
					stopToDistrict[stop] = id;
			}

			var fastMatrix = new Dictionary<Guid, Dictionary<Guid, double>>(network.StopConnectivityMatrix.Count);

			foreach (var (from, neighbors) in network.StopConnectivityMatrix)
			{
				var dict = new Dictionary<Guid, double>(neighbors.Count);
				foreach (var (dest, weight) in neighbors)
					dict[dest] = weight;

				fastMatrix[from] = dict;
			}

			double intraSum = 0, interSum = 0;
			int intraCount = 0, interCount = 0;

			foreach (var route in routes)
			{
				List<Guid>[] onback = [route.OnRoute, route.BackRoute];

				foreach (var r in onback)
				{
					var firstIndex = new Dictionary<Guid, int>();
					var lastIndex = new Dictionary<Guid, int>();

					for (int i = 0; i < r.Count; i++)
					{
						var stop = r[i];

						if (!stopToDistrict.TryGetValue(stop, out var d))
							continue;

						if (!firstIndex.ContainsKey(d))
							firstIndex[d] = i;

						lastIndex[d] = i;
					}

					foreach (var d in firstIndex.Keys)
					{
						int start = firstIndex[d];
						int end = lastIndex[d];

						if (start >= end)
							continue;

						double time = 0;
						bool valid = true;

						for (int i = start; i < end; i++)
						{
							var from = r[i];
							var to = r[i + 1];

							if (!fastMatrix.TryGetValue(from, out var neigh) ||
								!neigh.TryGetValue(to, out var w))
							{
								valid = false;
								break;
							}

							time += w;
						}

						if (valid)
						{
							intraSum += time;
							intraCount++;
						}
					}

					var districtsOrdered = firstIndex
						.Select(kvp => (District: kvp.Key, First: kvp.Value, Last: lastIndex[kvp.Key]))
						.OrderBy(x => x.First)
						.ToArray();

					for (int i = 0; i < districtsOrdered.Length - 1; i++)
					{
						int start = districtsOrdered[i].Last;
						int end = districtsOrdered[i + 1].First;

						if (start >= end)
							continue;

						double time = 0;
						bool valid = true;

						for (int j = start; j < end; j++)
						{
							var from = r[j];
							var to = r[j + 1];

							if (!fastMatrix.TryGetValue(from, out var neigh) ||
								!neigh.TryGetValue(to, out var w))
							{
								valid = false;
								break;
							}

							time += w;
						}

						if (valid)
						{
							interSum += time;
							interCount++;
						}
					}
				}
			}

			double T_intra = intraCount > 0 ? intraSum / intraCount : 0;
			double T_inter = interCount > 0 ? interSum / interCount : 0;

			double intraScore = (T_intra > 0) ? (1 - (T_intra / intraSum)) : 1;
			double interScore = (T_inter > 0) ? (1 - (T_inter / interSum)) : 1;

			double result = alpha * intraScore + beta * interScore;

			return Result<double>.Success(Math.Clamp(result, 0, 1));
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
			var hubsList = network.Hubs;

			var unmetStops = new HashSet<Guid>(network.AllStops);
			var visited = new HashSet<Guid>();

			Dictionary<Guid, int> hubDegrees = [];
			Dictionary<Guid, int> terminalUsage = [];

			foreach (var h in hubsList) hubDegrees[h] = 0;

			int loopCount = 0;
			int terminalMistakes = 0;
			double lengthDeviation = 0;
			double deviationThreshold = 0.5;

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
					if (!visited.Add(stop) && !stop.Equals(onRoute[0]) && !stop.Equals(onRoute[^1])) loopCount++;
					unmetStops.Remove(stop);
					routeStops.Add(stop);

					if (hubDegrees.TryGetValue(stop, out int value)) hubDegrees[stop] = ++value;
				}

				visited.Clear();

				if (backRoute.Count > 0)
				{
					foreach (var stop in backRoute)
					{
						if (!visited.Add(stop) && !stop.Equals(backRoute[0]) && !stop.Equals(backRoute[^1])) loopCount++;
						unmetStops.Remove(stop);
						routeStops.Add(stop);

						if (hubDegrees.TryGetValue(stop, out int value)) hubDegrees[stop] = ++value;
					}
				}

				visited.Clear();

				if(backRoute.Count > 0)
				{
					// terminal constraint
					var fromOn = onRoute[0];
					var toOn = onRoute[^1];
					var fromBack = backRoute[0];
					var toBack = backRoute[^1];

					if (!fromBack.Equals(toOn)) 
						terminalMistakes++;
					if (!fromOn.Equals(toBack)) 
						terminalMistakes++;

					if (!terminals.Contains(fromOn)) terminalMistakes++;
					if (!terminals.Contains(toOn)) terminalMistakes++;
					if (!terminals.Contains(fromBack)) terminalMistakes++;
					if (!terminals.Contains(toBack)) terminalMistakes++;

					if (!terminalUsage.ContainsKey(fromOn)) terminalUsage[fromOn] = 0;
					if (!terminalUsage.ContainsKey(toOn)) terminalUsage[toOn] = 0;
					if (!terminalUsage.ContainsKey(fromBack)) terminalUsage[fromBack] = 0;
					if (!terminalUsage.ContainsKey(toBack)) terminalUsage[toBack] = 0;

					terminalUsage[fromOn]++;
					terminalUsage[toOn]++;
					terminalUsage[fromBack]++;
					terminalUsage[toBack]++;
				}
				else
				{
					// terminal constraint
					var fromOn = onRoute[0];
					var toOn = onRoute[^1];
					if (!terminals.Contains(fromOn)) terminalMistakes++;
					if (!terminals.Contains(toOn)) terminalMistakes++;
					if (!terminalUsage.ContainsKey(fromOn)) terminalUsage[fromOn] = 0;
					if (!terminalUsage.ContainsKey(toOn)) terminalUsage[toOn] = 0;
					terminalUsage[fromOn]++;
					terminalUsage[toOn]++;
				}
				

				// length deviation
				double onDev = Math.Abs(onRoute.Count * invTargetLength - 1);
				if (onDev > deviationThreshold) lengthDeviation += onDev;

				double backDev = Math.Abs(backRoute.Count * invTargetLength - 1);
				if (backDev > deviationThreshold) lengthDeviation += backDev;
			}

			// redundancy check
			int redundantPairs = 0;
			double threshold = (double)parameters.Fitness_RedundancyPercentParameter / (double)100;

			for (int i = 0; i < routeStopSets.Count; i++)
			{
				var setA = routeStopSets[i];

				for (int j = i + 1; j < routeStopSets.Count; j++)
				{
					var setB = routeStopSets[j];

					int currentStreak = 0;
					int previousStreak = 0;

					foreach (var stop in setA)
					{
						if (setB.Contains(stop))
						{
							currentStreak++;
						}
						else
						{
							if (currentStreak > previousStreak)
							{
								previousStreak = currentStreak;
							}
							currentStreak = 0;
						}
					}

					double matchPercent = (double)currentStreak / (((double)setA.Count + (double)setB.Count)/2);

					// check streak at the end
					if (matchPercent > threshold)
					{
						redundantPairs++;
					}
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

			List<Guid> checkedDistricts = [];
			foreach (var (id1, d1)  in districts)
			{
				checkedDistricts.Add(id1);

				foreach (var (id2, d2) in districts)
				{
					if (checkedDistricts.Contains(id2))
					{
						continue;
					}

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

			double allConnections = Factorial(districts.Count / 3);

			UnMetStopPercentage = (double)unmetStops.Count / (double)network.AllStops.Count;
			UnMetStopList = unmetStops.ToList();
			

			double score =
				  redundantPairs * 10
				+ loopCount * 100
				+ terminalMistakes * 1000
				+ unmetStops.Count * 1000
				+ (lengthDeviation == 0 ? 0 : lengthDeviation / routeCount)
				+ districtConnections / allConnections
				+ hubGini
				+ terminalDeviation;

			if(score< 2000.0)
			{
				var temp = districtConnections / allConnections;
				Console.WriteLine();
			}

			 return Result<double>.Success(score);
		}
	}
}
