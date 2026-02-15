using GTFS;
using GTFS.Fields;
using GTFS.IO;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.gtfs;

namespace Urbanflow.src.backend.services
{
	public class GtfsManagerService
	{
		public GtfsManagerService()
		{
		}

		public static Guid UploadGtfsData(string gtfsPath)
		{
			//gets a gtfs data feed
			//parses the data up to the database
			//returns the GTFS version string
			GTFSFeed feed = ParseGtfsData(gtfsPath);
			GtfsFeed gtfsFeed = new(feed);

			return gtfsFeed.Id; //placeholder
		}

		private static GTFSFeed ParseGtfsData(string gtfsPath)
		{
			var reader = new GTFSReader<GTFSFeed>
			{
				LinePreprocessor = delegate (string s) { return s.Replace(", ", " ").Replace("\"\",3", "\"700\",\"700\"").Replace("\"", ""); }
			};
			var feed = null as GTFSFeed;
			try
			{
				feed = reader.Read(gtfsPath);
			} catch (GTFS.Exceptions.GTFSParseException ex)
			{
				throw new("Failed to parse GTFS data: " + ex.Message);
			}
			return feed;
		}

		internal static List<Route> GetRoutesForWorkflow(Guid workflowId)
		{
			throw new NotImplementedException();
		}

		public GraphDataDTO GetDataForNetworkGraph(Guid workflowId)
		{
			using var db = new DatabaseContext();
			var workflow = db.Workflows?.Find(workflowId);
			if (workflow == null) { throw new("Workflow not found"); }

			GtfsFeed feed = new(workflow.GtfsFeedId);
			feed.SetNodeTypeForStops();
			var networkEdgesData = feed.GetDataForEdgesOfNetwork();
			var networkNodeData = feed.GetStopsForNetworkGraph();

			return new GraphDataDTO(networkEdgesData, networkNodeData);
		}

		public GraphDataDTO GetDataForRouteGraph(Guid workflowId, Guid routeId)
		{
			using var db = new DatabaseContext();
			var workflow = db.Workflows?.Find(workflowId);
			if (workflow == null) { throw new("Workflow not found"); }

			GtfsFeed feed = new(workflow.GtfsFeedId);
			string name = $"Route {feed.GetRouteName(routeId)} graph";
			var routeEdgesData = feed.GetDataForEdgesOfRoute(routeId);
			var routeNodeData = feed.GetStopsForRoute(routeId);

			return new GraphDataDTO(routeEdgesData, routeNodeData, name);
		}

		public List<Guid> GetRouteIds(Guid workflowId)
		{
			using var db = new DatabaseContext();
			var workflow = db.Workflows?.Find(workflowId);
			if (workflow == null) { throw new("Workflow not found"); }

			GtfsFeed feed = new(workflow.GtfsFeedId);

			return [.. feed.Routes.Select(r => r.Id)];

		}
	}
}
