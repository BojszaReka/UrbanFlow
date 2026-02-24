using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.enums;
using Urbanflow.src.backend.models.graph;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.backend.models
{
	[Table("Workflows")]
	public class Workflow
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid GtfsFeedId { get; set; } = Guid.Empty;
		public string Name { get; set; } = string.Empty;
		public Guid CityId { get; set; } = Guid.Empty;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string Description { get; set; } = string.Empty; 
		public DateTime LastModified { get; set; } = DateTime.UtcNow;
		public bool IsActive { get; set; } = true;


		[ForeignKey("CityId")]
		public City City { get; set; }

		[ForeignKey("GtfsFeedId")]
		private readonly GtfsFeed GtfsFeed;
		[NotMapped]
		private HashSet<Graph> Graphs;


		[NotMapped]
		private readonly GtfsManagerService gtfsManager;
		[NotMapped]
		private readonly GraphManagerService graphManager;


		public Workflow() { }

		public Workflow(string name, Guid cityId, string description, Guid feedid)
		{
			Name = name;
			CityId = cityId;
			Description = description;
			GtfsFeedId = feedid;

			using var db = new DatabaseContext();
			var feed = db.GtfsFeeds.Where(g => g.Id == GtfsFeedId).FirstOrDefault();
			if (GtfsFeed == null)
			{
				throw new Exception("The attached feed does not exists");
			}
			City = db.Cities.Where(c => c.Id == CityId).FirstOrDefault();
			if (City == null)
			{
				throw new Exception("The atached city does not exists");
			}

			GtfsFeed = new GtfsFeed(GtfsFeedId);

			gtfsManager = new GtfsManagerService();
			graphManager = new GraphManagerService();
		}
		 
		public Workflow(Guid workflowId)
		{
			using var db = new DatabaseContext();
			var workflow = db.Workflows.Where(w => w.Id == workflowId).First() ?? throw new Exception("Workflow not found");
			if (!IsActive)
			{
				throw new Exception("The workflow is deleted");
			}

			var feed = db.GtfsFeeds.Where(g => g.Id == workflow.GtfsFeedId).First() ?? throw new Exception("The attached feed does not exists");

			City = db.Cities.Where(c => c.Id == workflow.CityId).First() ?? throw new Exception("The atached city does not exists");

			Id = workflow.Id;
			GtfsFeedId = workflow.GtfsFeedId;
			Name = workflow.Name;
			Description = workflow.Description;
			CreatedAt = workflow.CreatedAt;
			LastModified = workflow.LastModified;
			IsActive = workflow.IsActive;

			GtfsFeed = new GtfsFeed(GtfsFeedId);

			gtfsManager = new GtfsManagerService();
			graphManager = new GraphManagerService();
		}

		internal Result<Graph> GetNetWorkGraphData()
		{
			if(Graphs == null || Graphs.Count == 0)
			{
				var networkResult = GtfsManagerService.GetDataForNetworkGraph(GtfsFeed);
				if (networkResult.IsFailure)
					return Result<Graph>.Failure(networkResult.Error);
				var graphDataDTO = networkResult.Value;

				var routesResult = GtfsManagerService.GetRouteIds(GtfsFeed);
				if(routesResult.IsFailure)
					return Result<Graph>.Failure(routesResult.Error);
				var routeIds = routesResult.Value;

				Dictionary<Guid, GraphDataDTO> routeGraphDTO = [];
				foreach(var routeId in routeIds)
				{
					var routeDataRes = GtfsManagerService.GetDataForRouteGraph(GtfsFeed, routeId);
					if(routeDataRes.IsFailure)
						return Result<Graph>.Failure(routesResult.Error);
					routeGraphDTO[routeId] = routeDataRes.Value;
				}

				var graphsResult = GraphManagerService.CreateAllGraphsForFeed(Id, graphDataDTO, routeGraphDTO);
				if(graphsResult.IsFailure)
					return Result<Graph>.Failure(graphsResult.Error);
				Graphs = graphsResult.Value;
			}
			

			var networkGraph = Graphs.Where(g => g.Type == EGraphType.Network).ToList();
			if(!networkGraph.Any())
				return Result<Graph>.Failure("No network graphs found");
			if(networkGraph.Count() > 0)
				return Result<Graph>.Failure("Multiple network graphs found");
			return Result<Graph>.Success(networkGraph.First());
		}

		internal Result<HashSet<Route>> GetAllRoutes()
		{
			throw new NotImplementedException();
		}
	}
}
