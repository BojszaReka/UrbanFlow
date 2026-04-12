using GTFS;
using GTFS.Fields;
using GTFS.IO;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.enums;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;

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

		internal static Guid UploadGtfsData(string gtfsPath, in DatabaseContext db)
		{
			GTFSFeed feed = ParseGtfsData(gtfsPath);
			GtfsFeed gtfsFeed = new(feed, db);

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
			}
			catch (GTFS.Exceptions.GTFSParseException ex)
			{
				throw new("Failed to parse GTFS data: " + ex.Message);
			}
			return feed;
		}

		internal static List<Route> GetRoutesForWorkflow(in GtfsFeed feed)
		{
			throw new NotImplementedException();
		}

		public static Result<GraphDataDTO> GetDataForNetworkGraph(in GtfsFeed feed)
		{
			//feed.SetNodeTypeForStops();
			var edgeResult = feed.GetDataForEdgesOfNetwork();
			if (edgeResult.IsFailure)
				return Result<GraphDataDTO>.Failure(edgeResult.Error);
			var networkEdgesData = edgeResult.Value;

			var nodeResult = feed.GetStopsForNetworkGraph();
			if (edgeResult.IsFailure)
				return Result<GraphDataDTO>.Failure(edgeResult.Error);
			var networkNodeData = nodeResult.Value;

			return new GraphDataDTO(networkEdgesData, networkNodeData);
		}

		public static Result<GraphDataDTO> GetDataForRouteGraph(in GtfsFeed feed, Guid routeId)
		{
			string name = $"Route {feed.GetRouteName(routeId)} graph";
			var edgeResult = feed.GetDataForEdgesOfRoute(routeId);
			if (edgeResult.IsFailure)
				return Result<GraphDataDTO>.Failure(edgeResult.Error);
			var routeEdgesData = edgeResult.Value;

			var nodeResult = feed.GetStopsForRoute(routeId);
			if (nodeResult.IsFailure)
				return Result<GraphDataDTO>.Failure(nodeResult.Error);
			var routeNodeData = nodeResult.Value;

			return new GraphDataDTO([.. routeEdgesData], routeNodeData, name);
		}

		public static Result<List<Guid>> GetRouteIds(in GtfsFeed feed)
		{
			List<Guid> routeIds = [.. feed.Routes.Select(r => r.Id)];
			if (routeIds.Count != 0)
				return Result<List<Guid>>.Success(routeIds);
			return Result<List<Guid>>.Failure("Couln't get route ids");
		}

		public Result<NetworkInformation> ExtractNetworkInformationForGA(in GtfsFeed feed)
		{


			var classifiedResult = feed.ExtractClassifiedStops();
			if (classifiedResult.IsFailure)
			{
				return Result<NetworkInformation>.Failure("Extracting classified stops failed: " + classifiedResult.Error);
			}
			List<Guid> terminals = [.. classifiedResult.Value.Where(x => x.Item2 == ENodeType.Terminal).Select(x => x.Item1)];
			List<Guid> hubs = [.. classifiedResult.Value.Where(x => x.Item2 == ENodeType.Junction).Select(x => x.Item1)];
			List<Guid> genStops = [.. classifiedResult.Value.Where(x => x.Item2 == ENodeType.Default).Select(x => x.Item1)];
			List<Guid> allStops = [.. classifiedResult.Value.Select(x => x.Item1)];

			var matrixResult = feed.ExtractStopConnectivityMatrix();
			if (matrixResult.IsFailure)
			{
				return Result<NetworkInformation>.Failure("Extracting connectivity matrix failed: " + matrixResult.Error);
			}
			var matrix = matrixResult.Value;

			var staticRouteResult = feed.GatherRoutesAsGenomeRoute(true);
			if (staticRouteResult.IsFailure)
			{
				return Result<NetworkInformation>.Failure("Extracting static routes failed: " + staticRouteResult.Error);
			}
			var staticRoutes = staticRouteResult.Value;

			var districtResult = feed.CollectStopIdsGroupedByDistrict();
			if (districtResult.IsFailure)
			{
				return Result<NetworkInformation>.Failure("Extracting districts failed: " + districtResult.Error);
			}
			var districts = districtResult.Value;

			NetworkInformation networkInformation = new(terminals, hubs, genStops, allStops, matrix, staticRoutes, districts);

			return Result<NetworkInformation>.Success(networkInformation);
		}

		public Result<List<(byte r, byte g, byte b)>> GatherRouteColors(in GtfsFeed feed)
		{
			return feed.GatherRouteColors();
		}
	}
}
