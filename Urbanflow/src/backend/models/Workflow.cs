using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.enums;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.db_ga;
using Urbanflow.src.backend.models.graph;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;
using Urbanflow.src.backend.test_automater;

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
		private GtfsFeed GtfsFeed;

		private List<db_ga.Genome> Genomes;

		[NotMapped]
		private HashSet<Graph> Graphs;


		[NotMapped]
		private readonly GtfsManagerService gtfsManager;

		[NotMapped]
		private NetworkInformation networkInformation;
		[NotMapped]
		private GAOptimizationService gaOptimizationService;

		public Workflow() { }

		public Workflow(string name, City city, string description, Guid feedid)
		{
			Name = name;
			CityId = city.Id;
			City = city;
			Description = description;
			GtfsFeedId = feedid;

			using var db = new DatabaseContext();
			var feed = (db.GtfsFeeds?.Where(g => g.Id == GtfsFeedId).FirstOrDefault()) ?? throw new Exception("The attached feed does not exists");
			GtfsFeed = new GtfsFeed(GtfsFeedId);

			gtfsManager = new GtfsManagerService();
		}

		public Workflow(string name, Guid cityId, string description, Guid feedid)
		{
			Name = name;
			CityId = cityId;
			Description = description;
			GtfsFeedId = feedid;

			using var db = new DatabaseContext();
			var feed = (db.GtfsFeeds?.Where(g => g.Id == GtfsFeedId).FirstOrDefault()) ?? throw new Exception("The attached feed does not exists");
			City = db.Cities?.Where(c => c.Id == CityId).FirstOrDefault() ?? throw new Exception("The atached city does not exists");
			GtfsFeed = new GtfsFeed(GtfsFeedId);

			gtfsManager = new GtfsManagerService();
		}

		public Workflow(Guid workflowId)
		{
			using var db = new DatabaseContext();
			var workflow = db.Workflows?.Where(w => w.Id == workflowId).First() ?? throw new Exception("Workflow not found");
			if (!IsActive)
			{
				throw new Exception("The workflow is deleted");
			}

			var feed = db.GtfsFeeds?.Where(g => g.Id == workflow.GtfsFeedId).First() ?? throw new Exception("The attached feed does not exists");

			City = db.Cities?.Where(c => c.Id == workflow.CityId).First() ?? throw new Exception("The atached city does not exists");

			Id = workflow.Id;
			GtfsFeedId = workflow.GtfsFeedId;
			Name = workflow.Name;
			Description = workflow.Description;
			CreatedAt = workflow.CreatedAt;
			LastModified = workflow.LastModified;
			IsActive = workflow.IsActive;

			GtfsFeed = new GtfsFeed(GtfsFeedId);

			gtfsManager = new GtfsManagerService();
			LoadSavedGenomes();
		}

		internal Result<Graph> GetNetWorkGraphData()
		{
			if (GtfsFeed == null)
			{
				return Result<Graph>.Failure("The feed is empty");
			}


			if ((Graphs == null || Graphs.Count == 0))
			{
				var networkResult = GtfsManagerService.GetDataForNetworkGraph(GtfsFeed);
				if (networkResult.IsFailure)
					return Result<Graph>.Failure(networkResult.Error);
				var graphDataDTO = networkResult.Value;

				var routesResult = GtfsManagerService.GetRouteIds(GtfsFeed);
				if (routesResult.IsFailure)
					return Result<Graph>.Failure(routesResult.Error);
				var routeIds = routesResult.Value;

				Dictionary<Guid, GraphDataDTO> routeGraphDTO = [];
				foreach (var routeId in routeIds)
				{
					var routeDataRes = GtfsManagerService.GetDataForRouteGraph(GtfsFeed, routeId);
					if (routeDataRes.IsFailure)
						continue;
					//return Result<Graph>.Failure(routeDataRes.Error);
					routeGraphDTO[routeId] = routeDataRes.Value;
				}

				var graphsResult = GraphManagerService.CreateAllGraphsForFeed(Id, graphDataDTO, routeGraphDTO);
				if (graphsResult.IsFailure)
					return Result<Graph>.Failure(graphsResult.Error);
				Graphs = graphsResult.Value;
			} else if (!Graphs.Where(g => g.Type == EGraphType.Network).Any() && Graphs.Count > 0)
			{
				var networkResult = GtfsManagerService.GetDataForNetworkGraph(GtfsFeed);
				if (networkResult.IsFailure)
					return Result<Graph>.Failure(networkResult.Error);
				var graphDataDTO = networkResult.Value;

				var routesResult = GtfsManagerService.GetRouteIds(GtfsFeed);
				if (routesResult.IsFailure)
					return Result<Graph>.Failure(routesResult.Error);
				var routeIds = routesResult.Value;

				Dictionary<Guid, GraphDataDTO> routeGraphDTO = [];
				foreach (var routeId in routeIds)
				{
					var routeDataRes = GtfsManagerService.GetDataForRouteGraph(GtfsFeed, routeId);
					if (routeDataRes.IsFailure)
						return Result<Graph>.Failure(routesResult.Error);
					routeGraphDTO[routeId] = routeDataRes.Value;
				}

				var graphsResult = GraphManagerService.SafeGraphGeneration(Id, Graphs, graphDataDTO, routeGraphDTO);
				if (graphsResult.IsFailure)
					return Result<Graph>.Failure(graphsResult.Error);
				Graphs = graphsResult.Value;
			}


			var networkGraph = Graphs.Where(g => g.Type == EGraphType.Network).ToList();
			if (networkGraph.Count == 0)
				return Result<Graph>.Failure("No network graphs found");
			if (networkGraph.Count > 1)
				return Result<Graph>.Failure("Multiple network graphs found");
			return Result<Graph>.Success(networkGraph.First());
		}

		internal Result<HashSet<Route>> GetAllRoutes()
		{
			throw new NotImplementedException();
		}

		internal void SetNetworkinformationFromInnerGtfsFeed()
		{
			//GtfsFeed.TryLoadingStopsTypes();
			Result<NetworkInformation> networkInfoResult = gtfsManager.ExtractNetworkInformationForGA(GtfsFeed);
			if (networkInfoResult.IsFailure) throw new Exception($"Setting network information for workflow is unsucsessful, because extraction from GTFS Feed failed, error: {networkInfoResult.Error}");
			networkInformation = networkInfoResult.Value;
		}

		internal void CreateGAOptimizationService(in OptimizationSettings settings)
		{
			gaOptimizationService = new GAOptimizationService(networkInformation, settings);
		}

		internal Result<List<RunResults>> RunGA(string descriptor)
		{
			List<RunResults> runResults = [];
			var result = gaOptimizationService.RunGeneticAlgorithm(descriptor, false);
			if (result.IsFailure)
			{
				return Result<List<RunResults>>.Failure("New way genetic algorithm failed: " + result.Error);
			}
			runResults.Add(result.Value);
			result = gaOptimizationService.RunGeneticAlgorithm(descriptor, true);
			if (result.IsFailure)
			{
				return Result<List<RunResults>>.Failure("Old way genetic algorithm failed: " + result.Error);
			}
			runResults.Add(result.Value);
			return runResults;
		}

		internal ga.Genome GetGenomeForNetwork(in OptimizationParameters parameters)
		{
			var result = GtfsFeed.ExtractNetworkAsGenome(networkInformation, parameters);
			if (result.IsFailure) {
				throw new Exception(result.Error);
			}
			return result.Value;
		}

		internal async Task SetStopTypes()
		{
			GtfsFeed.TryLoadingStopsTypes();
		}

		internal Result<RunResults> UserRunGA(string descriptor)
		{
			return gaOptimizationService.RunGeneticAlgorithm(descriptor, false);
		}

		internal Result<RunResults> UserRunGAWithLogging(string descriptor, CancellationToken token)
		{
			return gaOptimizationService.RunGeneticAlgorithmWithLogging(descriptor, token, false);
		}

		internal void SetBestGenome(List<ga.Genome> bestGeneratedGenomes)
		{
			var bestGenome = bestGeneratedGenomes.LastOrDefault();
		}

		internal Result<db_ga.Genome> GetGenomeStructure(Guid genomeid)
		{
			db_ga.Genome Genome = new();
			using var db = new DatabaseContext();

			var genome = db.Genomes?.FirstOrDefault(g => g.Id.Equals(genomeid));
			if (genome == null)
				return Result<db_ga.Genome>.Failure("No Genome found with id: " + genomeid.ToString());

			var genomeRoutes = db.GenomesRoutes?.Where(g => g.GenomeId.Equals(genomeid)).ToList();
			if (genomeRoutes == null)
				return Result<db_ga.Genome>.Failure("No routes found for the genome");

			foreach (var route in genomeRoutes)
			{
				var onRoute = db.RouteStops?.Where(r => r.GenomeRouteId.Equals(route.Id) && r.Direction == ERouteDirection.OnRoute).OrderBy(r => r.StopSequence).ToList();
				if (onRoute.Count == 0)
					return Result<db_ga.Genome>.Failure("No onRoute found for GenomeRoute with Id: " + route.Id);
				route.OnRoute = onRoute;

				if (!route.OneWay)
				{
					var backRoute = db.RouteStops?.Where(r => r.GenomeRouteId.Equals(route.Id) && r.Direction == ERouteDirection.BackRoute).OrderBy(r => r.StopSequence).ToList();
					if (backRoute.Count == 0)
						return Result<db_ga.Genome>.Failure("No backRoute found for GenomeRoute with Id: " + route.Id);
					route.BackRoute = backRoute;
				}

				Genome.MutableRoutes.Add(route);
			}

			return Result<db_ga.Genome>.Success(Genome);
		}

		public void LoadSavedGenomes()
		{
			using var db = new DatabaseContext();
			var genomes = db.Genomes?.Where(g => g.WorkflowId == Id).ToList();
			if (genomes == null || genomes.Count == 0)
				return;


			foreach (var genome in genomes) {
				var genomeResult = GetGenomeStructure(genome.Id);
				if (genomeResult.IsFailure)
					throw new Exception(genomeResult.Error);

				Genomes.Add(genomeResult.Value);
			}
		}

		public async void SaveGenome(ga.Genome genome)
		{
			using var db = new DatabaseContext();
			var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				db_ga.Genome g = new(genome, Id);
				db.Genomes?.Add(g);
				await db.SaveChangesAsync();

				foreach (var route in genome.MutableRoutes)
				{
					db_ga.GenomeRoute gr = new(route, g.Id);
					db.GenomesRoutes?.Add(gr);
					await db.SaveChangesAsync();

					for (int i = 0; i < route.OnRoute.Count; i++)
					{
						RouteStop rs = new(gr.Id, route.OnRoute[i], ERouteDirection.OnRoute, i + 1);
						db.RouteStops?.Add(rs);
						await db.SaveChangesAsync();
					}

					if (!route.OneWay)
					{
						for (int i = 0; i < route.BackRoute.Count; i++)
						{
							RouteStop rs = new(gr.Id, route.OnRoute[i], ERouteDirection.BackRoute, i + 1);
							db.RouteStops?.Add(rs);
							await db.SaveChangesAsync();
						}
					}
				}

			}catch(Exception ex) {
				await transaction.RollbackAsync();
				throw new Exception($"Saving genome into database failed because: "+ex.Message);
			}
			finally { 
				await transaction.DisposeAsync();
				await db.DisposeAsync();
			}
			

		}

	}
}
