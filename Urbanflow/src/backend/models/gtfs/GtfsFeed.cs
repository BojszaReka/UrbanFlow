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

		//other
		public List<District> Districts { get; internal set; }


		// Contructors

		public GtfsFeed() { }

		public GtfsFeed(Guid gtfsFeedId)
		{
			UpdateFromDatabase(gtfsFeedId);
			TryLoadingDistricts();
		}

		

		public GtfsFeed(in GTFSFeed feed)
		{
			GTFS.Entities.FeedInfo feedInfo = feed.GetFeedInfo();
			PublisherName = feedInfo.PublisherName ?? "Unknown";
			PublisherUrl = feedInfo.PublisherUrl ?? "Unknown";
			Lang = feedInfo.Lang ?? "Unknown";
			StartDate = feedInfo.StartDate ?? "Unknown";
			EndDate = feedInfo.EndDate ?? "Unknown";
			Version = feedInfo.Version ?? "Unknown";
			AddtoDatabase();

			var District = new District("General City District (Collector)", Id, true);
			Districts?.Add(District);

			AdaptCollection(feed.Agencies, Agencies, Id, (a, id) => new Agency(a, id));
			AdaptCollection(feed.Calendars, Calendars, Id, (c, id) => new Calendar(c, id));
			AdaptCollection(feed.CalendarDates, CalendarDates, Id, (cd, id) => new CalendarDate(cd, id));
			AdaptCollection(feed.Routes, Routes, Id, (r, id) => new Route(r, id));
			AdaptCollection(feed.Shapes, Shapes, Id, (s, id) => new Shape(s, id));
			AdaptCollection(feed.Stops, Stops, Id, (s, id) => new Stop(s, id, District.Id));
			AdaptCollection(feed.StopTimes, StopTimes, Id, (st, id) => new StopTime(st, id));
			AdaptCollection(feed.Trips, Trips, Id, (t, id) => new Trip(t, id));

			TryLoadingDistricts();
		}

		public GtfsFeed(in GTFSFeed feed, in DatabaseContext db)
		{
			GTFS.Entities.FeedInfo feedInfo = feed.GetFeedInfo();
			PublisherName = feedInfo.PublisherName ?? "Unknown";
			PublisherUrl = feedInfo.PublisherUrl ?? "Unknown";
			Lang = feedInfo.Lang ?? "Unknown";
			StartDate = feedInfo.StartDate ?? "Unknown";
			EndDate = feedInfo.EndDate ?? "Unknown";
			Version = feedInfo.Version ?? "Unknown";
			AddtoDatabase(db);

			var District = new District("General City District (Collector)", Id, db, true);
			Districts?.Add(District);

			AdaptCollection(feed.Agencies, Agencies, Id, (a, id) => new Agency(a, id, true));
			AdaptCollection(feed.Calendars, Calendars, Id, (c, id) => new Calendar(c, id, true));
			AdaptCollection(feed.CalendarDates, CalendarDates, Id, (cd, id) => new CalendarDate(cd, id, true));
			AdaptCollection(feed.Routes, Routes, Id, (r, id) => new Route(r, id, true));
			AdaptCollection(feed.Shapes, Shapes, Id, (s, id) => new Shape(s, id, true));
			AdaptCollection(feed.Stops, Stops, Id, (s, id) => new Stop(s, id, District.Id, true));
			AdaptCollection(feed.StopTimes, StopTimes, Id, (st, id) => new StopTime(st, id, true));
			AdaptCollection(feed.Trips, Trips, Id, (t, id) => new Trip(t, id, true));

			db.Agencies?.AddRange(Agencies);
			db.Calendars?.AddRange(Calendars);
			db.CalendarDates?.AddRange(CalendarDates);
			db.Routes?.AddRange(Routes);
			db.Shapes?.AddRange(Shapes);
			db.Stops?.AddRange(Stops);
			db.StopTimes?.AddRange(StopTimes);
			db.Trips?.AddRange(Trips);

			db.SaveChanges();

			TryLoadingDistricts(db);
		}

		// ----------------------------


		// Generic method to for collections
		private static void AdaptCollection<TSource, TTarget, TContext>(
			in IEnumerable<TSource> source,
			ICollection<TTarget> target,
			TContext context,
			Func<TSource, TContext, TTarget> factory)
		{
			foreach (var item in source)
			{
				target.Add(factory(item, context));
			}
		}

		private static void AdaptCollection<TSource, TTarget, TContext, TExtra>(
			IEnumerable<TSource> source,
			ICollection<TTarget> target,
			TContext context,
			TExtra extra,
			Func<TSource, TContext, TExtra, TTarget> factory)
		{
			var results = source
				.AsParallel()
				.Select(item => factory(item, context, extra))
				.ToList();

			foreach (var result in results)
			{
				target.Add(result);
			}
		}

		private static void ExportCollection<TSource, TTarget, TCollection>(
			TCollection target,
			in IEnumerable<TSource> source,
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
			Id = id;
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

			this.Districts = [.. context.Districts.Where(a => a.GtfsFeedId == id)];
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
			context.SaveChanges();
		}

		private void AddtoDatabase(in DatabaseContext context)
		{
			context.GtfsFeeds?.Add(this);
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

			HashSet<EdgeDataDTO> dataDTOs = [];
			foreach (var (stop1id, stop2id, minutes) in allEdges)
			{
				dataDTOs.Add(new EdgeDataDTO
				{
					FromStopId = stop1id,
					ToStopId = stop2id,
					TravelTimeMinutes = minutes,
				});
			}

			return Result<List<EdgeDataDTO>>.Success([.. dataDTOs]);
		}

		public Result<List<NodeDataDTO>> GetStopsForNetworkGraph()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<NodeDataDTO>>.Failure("No routes found.");

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

			List<NodeDataDTO> dataDTOs = [];
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

				uniqueStops.Add(data);
			}

			if (uniqueStops.Count == 0)
				return Result<List<NodeDataDTO>>.Failure($"No stops could be gathered for route (ID: {routeId})");

			return Result<List<NodeDataDTO>>.Success([.. uniqueStops]);
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
			var route = Routes.Where(r => r.Id == routeId).FirstOrDefault();
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

		internal Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>> ExtractStopConnectivityMatrix()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure("No routes found.");

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
						return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure($"Stop not found (ID: {fromStopTime.StopId}).");

					if (!stopDict.TryGetValue(toStopTime.StopId, out var toStop))
						return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure($"Stop not found (ID: {toStopTime.StopId}).");

					var departureTime = TimeSpan.Parse(fromStopTime.DepartureTime);
					var arrivalTime = TimeSpan.Parse(toStopTime.ArrivalTime);

					int travelTimeMinutes = (int)(arrivalTime - departureTime).TotalMinutes;

					allEdges.Add((GetParentStopOfStop(fromStop.Id).Id, GetParentStopOfStop(toStop.Id).Id, travelTimeMinutes));
				}
			}

			if (allEdges.Count == 0)
				return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure("No data found for the edges of the network");

			Dictionary<Guid, List<(Guid Destination, double Weight)>> connectivityMatrix = [];
			foreach (var (stop1id, stop2id, minutes) in allEdges)
			{
				connectivityMatrix[stop1id].Add((stop2id, minutes));
			}

			return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Success(connectivityMatrix);
		}

		internal Result<List<Guid>> GatherAllStopIds()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<Guid>>.Failure("No routes found.");

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
				return Result<List<Guid>>.Failure("No data found for the stops of the network");

			List<Guid> stopIds = [];
			foreach (var stop in allStops)
			{
				stopIds.Add(stop.Id);
			}

			return Result<List<Guid>>.Success(stopIds);
		}

		internal Result<List<(Guid, ENodeType)>> ExtractClassifiedStops()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<(Guid, ENodeType)>>.Failure("No routes found.");

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
				return Result<List<(Guid, ENodeType)>>.Failure("No data found for the stops of the network");

			List<(Guid, ENodeType)> classifiedStops = [];
			foreach (var stop in allStops)
			{
				classifiedStops.Add((stop.Id, stop.NodeType));
			}

			return Result<List<(Guid, ENodeType)>>.Success(classifiedStops);
		}

		internal Result<List<GenomeRoute>> GatherStaticRoutes()
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<GenomeRoute>>.Failure("No routes found in the feed");

			var genomeRoutes = new List<GenomeRoute>();

			var tripsByRoute = Trips
				.GroupBy(t => t.RouteId)
				.ToDictionary(g => g.Key, g => g.ToList());

			var stopTimesByTrip = StopTimes
				.Where(s => s.PickupType == GTFS.Entities.Enumerations.PickupType.Regular &&
							s.DropOffType == GTFS.Entities.Enumerations.DropOffType.NoPickup)
				.GroupBy(s => s.TripId)
				.ToDictionary(g => g.Key, g => g.OrderBy(st => st.StopSequence).ToList());

			var stopsById = Stops.ToDictionary(s => s.StopId);

			foreach (var route in Routes)
			{
				if (!route.IsStatic)
					continue;

				if (!tripsByRoute.TryGetValue(route.RouteId, out var routeTrips))
					return Result<List<GenomeRoute>>.Failure($"No trips for route ({route.ShortName}, {route.LongName}), route id: {route.Id}");

				var onTrips = routeTrips
					.Where(t => t.Direction == GTFS.Entities.Enumerations.DirectionType.OneDirection)
					.ToList();

				if (onTrips.Count == 0)
					return Result<List<GenomeRoute>>.Failure($"No ON direction trips for route ({route.ShortName}, {route.LongName}), route id: {route.Id}");

				var backTrips = routeTrips
					.Where(t => t.Direction == GTFS.Entities.Enumerations.DirectionType.OppositeDirection)
					.ToList();
				bool oneWay = backTrips.Count == 0;

				var onRoute = new List<Guid>();
				var backRoute = new List<Guid>();

				int onStartTime = -1;
				int backStartTime = -1;
				int headway = -1;

				// ---------- ON ROUTE ----------
				var onCounts = new Dictionary<int, int>();
				int onSum = 0;

				foreach (var trip in onTrips)
				{
					if (!stopTimesByTrip.TryGetValue(trip.TripId, out var tripStopTimes) || tripStopTimes.Count == 0)
						return Result<List<GenomeRoute>>.Failure($"Couldn't get stopTimes for Trip ({trip.TripId}), route id: {route.Id}");

					var first = tripStopTimes[0];

					if (!TryParseMinute(first.DepartureTime, out int minute))
						return Result<List<GenomeRoute>>.Failure($"Couldn't parse departure time for Trip ({trip.TripId}), route id: {route.Id}");

					// Count frequency
					if (!onCounts.TryAdd(minute, 1))
						onCounts[minute]++;

					onSum += minute;

					// Build route
					if (onRoute.Count == 0)
					{
						foreach (var st in tripStopTimes)
						{
							if (stopsById.TryGetValue(st.StopId, out var stop))
								onRoute.Add(stop.Id);
						}
					}
				}

				if (onCounts.Count == 0)
					return Result<List<GenomeRoute>>.Failure($"No valid stop times for route ({route.ShortName}, {route.LongName})");

				onStartTime = onCounts.Keys.Min();
				headway = onSum / onCounts.Count;

				// ---------- BACK ROUTE ----------


				if (!oneWay)
				{
					var backCounts = new Dictionary<int, int>();
					int backSum = 0;

					foreach (var trip in backTrips)
					{
						if (!stopTimesByTrip.TryGetValue(trip.TripId, out var tripStopTimes) || tripStopTimes.Count == 0)
							return Result<List<GenomeRoute>>.Failure($"Couldn't get stopTimes for Trip ({trip.TripId}), route id: {route.Id}");

						var first = tripStopTimes[0];

						if (!TryParseMinute(first.DepartureTime, out int minute))
							return Result<List<GenomeRoute>>.Failure($"Couldn't parse departure time for Trip ({trip.TripId}), route id: {route.Id}"); ;

						if (!backCounts.TryAdd(minute, 1))
							backCounts[minute]++;

						backSum += minute;

						if (backRoute.Count == 0)
						{
							foreach (var st in tripStopTimes)
							{
								if (stopsById.TryGetValue(st.StopId, out var stop))
									backRoute.Add(stop.Id);
							}
						}
					}

					if (backCounts.Count > 0)
					{
						backStartTime = backCounts.Keys.Min();
						int backHeadway = backSum / backCounts.Count;

						if (headway != backHeadway)
							headway = (headway + backHeadway) / 2;
					}
					else
					{
						oneWay = true;
					}
				}

				// ---------- BUILD RESULT ----------
				genomeRoutes.Add(
					oneWay
						? new GenomeRoute(onRoute, onStartTime, headway, true)
						: new GenomeRoute(onRoute, onStartTime, backRoute, backStartTime, headway, false)
				);
			}

			return Result<List<GenomeRoute>>.Success(genomeRoutes);
		}

		private static bool TryParseMinute(string time, out int minute)
		{
			minute = 0;

			if (string.IsNullOrEmpty(time) || time.Length < 5)
				return false;

			if (!char.IsDigit(time[3]) || !char.IsDigit(time[4]))
				return false;

			minute = (time[3] - '0') * 10 + (time[4] - '0');
			return true;
		}

		internal Result<Dictionary<Guid, List<Guid>>> CollectStopIdsGroupedByDistrict()
		{
			if (Districts == null || Districts.Count == 0) {
				return Result<Dictionary<Guid, List<Guid>>>.Failure("No Ddistricts in the feed");
			}
			if (Districts.Count == 1 && Districts[0].IsCollectorDistrict)
			{
				return Result<Dictionary<Guid, List<Guid>>>.Failure("Can't collect districts, because there is no district beside collector district");
			}

			Dictionary<Guid, List<Guid>> stopIdsGroupedByDistrict = [];
			foreach (var district in Districts)
			{
				HashSet<Guid> stopsOfDistrict = [];
				foreach (var stop in Stops)
				{
					if (stop.DistrictId == district.Id)
					{
						stopsOfDistrict.Add(stop.Id);
					}
				}
				stopIdsGroupedByDistrict[district.Id] = [.. stopsOfDistrict];
			}
			return Result<Dictionary<Guid, List<Guid>>>.Success(stopIdsGroupedByDistrict);
		}

		internal Result<List<Stop>> GatherStopsInCollectorDistricts()
		{
			List<Stop> stopList = [];
			foreach (var stop in Stops)
			{
				var district = Districts.Where(d => d.Id == stop.DistrictId).First();
				if (district != null && district.IsCollectorDistrict)
				{
					stopList.Add(stop);
				}
			}

			return Result<List<Stop>>.Success(stopList);
		}

		// ----------------------------

		public async void TryLoadingDistricts()
		{
			using var context = new DatabaseContext();
			TryLoadingDistricts(context);
		}

		public void TryLoadingDistricts(in DatabaseContext context)
		{
			//no district or only collector district, the gtfs is for veszprem
			if (Districts.Count <= 1 )
			{
				AddDistricts(VeszpremDistrict.DistrictNames, context);
				AddStopsToDistricts(VeszpremDistrict.DistrictNames, context);
				this.Stops = [.. context.Stops.Where(a => a.GtfsFeedId == Id)];
			}
		}

		public async void AddDistricts(List<(string, List<string>)> districts, DatabaseContext db)
		{
			var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				foreach (var (name, list) in districts)
				{
					var d = new District(name, Id, false, true);
					Districts.Add(d);
					db.Districts?.Add(d);
				}
				await db.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				await db.DisposeAsync();
				throw new Exception("" + ex.InnerException);
			}
			finally
			{
				await db.DisposeAsync();
				await transaction.DisposeAsync();
			}
		}

		private async void AddStopsToDistricts(List<(string, List<string>)> districts, DatabaseContext db)
		{
			var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				foreach (var (name, list) in districts)
				{
					var d = db.Districts.Where(d => d.Name.Equals(name)).First() ?? throw new Exception($"District with {name} not found");
					foreach (var stop in Stops)
					{
						if (list.Contains(stop.StopId))
						{
							var s = (db.Stops?.Where(s => s.StopId.Equals(stop.StopId)).First()) ?? throw new Exception($"Stop with stopid {stop.StopId} not found");
							s.DistrictId = d.Id;
							db.Stops?.Update(s);
						}
					}
				}
				await db.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				await db.DisposeAsync();
				throw new Exception("" + ex.InnerException);
			}
			finally
			{
				await db.DisposeAsync();
				await transaction.DisposeAsync();
			}
		}

		public void TryLoadingStopsTypes()
		{
			using var context = new DatabaseContext();
			TryLoadingStopsTypes(context);
		}

		private void TryLoadingStopsTypes(DatabaseContext db)
		{
			AddNodeTypesToStops(VeszpremDistrict.specificStops, db);
			this.Stops = [.. db.Stops.Where(a => a.GtfsFeedId == Id)];
			db.DisposeAsync();
		}

		private async void AddNodeTypesToStops(List<(ENodeType, List<string>)> specificStops, DatabaseContext db)
		{
			var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				foreach (var (type, stopids) in specificStops)
				{
					foreach (var stopid in stopids)
					{
						var s = (db.Stops?.Where(s => s.StopId.Equals(stopid)).First()) ?? throw new Exception($"Stop with stopid {stopid} not found");
						s.NodeType = type;
						db.Stops.Update(s);
					}
				}
				await db.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				await db.DisposeAsync();
				throw new Exception("" + ex.InnerException);
			}
			finally
			{
				await transaction.DisposeAsync();
			}
		}
	}
}
