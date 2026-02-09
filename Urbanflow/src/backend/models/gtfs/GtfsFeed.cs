using GTFS;
using GTFS.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("GtfsFeeds")]
	public class GtfsFeed
	{
		// Properties
		[Key]
		public Guid Id { get; internal set; } = Guid.NewGuid();
		public Guid GtfsSourceId { get; internal set; }

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

		public GtfsFeed(Guid gtfsSourceId)
		{
			GtfsSourceId = gtfsSourceId;
			UpdateFromDatabase(gtfsSourceId);
		}

		public GtfsFeed(GTFSFeed feed)
		{
			AdaptCollection(feed.Agencies, Agencies, Id, (a, id) => new Agency(a, id));
			AdaptCollection(feed.Calendars, Calendars, Id, (c, id) => new Calendar(c, id));
			AdaptCollection(feed.CalendarDates, CalendarDates, Id, (cd, id) => new CalendarDate(cd, id));
			AdaptCollection(feed.Routes, Routes, Id, (r, id) => new Route(r, id));
			AdaptCollection(feed.Shapes, Shapes, Id, (s, id) => new Shape(s, id));
			AdaptCollection(feed.Stops, Stops, Id, (s, id) => new Stop(s, id));
			AdaptCollection(feed.StopTimes, StopTimes, Id, (st, id) => new StopTime(st, id));
			AdaptCollection(feed.Trips, Trips, Id, (t, id) => new Trip(t, id));

			GTFS.Entities.FeedInfo feedInfo = feed.GetFeedInfo();
			PublisherName = feedInfo.PublisherName ?? "Unknown";
			PublisherUrl = feedInfo.PublisherUrl ?? "Unknown";
			Lang = feedInfo.Lang ?? "Unknown";
			StartDate = feedInfo.StartDate ?? "Unknown";
			EndDate = feedInfo.EndDate ?? "Unknown";
			Version = feedInfo.Version ?? "Unknown";
			AddtoDatabase();
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
			DatabaseContext context = new();
			GtfsFeed? dbFeed = (context.GtfsFeeds?.Find(id)) ?? throw new Exception("GTFS Feed not found in database.");
			this.PublisherName = dbFeed.PublisherName;
			this.PublisherUrl = dbFeed.PublisherUrl;
			this.Lang = dbFeed.Lang;
			this.StartDate = dbFeed.StartDate;
			this.EndDate = dbFeed.EndDate;
			this.Version = dbFeed.Version;

			this.Agencies = [.. dbFeed.Agencies.Where(a => a.GtfsFeedId == id)];
			this.Calendars = [.. dbFeed.Calendars.Where(a => a.GtfsFeedId == id)];
			this.CalendarDates = [.. dbFeed.CalendarDates.Where(a => a.GtfsFeedId == id)];
			this.Routes = [.. dbFeed.Routes.Where(a => a.GtfsFeedId == id)];
			this.Shapes = [.. dbFeed.Shapes.Where(a => a.GtfsFeedId == id)];
			this.Stops = [.. dbFeed.Stops.Where(a => a.GtfsFeedId == id)];
			this.StopTimes = [.. dbFeed.StopTimes.Where(a => a.GtfsFeedId == id)];
			this.Trips = [.. dbFeed.Trips.Where(a => a.GtfsFeedId == id)];
		}

		public void UpdateDatabase()
		{
			DatabaseContext context = new();
			var existingFeed = context.GtfsFeeds?.Find(Id);
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

		private void AddtoDatabase()
		{
			DatabaseContext context = new();
			context.GtfsFeeds?.Add(this);
			context.Agencies?.AddRange(this.Agencies);
			context.Calendars?.AddRange(this.Calendars);
			context.CalendarDates?.AddRange(this.CalendarDates);
			context.Routes?.AddRange(this.Routes);
			context.Shapes?.AddRange(this.Shapes);
			context.Stops?.AddRange(this.Stops);
			context.StopTimes?.AddRange(this.StopTimes);
			context.Trips?.AddRange(this.Trips);
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

	}
}
