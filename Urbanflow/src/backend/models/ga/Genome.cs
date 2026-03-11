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
		public List<GenomeRoute> Routes { get; } = new List<GenomeRoute>();
		public double FitnessValue { get; }

		//helper values
		public List<int> Parents { get; } = new List<int>();
		public Guid StartTerminal { get;  }
		public Guid EndTerminal { get;  }
		public List<Guid> MetHubs { get; } = new List<Guid>();
		

		//initialization
		public Genome(int id, int generation, in OptimizationParameters parameters, in NetworkInformation network)
		{
			GenomeID = id;
			GenerationID = generation;

			int hubCount = parameters.Genome_HubNumberInRoute;

			var terminals = network.Terminals;
			var hubs = network.Hubs;
			var otherStops = network.GenericStops;
			var networkConnectivity = network.StopConnectivityMatrix;

			// choose 2 on random from Terminals
			// choose hubCount number of stops from Hub on random
			// based on network connectivity connect the terminals and hubs with the (shortest) possible paths 
			CalculateFitnessValue(parameters, network);
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
						var crossoverResult = PerformCrossover(parent1.Routes[i], parent2.Routes[i], network);
						if (crossoverResult.IsFailure)
							throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) route crossing failed when performing crossover, error: {crossoverResult.Error}");
						Routes.Add(crossoverResult.Value);
					}
					break;
				case "time":
					for (int i = 0; i < parameters.Genome_RouteCount; i++)
					{
						int s1Child = Random.Shared.Next(2) == 0 ? parent1.Routes[i].OnStartTime : parent2.Routes[i].OnStartTime;
						int s2Child = Random.Shared.Next(2) == 0 ? parent1.Routes[i].BackStartTime : parent2.Routes[i].BackStartTime;
						int fChild = Random.Shared.Next(2) == 0 ? parent1.Routes[i].Headway : parent2.Routes[i].Headway;
						Routes.Add(new GenomeRoute(parent1.Routes[i].OnRoute, s1Child, parent1.Routes[i].BackRoute, s2Child, fChild));
					}
					break;
				default: break;
			}
			CalculateFitnessValue(parameters, network);
		}


		//Mutation
		public Genome(int id, int generation, Genome parent, in OptimizationParameters parameters, in NetworkInformation network ,string step)
		{
			GenomeID = id;
			GenerationID = generation;
			Parents.Add(parent.GenomeID);
			switch (step)
			{
				case "route": 
					for (int i = 0; i < parameters.Genome_RouteCount; i++)
					{
						var mutationResult = PerformRouteMutation(parent.Routes[i], network);
						if (mutationResult.IsFailure)
							throw new Exception($"Genome (ID: {id}, Generation: {GenerationID}) route crossing failed when performing crossover, error: {mutationResult.Error}");
						Routes.Add(mutationResult.Value);
					}
					break;
					
				case "time":
					for (int i = 0;	i< parameters.Genome_RouteCount; i++)
					{
						int uS = Random.Shared.Next(-30, 31);
						int uF = Random.Shared.Next(5, 31);

						int newS1 = (parent.Routes[i].OnStartTime + uS + 60) % 60;
						int newS2 = (parent.Routes[i].BackStartTime + uS + 60) % 60;
						int newF = Math.Clamp(parent.Routes[i].Headway + (Random.Shared.Next(2) == 0 ? uF : -uF), 5, 60);

						Routes.Add(new GenomeRoute(parent.Routes[i].OnRoute, newS1, parent.Routes[i].BackRoute, newS2, newF));
					}
					
					break;
				default: break;
			}
			CalculateFitnessValue(parameters, network);
		}


		//Fitness function
		public void CalculateFitnessValue(in OptimizationParameters parameters, in NetworkInformation network) { 
		
			throw new NotImplementedException();
		}


		//helper functions
		private Result<GenomeRoute> PerformCrossover(GenomeRoute route1, GenomeRoute route2, NetworkInformation network)
		{
			var vStart1 = route1.OnRoute.First();
			var vEnd1 = route1.OnRoute.Last();
			var vStart2 = route2.OnRoute.First();
			var vEnd2 = route2.OnRoute.Last();

			// Mind2 végállomás megegyezik
			if (vStart1 == vStart2 && vEnd1 == vEnd2)
			{
				return Random.Shared.Next(2) == 0 ? route1 : route2;
			}

			// 2. Közös megálló keresése (mn) -> ugyanaz van ha van egy közös megálló és ha nincs
			var commonStops = route1.OnRoute.Intersect(route2.OnRoute).ToList();
			commonStops.Remove(vStart1);
			commonStops.Remove(vEnd1);
			commonStops.Remove(vStart2);
			commonStops.Remove(vEnd2);
			var CommonStopCount = commonStops.Count;
			if (CommonStopCount > 0)
			{
				Guid mn = commonStops[Random.Shared.Next(CommonStopCount)];
				List<Guid> ChildOnRoute;
				List<List<Guid>> splits = new List<List<Guid>>();
				splits.Add(route1.OnRoute.Slice(0, route1.OnRoute.IndexOf(mn)));
				splits.Add(route1.OnRoute.Slice(route1.OnRoute.IndexOf(mn) + 1, route1.OnRoute.Count));
				splits.Add(route1.OnRoute.Slice(0, route2.OnRoute.IndexOf(mn)));
				splits.Add(route1.OnRoute.Slice(route2.OnRoute.IndexOf(mn) + 1, route2.OnRoute.Count));

				int i = Random.Shared.Next(4);
				ChildOnRoute = splits[i];
				splits.Remove(splits[i]);
				i = Random.Shared.Next(3);
				ChildOnRoute.Concat(splits[i]);

				var ChildBackRouteResult = GetBackRoute(ChildOnRoute, network);
				if (ChildBackRouteResult.IsFailure)
				{
					return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
				}
				return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route1.OnStartTime, ChildBackRouteResult.Value, route1.BackStartTime ,route1.Headway));
			}

			// 3. Fizikai él mentén történő összekötés (mx -> my) -> nincs semmilyen szinten közös megálló
			foreach (var mx in route1.OnRoute)
			{
				if (network.StopConnectivityMatrix.TryGetValue(mx, out var neighbors))
				{
					foreach (var edge in neighbors)
					{
						if (route2.OnRoute.Contains(edge.Destination))
						{
							var ChildOnRoute = route1.OnRoute.TakeWhile(s => s != mx).ToList();
							ChildOnRoute.Add(mx);
							ChildOnRoute.AddRange(route2.OnRoute.SkipWhile(s => s != edge.Destination));
							var ChildBackRouteResult = GetBackRoute(ChildOnRoute, network);
							if (ChildBackRouteResult.IsFailure)
							{
								return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
							}
							return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route1.OnStartTime, ChildBackRouteResult.Value, route1.BackStartTime, route1.Headway));
						}
					}
				}
			}

			return Result<GenomeRoute>.Success(Random.Shared.Next(2) == 0 ? route1 : route2); // Fallback
		}

		private Result<GenomeRoute> PerformRouteMutation(GenomeRoute route, in NetworkInformation network)
		{
			var newTerminalList = network.Terminals;
			newTerminalList.Remove(route.OnRoute[0]);
			newTerminalList.Remove(route.OnRoute[route.OnRoute.Count - 1]);

			var newTerminal = newTerminalList[Random.Shared.Next(newTerminalList.Count)];
			var splitNodeIndex = Random.Shared.Next(route.OnRoute.Count - 6);
			var splitNode = route.OnRoute[splitNodeIndex + 3];

			List<Guid> ChildOnRoute = new List<Guid>();
			if(splitNodeIndex < route.OnRoute.Count / 2)
			{
				List<Guid> remainingSplitRoute = route.OnRoute.TakeWhile(s => s != splitNode).ToList();
				var pathResult = GAUtil.GetShortestPath(splitNode, newTerminal, network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				ChildOnRoute = remainingSplitRoute.Concat(pathResult.Value).ToList();
			}
			else
			{
				var remainingSplitRoute = route.OnRoute.SkipWhile(s => s != splitNode).ToList();
				var pathResult = GAUtil.GetShortestPath(newTerminal, splitNode, network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				ChildOnRoute = pathResult.Value.Concat(remainingSplitRoute).ToList();
			}

			var ChildBackRouteResult = GetBackRoute(ChildOnRoute, network);
			if (ChildBackRouteResult.IsFailure)
			{
				return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
			}
			return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route.OnStartTime, ChildBackRouteResult.Value, route.BackStartTime, route.Headway));
		}

		private Result<List<Guid>> GetBackRoute(List<Guid> onRoute, in NetworkInformation network)
		{
			var backRoute = new List<Guid>(onRoute);
			backRoute.Reverse();

			for (int i = 0; i < backRoute.Count - 1; i++)
			{
				var from = backRoute[i];
				var to = backRoute[i + 1];

				if (!network.StopConnectivityMatrix.TryGetValue(from, out var neighbors))
					return Result<List<Guid>>.Failure($"Couldn't get stop's (GUID: {from}) neighbours from connectivity matrix");

				bool direct = false;
				for (int n = 0; n < neighbors.Count; n++)
				{
					if (neighbors[n].Destination == to)
					{
						direct = true;
						break;
					}
				}

				if (direct)
					continue;

				var pathResult = GAUtil.GetShortestPath(from, to, network);
				if (pathResult.IsFailure)
					return Result<List<Guid>>.Failure(pathResult.Error);

				var path = pathResult.Value;

				if (path.Count < 3)
				{
					backRoute.RemoveAt(i + 1);

					for (int p = 1; p < path.Count; p++)
						backRoute.Insert(i + p, path[p]);

					continue;
				}

				if (i + 2 < backRoute.Count)
				{
					var next = backRoute[i + 2];

					pathResult = GAUtil.GetShortestPath(from, next, network);
					if (pathResult.IsFailure)
						return Result<List<Guid>>.Failure(pathResult.Error);

					path = pathResult.Value;

					if (path.Count < 3)
					{
						backRoute.RemoveAt(i + 1);

						for (int p = 1; p < path.Count; p++)
							backRoute.Insert(i + p, path[p]);

						continue;
					}
				}

				return Result<List<Guid>>.Failure("Backroute is not possible to generate, deviation from route is over threshold");
			}

			return Result<List<Guid>>.Success(backRoute);
		}





	}
}
