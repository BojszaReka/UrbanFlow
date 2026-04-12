using System;
using System.Collections.Generic;
using System.Text;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.backend.models.ga
{
	internal class FitnessCalculator : IDisposable
	{

		private List<GenomeRoute> AllRoutes { get; set; } = [];
		private Dictionary<(Guid, int, int), List<double>> AtStopFromRouteToRouteWaitingTimes { get; set; } = [];

		public double UnMetStopPercentage { get; private set; } = 0.0;
		public List<Guid> UnMetStopList { get; private set; } = [];

		public void Dispose()
		{
			AllRoutes.Clear();
			AtStopFromRouteToRouteWaitingTimes = [];
		}

		public Result<double> CalculateFitnessValue(in Genome genome, in OptimizationParameters parameters, in NetworkInformation network, string step = "", bool withLogging = false)
		{
			try
			{
				switch (step)
				{
					case "route":
						return CalculateRouteFitnessValue(parameters, network, genome, withLogging);

					case "time":
						return CalculateTimeFitnessValue(parameters, network, genome, withLogging);
					default:
						throw new Exception("No such step as: " + step);
				}
				
			}
			catch (Exception ex)
			{
				return Result<double>.Failure("Fitness value calculation failed: " + ex.Message);
			}
		}

		private Result<double> CalculateRouteFitnessValue(in OptimizationParameters parameters, in NetworkInformation network, in Genome genome, bool withLogging)
		{
			try
			{
				double fitnessValue = 0;

				AllRoutes = [.. genome.MutableRoutes];
				int baseCount = AllRoutes.Count;
				foreach (var staticRoute in network.StaticRoutes)
				{
					staticRoute.RouteIndex = baseCount++;
				}

				if (AllRoutes == null || AllRoutes.Count == 0)
				{
					return Result<double>.Failure("No routes to work with.");
				}

				Result<double> result= CalculateRouteFitness(parameters, network, withLogging);
				if (result.IsFailure) return result;
				fitnessValue += result.Value;

				//Hard Constraint: Transfer count over allowed treshold
				result = CalculateHardConstraint_Route_Transfer(parameters, withLogging);
				if (result.IsFailure) return result;
				fitnessValue += result.Value;

				//Soft Constraint: Avarage travel time is optimal
				result = CalculateSoftConstraint_Route_Traveltime(network, withLogging);
				if (result.IsFailure) return result;
				fitnessValue += result.Value;

				if(withLogging)
					OptimizationLoggerService.Instance.Log($"The value of 'route' fitness constrains: {fitnessValue}.");

				return Result<double>.Success(fitnessValue);
			}
			catch (Exception ex)
			{
				throw new Exception("Fitness value calculation failed: " + ex.Message);
			}
		}

		private Result<double> CalculateTimeFitnessValue(in OptimizationParameters parameters, in NetworkInformation network, in Genome genome, bool withLogging)
		{
			try
			{
				double fitnessValue = 0;
				int baseCount = 0;
				foreach (var route in genome.MutableRoutes)
				{
					route.RouteIndex = baseCount++;
					AllRoutes.Add(route);
				}				
				foreach (var staticRoute in network.StaticRoutes)
				{
					staticRoute.RouteIndex = baseCount++;
					AllRoutes.Add(staticRoute);
				}

				if (AllRoutes == null || AllRoutes.Count == 0)
				{
					return Result<double>.Failure("No routes to work with.");
				}
				
				foreach(var route in AllRoutes)
				{
					route.CalculateArrivalTimesForStops(network);
				}

				//Soft Constraint: Optimize the waiting time at changes
				Result<double> result = CalculateSoftConstraint_Time_Wait(parameters, network, withLogging);
				if (result.IsFailure) return result;
				fitnessValue += result.Value;

				//Soft Constraint: Optimize the total time the travel takes, including waiting for changes
				result = CalculateSoftConstraint_Time_TotalTravel(parameters, network, withLogging);
				if (result.IsFailure) return result;
				fitnessValue += result.Value;

				//Hard Constraint: The busses needed to aperate at the same time are below fleet size
				result = CalculateHardConstraint_Time_Fleet(parameters, withLogging);
				if (result.IsFailure) return result;
				fitnessValue += result.Value;

				if(withLogging)
					OptimizationLoggerService.Instance.Log($"The value of 'time' fitness constrains: {fitnessValue}.");

				return Result<double>.Success(fitnessValue);
			}
			catch (Exception ex)
			{
				throw new Exception("Fitness value calculation failed: " + ex.Message);
			}
		}

		private Result<double> CalculateHardConstraint_Time_Fleet(in OptimizationParameters parameters, bool withLogging)
		{
			try
			{
				var events = new SortedDictionary<int, int>();

				int maxRouteDuration = 0;

				foreach (var route in AllRoutes)
				{
					// --- compute ON route duration ---
					double onDuration = double.MinValue;
					double startTime = double.MaxValue;
					foreach (var (stopid, arrivalTime) in route.OnRouteArrivalToStopInMinutes)
					{
						if (onDuration < arrivalTime)
						{
							onDuration = arrivalTime;
						}
						if (startTime > arrivalTime)
						{
							startTime = arrivalTime;
						}
					}
					onDuration -= startTime;

					// --- compute BACK route duration ---
					double backDuration = double.MinValue;
					if (!route.OneWay)
					{
						startTime = double.MaxValue;
						foreach (var (stopid, arrivalTime) in route.BackRouteArrivalToStopInMinutes)
						{
							if (backDuration < arrivalTime)
							{
								backDuration = arrivalTime;
							}
							if (startTime > arrivalTime)
							{
								startTime = arrivalTime;
							}
						}
					}

					if (onDuration > maxRouteDuration)
						maxRouteDuration = (int)onDuration;

					if (backDuration > maxRouteDuration)
						maxRouteDuration = (int)backDuration;

					// --- generate departures using headway ---
					for (int t = route.OnStartTime; t < 60; t += route.Headway)
					{
						int start = t;
						int end = t + maxRouteDuration;

						// add start event
						if (!events.ContainsKey(start))
							events[start] = 0;
						events[start] += 1;

						// add end event
						if (!events.ContainsKey(end))
							events[end] = 0;
						events[end] -= 1;
					}
				}

				// --- sweep through time ---
				int currentBuses = 0;
				int maxBuses = 0;

				int simulationEnd = 60 + maxRouteDuration;

				for (int t = 0; t <= simulationEnd; t++)
				{
					if (events.TryGetValue(t, out int delta))
					{
						currentBuses += delta;
					}

					if (currentBuses > maxBuses)
					{
						maxBuses = currentBuses;
					}
				}

				// --- penalty calculation ---
				int threshold = parameters.Fitness_FleetCapacityParameter;

				double penalty = 0;
				if (maxBuses > threshold)
				{
					penalty = (maxBuses - threshold) * 100;
				}

				if(withLogging)
					OptimizationLoggerService.Instance.Log($"The maximum number of busses needed at one time: {maxBuses}, penalty: {penalty}.");

				return Result<double>.Success(penalty);
			}
			catch (Exception ex)
			{
				throw new Exception("Calculate Hard Constraint Time Fleet failed" + ex.Message);
			}
		}

		private Result<double> CalculateSoftConstraint_Time_TotalTravel(in OptimizationParameters parameters, in NetworkInformation network, bool withLogging)
		{
			try
			{
				double intraSum = 0, interSum = 0;
				int intraCount = 0, interCount = 0;

				var stopToDistrict = new Dictionary<Guid, Guid>();

				foreach (var (id, list) in network.Districts)
					foreach (var stop in list)
						stopToDistrict[stop] = id;

				var fastMatrix = new Dictionary<Guid, Dictionary<Guid, double>>(network.StopConnectivityMatrix.Count);

				foreach (var (from, neighbors) in network.StopConnectivityMatrix)
				{
					var dict = new Dictionary<Guid, double>(neighbors.Count);
					foreach (var (dest, weight) in neighbors)
						dict[dest] = weight;

					fastMatrix[from] = dict;
				}

				foreach (var route in AllRoutes)
				{

					var r = route.OnRoute;

					if (r != null && r.Count > 1)
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

						// INTRA
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

						// INTER
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

					r = route.BackRoute;

					if (r != null && r.Count > 1)
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

						// INTRA
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

						// INTER
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
				double T_travel = (T_intra + T_inter) / 2.0;

				double waitingSum = 0;
				int waitingCount = 0;

				foreach (var kvp in AtStopFromRouteToRouteWaitingTimes)
				{
					var list = kvp.Value;
					if (list == null || list.Count == 0)
						continue;

					foreach (var w in list)
					{
						waitingSum += w;
						waitingCount++;
					}
				}

				double AvgWaitingTime = waitingCount > 0 ? waitingSum / waitingCount : 0;

				double maxTravel = parameters.Fitness_MaximumTravelTimeParameter;
				double maxWait = parameters.Fitness_MaximumWaitingMinutesParameter;

				double travelPenalty = maxTravel > 0 ? Math.Min(1.0, (double)T_travel / (double)maxTravel) : 0;
				double waitingPenalty = maxWait > 0 ? Math.Min(1.0,(double) AvgWaitingTime / (double)maxWait) : 0;

				double alpha = 0.6;
				double beta = 0.4;

				double fitness = alpha * travelPenalty + beta * waitingPenalty;

				if (withLogging)
				{
					OptimizationLoggerService.Instance.Log($"Avarage waiting time: {AvgWaitingTime}, avarage intra travel time: {T_intra}, avarage inter travel time: {T_intra}, avarage travel time: {T_travel}.");
					OptimizationLoggerService.Instance.Log($"Travel time penalty: {travelPenalty}, waiting time penalty: {waitingPenalty}.");
					OptimizationLoggerService.Instance.Log($"Calculated fitness for travel time: {fitness}.");
				}

				return Result<double>.Success(Math.Clamp(fitness, 0, 1));
			}
			catch (Exception ex)
			{
				return Result<double>.Failure("CalculateSoftConstraint_Time_TotalTravel failed: " + ex.Message);
			}
		}

		private Result<double> CalculateSoftConstraint_Time_Wait(in OptimizationParameters parameters, in NetworkInformation network, bool withLogging)
		{
			try
			{
				double AllWaitingTime = 0.0;
				double BestChangeTimeSum = 0.0;
				int PossibleChangeCounter = 0;
				int BestChangeCount = 0;
				int UnmanagableChangeCounter = 0;
				foreach (var stop in network.AllStops)
				{
					foreach (var fromRoute in AllRoutes)
					{
						List<double>? FromOnArrivalTime = fromRoute.GetArrivalTimesAtStop(stop);
						List<double>? FromBackArrivalTime = fromRoute.GetArrivalTimesAtStop(stop, false);

						if (FromOnArrivalTime == null && FromBackArrivalTime == null)
							continue;

						foreach (var toRoute in AllRoutes)
						{
							if (fromRoute.RouteIndex == toRoute.RouteIndex)
								continue;

							List<double>? ToOnArrivalTime = toRoute.GetArrivalTimesAtStop(stop);
							List<double>? ToBackArrivalTime = toRoute.GetArrivalTimesAtStop(stop, false);

							if (ToOnArrivalTime == null && ToBackArrivalTime == null)
								continue;

							var FromArrivalTimes = new List<double>();
							if (FromOnArrivalTime != null)
								FromArrivalTimes.AddRange(FromOnArrivalTime);
							FromArrivalTimes.AddRange(FromBackArrivalTime);

							var ToArrivalTimes = new List<double>();
							if (ToOnArrivalTime != null)
								ToArrivalTimes.AddRange(ToOnArrivalTime);
							ToArrivalTimes.AddRange(ToBackArrivalTime);

							if (FromArrivalTimes.Count == 0 || ToArrivalTimes.Count == 0)
								continue;

							var key = (stop, fromRoute.RouteIndex, toRoute.RouteIndex);
							if (!AtStopFromRouteToRouteWaitingTimes.TryGetValue(key, out var list) || list == null)
							{
								list = [];
								AtStopFromRouteToRouteWaitingTimes[key] = list;
							}

							foreach (var fromArrivalTime in FromArrivalTimes)
							{
								double bestChangeTime = double.MaxValue;
								foreach (var toArrivalTime in ToArrivalTimes)
								{
									var fromTime = fromArrivalTime;
									var toTime = toArrivalTime;
									if (fromTime >= 60)
									{
										fromTime %= 60;
									}
									if (toTime >= 60)
									{
										toTime %= 60;
									}

									if (fromTime > toTime)
									{
										toTime += 60;
									}

									if (toTime > fromTime)
									{
										double waitingTime = toTime - fromTime;
										if (waitingTime < bestChangeTime && waitingTime >= (double)parameters.Fitness_MinimalWaitingMinutesParameter)
											bestChangeTime = waitingTime;
										AtStopFromRouteToRouteWaitingTimes[key].Add(waitingTime);
										AllWaitingTime += waitingTime;
										PossibleChangeCounter++;
										if (waitingTime < (double)parameters.Fitness_MinimalWaitingMinutesParameter)
										{
											UnmanagableChangeCounter++;
										}
									}
								}
								if (bestChangeTime < double.MaxValue)
								{
									BestChangeTimeSum += bestChangeTime;
									BestChangeCount++;
								}
							}

						}
					}
				}

				if (PossibleChangeCounter == 0)
					return Result<double>.Success(1.0);

				double AvgWaitingTimePerChange = AllWaitingTime / (double)PossibleChangeCounter;
				double AvgBestWaitingTimePerChange = BestChangeTimeSum / (double)BestChangeCount;

				double WaitingTimeDeviationPercentage = Math.Abs(AvgWaitingTimePerChange / parameters.Fitness_MaximumWaitingMinutesParameter - 1);
				double BestWaitingTimeDeviationPercentage = Math.Abs(AvgBestWaitingTimePerChange / parameters.Fitness_MaximumWaitingMinutesParameter - 1);

				double UnmanagableChangePercentage = (double)UnmanagableChangeCounter / (double)PossibleChangeCounter;

				//double waitingPenalty = Math.Min(1.0, Math.Abs(AvgWaitingTimePerChange / parameters.Fitness_MaximumWaitingMinutesParameter));
				double waitingPenalty = Math.Abs(AvgWaitingTimePerChange / parameters.Fitness_MaximumWaitingMinutesParameter);

				//double bestWaitingPenalty = Math.Min(1.0, Math.Abs(AvgBestWaitingTimePerChange / parameters.Fitness_MaximumWaitingMinutesParameter));
				double bestWaitingPenalty = Math.Abs(AvgBestWaitingTimePerChange / parameters.Fitness_MaximumWaitingMinutesParameter);

				double unmanageablePenalty = PossibleChangeCounter == 0 ? 1.0 : (double)UnmanagableChangeCounter /PossibleChangeCounter;

				double fitness =
					waitingPenalty +
					bestWaitingPenalty +
					unmanageablePenalty;

				if (withLogging)
				{
					OptimizationLoggerService.Instance.Log($"Avarage waiting time per change: {AvgWaitingTimePerChange}.");
					OptimizationLoggerService.Instance.Log($"Avarage of the best waiting times for changes: {AvgBestWaitingTimePerChange}.");
					OptimizationLoggerService.Instance.Log($"Waiting time deviations from avarage: {WaitingTimeDeviationPercentage}%.");
					OptimizationLoggerService.Instance.Log($"Waiting time deviations  for best changes from avarage: {BestWaitingTimeDeviationPercentage}%.");
					OptimizationLoggerService.Instance.Log($"number of unmanagable changes: {UnmanagableChangeCounter}.");
					OptimizationLoggerService.Instance.Log($"Percentage of unmanagable changes in all changes: {UnmanagableChangeCounter / PossibleChangeCounter}%.");
					OptimizationLoggerService.Instance.Log($"Waiting penalty: {waitingPenalty}, best waiting penaly: {bestWaitingPenalty}, unmanagable change penalties: {unmanageablePenalty}.");

					OptimizationLoggerService.Instance.Log($"The calculated waiting fitness: {fitness}.");
				}
				

				return Result<double>.Success(fitness);
			}
			catch (Exception ex)
			{
				return Result<double>.Failure("Calculate Soft Constraint Time Wait failed: " + ex.Message);
			}
		}

		private Result<double> CalculateHardConstraint_Route_Transfer(in OptimizationParameters parameters, bool withLogging)
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
						list = [];
						stopToRoutes[stop] = list;
					}

					list.Add(routeIndex);
				}
			}

			var adjacency = new HashSet<int>[routes.Count];
			for (int i = 0; i < adjacency.Length; i++)
				adjacency[i] = [];

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
				? 100 * (maxShortestPathEdges - changeCount)
				: 0d;

			if (withLogging)
			{
				OptimizationLoggerService.Instance.Log($"Maximum needed changes in the network: {changeCount}.");
				OptimizationLoggerService.Instance.Log($"Change penalty: {penalty}.");
			}

			return Result<double>.Success(penalty);
		}

		private Result<double> CalculateSoftConstraint_Route_Traveltime(in NetworkInformation network, bool withLogging)
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

			if (withLogging)
			{
				OptimizationLoggerService.Instance.Log($"Avarage intra travel time: {T_intra}, intra score: {intraScore}.");
				OptimizationLoggerService.Instance.Log($"Avarage inter travel time: {T_inter}, inter score {interScore}.");
				OptimizationLoggerService.Instance.Log($"Travel time fitness: {result}.");
			}

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
		private Result<double> CalculateRouteFitness(in OptimizationParameters parameters, in NetworkInformation network, bool withLogging)
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
			int OneWayCoutner = 0;
			int minRouteLength = int.MaxValue;
			int maxRouteLength = int.MinValue;
			int minmumLengthViolationCounter = 0;

			double invTargetLength = 1.0 / parameters.Fitness_RouteLengthParameter;

			// store route stop sets for redundancy later
			var routeStopSets = new List<HashSet<Guid>>(routeCount);

			foreach (var route in routes)
			{
				if (route.OneWay)
				{
					OneWayCoutner++;
				}

				var onRoute = route.OnRoute;
				var backRoute = route.BackRoute;

				if(onRoute.Count < minRouteLength)
				{
					minRouteLength = onRoute.Count;
				}
				if (onRoute.Count > maxRouteLength)
				{
					maxRouteLength = onRoute.Count;
				}

				if (!route.OneWay && backRoute.Count < minRouteLength)
				{
					minRouteLength = backRoute.Count;
				}
				if (!route.OneWay && backRoute.Count > maxRouteLength)
				{
					maxRouteLength = backRoute.Count;
				}

				if (onRoute.Count < parameters.Fitness_MinimumRouteLengthParameter)
				{
					minmumLengthViolationCounter++;
				}

				if (!route.OneWay && backRoute.Count < parameters.Fitness_MinimumRouteLengthParameter)
				{
					minmumLengthViolationCounter++;
				}

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

				if (backRoute.Count > 0)
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

					double matchPercent = (double)currentStreak / (((double)setA.Count + (double)setB.Count) / 2);

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
			foreach (var (id1, d1) in districts)
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

			double oneWayPenalty = 0;
			var oneWayPercentage = (double)((double)OneWayCoutner / (double)AllRoutes.Count);
			if (parameters.Genome_AllowOneWayRoutes && oneWayPercentage > (double)parameters.Genome_OneWayRoutePercentageTreshold/100.0)
			{
				oneWayPenalty = oneWayPercentage * 100.0;
			}

			UnMetStopPercentage = (double)unmetStops.Count / (double)network.AllStops.Count;
			UnMetStopList = [.. unmetStops];


			double score =
				  redundantPairs * 10
				+ loopCount * 100
				+ terminalMistakes * 1000
				+ unmetStops.Count * 1000
				+ oneWayPenalty
				+ (lengthDeviation == 0 ? 0 : Math.Abs((lengthDeviation / routeCount)-1))
				+ minmumLengthViolationCounter * 10
				+ districtConnections / allConnections
				+ Math.Abs(hubGini-1)
				+ terminalDeviation;

			if (withLogging)
			{
				OptimizationLoggerService.Instance.Log($"Redundant pairs: {redundantPairs}, penalty: {redundantPairs*10}.");
				OptimizationLoggerService.Instance.Log($"Number of loops in the routes: {loopCount}, penalty: {loopCount*100}.");
				OptimizationLoggerService.Instance.Log($"Number of one way routes: {OneWayCoutner}, percentage: {oneWayPercentage}, penalty: {oneWayPenalty}.");
				OptimizationLoggerService.Instance.Log($"Violated terminal hard constrainst: {terminalMistakes}, penalty: {terminalMistakes*1000}.");
				OptimizationLoggerService.Instance.Log($"Number of unmet stops: {unmetStops.Count}, penalty: {unmetStops.Count*1000}.");
				OptimizationLoggerService.Instance.Log($"Route length deviation: {(lengthDeviation == 0 ? 0 : Math.Abs((lengthDeviation / routeCount) - 1))}");
				OptimizationLoggerService.Instance.Log($"Minimum route length: {minRouteLength}, max route length: {maxRouteLength}");
				OptimizationLoggerService.Instance.Log($"Minimum route length violation: {minmumLengthViolationCounter}, penalty: {minmumLengthViolationCounter * 10}");
				OptimizationLoggerService.Instance.Log($"Connections between districts: {districtConnections / allConnections}");
				OptimizationLoggerService.Instance.Log($"Gini index of hubs: {Math.Abs(hubGini - 1)}");
				OptimizationLoggerService.Instance.Log($"Deviation of terminal usage: {terminalDeviation}");
				OptimizationLoggerService.Instance.Log($"Fitness score: {score}");

			}

			return Result<double>.Success(score);
		}

		
	}
}
