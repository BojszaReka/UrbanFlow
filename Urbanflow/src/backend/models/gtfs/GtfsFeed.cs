using GTFS;
using GTFS.Entities;
using NetTopologySuite.EdgeGraph;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.enums;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.graph;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("GtfsFeeds")]
	public class GtfsFeed
	{
		// Properties
		[Key]
		public Guid Id { get; internal set; } = Guid.NewGuid();
		public string PublisherName { get; internal set; }
		public string PublisherUrl { get; internal set; }
		public string Lang { get; internal set; }
		public string StartDate { get; internal set; }
		public string EndDate { get; internal set; }
		public string Version { get; internal set; }

		// GTFS Data Collections
		public List<Agency> Agencies { get; internal set; } = [];
		public List<Calendar> Calendars { get; internal set; } = [];
		public List<CalendarDate> CalendarDates { get; internal set; } = [];
		public List<Route> Routes { get; internal set; } = [];
		public List<Shape> Shapes { get; internal set; } = [];
		public List<Stop> Stops { get; internal set; } = [];
		public List<StopTime> StopTimes { get; internal set; } = [];
		public List<Trip> Trips { get; internal set; } = [];


		// Contructors

		public GtfsFeed() { }

		public GtfsFeed(Guid gtfsFeedId)
		{
			UpdateFromDatabase(gtfsFeedId);
		}

		public GtfsFeed(GTFSFeed feed)
		{
			GTFS.Entities.FeedInfo feedInfo = feed.GetFeedInfo();
			PublisherName = feedInfo.PublisherName ?? "Unknown";
			PublisherUrl = feedInfo.PublisherUrl ?? "Unknown";
			Lang = feedInfo.Lang ?? "Unknown";
			StartDate = feedInfo.StartDate ?? "Unknown";
			EndDate = feedInfo.EndDate ?? "Unknown";
			Version = feedInfo.Version ?? "Unknown";
			AddtoDatabase();

			AdaptCollection(feed.Agencies, Agencies, Id, (a, id) => new Agency(a, id));
			AdaptCollection(feed.Calendars, Calendars, Id, (c, id) => new Calendar(c, id));
			AdaptCollection(feed.CalendarDates, CalendarDates, Id, (cd, id) => new CalendarDate(cd, id));
			AdaptCollection(feed.Routes, Routes, Id, (r, id) => new Route(r, id));
			AdaptCollection(feed.Shapes, Shapes, Id, (s, id) => new Shape(s, id));
			AdaptCollection(feed.Stops, Stops, Id, (s, id) => new Stop(s, id));
			AdaptCollection(feed.StopTimes, StopTimes, Id, (st, id) => new StopTime(st, id));
			AdaptCollection(feed.Trips, Trips, Id, (t, id) => new Trip(t, id));			
		}

		// ----------------------------


		// Generic method to for collections
		private static void AdaptCollection<TSource, TTarget, TContext>(
			IEnumerable<TSource> source,
			ICollection<TTarget> target,
			TContext context,
			Func<TSource, TContext, TTarget> factory)
		{
			foreach (var item in source)
			{
				target.Add(factory(item, context));
			}
		}

		private static void ExportCollection<TSource, TTarget, TCollection>(
			TCollection target,
			IEnumerable<TSource> source,
			Func<TSource, TTarget> exporter)
			where TCollection : class
		{
			foreach (var item in source)
			{
				((dynamic)target).Add(exporter(item));
			}
		}
		// ----------------------------



		// Database operations
		public void UpdateFromDatabase(Guid id)
		{
			using var context = new DatabaseContext();
			var dbFeed = (context.GtfsFeeds?.Find(id)) ?? throw new Exception("GTFS Feed not found in database.");
			this.PublisherName = dbFeed.PublisherName;
			this.PublisherUrl = dbFeed.PublisherUrl;
			this.Lang = dbFeed.Lang;
			this.StartDate = dbFeed.StartDate;
			this.EndDate = dbFeed.EndDate;
			this.Version = dbFeed.Version;

			this.Agencies = [.. context.Agencies.Where(a => a.GtfsFeedId == id)];
			this.Calendars = [.. context.Calendars.Where(a => a.GtfsFeedId == id)];
			this.CalendarDates = [.. context.CalendarDates.Where(a => a.GtfsFeedId == id)];
			this.Routes = [.. context.Routes.Where(a => a.GtfsFeedId == id)];
			this.Shapes = [.. context.Shapes.Where(a => a.GtfsFeedId == id)];
			this.Stops = [.. context.Stops.Where(a => a.GtfsFeedId == id)];
			this.StopTimes = [.. context.StopTimes.Where(a => a.GtfsFeedId == id)];
			this.Trips = [.. context.Trips.Where(a => a.GtfsFeedId == id)];
			Console.WriteLine();
		}

		public void UpdateDatabase()
		{
			using var context = new DatabaseContext();
			var existingFeed = context.GtfsFeeds?.Find(Id);
			try
			{
				if (existingFeed == null)
				{
					context.GtfsFeeds?.Add(this);
					context.Agencies?.AddRange(this.Agencies);
					context.Calendars?.AddRange(this.Calendars);
					context.CalendarDates?.AddRange(this.CalendarDates);
					context.Routes?.AddRange(this.Routes);
					context.Shapes?.AddRange(this.Shapes);
					context.Stops?.AddRange(this.Stops);
					context.StopTimes?.AddRange(this.StopTimes);
					context.Trips?.AddRange(this.Trips);
				}
				else
				{
					context.GtfsFeeds?.Update(this);
					context.Agencies?.UpdateRange(this.Agencies);
					context.Calendars?.UpdateRange(this.Calendars);
					context.CalendarDates?.UpdateRange(this.CalendarDates);
					context.Routes?.UpdateRange(this.Routes);
					context.Shapes?.UpdateRange(this.Shapes);
					context.Stops?.UpdateRange(this.Stops);
					context.StopTimes?.UpdateRange(this.StopTimes);
					context.Trips?.UpdateRange(this.Trips);
				}
				context.SaveChanges();
			}
			catch (Exception ex) {
				throw new Exception($"Error updating database: {ex.Message}");
			}

		}

		private void AddtoDatabase()
		{
			using var context = new DatabaseContext();
			context.GtfsFeeds?.Add(this);
			//context.Agencies?.AddRange(this.Agencies);
			//context.Calendars?.AddRange(this.Calendars);
			//context.CalendarDates?.AddRange(this.CalendarDates);
			//context.Routes?.AddRange(this.Routes);
			//context.Shapes?.AddRange(this.Shapes);
			//context.Stops?.AddRange(this.Stops);
			//context.StopTimes?.AddRange(this.StopTimes);
			//context.Trips?.AddRange(this.Trips);
			context.SaveChanges();
		}
		// ----------------------------



		// GTFS related methods
		public GTFSFeed ExportTGtfs()
		{
			GTFSFeed feed = new();
			ExportCollection(feed.Agencies, Agencies, a => a.Export());
			ExportCollection(feed.Calendars, Calendars, c => c.Export());
			ExportCollection(feed.CalendarDates, CalendarDates, cd => cd.Export());
			ExportCollection(feed.Routes, Routes, r => r.Export());
			ExportCollection(feed.Shapes, Shapes, s => s.Export());
			ExportCollection(feed.Stops, Stops, s => s.Export());
			ExportCollection(feed.StopTimes, StopTimes, st => st.Export());
			ExportCollection(feed.Trips, Trips, t => t.Export());
			feed.SetFeedInfo(new GTFS.Entities.FeedInfo
			{
				PublisherName = this.PublisherName,
				PublisherUrl = this.PublisherUrl,
				Lang = this.Lang,
				StartDate = this.StartDate,
				EndDate = this.EndDate,
				Version = this.Version
			});
			return feed;
		}
		// ----------------------------


		// Graph related methods
		private Result<List<(uint SequenceNumber, string StopId)>> RouteStops(string routeId)
		{
			var route = Routes.Where(r => r.RouteId == routeId).First();
			if (route == null) return Result<List<(uint SequenceNumber, string StopId)>>.Failure("Route with RouteId does not exists");
			return RouteStops(route.Id);
		}

		private Result<List<(uint SequenceNumber, string StopId)>> RouteStops(Guid routeId)
		{
			// Use dictionary for O(1) lookups
			var route = Routes.FirstOrDefault(r => r.Id == routeId);
			if (route == null)
				return Result<List<(uint, string)>>.Failure($"Route not found. (ID: {routeId})");

			var trip = Trips.FirstOrDefault(t => t.RouteId == route.RouteId);
			if (trip == null)
				return Result<List<(uint, string)>>.Failure($"No trips found for route. (ID: {routeId})");

			// Materialize once and sort once
			var stopTimes = StopTimes
				.Where(st => st.TripId == trip.TripId)
				.OrderBy(st => st.StopSequence)
				.ToList();

			// Pre-index stops for fast lookup
			var stopDict = Stops.ToDictionary(s => s.StopId);

			var routeStops = new List<(uint SequenceNumber, string StopId)>(stopTimes.Count);

			foreach (var stopTime in stopTimes)
			{
				if (!stopDict.TryGetValue(stopTime.StopId, out var stop))
					return Result<List<(uint, string)>>.Failure("Stop not found for specific stoptime.");

				routeStops.Add((stopTime.StopSequence, stop.StopId));
			}

			return Result<List<(uint, string)>>.Success(routeStops);
		}

		private Result<HashSet<EdgeDataDTO>> GetDataForEdgesOfRoute(string routeId) {
			var route = Routes.Where(r => r.RouteId == routeId).First();
			if (route == null) return Result<HashSet<EdgeDataDTO>>.Failure("Route with RouteId does not exists");
			return GetDataForEdgesOfRoute(route.Id);
		}

		public Result<HashSet<EdgeDataDTO>> GetDataForEdgesOfRoute(Guid routeId)
		{
			var route = Routes.First(r => r.Id == routeId);
			if (route == null)
				return Result<HashSet<EdgeDataDTO>>.Failure($"Route not found (ID: {routeId}).");

			var trip = Trips.FirstOrDefault(t => t.RouteId == route.RouteId);
			if (trip == null)
				return Result<HashSet<EdgeDataDTO>>.Failure($"GDFEOR: Trip not found for route {routeId}.");

			var stopTimes = StopTimes
				.Where(st => st.TripId == trip.TripId)
				.OrderBy(st => st.StopSequence)
				.ToList();

			// Pre-index stops for O(1) lookup
			var stopDict = Stops.ToDictionary(s => s.StopId);

			var edgeData = new HashSet<EdgeDataDTO>();

			for (int i = 0; i < stopTimes.Count - 1; i++)
			{
				var fromStopTime = stopTimes[i];
				var toStopTime = stopTimes[i + 1];

				if (!stopDict.TryGetValue(fromStopTime.StopId, out var fromStop))
					return Result<HashSet<EdgeDataDTO>>.Failure($"Stop not found (ID: {fromStopTime.StopId}).");

				if (!stopDict.TryGetValue(toStopTime.StopId, out var toStop))
					return Result<HashSet<EdgeDataDTO>>.Failure($"Stop not found (ID: {toStopTime.StopId}).");

				// Parse once
				var departureTime = TimeSpan.Parse(fromStopTime.DepartureTime);
				var arrivalTime = TimeSpan.Parse(toStopTime.ArrivalTime);

				int travelTimeMinutes = (int)(arrivalTime - departureTime).TotalMinutes;

				edgeData.Add(new EdgeDataDTO
				{
					FromStopId = GetParentStopOfStop(fromStop.Id).Id,
					ToStopId = GetParentStopOfStop(toStop.Id).Id,
					TravelTimeMinutes = travelTimeMinutes,
				});
			}

			return Result<HashSet<EdgeDataDTO>>.Success(edgeData);
		}

		public Result<List<EdgeDataDTO>> GetDataForEdgesOfNetwork()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<EdgeDataDTO>>.Failure("No routes found.");

			// Precompute lookups ONCE
			var tripsByRoute = Trips
				.GroupBy(t => t.RouteId)
				.ToDictionary(g => g.Key, g => g.First());

			var stopTimesByTrip = StopTimes
				.GroupBy(st => st.TripId)
				.ToDictionary(g => g.Key, g => g.OrderBy(st => st.StopSequence).ToList());

			var stopDict = Stops.ToDictionary(s => s.StopId);

			var allEdges = new HashSet<(Guid, Guid, int)>();

			foreach (var route in Routes)
			{
				if (!tripsByRoute.TryGetValue(route.RouteId, out var trip))
					continue;

				if (!stopTimesByTrip.TryGetValue(trip.TripId, out var stopTimes))
					continue;

				for (int i = 0; i < stopTimes.Count - 1; i++)
				{
					var fromStopTime = stopTimes[i];
					var toStopTime = stopTimes[i + 1];

					if (!stopDict.TryGetValue(fromStopTime.StopId, out var fromStop))
						return Result<List<EdgeDataDTO>>.Failure($"Stop not found (ID: {fromStopTime.StopId}).");

					if (!stopDict.TryGetValue(toStopTime.StopId, out var toStop))
						return Result<List<EdgeDataDTO>>.Failure($"Stop not found (ID: {toStopTime.StopId}).");

					var departureTime = TimeSpan.Parse(fromStopTime.DepartureTime);
					var arrivalTime = TimeSpan.Parse(toStopTime.ArrivalTime);

					int travelTimeMinutes = (int)(arrivalTime - departureTime).TotalMinutes;

					allEdges.Add((GetParentStopOfStop(fromStop.Id).Id, GetParentStopOfStop(toStop.Id).Id, travelTimeMinutes));
				}
			}

			if (allEdges.Count == 0)
				return Result<List<EdgeDataDTO>>.Failure("No data found for the edges of the network");

			HashSet<EdgeDataDTO> dataDTOs = new HashSet<EdgeDataDTO>();
			foreach (var (stop1id, stop2id, minutes) in allEdges)
			{
				dataDTOs.Add(new EdgeDataDTO
				{
					FromStopId = stop1id,
					ToStopId = stop2id,
					TravelTimeMinutes = minutes,
				});
			}

			return Result<List<EdgeDataDTO>>.Success(dataDTOs.ToList());
		}

		public Result<List<NodeDataDTO>> GetStopsForNetworkGraph()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<NodeDataDTO>>.Failure("No routes found.");

			// Precompute everything ONCE
			var tripsByRoute = Trips
				.GroupBy(t => t.RouteId)
				.ToDictionary(g => g.Key, g => g.First());

			var stopTimesByTrip = StopTimes
				.GroupBy(st => st.TripId)
				.ToDictionary(g => g.Key, g => g.ToList());

			var stopDict = Stops.ToDictionary(s => s.StopId);

			var allStops = new HashSet<Stop>();

			foreach (var route in Routes)
			{
				if (!tripsByRoute.TryGetValue(route.RouteId, out var trip))
					continue;

				if (!stopTimesByTrip.TryGetValue(trip.TripId, out var stopTimes))
					continue;

				foreach (var stopTime in stopTimes)
				{
					if (!stopDict.TryGetValue(stopTime.StopId, out var stop))
						continue;

					allStops.Add(GetParentStopOfStop(stop.Id)); 
				}
			}

			if (allStops.Count == 0)
				return Result<List<NodeDataDTO>>.Failure("No data found for the nodes of the network");

			List<NodeDataDTO> dataDTOs = new List<NodeDataDTO>();
			foreach (var stop in allStops) {
				dataDTOs.Add(new NodeDataDTO
				{
					Stop = stop
				});
			}

			return Result<List<NodeDataDTO>>.Success(dataDTOs);
		}


		public Result<List<NodeDataDTO>> GetStopForRoute(string routeId)
		{
			var route = Routes.FirstOrDefault(r => r.RouteId == routeId);
			if (route == null)
				return Result<List<NodeDataDTO>>.Failure("Route with RouteId does not exist");

			return GetStopsForRoute(route.Id);
		}

		public Result<List<NodeDataDTO>> GetStopsForRoute(Guid routeId)
		{
			var route = Routes.FirstOrDefault(r => r.Id == routeId);
			if (route == null)
				return Result<List<NodeDataDTO>>.Failure($"Route not found (ID: {routeId})");

			var trip = Trips.FirstOrDefault(t => t.RouteId == route.RouteId);
			if (trip == null)
				return Result<List<NodeDataDTO>>.Failure($"No trip found for route (ID: {routeId})");

			var stopTimes = StopTimes
				.Where(st => st.TripId == trip.TripId)
				.ToList();

			// Precompute lookup
			var stopDict = Stops.ToDictionary(s => s.StopId);

			var uniqueStops = new HashSet<NodeDataDTO>();

			foreach (var stopTime in stopTimes)
			{
				if (!stopDict.TryGetValue(stopTime.StopId, out var stop))
					continue;

				var data = new NodeDataDTO
				{
					Stop = GetParentStopOfStop(stop.Id)
				};

				uniqueStops.Add(data); // HashSet handles duplicates
			}

			if (uniqueStops.Count == 0)
				return Result<List<NodeDataDTO>>.Failure($"No stops could be gathered for route (ID: {routeId})");

			return Result<List<NodeDataDTO>>.Success(uniqueStops.ToList());
		}

		public void SetNodeTypeForStops()
		{
			if (Routes == null || Routes.Count == 0)
				return;

			// Precompute lookups ONCE
			var tripsByRoute = Trips
				.GroupBy(t => t.RouteId)
				.ToDictionary(g => g.Key, g => g.First());

			var stopTimesByTrip = StopTimes
				.GroupBy(st => st.TripId)
				.ToDictionary(g => g.Key, g => g.OrderBy(st => st.StopSequence).ToList());

			var stopDict = Stops.ToDictionary(s => s.StopId);

			foreach (var route in Routes)
			{
				if (!tripsByRoute.TryGetValue(route.RouteId, out var trip))
					continue;

				if (!stopTimesByTrip.TryGetValue(trip.TripId, out var stopTimes) || stopTimes.Count == 0)
					continue;

				int lastIndex = stopTimes.Count - 1;

				for (int i = 0; i < stopTimes.Count; i++)
				{
					var stopTime = stopTimes[i];

					if (!stopDict.TryGetValue(stopTime.StopId, out var stop))
						continue;

					if (stop.NodeType == ENodeType.Terminal)
						continue;

					if (i == 0 || i == lastIndex)
					{
						stop.NodeType = ENodeType.Terminal;
					}
				}
			}

			using var db = new DatabaseContext();
			db.Routes.UpdateRange(Routes);
			db.SaveChanges();
		}

		internal string GetRouteName(Guid routeId)
		{
			var route = Routes.Where(r => r.Id== routeId).FirstOrDefault();
			return route.ShortName;
		}

		private Stop GetParentStopOfStop(Guid id)
		{
			var stop = Stops.Where(x => x.Id == id).FirstOrDefault();
			if (stop != null && stop.ParentStation != null)
			{
				var parentStation = Stops.Where(x => x.StopId == stop.ParentStation).FirstOrDefault();
				return parentStation;
			}
			return stop;
		}

		internal Dictionary<Guid, List<(Guid Destination, double Weight)>> ExtractStopConnectivityMatrix()
		{
			throw new NotImplementedException();
		}

		internal List<Guid> GatherAllStopIds()
		{
			throw new NotImplementedException();
		}

		internal List<(Guid, ENodeType)> ExtractClassifiedStops()
		{
			throw new NotImplementedException();
		}

		internal List<GenomeRoute> GatherStaticRoutes()
		{
			throw new NotImplementedException();
		}

		internal List<List<Guid>> CollectDistricts()
		{
			throw new NotImplementedException();
		}

		// ----------------------------
	}
}
