using System;
using System.Collections.Generic;
using System.Text;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.ga
{
	public class GAUtil
	{
		public static Result<List<Guid>> GetShortestPath(Guid start, Guid end, in NetworkInformation network)
		{
			var distances = new Dictionary<Guid, double>();
			var previousNodes = new Dictionary<Guid, Guid>();
			var priorityQueue = new PriorityQueue<Guid, double>();

			// Távolságok inicializálása
			foreach (var node in network.StopConnectivityMatrix.Keys)
			{
				distances[node] = double.MaxValue;
			}

			distances[start] = 0;
			priorityQueue.Enqueue(start, 0);

			while (priorityQueue.Count > 0)
			{
				var current = priorityQueue.Dequeue();

				if (current == end) break;
				if (!network.StopConnectivityMatrix.ContainsKey(current)) continue;

				foreach (var (Destination, Weight) in network.StopConnectivityMatrix[current])
				{
					double alt = distances[current] + Weight;

					// Relaxáció
					if (!distances.TryGetValue(Destination, out double value) || alt < value)
					{
						value = alt;
						distances[Destination] = value;
						previousNodes[Destination] = current;
						priorityQueue.Enqueue(Destination, alt);
					}
				}
			}

			// Útvonal visszafejtése
			var path = new List<Guid>();
			var curr = end;
			if (!previousNodes.ContainsKey(curr) && curr != start)
				return Result<List<Guid>>.Failure("No path found");

			while (curr != start)
			{
				path.Add(curr);
				curr = previousNodes[curr];
			}
			path.Add(start);
			path.Reverse();

			return Result<List<Guid>>.Success(path);
		}

		public static Result<Genome> TournamentSelect(in List<Genome> pop, int k, in Random rnd)
		{
			Genome? best = null;
			for (int i = 0; i < k; i++)
			{
				var candidate = pop[rnd.Next(pop.Count)];
				if (best == null || candidate.FitnessValue < best.FitnessValue)
				{
					best = candidate;
				}
			}
			if (best != null)
				return Result<Genome>.Success(best);
			
			return Result<Genome>.Failure("Couldn't select Genome at TournamentSelect");
		}

		public static Result<GenomeRoute> PerformRouteInitialization(in NetworkInformation network, in OptimizationParameters parameters)
		{
			// choose 2 on random from Terminals
			var fromTerminal = network.Terminals[Random.Shared.Next(0, network.Terminals.Count)];
			var toTerminal = network.Terminals[Random.Shared.Next(0, network.Terminals.Count)];

			// choose hubCount number of stops from Hub on random
			List<Guid> hubs = [];
			int i = 0;
			while (i < parameters.Genome_HubNumberInRoute)
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
				pathResult = GAUtil.GetShortestPath(hubs[i - 1], hubs[i], network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				OnRoute = [.. OnRoute, .. pathResult.Value];
				i++;
			}
			pathResult = GAUtil.GetShortestPath(hubs[i - 1], toTerminal, network);
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
				return Result<GenomeRoute>.Success(new GenomeRoute(OnRoute, Random.Shared.Next(0, 59), Random.Shared.Next(0, 59)));
			}
			else
			{
				//set starttime and headway to random values
				return Result<GenomeRoute>.Success(new GenomeRoute(OnRoute, Random.Shared.Next(0, 59), ChildBackRouteResult.Value, Random.Shared.Next(0, 59), Random.Shared.Next(0, 59)));
			}
		}


		public static Result<GenomeRoute> PerformCrossover(in GenomeRoute route1, in GenomeRoute route2, in NetworkInformation network, in OptimizationParameters parameters)
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

				if (ChildBackRouteResult.Value == null)
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

							if (ChildBackRouteResult.Value == null)
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

		public static Result<GenomeRoute> PerformRouteMutation(in GenomeRoute route, in NetworkInformation network, in OptimizationParameters parameters)
		{
			var newTerminalList = network.Terminals;
			newTerminalList.Remove(route.OnRoute[0]);
			newTerminalList.Remove(route.OnRoute[^1]);

			var newTerminal = newTerminalList[Random.Shared.Next(newTerminalList.Count)];
			var splitNodeIndex = Random.Shared.Next(route.OnRoute.Count - 6);
			var splitNode = route.OnRoute[splitNodeIndex + 3];

			List<Guid> ChildOnRoute = [];
			if (splitNodeIndex < route.OnRoute.Count / 2)
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

		public static Result<List<Guid>?> GetBackRoute(in List<Guid> onRoute, in NetworkInformation network, in OptimizationParameters parameters)
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
