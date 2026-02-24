using GTFS;
using GTFS.Entities;
using NetTopologySuite.EdgeGraph;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.enums;
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
			var routeStops = new List<(uint SequenceNumber, string StopId)>();

			Route route = Routes.First(r => r.Id == routeId);
			if(route == null)
				return Result<List<(uint SequenceNumber, string StopId)>>.Failure($"Route not found. (ID: {routeId})");

			var routeTrips = Trips.Where(t => t.RouteId == route.RouteId);

			var trip = routeTrips.First();

			if(trip == null)
			{
				return Result<List<(uint SequenceNumber, string StopId)>>.Failure($"No trips found for route. (ID: {routeId})");
			}

			var stopTimes = StopTimes
				.Where(st => st.TripId == trip.TripId)
				.OrderBy(st => st.StopSequence);

			foreach (var stopTime in stopTimes)
			{
				var stop = Stops.First(s => s.StopId == stopTime.StopId);
				if(stop == null)
					return Result<List<(uint SequenceNumber, string StopId)>>.Failure($"Stop not found for specific stoptime.");

				routeStops.Add((stopTime.StopSequence, stop.StopId));
			}

			return Result<List<(uint SequenceNumber, string StopId)>>.Success(routeStops);
		}

		private Result<HashSet<EdgeDataDTO>> GetDataForEdgesOfRoute(string routeId) {
			var route = Routes.Where(r => r.RouteId == routeId).First();
			if (route == null) return Result<HashSet<EdgeDataDTO>>.Failure("Route with RouteId does not exists");
			return GetDataForEdgesOfRoute(route.Id);
		}

		public Result<HashSet<EdgeDataDTO>> GetDataForEdgesOfRoute(Guid routeId)
		{
			var edgeData = new HashSet<EdgeDataDTO>();

			Route route = Routes.First(r => r.Id == routeId);
			
			if(route == null)
				return Result<HashSet<EdgeDataDTO>>.Failure($"Route not found (ID: {routeId}).");

			string rId = route.RouteId;
			var trip = Trips.Where(t => t.RouteId == route.RouteId).First();

			if(trip == null)
			{
				return Result<HashSet<EdgeDataDTO>>.Failure($"Trip not found for route {routeId}.");
			}

			var stopTimes = StopTimes.Where(st => st.TripId == trip.TripId).OrderBy(s => s.StopSequence).ToList();

			for (int i = 0; i < stopTimes.Count - 1; i++)
			{
				var fromStop = Stops.First(s => s.StopId == stopTimes[i].StopId);
				if(fromStop == null)
					return Result<HashSet<EdgeDataDTO>>.Failure($"Stop not found (ID: {stopTimes[i].StopId}).");

				var toStop = Stops.First(s => s.StopId == stopTimes[i + 1].StopId);
				if (fromStop == null)
					return Result<HashSet<EdgeDataDTO>>.Failure($"Stop not found (ID: {stopTimes[i+1].StopId}).");

				var departureTime = TimeSpan.Parse(stopTimes[i].DepartureTime);
				var arrivalTime = TimeSpan.Parse(stopTimes[i + 1].ArrivalTime);

				int travelTimeMinutes =
					(int)(arrivalTime - departureTime).TotalMinutes;

				edgeData.Add(new EdgeDataDTO()
				{
					FromStopId = GetParentStopOfStop(fromStop.Id).Id,
					ToStopId = GetParentStopOfStop(toStop.Id).Id,
					TravelTimeMinutes = travelTimeMinutes,
				});
			}

			return Result<HashSet<EdgeDataDTO>>.Success(edgeData); ;
		}

		public Result<List<EdgeDataDTO>> GetDataForEdgesOfNetwork()
		{
			HashSet<EdgeDataDTO> alledge = [];
			foreach (var route in Routes)
			{
				var edgeDataResult = GetDataForEdgesOfRoute(route.Id);
				if (edgeDataResult.IsFailure)
					return Result<List<EdgeDataDTO>>.Failure(edgeDataResult.Error);
				var edgeData = edgeDataResult.Value;
				if (edgeData == null && edgeData.Count < 0)
					continue;
				foreach (var edge in edgeData)						
				{
					alledge.Add(edge);
				}				
			}
			if(alledge.Count < 0)
				return Result<List<EdgeDataDTO>>.Failure("No data found for the edges of the network");
			return Result<List<EdgeDataDTO>>.Success([.. alledge]);
		}

		public Result<List<NodeDataDTO>> GetStopsForNetworkGraph() {
			HashSet<NodeDataDTO> allStops = [];
			foreach (var route in Routes) { 
				var routeStopsResult = RouteStops(route.Id);
				if(routeStopsResult.IsFailure)
					return Result < List < NodeDataDTO >>.Failure(routeStopsResult.Error);
				var routeStops = routeStopsResult.Value;
				foreach (var (SequenceNumber, StopId) in routeStops)
				{
					var stop= Stops.Where(s => s.StopId == StopId).FirstOrDefault();
					if (stop == null)
						continue;

					var data = new NodeDataDTO()
					{
						Stop = GetParentStopOfStop(stop.Id)
					};
					if (allStops != null && allStops.Contains(data))
						continue;
					allStops.Add(data);
				}
			}
			if (allStops.Count < 0)
				return Result<List<NodeDataDTO>>.Failure("No data found for the nodes of the network");
			return Result<List<NodeDataDTO>>.Success([.. allStops]);
		}


		public Result<List<NodeDataDTO>> GetStopForRoute(string routeId) { 
			var route = Routes.Where(r => r.RouteId == routeId).FirstOrDefault();
			if (route == null) return Result<List<NodeDataDTO>>.Failure("Route with RouteId does not exists");
			return GetStopsForRoute(route.Id);
		}

		public Result<List<NodeDataDTO>> GetStopsForRoute(Guid routeId)
		{
			List<NodeDataDTO> allStops = [];
			var routeStopsResult = RouteStops(routeId);
			if (routeStopsResult.IsFailure)
				return Result<List<NodeDataDTO>>.Failure(routeStopsResult.Error);
			var routeStops = routeStopsResult.Value;
			foreach (var (SequenceNumber, StopId) in routeStops)
			{
				var stop = Stops.Where(s => s.StopId == StopId).FirstOrDefault();
				if (stop == null)
					continue;
				var data = new NodeDataDTO()
				{
					Stop = GetParentStopOfStop(stop.Id)
				};
				if (allStops != null && allStops.Contains(data))
					continue;
				allStops.Add(data);
			}
			if (allStops.Count > 0)
				return Result<List<NodeDataDTO>>.Success(allStops);

			return Result<List<NodeDataDTO>>.Failure($"No stops could be gathered for route (ID: {routeId})"); ;
		}

		public void SetNodeTypeForStops()
		{
			foreach (var route in Routes)
			{
				var routeStopsResult = RouteStops(route.Id);
				if (routeStopsResult.IsFailure)
					continue;
				var routeStops = routeStopsResult.Value;
				if (routeStops != null && routeStops.Count > 0 ) {
					var lastSequenceNumber = routeStops.Last().SequenceNumber;
					foreach (var (SequenceNumber, StopId) in routeStops)
					{
						Stop stop = Stops.Where(s => s.StopId == StopId).FirstOrDefault();
						if (stop == null || stop.NodeType == ENodeType.Terminal)
							continue;

						if (SequenceNumber == 1 || SequenceNumber == lastSequenceNumber)
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

		// ----------------------------
	}
}
