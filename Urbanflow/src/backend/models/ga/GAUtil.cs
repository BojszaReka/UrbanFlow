using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;
using System.Windows;
using Urbanflow.src.backend.models.util;
using yWorks.Layout.Graph;
using yWorks.Utils;

namespace Urbanflow.src.backend.models.ga
{
	public class GAUtil
	{
		public static Result<List<Guid>> GetShortestPath(Guid start, Guid end, in NetworkInformation network)
		{
			if (!network.StopConnectivityMatrix.ContainsKey(start) || !network.StopConnectivityMatrix.ContainsKey(end))
				return Result<List<Guid>>.Failure("The start and/or end stop is not found in the network");

			if (network.CachedShortestPaths.TryGetValue((start, end), out var cachedPath))
			{
				return Result<List<Guid>>.Success(new List<Guid>(cachedPath));
			}

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

			const int MaxCacheSize = 100_000;
			if (network.CachedShortestPaths.Count > MaxCacheSize)
			{
				network.CachedShortestPaths.Clear(); 
			}

			var tempPath = new List<Guid>();
			for (var at = end; at != Guid.Empty; at = previous[at].GetValueOrDefault())
			{
				tempPath.Add(at);
				if (at == start) break;
			}

			tempPath.Reverse();

			var pathArray = tempPath.ToArray();

			if(pathArray.Length >= 5)
			{
				network.CachedShortestPaths[(start, end)] = pathArray;
			}

			return Result<List<Guid>>.Success(tempPath);
		}

		public static Result<List<Guid>> CreatePath(Guid start, Guid end, in List<Guid> includedStops, in NetworkInformation network)
		{
			try
			{
				var pathResult = GetShortestPath(start, end, network);
				if (pathResult.IsFailure)
					return Result<List<Guid>>.Failure(pathResult.Error);
				var directPath = pathResult.Value;

				if(includedStops.Count == 0)
				{
					return Result<List<Guid>>.Success(directPath);
				}

				bool isStopMissing = false;
				for (int i = 0; i < includedStops.Count && !isStopMissing; i++)
				{
					if (!directPath.Contains(includedStops[i]))
					{
						isStopMissing = true;
					}
				}

				if (!isStopMissing)
				{
					return Result<List<Guid>>.Success(directPath);
				}

				// less included stops
				if (includedStops.Count < 3)
				{
					List<Guid> pathVariation1 = [];
					List<Guid> pathVariation2 = [];

					// Create pathvariation1
					pathResult = GetShortestPath(start, includedStops[0], network);
					if (pathResult.IsFailure)
						return Result<List<Guid>>.Failure(pathResult.Error);
					pathVariation1 = pathResult.Value;
					var lastStop = includedStops[0];
					for (int i = 0; i < includedStops.Count - 1; i++)
					{
						var nextStop = includedStops[i + 1];
						if (pathVariation1.Contains(nextStop))
						{
							continue;
						}

						pathResult = GetShortestPath(lastStop, nextStop, network);
						if (pathResult.IsFailure)
							return Result<List<Guid>>.Failure(pathResult.Error);
						pathVariation1 = [.. pathVariation1, .. pathResult.Value.Skip(1)];
						lastStop = nextStop;
					}
					pathResult = GetShortestPath(lastStop, end, network);
					if (pathResult.IsFailure)
						return Result<List<Guid>>.Failure(pathResult.Error);
					pathVariation1 = [.. pathVariation1, .. pathResult.Value.Skip(1)];


					//Create pathvariation2
					pathResult = GetShortestPath(start, includedStops[^1], network);
					if (pathResult.IsFailure)
						return Result<List<Guid>>.Failure(pathResult.Error);
					pathVariation2 = pathResult.Value;
					lastStop = includedStops[^1];
					for (int i = includedStops.Count - 1; i > 1; i--)
					{
						var nextStop = includedStops[i - 1];
						if (pathVariation1.Contains(nextStop))
						{
							continue;
						}
						pathResult = GetShortestPath(lastStop, nextStop, network);
						if (pathResult.IsFailure)
							return Result<List<Guid>>.Failure(pathResult.Error);
						pathVariation2 = [.. pathVariation2, .. pathResult.Value.Skip(1)];
						lastStop = nextStop;
					}
					pathResult = GetShortestPath(lastStop, end, network);
					if (pathResult.IsFailure)
						return Result<List<Guid>>.Failure(pathResult.Error);
					pathVariation2 = [.. pathVariation2, .. pathResult.Value.Skip(1)];

					if (pathVariation1.Count > pathVariation2.Count)
					{
						RemoveLoops(pathVariation2);
						return Result<List<Guid>>.Success(pathVariation2);
					}
					else
					{
						RemoveLoops(pathVariation1);
						return Result<List<Guid>>.Success(pathVariation1);
					}
				}
				//more pathVariations
				else
				{
					List<List<Guid>> pathVariations = [];
					int tryCount = 4;
					while (tryCount > 0)
					{
						var tempIncludedStops = new List<Guid>(includedStops);
						List<Guid> pathVariation = [];

						var nextStop = tempIncludedStops[Random.Shared.Next(tempIncludedStops.Count)];
						pathResult = GetShortestPath(start, nextStop, network);
						if (pathResult.IsFailure)
							return Result<List<Guid>>.Failure(pathResult.Error);
						tempIncludedStops.Remove(nextStop);
						var previousStop = nextStop;
						pathVariation = pathResult.Value;
						while (tempIncludedStops.Count > 0)
						{
							nextStop = tempIncludedStops[Random.Shared.Next(tempIncludedStops.Count)];
							if (!pathVariation.Contains(nextStop))
							{
								pathResult = GetShortestPath(previousStop, nextStop, network);
								if (pathResult.IsFailure)
									return Result<List<Guid>>.Failure(pathResult.Error);
								pathVariation = [.. pathVariation, .. pathResult.Value.Skip(1)];
								previousStop = nextStop;
							}							
							tempIncludedStops.Remove(nextStop);
						}
						pathResult = GetShortestPath(previousStop, end, network);
						if (pathResult.IsFailure)
							return Result<List<Guid>>.Failure(pathResult.Error);
						pathVariation = [.. pathVariation, .. pathResult.Value.Skip(1)];

						pathVariations.Add(pathVariation);
						tryCount--;
					}

					int bestLength = int.MaxValue;
					List<Guid> finalPath = [];
					foreach (var path in pathVariations)
					{
						if (path.Count < bestLength)
						{
							finalPath = path;
							bestLength = path.Count;
						}
					}
					RemoveLoops(finalPath);
					return Result<List<Guid>>.Success(finalPath);
				}
			}catch(Exception ex)
			{
				return Result<List<Guid>>.Failure("Creating path failed: "+ex.Message);
			}			
		}

		public static void RemoveLoops(in List<Guid> path)
		{
			bool removeLoops = Random.Shared.Next(2) == 0;

			if (path.Count < 7 || removeLoops) // nothing to do if too small
				return;

			var seen = new Dictionary<Guid, int>();

			int i = 2;
			while (i < path.Count-2)
			{
				var current = path[i];

				if (seen.TryGetValue(current, out int firstIndex))
				{
					// Remove everything between firstIndex and i
					int removeStart = firstIndex + 1;
					int removeCount = i - firstIndex - 1;

					if (removeCount > 0)
					{
						path.RemoveRange(removeStart, removeCount);

						// Reset and restart (simpler + safe)
						seen.Clear();
						i = 3;
						continue;
					}
				}
				else
				{
					seen[current] = i;
				}

				i++;
			}
		}

		public static Result<Genome> TournamentSelect(in List<Genome> parentGenomes, int tryCount, in Random rnd)
		{
			Genome? best = null;
			for (int i = 0; i < tryCount; i++)
			{
				var candidate = parentGenomes[rnd.Next(parentGenomes.Count)];
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
			try
			{
				var random = Random.Shared;
				var terminals = network.Terminals;
				var hubList = new List<Guid>(network.Hubs);
				var stopList = new List<Guid>(network.GenericStops);

				var fromTerminal = terminals[random.Next(0, network.Terminals.Count)];
				var toTerminal = terminals[random.Next(0, network.Terminals.Count)];

				List<Guid> neededStops = [];
				int i = 0;
				while (i < parameters.Genome_HubNumberInRoute)
				{
					var hub = hubList[random.Next(hubList.Count)];
					var stop = stopList[random.Next(stopList.Count)];
					neededStops.Add(stop);
					neededStops.Add(hub);
					hubList.Remove(hub);
					stopList.Remove(stop);
					i++;
				}
				var LastStop = stopList[random.Next(stopList.Count)];
				neededStops.Add(LastStop);

				if (neededStops.Count == 0)
				{
					return Result<GenomeRoute>.Failure("Route initialization couldn't gather stops");
				}

				var finalPathResult = CreatePath(fromTerminal, toTerminal, neededStops, network);
				if (finalPathResult.IsFailure)
					return Result<GenomeRoute>.Failure(finalPathResult.Error);
				List<Guid> OnRoute = finalPathResult.Value;

				var ChildBackRouteResult = GetBackRoute(OnRoute, network, parameters);
				if (ChildBackRouteResult.IsFailure)
				{
					return Result<GenomeRoute>.Failure(ChildBackRouteResult.ErrorCode);
				}

				if (ChildBackRouteResult.Value == null)
				{
					return Result<GenomeRoute>.Success(new GenomeRoute(OnRoute, random.Next(0, 59), random.Next(0, 59)));
				}
				else
				{
					return Result<GenomeRoute>.Success(new GenomeRoute(OnRoute, random.Next(0, 59), ChildBackRouteResult.Value, random.Next(0, 59), random.Next(0, 59)));
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Route initialization unsucsessful: " + ex.Message);
			}
		}


		public static Result<GenomeRoute> PerformCrossover(in GenomeRoute route1, in GenomeRoute route2, in NetworkInformation network, in OptimizationParameters parameters)
		{
			var vStart1 = route1.OnRoute.First();
			var vEnd1 = route1.OnRoute.Last();
			var vStart2 = route2.OnRoute.First();
			var vEnd2 = route2.OnRoute.Last();

			if (vStart1 == vStart2 && vEnd1 == vEnd2)
			{
				return Random.Shared.Next(2) == 0 ? route1 : route2;
			}

			var commonStops = route1.OnRoute.Intersect(route2.OnRoute).ToList();
			commonStops.Remove(vStart1);
			commonStops.Remove(vEnd1);
			commonStops.Remove(vStart2);
			commonStops.Remove(vEnd2);
			var CommonStopCount = commonStops.Count;
			if (CommonStopCount > 0)
			{
				Guid mn = commonStops[Random.Shared.Next(CommonStopCount)];
				List<Guid> ChildOnRoute = [];
				List<List<Guid>> splits = [];

				var index1 = route1.OnRoute.IndexOf(mn);
				var index2 = route2.OnRoute.LastIndexOf(mn);

				var start1 = route1.OnRoute[..index1];
				var start2 = route2.OnRoute[..index2];

				splits.Add(route1.OnRoute[..index1]);
				splits.Add(route2.OnRoute[..index2]);

				if (index1 != route1.OnRoute.Count - 1)
				{
					splits.Add(route1.OnRoute[(index1 + 1)..]);
					splits.Add(route2.OnRoute[(index2 + 1)..]);
				}

				var selectedIdx1 = Random.Shared.Next(splits.Count);
				var selectedIdx2 = Random.Shared.Next(splits.Count);
				while (selectedIdx1 == selectedIdx2)
				{
					selectedIdx2 = Random.Shared.Next(splits.Count);
				}

				
				if (selectedIdx1 < 2 && selectedIdx2 > 1)
				{
					ChildOnRoute = [.. splits[selectedIdx1], mn, .. splits[selectedIdx2]];
				}
				else if (selectedIdx1 < 2 && selectedIdx2 < 2) 
				{
					var reversedList = splits[selectedIdx2];
					reversedList.Reverse();
					ChildOnRoute = [.. splits[selectedIdx1], mn, .. reversedList];
				}else if (selectedIdx1 > 1 && selectedIdx2 < 2)
				{
					var reversedList1 = splits[selectedIdx1];
					reversedList1.Reverse();
					var reversedList2 = splits[selectedIdx2];
					reversedList2.Reverse();
					ChildOnRoute = [.. reversedList1, mn, .. reversedList2];
				}
				else if(selectedIdx1 > 1 && selectedIdx2 > 1)
				{
					var reversedList = splits[selectedIdx1];
					reversedList.Reverse();
					ChildOnRoute = [.. reversedList, mn, .. splits[selectedIdx2]];
				}

				RemoveLoops(ChildOnRoute);

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
							RemoveLoops(ChildOnRoute);
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

			var randomRoute = Random.Shared.Next(2) == 0 ? route1 : route2;
			return Result<GenomeRoute>.Success(randomRoute); // Fallback
		}

		public static Result<GenomeRoute> PerformRouteMutation(in GenomeRoute route, List<Guid> unMetStopList, in NetworkInformation network, in OptimizationParameters parameters)
		{
			var index = Random.Shared.Next(2);
			if (index == 1)
			{
				return ReorderMutation(route, unMetStopList, network, parameters);
			}
			else
			{
				return SlicingMutation(route, unMetStopList, network, parameters);
			}
		}

		private static Result<GenomeRoute> SlicingMutation(in GenomeRoute route, in List<Guid> unMetStopList, in NetworkInformation network, in OptimizationParameters parameters)
		{
			try
			{
				var newTerminalList = new List<Guid>(network.Terminals);

				if (newTerminalList.Count == 0)
					return Result<GenomeRoute>.Failure("No available terminals.");

				newTerminalList.Remove(route.OnRoute[0]);
				newTerminalList.Remove(route.OnRoute[^1]);

				var newTerminal = newTerminalList[Random.Shared.Next(newTerminalList.Count)];
				int splitNodeIndex = 0;
				Guid splitNode;

				if (route.OnRoute.Count < 6)
				{
					splitNodeIndex = Random.Shared.Next(route.OnRoute.Count);
					splitNode = route.OnRoute[splitNodeIndex];
				}
				else
				{
					splitNodeIndex = Random.Shared.Next(route.OnRoute.Count - 6);
					splitNode = route.OnRoute[splitNodeIndex + 3];
				}

				int remainingLength = route.OnRoute.Count - (splitNodeIndex + 3);

				bool ReplaceTerminalAtEnd = Random.Shared.Next(2) == 1;
				List<Guid> remainingSplitRoute = [];
				if (ReplaceTerminalAtEnd)
				{
					remainingSplitRoute = [.. route.OnRoute.TakeWhile(s => s != splitNode)];
				}
				else
				{
					remainingSplitRoute = [.. route.OnRoute.SkipWhile(s => s != splitNode)];
				}
					
				List<Guid> includedStops = [];
				if (unMetStopList.Count != 0)
				{
					var unmets = new List<Guid>(unMetStopList);
					int i = 0;
					int includecount = Random.Shared.Next(0, remainingLength / 4);
					while (i < includecount + 1 && unmets.Count > 0)
					{
						var stop = unmets[Random.Shared.Next(0, unmets.Count)];
						includedStops.Add(stop);
						unmets.Remove(stop);
						i++;
					}
				}

				List<Guid> ChildOnRoute = [];
				List <Guid> path = [];
				
				if (ReplaceTerminalAtEnd)
				{
					// go through included stops
					var finalPathResult = CreatePath(splitNode, newTerminal, includedStops, network);
					if (finalPathResult.IsFailure)
						return Result<GenomeRoute>.Failure(finalPathResult.Error);
					path = finalPathResult.Value;

					ChildOnRoute = [.. remainingSplitRoute, .. path];
				}
				else
				{
					var finalPathResult = CreatePath(newTerminal, splitNode, includedStops, network);
					if (finalPathResult.IsFailure)
						return Result<GenomeRoute>.Failure(finalPathResult.Error);
					path = finalPathResult.Value;

					ChildOnRoute = [.. path, .. remainingSplitRoute];
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
			catch (Exception ex)
			{
				return Result<GenomeRoute>.Failure("Performing slicing mutation failed: " + ex.Message);
			}
		}

		private static Result<GenomeRoute> ReorderMutation(in GenomeRoute route, in List<Guid> unMetStopList, in NetworkInformation network, in OptimizationParameters parameters)
		{
			if (route.OnRoute == null || route.OnRoute.Count == 0)
			{
				return Result<GenomeRoute>.Failure("Route is empty, can't perform reorder mutation");
			}

			var onRoute = route.OnRoute;

			var fromTerminal = onRoute[0];
			var toTerminal = onRoute[^1];
			var otherStops = new List<Guid>(unMetStopList);

			var hubSet = new HashSet<Guid>(network.Hubs);
			var genericSet = new HashSet<Guid>(network.GenericStops);

			var hubs = new List<Guid>();
			var generalStops = new List<Guid>();

			foreach (var stop in onRoute)
			{
				if (hubSet.Contains(stop))
					hubs.Add(stop);
				else if (genericSet.Contains(stop))
					generalStops.Add(stop);
			}

			int count = 2;
			var newStops = new List<Guid>(count * 2 + hubs.Count);
			for (int i = 0; i < count; i++)
			{
				if(generalStops.Count > 0)
				{
					int idx = Random.Shared.Next(generalStops.Count);
					newStops.Add(generalStops[idx]);
					generalStops[idx] = generalStops[^1];
					generalStops.RemoveAt(generalStops.Count - 1);
				}
				
				if(otherStops.Count > 0)
				{
					int idx = Random.Shared.Next(otherStops.Count);
					newStops.Add(otherStops[idx]);
					otherStops[idx] = otherStops[^1];
					otherStops.RemoveAt(otherStops.Count - 1);
				}
				
			}

			newStops.AddRange(hubs);

			for (int i = newStops.Count - 1; i > 0; i--)
			{
				int j = Random.Shared.Next(i + 1);
				(newStops[i], newStops[j]) = (newStops[j], newStops[i]);
			}

			var finalPathResult = CreatePath(fromTerminal, toTerminal, newStops, network);
			if (finalPathResult.IsFailure)
				return Result<GenomeRoute>.Failure(finalPathResult.Error);
			List<Guid> ChildOnRoute = finalPathResult.Value;

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
			var count = onRoute.Count;
			var backRoute = new List<Guid>(count);
			for (int i = count - 1; i >= 0; i--)
				backRoute.Add(onRoute[i]);

			var endTerminal = backRoute[^1];
			var connectivity = network.StopConnectivityMatrix;

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
					backRoute.InsertRange(i + 1, path.GetRange(1, path.Count - 1));
					continue;
				}

				int nexthop = 2;
				int trycount = 0;

				while (i + nexthop < backRoute.Count - 1 && trycount <= 6)
				{
					var next = backRoute[i + nexthop];

					pathResult = GetShortestPath(from, next, network);
					if (pathResult.IsFailure)
						return Result<List<Guid>?>.Failure(pathResult.Error);

					path = pathResult.Value;

					if (path.Count < nexthop + 4)
					{
						// Remove intermediate nodes in one go
						backRoute.RemoveRange(i + 1, nexthop - 1);
						backRoute.InsertRange(i + 1, path.GetRange(1, path.Count - 1));
						goto ContinueOuterLoop;
					}
					trycount++;
					nexthop++;
				}

				if (parameters.Genome_AllowOneWayRoutes)
				{
					return Result<List<Guid>?>.Success(null);
				}

				return Result<List<Guid>?>.Failure("Backroute is not possible to generate, deviation from route is over threshold");

			ContinueOuterLoop: continue;
			}

			if (!backRoute[^1].Equals(endTerminal))
			{
				var pathResult = GetShortestPath(backRoute[^1], endTerminal, network);
				if (pathResult.IsFailure)
					return Result<List<Guid>?>.Failure(pathResult.Error);

				var path = pathResult.Value;
				backRoute.AddRange(path);
			}

			return Result<List<Guid>?>.Success(backRoute);
		}


	}
}
