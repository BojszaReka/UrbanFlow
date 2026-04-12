using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
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
		public Guid Id { get; private set; } = Guid.NewGuid();
		public string PublisherName { get; private set; }
		public string PublisherUrl { get; private set; }
		public string Lang { get; private set; }
		public string StartDate { get; private set; }
		public string EndDate { get; private set; }
		public string Version { get; private set; }

		// GTFS Data Collections
		public List<Agency> Agencies { get; private set; } = [];
		public List<Calendar> Calendars { get; private set; } = [];
		public List<CalendarDate> CalendarDates { get; private set; } = [];
		public List<Route> Routes { get; private set; } = [];
		public List<Shape> Shapes { get; private set; } = [];
		public List<Stop> Stops { get; private set; } = [];
		public List<StopTime> StopTimes { get; private set; } = [];
		public List<Trip> Trips { get; private set; } = [];

		//other
		public List<District> Districts { get; private set; }


		// Contructors

		public GtfsFeed() { }

		public GtfsFeed(Guid gtfsFeedId)
		{
			UpdateFromDatabase(gtfsFeedId);
			TryLoadingDistricts();
			TryLoadingStopsTypes();
		}

		public GtfsFeed(Guid gtfsFeedId, bool skipDistricts)
		{
			UpdateFromDatabase(gtfsFeedId);
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

			var agencies = context.Agencies?.Where(a => a.GtfsFeedId == id);
			if (agencies != null)
				this.Agencies = [.. agencies];

			var calendars = context.Calendars?.Where(a => a.GtfsFeedId == id);
			if (calendars != null)
				this.Calendars = [.. calendars];

			var calendarDates = context.CalendarDates?.Where(a => a.GtfsFeedId == id);
			if (calendarDates != null)
				this.CalendarDates = [.. calendarDates];

			var routes = context.Routes?.Where(a => a.GtfsFeedId == id);
			if(routes != null)
				this.Routes = [.. routes];

			var shapes = context.Shapes?.Where(a => a.GtfsFeedId == id);
			if(shapes != null)
				this.Shapes = [.. shapes];

			var stops = context.Stops?.Where(a => a.GtfsFeedId == id);
			if(stops != null)
				this.Stops = [.. stops];

			var stopTimes = context.StopTimes?.Where(a => a.GtfsFeedId == id);
			if(stopTimes != null)
				this.StopTimes = [.. stopTimes];

			var trips = context.Trips?.Where(a => a.GtfsFeedId == id);
			if(	trips!= null)
				this.Trips = [.. trips];

			var districts = context.Districts?.Where(a => a.GtfsFeedId == id);
			if(districts != null)
				this.Districts = [.. districts];

			context.Dispose();
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
				var departureTime = ParseGtfsTime(fromStopTime.DepartureTime);
				var arrivalTime = ParseGtfsTime(toStopTime.ArrivalTime);

				int travelTimeMinutes = (int)(arrivalTime - departureTime).TotalMinutes;

				var fromStopParentResult = GetParentStopOfStop(fromStop.Id);
				if (fromStopParentResult.IsFailure)
					Result<HashSet<EdgeDataDTO>>.Failure(fromStopParentResult.Error);

				var toStopParentResult = GetParentStopOfStop(toStop.Id);
				if (toStopParentResult.IsFailure)
					Result<HashSet<EdgeDataDTO>>.Failure(toStopParentResult.Error);

				edgeData.Add(new EdgeDataDTO
				{
					FromStopId = fromStopParentResult.Value.Id,
					ToStopId = toStopParentResult.Value.Id,
					TravelTimeMinutes = travelTimeMinutes,
				});
			}

			return Result<HashSet<EdgeDataDTO>>.Success(edgeData);
		}

		private static TimeSpan ParseGtfsTime(string time)
		{
			var parts = time.Split(':');

			int hours = int.Parse(parts[0]);
			int minutes = int.Parse(parts[1]);
			int seconds = parts.Length > 2 ? int.Parse(parts[2]) : 0;

			return new TimeSpan(hours, minutes, seconds);
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

					var departureTime = ParseGtfsTime(fromStopTime.DepartureTime);
					var arrivalTime = ParseGtfsTime(toStopTime.ArrivalTime);

					int travelTimeMinutes = (int)(arrivalTime - departureTime).TotalMinutes;

					var fromStopParentResult = GetParentStopOfStop(fromStop.Id);
					if (fromStopParentResult.IsFailure)
						return Result<List<EdgeDataDTO>>.Failure(fromStopParentResult.Error);

					var toStopParentResult = GetParentStopOfStop(toStop.Id);
					if (toStopParentResult.IsFailure)
						return Result<List<EdgeDataDTO>>.Failure(toStopParentResult.Error);

					allEdges.Add((fromStopParentResult.Value.Id, toStopParentResult.Value.Id, travelTimeMinutes));
				}
			}

			var additionalEdgeResults = GetStopToStopEdges();
			if (additionalEdgeResults.IsFailure)
				return Result<List<EdgeDataDTO>>.Failure(additionalEdgeResults.Error);

			foreach(var edge in additionalEdgeResults.Value)
			{
				allEdges.Add(edge);
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

		public Result<List<(Guid, Guid, int)>> GetStopToStopEdges()
		{
			try
			{
				var stopDict = Stops.ToDictionary(s => s.StopId);
				List<(Guid, Guid, int)> allEdges = [];

				foreach (var (fromlist, tolist) in VeszpremDistrict.stoptostopList)
				{
					foreach (var fromstopId in fromlist)
					{
						foreach (var tostopId in tolist)
						{

							if (!stopDict.TryGetValue(fromstopId, out var fromStop))
								return Result<List<(Guid, Guid, int)>>.Failure($"Stop not found (ID: {fromstopId}).");

							if (!stopDict.TryGetValue(tostopId, out var toStop))
								return Result<List<(Guid, Guid, int)>>.Failure($"Stop not found (ID: {tostopId}).");

							int travelTimeMinutes = 6;

							var fromStopParentResult = GetParentStopOfStop(fromStop.Id);
							if (fromStopParentResult.IsFailure)
								return Result<List<(Guid, Guid, int)>>.Failure(fromStopParentResult.Error);

							var toStopParentResult = GetParentStopOfStop(toStop.Id);
							if (toStopParentResult.IsFailure)
								return Result<List<(Guid, Guid, int)>>.Failure(toStopParentResult.Error);

							allEdges.Add((fromStopParentResult.Value.Id, toStopParentResult.Value.Id, travelTimeMinutes));
						}
					}
				}

				return Result<List<(Guid, Guid, int)>>.Success(allEdges);
			}
			catch (Exception ex)
			{
				return Result<List<(Guid, Guid, int)>>.Failure(ex.Message);
			}
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

					var stopParentResult = GetParentStopOfStop(stop.Id);
					if (stopParentResult.IsFailure)
						Result<HashSet<EdgeDataDTO>>.Failure(stopParentResult.Error);

					allStops.Add(stopParentResult.Value);
				}
			}

			var additionalEdgeResults = GetStopToStopEdges();
			if (additionalEdgeResults.IsFailure)
				return Result<List<NodeDataDTO>>.Failure(additionalEdgeResults.Error);

			foreach(var (fromid, toid, min) in additionalEdgeResults.Value)
			{
				bool containsFrom = false;
				bool containsTo = false;
				foreach (var stop in allStops)
				{
					if(stop.Id == fromid)
						containsFrom = true;

					if(stop.Id == toid)
						containsTo = true;
				}

				if (containsFrom && containsTo)
					continue;

				if (!containsFrom)
				{
					var stopParentResult = GetParentStopOfStop(fromid);
					if (stopParentResult.IsFailure)
						Result<HashSet<EdgeDataDTO>>.Failure(stopParentResult.Error);

					if (!fromid.Equals(stopParentResult.Value.Id)){
						Console.WriteLine();
					}
						

					allStops.Add(stopParentResult.Value);
				}

				if (!containsTo)
				{
					var stopParentResult = GetParentStopOfStop(toid);
					if (stopParentResult.IsFailure)
						Result<HashSet<EdgeDataDTO>>.Failure(stopParentResult.Error);

					if (!toid.Equals(stopParentResult.Value.Id))
					{
						Console.WriteLine();
					}

					allStops.Add(stopParentResult.Value);
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

				var stopParentResult = GetParentStopOfStop(stop.Id);
				if (stopParentResult.IsFailure)
					Result<HashSet<EdgeDataDTO>>.Failure(stopParentResult.Error);

				var data = new NodeDataDTO
				{
					Stop = stopParentResult.Value
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
			db.Routes?.UpdateRange(Routes);
			db.SaveChanges();
		}

		internal Result<string> GetRouteName(Guid routeId)
		{
			var route = Routes.Where(r => r.Id == routeId).FirstOrDefault();
			if (route == null)
				return Result<string>.Failure($"Route with id ({routeId}) not found");
			return Result<string>.Success(route.ShortName);
		}

		public Result<Stop> GetParentStopOfStop(Guid id)
		{
			var stop = Stops.Where(x => x.Id == id).FirstOrDefault();
			if (stop != null && stop.ParentStation != null)
			{
				var parentStation = Stops.Where(x => x.StopId == stop.ParentStation).FirstOrDefault();
				if (parentStation != null)
				{
					return Result<Stop>.Success(parentStation);
				}
				
			}
			if(stop != null)
				return Result<Stop>.Success(stop);

			return Result<Stop>.Failure($"Couldn't get parent stop for stop, id: {id}");
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
				try
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

						var departureTime = ParseGtfsTime(fromStopTime.DepartureTime);
						var arrivalTime = ParseGtfsTime(toStopTime.ArrivalTime);

						int travelTimeMinutes = (int)(arrivalTime - departureTime).TotalMinutes;

						var fromStopParentResult = GetParentStopOfStop(fromStop.Id);
						if (fromStopParentResult.IsFailure)
							Result<HashSet<EdgeDataDTO>>.Failure(fromStopParentResult.Error);

						var toStopParentResult = GetParentStopOfStop(toStop.Id);
						if (toStopParentResult.IsFailure)
							Result<HashSet<EdgeDataDTO>>.Failure(toStopParentResult.Error);

						allEdges.Add((fromStopParentResult.Value.Id, toStopParentResult.Value.Id, travelTimeMinutes));
					}
				}
				catch (Exception ex)
				{
					return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure($"Extract StopConnectivityMatrix failed for route {route.LongName}, error: {ex.Message}");
				}

			}

			foreach (var (fromlist, tolist) in VeszpremDistrict.stoptostopList)
			{
				foreach (var fromstopId in fromlist)
				{
					foreach (var tostopId in tolist)
					{

						if (!stopDict.TryGetValue(fromstopId, out var fromStop))
							return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure($"Stop not found (ID: {fromstopId}).");

						if (!stopDict.TryGetValue(tostopId, out var toStop))
							return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure($"Stop not found (ID: {tostopId}).");

						int travelTimeMinutes = 6;

						var fromStopParentResult = GetParentStopOfStop(fromStop.Id);
						if (fromStopParentResult.IsFailure)
							Result<HashSet<EdgeDataDTO>>.Failure(fromStopParentResult.Error);

						var toStopParentResult = GetParentStopOfStop(toStop.Id);
						if (toStopParentResult.IsFailure)
							Result<HashSet<EdgeDataDTO>>.Failure(toStopParentResult.Error);

						allEdges.Add((fromStopParentResult.Value.Id, toStopParentResult.Value.Id, travelTimeMinutes));
					}
				}
			}

			if (allEdges.Count == 0)
				return Result<Dictionary<Guid, List<(Guid Destination, double Weight)>>>.Failure("No data found for the edges of the network");

			Dictionary<Guid, List<(Guid Destination, double Weight)>> connectivityMatrix = [];
			foreach (var (stop1id, stop2id, minutes) in allEdges)
			{
				if (!connectivityMatrix.TryGetValue(stop1id, out List<(Guid Destination, double Weight)>? neighbours))
				{
					neighbours = [];
					connectivityMatrix[stop1id] = neighbours;
				}

				neighbours.Add((stop2id, minutes));
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

					var stopParentResult = GetParentStopOfStop(stop.Id);
					if (stopParentResult.IsFailure)
						Result<List<Guid>>.Failure(stopParentResult.Error);

					allStops.Add(stopParentResult.Value);
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

					var stopParentResult = GetParentStopOfStop(stop.Id);
					if (stopParentResult.IsFailure)
						Result<List<(Guid, ENodeType)>>.Failure(stopParentResult.Error);

					allStops.Add(stopParentResult.Value);
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

		public Result<Genome> ExtractNetworkAsGenome(in NetworkInformation network, in OptimizationParameters parameters)
		{
			var genomeListResult = GatherRoutesAsGenomeRoute(false);
			if (genomeListResult.IsFailure)
			{
				return Result<Genome>.Failure("Extracting routes as genomeroutes failed, because: " + genomeListResult.Error);
			}
			var genomeList = genomeListResult.Value;

			Genome g = new(-1 ,-1 , genomeList, parameters, network, "route");
			return Result<Genome>.Success(g);

		}

		internal Result<List<GenomeRoute>> GatherRoutesAsGenomeRoute(bool isStatic)
		{
			if (Routes == null || Routes.Count == 0)
				return Result<List<GenomeRoute>>.Failure("No routes found in the feed");

			var genomeRoutes = new List<GenomeRoute>();

			var tripsByRoute = Trips
				.GroupBy(t => t.RouteId)
				.ToDictionary(g => g.Key, g => g.ToList());

			var stopTimesByTrip = StopTimes
				.GroupBy(s => s.TripId)
				.ToDictionary(g => g.Key, g => g.OrderBy(st => st.StopSequence).ToList());

			var stopsById = Stops.ToDictionary(s => s.StopId);

			foreach (var route in Routes)
			{
				if (isStatic && !route.IsStatic)
					continue;

				if (!tripsByRoute.TryGetValue(route.RouteId, out var routeTrips))
					//return Result<List<GenomeRoute>>.Failure($"No trips for route ({route.ShortName}, {route.LongName}), route id: {route.Id}");
					continue;

				var onTrips = routeTrips
					.Where(t => t.Direction == GTFS.Entities.Enumerations.DirectionType.OneDirection)
					.ToList();

				if (onTrips.Count == 0)
					//return Result<List<GenomeRoute>>.Failure($"No ON direction trips for route ({route.ShortName}, {route.LongName}), route id: {route.Id}");
					continue;

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
							if (stopsById.TryGetValue(st.StopId, out var stop)){
								var stopParentResult = GetParentStopOfStop(stop.Id);
								if (stopParentResult.IsFailure)
									Result<List<GenomeRoute>>.Failure(stopParentResult.Error);

								onRoute.Add(stopParentResult.Value.Id); 
							}
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
								if (stopsById.TryGetValue(st.StopId, out var stop)){
									var stopParentResult = GetParentStopOfStop(stop.Id);
									if (stopParentResult.IsFailure)
										Result<List<GenomeRoute>>.Failure(stopParentResult.Error);

									backRoute.Add(stopParentResult.Value.Id); 
								}
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

			if (string.IsNullOrWhiteSpace(time))
				return false;

			var parts = time.Split(':');
			if (parts.Length < 2)
				return false;

			return int.TryParse(parts[1], out minute);
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
				var stops = context.Stops?.Where(a => a.GtfsFeedId == Id);
				if(stops != null)
					this.Stops = [.. stops];
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
				throw new Exception("" + ex.InnerException);
			}
			finally
			{
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
					var d = db.Districts?.Where(d => d.Name.Equals(name)).First() ?? throw new Exception($"District with {name} not found");
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
				throw new Exception("" + ex.InnerException);
			}
			finally
			{
				await transaction.DisposeAsync();
			}
		}

		public void TryLoadingStopsTypes()
		{
			using var context = new DatabaseContext();
			TryLoadingStopsTypes(context);
		}

		public void TryLoadingStopsTypes(DatabaseContext db)
		{
			AddNodeTypesToStops(VeszpremDistrict.specificStops, db);
			var stops = db.Stops?.Where(a => a.GtfsFeedId == Id);
			if(stops != null)
				this.Stops = [.. stops];
		}

		private static async void AddNodeTypesToStops(List<(ENodeType, List<string>)> specificStops, DatabaseContext db)
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

		public Result<List<(byte r, byte g, byte b)>> GatherRouteColors()
		{
			List<int?> colors = Routes.Select(r => r.Color).Where(c => c != null).ToList();
			if (colors == null)
				return Result<List<(byte r, byte g, byte b)>>.Failure("No colors found for routes");

			List<(byte r, byte g, byte b)> rgbColors = [];

			foreach (var color in colors) {
				byte a = (byte)((color >> 24) & 0xFF);
				byte r = (byte)((color >> 16) & 0xFF);
				byte g = (byte)((color >> 8) & 0xFF);
				byte b = (byte)(color & 0xFF);

				rgbColors.Add((r,g,b));
			}

			return Result<List<(byte r, byte g, byte b)>>.Success(rgbColors);			
		}

		internal Result<List<NodeDataDTO>> GatherStops(in db_ga.Genome genome, IReadOnlyList<Guid> allStops)
		{
			List<Guid> concatedStopIds = new List<Guid>(allStops);
			concatedStopIds.AddRange(genome.GetStopIdList());

			List<NodeDataDTO> nodes = [];

			HashSet<Guid> uniqueStopIds = new HashSet<Guid>(concatedStopIds);

			foreach (var stopId in uniqueStopIds)
			{
				var id = stopId;
				var stop = Stops.FirstOrDefault(s => s.Id.Equals(stopId));
				if (stop == null)
					return Result<List<NodeDataDTO>>.Failure("No stop found with the id: " + stopId);

				var node = new NodeDataDTO()
				{
					Stop = stop
				};
				nodes.Add(node);
			}

			if(nodes.Count == 0)
				return Result<List<NodeDataDTO>>.Failure("Couldn't gather all the stops");

			return Result<List<NodeDataDTO>>.Success(nodes);
		}
	}
}
