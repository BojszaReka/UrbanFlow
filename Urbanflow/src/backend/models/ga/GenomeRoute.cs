using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class GenomeRoute
	{
		//main fields -> computed
		public List<Guid> OnRoute { get; set; } = [];
		public List<Guid> BackRoute { get; set; } = [];
		public int OnStartTime { get; private set; } // 0-59 közötti diszkrét érték
		public int BackStartTime { get; private set; } // 0-59 közötti diszkrét érték
		public int Headway { get; private set; } // 5-60 közötti érték
		public bool OneWay { get; }

		//helper values for Time optimization
		public int RouteIndex { get; set; } = 0;
		public List<(int, double)> OnRouteArrivalToStopInMinutes { get; set; } = [];
		public List<(int, double)> BackRouteArrivalToStopInMinutes { get; set; } = [];

		public GenomeRoute(in List<Guid> onRoute, int onStartTime, in List<Guid> backRoute, int backStartTime, int headway, bool oneWay = false)
		{
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = backRoute;
			BackStartTime = backStartTime;
			Headway = headway;
			OneWay = oneWay;
		}

		public GenomeRoute(in List<Guid> onRoute, int onStartTime, int headway, bool oneWay = true)
		{
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = [];
			BackStartTime = -1;
			Headway = headway;
			OneWay = oneWay;
		}

		public GenomeRoute(int index, in List<Guid> onRoute, int onStartTime, in List<Guid> backRoute, int backStartTime, int headway, bool oneWay = false)
		{
			RouteIndex = index;
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = backRoute;
			BackStartTime = backStartTime;
			Headway = headway;
			OneWay = oneWay;
		}

		public GenomeRoute(int index, in List<Guid> onRoute, int onStartTime, int headway, bool oneWay = true)
		{
			RouteIndex = index;
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = [];
			BackStartTime = -1;
			Headway = headway;
			OneWay = oneWay;
		}

		public void CalculateArrivalTimesForStops(in NetworkInformation network)
		{
			try
			{
				double baseMinute = OnStartTime;
				OnRouteArrivalToStopInMinutes.Add((0, baseMinute));
				var previousStop = OnRoute[0];
				for (int i = 1; i < OnRoute.Count; i++)
				{
					if (!network.StopConnectivityMatrix.TryGetValue(previousStop, out var neighbors))
						throw new Exception("Stop not found in the connectivity matrix");

					var stop = OnRoute[i];
					double distMin = -1.0;

					foreach (var (neighbor, weight) in neighbors)
					{
						if (neighbor.Equals(stop))
						{
							distMin = weight;
							break; 
						}
					}

					if (distMin < 0)
						throw new Exception("No connection found in the connectivity matrix");

					baseMinute += distMin;
					OnRouteArrivalToStopInMinutes.Add((i, baseMinute));
					previousStop = stop;
				}

				if (!OneWay && BackRoute.Count > 0)
				{
					baseMinute = BackStartTime;
					BackRouteArrivalToStopInMinutes.Add((0, baseMinute));
					previousStop = BackRoute[0];
					for (int i = 1; i < BackRoute.Count; i++)
					{
						if (!network.StopConnectivityMatrix.TryGetValue(previousStop, out var neighbors))
							throw new Exception("Stop not found in the connectivity matrix");

						var stop = BackRoute[i];
						double distMin = -1.0;

						foreach (var (neighbor, weight) in neighbors)
						{
							if (neighbor.Equals(stop))
							{
								distMin = weight;
								break;
							}
						}

						if (distMin < 0)
							throw new Exception("No connection found in the connectivity matrix");

						baseMinute += distMin;
						BackRouteArrivalToStopInMinutes.Add((i, baseMinute));
						previousStop = stop;
					}
				}

			}
			catch (Exception ex)
			{
				throw new Exception("Arrival time calculation failed: " + ex.Message, ex);
			}
		}

		internal void RandomiseTimeValues()
		{
			Headway = Random.Shared.Next(1, 29);
			OnStartTime = Random.Shared.Next(0, 59);
			if (!OneWay)
			{
				BackStartTime = Random.Shared.Next(0, 59);
			}
		}

		internal List<double> GetArrivalTimesAtStop(Guid stop, bool onRoute = true)
		{
			var route = OnRoute;
			var RouteArrivalTimes = OnRouteArrivalToStopInMinutes;
			if (!onRoute)
			{
				route = BackRoute;
				RouteArrivalTimes = BackRouteArrivalToStopInMinutes;
			}

			List<double> arrivalTimes = [];
			for (int i = 0; i < route.Count; i++)
			{
				if (!route[i].Equals(stop))
					continue;

				var baseArrivalTime = 0.0;
				foreach (var (index, time) in RouteArrivalTimes)
				{
					if (index == i)
					{
						baseArrivalTime = time;
						break;
					}
				}

				arrivalTimes.Add(baseArrivalTime);
				var temptime = baseArrivalTime;
				var count = 60 / Headway;
				while (count > 0)
				{

					temptime += Headway;
					temptime %= 60;
					arrivalTimes.Add(temptime);
					count--;
				}
			}
			return arrivalTimes;
		}
	}
}
