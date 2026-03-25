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
			if (!network.StopConnectivityMatrix.ContainsKey(start) || !network.StopConnectivityMatrix.ContainsKey(end))
				return Result<List<Guid>>.Failure("The start and/or end stop is not found in the network");

			var distances = new Dictionary<Guid, double>();
			var previous = new Dictionary<Guid, Guid?>();

			var queue = new PriorityQueue<Guid, double>();

			// Initialize
			foreach (var node in network.StopConnectivityMatrix.Keys)
			{
				distances[node] = double.MaxValue;
				previous[node] = null;
			}

			distances[start] = 0;
			queue.Enqueue(start, 0);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();

				// Early exit → BIG performance win
				if (current == end)
					break;

				if (!network.StopConnectivityMatrix.TryGetValue(current, out var neighbors))
					continue;

				foreach (var (neighbor, weight) in neighbors)
				{
					var newDist = distances[current] + weight;

					if (newDist < distances[neighbor])
					{
						distances[neighbor] = newDist;
						previous[neighbor] = current;
						queue.Enqueue(neighbor, newDist);
					}
				}
			}

			// No path
			if (distances[end] == double.MaxValue)
				return Result<List<Guid>>.Failure("No path found");

			// Reconstruct path
			var path = new List<Guid>();
			for (var at = end; at != null; at = previous[at].GetValueOrDefault())
			{
				path.Add(at);
				if (at == start) break;
			}

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

			List<Guid> randomStops = new List<Guid>();
			i = 0;
			while (i < parameters.Genome_HubNumberInRoute+1)
			{
				randomStops.Add(network.GenericStops[Random.Shared.Next(0, network.GenericStops.Count)]);
				i++;
			}

			List<Guid> neededStops = new();
			for (int j = 0; j < hubs.Count; j++)
			{
				neededStops.Add(randomStops[j]); // stop
				neededStops.Add(hubs[j]);        // hub
			}

			// add the final stop (since stops = hubs + 1)
			neededStops.Add(randomStops[^1]);

			// based on network connectivity connect the terminals and hubs with the (shortest) possible paths 
			List<Guid> OnRoute = [];
			var pathResult = GAUtil.GetShortestPath(fromTerminal, neededStops[0], network);
			if (pathResult.IsFailure)
				return Result<GenomeRoute>.Failure(pathResult.Error);
			OnRoute = [.. OnRoute, .. pathResult.Value];
			i = 1;
			while (i < hubs.Count)
			{
				pathResult = GAUtil.GetShortestPath(neededStops[i - 1], neededStops[i], network);
				if (pathResult.IsFailure)
					return Result<GenomeRoute>.Failure(pathResult.Error);
				OnRoute = [.. OnRoute, .. pathResult.Value];
				i++;
			}
			pathResult = GAUtil.GetShortestPath(neededStops[i - 1], toTerminal, network);
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

				var index1 = route1.OnRoute.IndexOf(mn);
				var index2 = route2.OnRoute.IndexOf(mn);

				splits.Add(route1.OnRoute[..index1]);
				splits.Add(route2.OnRoute[..index2]);

				if (index1 != route1.OnRoute.Count - 1)
				{
					splits.Add(route1.OnRoute.Slice(index1 + 1, route1.OnRoute.Count - (index1 + 1)));
					splits.Add(route2.OnRoute.Slice(index2 + 1, route2.OnRoute.Count - (index2 + 1)));
				}

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

		public static Result<GenomeRoute> PerformRouteMutation(in GenomeRoute route, List<Guid> unMetStopList, in NetworkInformation network, in OptimizationParameters parameters)
		{
			var newTerminalList = network.Terminals;
			newTerminalList.Remove(route.OnRoute[0]);
			newTerminalList.Remove(route.OnRoute[^1]);

			var newTerminal = newTerminalList[Random.Shared.Next(newTerminalList.Count)];
			var splitNodeIndex = Random.Shared.Next(route.OnRoute.Count - 6);
			var splitNode = route.OnRoute[splitNodeIndex + 3];
			int remainingLength = route.OnRoute.Count - (splitNodeIndex + 3);

			List<Guid> includedStops = new List<Guid>();
			int i = 0;
			int includecount = (remainingLength / 3) + Random.Shared.Next(0, 2);
			while (i < includecount + 1)
			{
				includedStops.Add(unMetStopList[Random.Shared.Next(0, unMetStopList.Count)]);
				i++;
			}


			List<Guid> ChildOnRoute = [];

			List<Guid> remainingSplitRoute = [.. route.OnRoute.TakeWhile(s => s != splitNode)];

			List<Guid> path = new();
			Guid current = newTerminal;

			// go through included stops
			foreach (var stop in includedStops)
			{
				var segment = GAUtil.GetShortestPath(current, stop, network);
				if (segment.IsFailure)
					return Result<GenomeRoute>.Failure(segment.Error);

				if (path.Count > 0)
					path.AddRange(segment.Value.Skip(1));
				else
					path.AddRange(segment.Value);

				current = stop;
			}

			// final segment to splitNode
			var lastSegment = GAUtil.GetShortestPath(current, splitNode, network);
			if (lastSegment.IsFailure)
				return Result<GenomeRoute>.Failure(lastSegment.Error);

			if (path.Count > 0)
				path.AddRange(lastSegment.Value.Skip(1));
			else
				path.AddRange(lastSegment.Value);

			ChildOnRoute = [.. remainingSplitRoute, .. path];

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

				var pathResult = GetShortestPath(from, to, network);
				if (pathResult.IsFailure)
					return Result<List<Guid>?>.Failure(pathResult.Error);

				var path = pathResult.Value;

				if (path.Count < 4)
				{
					backRoute.RemoveAt(i + 1);

					for (int p = 1; p < path.Count; p++)
						backRoute.Insert(i + p, path[p]);

					continue;
				}

				int nexthop = 2;
				bool foundBackroute = false;
				bool keepon = true;
				int trycount = 0;

				while (i + nexthop < backRoute.Count && !foundBackroute && keepon)
				{
					var next = backRoute[i + nexthop];

					pathResult = GetShortestPath(from, next, network);
					if (pathResult.IsFailure)
						return Result<List<Guid>?>.Failure(pathResult.Error);

					path = pathResult.Value;

					if (path.Count < nexthop + 4)
					{
						backRoute.RemoveRange(i, nexthop-1);

						for (int p = 1; p < path.Count; p++)
							backRoute.Insert(i + p, path[p]);

						foundBackroute = true;
					}

					if(trycount > 6)
					{
						keepon = false;
					}

					trycount++;
					nexthop++;
				}

				if (foundBackroute)
				{
					continue;
				}

				if (parameters.Genome_AllowOneWayRoutes && !foundBackroute)
				{
					return Result<List<Guid>?>.Success(null);
				}

				return Result<List<Guid>?>.Failure("Backroute is not possible to generate, deviation from route is over threshold");
			}

			return Result<List<Guid>?>.Success(backRoute);
		}
	}
}
