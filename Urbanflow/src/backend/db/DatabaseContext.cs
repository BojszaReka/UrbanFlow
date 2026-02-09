using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using System.IO;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.graph;
using Urbanflow.src.backend.models.gtfs;

namespace Urbanflow.src.backend.db
{
	public class DatabaseContext : DbContext
	{

		public DbSet<City>? Cities { get; set; }
		public DbSet<Workflow>? Workflows { get; set; }

		public DbSet<Graph>? Graphs { get; set; }
		public DbSet<Node>? Nodes { get; set; }
		public DbSet<Edge>? Edges { get; set; }
		public DbSet<GraphEdge>? GraphEdges { get; set; }
		public DbSet<GraphNode>? GraphNodes { get; set; }

		public DbSet<Agency>? Agencies { get; set; }
		public DbSet<Calendar>? Calendars { get; set; }
		public DbSet<CalendarDate>? CalendarDates { get; set; }
		public DbSet<FeedInfo>? FeedInfos { get; set; }
		public DbSet<GtfsFeed>? GtfsFeeds { get; set; }
		public DbSet<Route>? Routes { get; set; }
		public DbSet<Shape>? Shapes { get; set; }
		public DbSet<Stop>? Stops { get; set; }
		public DbSet<StopTime>? StopTimes { get; set; }
		public DbSet<Trip>? Trips { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string urbanflowDir = Path.Combine(appDataPath, "Urbanflow");
			Directory.CreateDirectory(urbanflowDir);
			string dbPath = Path.Combine(urbanflowDir, "urbanflow.db");
			optionsBuilder.UseSqlite($"Data Source={dbPath}");

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<City>().HasKey(c => c.Id);
			modelBuilder.Entity<Workflow>().HasKey(w => w.Id);

			modelBuilder.Entity<Agency>().HasKey(a => a.Id);
			modelBuilder.Entity<Calendar>().HasKey(c => c.Id);
			modelBuilder.Entity<CalendarDate>().HasKey(cd => cd.Id);
			modelBuilder.Entity<FeedInfo>().HasKey(fi => fi.Id);
			modelBuilder.Entity<GtfsFeed>().HasKey(gf => gf.Id);
			modelBuilder.Entity<Route>().HasKey(r => r.Id);
			modelBuilder.Entity<Shape>().HasKey(s => s.Id);
			modelBuilder.Entity<Stop>().HasKey(s => s.Id);
			modelBuilder.Entity<StopTime>().HasKey(st => st.Id);
			modelBuilder.Entity<Trip>().HasKey(t => t.Id);

			modelBuilder.Entity<Graph>().HasKey(g => g.Id);
			modelBuilder.Entity<Node>().HasKey(n => n.Id);
			modelBuilder.Entity<Edge>().HasKey(e => e.Id);
			modelBuilder.Entity<GraphEdge>().HasKey( x => new { x.GraphId, x.EdgeId });
			modelBuilder.Entity<GraphNode>().HasKey(x => new { x.GraphId, x.NodeId });


			modelBuilder.Entity<City>().ToTable("Cities");
			modelBuilder.Entity<Workflow>().ToTable("Workflows");

			modelBuilder.Entity<Agency>().ToTable("Agencies");
			modelBuilder.Entity<Calendar>().ToTable("Calendars");
			modelBuilder.Entity<CalendarDate>().ToTable("CalendarDates");
			modelBuilder.Entity<FeedInfo>().ToTable("FeedInfos");
			modelBuilder.Entity<GtfsFeed>().ToTable("GtfsFeeds");
			modelBuilder.Entity<Route>().ToTable("Routes");
			modelBuilder.Entity<Shape>().ToTable("Shapes");
			modelBuilder.Entity<Stop>().ToTable("Stops");
			modelBuilder.Entity<StopTime>().ToTable("StopTimes");
			modelBuilder.Entity<Trip>().ToTable("Trips");

			modelBuilder.Entity<Graph>().ToTable("Graphs");
			modelBuilder.Entity<Node>().ToTable("Nodes");
			modelBuilder.Entity<Edge>().ToTable("Edges");
			modelBuilder.Entity<GraphEdge>().ToTable("GraphEdges");
			modelBuilder.Entity<GraphNode>().ToTable("GraphNodes");

		}
	}
}
