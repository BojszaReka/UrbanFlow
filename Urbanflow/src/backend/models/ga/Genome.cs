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
		public double FitnessValue { get; }

		//helper values
		public List<int> Parents { get; } = [];
		public Guid StartTerminal { get;  }
		public Guid EndTerminal { get;  }
		public List<Guid> MetHubs { get; } = [];
		

		//initialization
		public Genome(int id, int generation, in OptimizationParameters parameters, in NetworkInformation network)
		{
			GenomeID = id;
			GenerationID = generation;

			int i = 0;
			int failures = 0;
			while (i < parameters.Genome_RouteCount && failures<parameters.Genome_RouteCount*2) {
				var routeInitializationResult = PerformRouteInitialization(network, parameters);
				if (routeInitializationResult.IsFailure) failures++;
				if (routeInitializationResult.IsSuccess)
				{
					MutableRoutes.Add(routeInitializationResult.Value);
					i++;
				}
			}			
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
						var crossoverResult = PerformCrossover(parent1.MutableRoutes[i], parent2.MutableRoutes[i], network, parameters);
						int trycount = 0;
						while (crossoverResult.IsFailure && trycount < 4)
						{
							crossoverResult = PerformCrossover(parent1.MutableRoutes[i], parent2.MutableRoutes[i], network, parameters);
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
			CalculateFitnessValue(parameters, network);
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
						var mutationResult = PerformRouteMutation(parent.MutableRoutes[i], network, parameters);
						int trycount = 0;
						while (mutationResult.IsFailure)
						{
							mutationResult = PerformRouteMutation(parent.MutableRoutes[i], network, parameters);
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
			CalculateFitnessValue(parameters, network);
		}


		//Fitness function
		public void CalculateFitnessValue(in OptimizationParameters parameters, in NetworkInformation network) { 
		
			throw new NotImplementedException();
		}


		//helper functions
		private static Result<GenomeRoute> PerformRouteInitialization(in NetworkInformation network, in OptimizationParameters parameters)
		{
			// choose 2 on random from Terminals
			var fromTerminal = network.Terminals[Random.Shared.Next(0, network.Terminals.Count)];
			var toTerminal = network.Terminals[Random.Shared.Next(0, network.Terminals.Count)];

			// choose hubCount number of stops from Hub on random
			List<Guid> hubs = [];
			int i = 0;
			while(i < parameters.Genome_HubNumberInRoute)
			{
				hubs.Add(network.Hubs[Random.Shared.Next(0, network.Hubs.Count)]);
				i++;
			}

			// based on network connectivity connect the terminals and hubs with the (shortest) possible paths 
			List<Guid> OnRoute = [];
			var pathResult = GAUtil.GetShortestPath(fromTerminal, hubs[0], network);
			if (pathResult.IsFailure)
				return Result<GenomeRoute>.Failure(pathResult.Error);
			OnRoute = [.. OnRoute, .. pathResult.Value];
			i = 1;
			while (i < hubs.Count)
			{
				pathResult = GAUtil.GetShortestPath(hubs[i-1], hubs[i], network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				OnRoute = [.. OnRoute, .. pathResult.Value];
				i++;
			}
			pathResult = GAUtil.GetShortestPath(hubs[i-1], toTerminal, network);
			if (pathResult.IsFailure)
				return Result<GenomeRoute>.Failure(pathResult.Error);
			OnRoute = [.. OnRoute, .. pathResult.Value];

			// get the the inverse of the route
			var ChildBackRouteResult = GetBackRoute(OnRoute, network, parameters);
			if (ChildBackRouteResult.IsFailure)
			{
				return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
			}

			if (ChildBackRouteResult.Value == null)
			{
				//set starttime and headway to random values
				return Result<GenomeRoute>.Success(new GenomeRoute(OnRoute, Random.Shared.Next(0,59), Random.Shared.Next(0, 59)));
			}
			else
			{
				//set starttime and headway to random values
				return Result<GenomeRoute>.Success(new GenomeRoute(OnRoute, Random.Shared.Next(0, 59), ChildBackRouteResult.Value, Random.Shared.Next(0, 59), Random.Shared.Next(0, 59)));
			}
		}


		private static Result<GenomeRoute> PerformCrossover(GenomeRoute route1, GenomeRoute route2, in NetworkInformation network, in OptimizationParameters parameters)
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
				List<List<Guid>> splits = [];
				splits.Add(route1.OnRoute[..route1.OnRoute.IndexOf(mn)]);
				splits.Add(route1.OnRoute.Slice(route1.OnRoute.IndexOf(mn) + 1, route1.OnRoute.Count));
				splits.Add(route1.OnRoute[..route2.OnRoute.IndexOf(mn)]);
				splits.Add(route1.OnRoute.Slice(route2.OnRoute.IndexOf(mn) + 1, route2.OnRoute.Count));

				int i = Random.Shared.Next(4);
				ChildOnRoute = splits[i];
				splits.Remove(splits[i]);
				i = Random.Shared.Next(3);
				ChildOnRoute = [.. ChildOnRoute, .. splits[i]];

				var ChildBackRouteResult = GetBackRoute(ChildOnRoute, network, parameters);
				if (ChildBackRouteResult.IsFailure)
				{
					return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
				}

				if(ChildBackRouteResult.Value == null)
				{
					return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route1.OnStartTime, route1.Headway));
				}
				else
				{
					return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route1.OnStartTime, ChildBackRouteResult.Value, route1.BackStartTime, route1.Headway));
				}
				
			}

			// 3. Fizikai él mentén történő összekötés (mx -> my) -> nincs semmilyen szinten közös megálló
			foreach (var mx in route1.OnRoute)
			{
				if (network.StopConnectivityMatrix.TryGetValue(mx, out var neighbors))
				{
					foreach (var (Destination, Weight) in neighbors)
					{
						if (route2.OnRoute.Contains(Destination))
						{
							var ChildOnRoute = route1.OnRoute.TakeWhile(s => s != mx).ToList();
							ChildOnRoute.Add(mx);
							ChildOnRoute.AddRange(route2.OnRoute.SkipWhile(s => s != Destination));
							var ChildBackRouteResult = GetBackRoute(ChildOnRoute, network, parameters);
							if (ChildBackRouteResult.IsFailure)
							{
								return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
							}

							if(ChildBackRouteResult.Value == null)
							{
								return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route1.OnStartTime, route1.Headway));
							}
							else
							{
								return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route1.OnStartTime, ChildBackRouteResult.Value, route1.BackStartTime, route1.Headway));
							}
							
						}
					}
				}
			}

			return Result<GenomeRoute>.Success(Random.Shared.Next(2) == 0 ? route1 : route2); // Fallback
		}

		private static Result<GenomeRoute> PerformRouteMutation(GenomeRoute route, in NetworkInformation network, in OptimizationParameters parameters)
		{
			var newTerminalList = network.Terminals;
			newTerminalList.Remove(route.OnRoute[0]);
			newTerminalList.Remove(route.OnRoute[^1]);

			var newTerminal = newTerminalList[Random.Shared.Next(newTerminalList.Count)];
			var splitNodeIndex = Random.Shared.Next(route.OnRoute.Count - 6);
			var splitNode = route.OnRoute[splitNodeIndex + 3];

			List<Guid> ChildOnRoute = [];
			if(splitNodeIndex < route.OnRoute.Count / 2)
			{
				List<Guid> remainingSplitRoute = [.. route.OnRoute.TakeWhile(s => s != splitNode)];
				var pathResult = GAUtil.GetShortestPath(splitNode, newTerminal, network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				ChildOnRoute = [.. remainingSplitRoute, .. pathResult.Value];
			}
			else
			{
				var remainingSplitRoute = route.OnRoute.SkipWhile(s => s != splitNode).ToList();
				var pathResult = GAUtil.GetShortestPath(newTerminal, splitNode, network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				ChildOnRoute = [.. pathResult.Value, .. remainingSplitRoute];
			}

			var ChildBackRouteResult = GetBackRoute(ChildOnRoute, network, parameters);
			if (ChildBackRouteResult.IsFailure)
			{
				return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
			}

			if (ChildBackRouteResult.Value == null)
			{
				return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route.OnStartTime, route.Headway));
			}
			else
			{
				return Result<GenomeRoute>.Success(new GenomeRoute(ChildOnRoute, route.OnStartTime, ChildBackRouteResult.Value, route.BackStartTime, route.Headway));
			}			
		}

		private static Result<List<Guid>?> GetBackRoute(List<Guid> onRoute, in NetworkInformation network, in OptimizationParameters parameters)
		{
			var backRoute = new List<Guid>(onRoute);
			backRoute.Reverse();

			for (int i = 0; i < backRoute.Count - 1; i++)
			{
				var from = backRoute[i];
				var to = backRoute[i + 1];

				if (!network.StopConnectivityMatrix.TryGetValue(from, out var neighbors))
					return Result<List<Guid>?>.Failure($"Couldn't get stop's (GUID: {from}) neighbours from connectivity matrix");

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
					return Result<List<Guid>?>.Failure(pathResult.Error);

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
						return Result<List<Guid>?>.Failure(pathResult.Error);

					path = pathResult.Value;

					if (path.Count < 3)
					{
						backRoute.RemoveAt(i + 1);

						for (int p = 1; p < path.Count; p++)
							backRoute.Insert(i + p, path[p]);

						continue;
					}
				}

				if (parameters.Genome_AllowOneWayRoutes)
				{
					return Result<List<Guid>?>.Success(null);
				}

				return Result<List<Guid>?>.Failure("Backroute is not possible to generate, deviation from route is over threshold");
			}

			return Result<List<Guid>?>.Success(backRoute);
		}





	}
}
