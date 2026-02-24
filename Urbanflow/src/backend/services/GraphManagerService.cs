using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.enums;
using Urbanflow.src.backend.models.graph;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.services
{
	public class GraphManagerService
	{

		public static Result<HashSet<Graph>> LoadGraphs(Guid workflowId)
		{
			try
			{
				using var db = new DatabaseContext();

				var graphs = db.Graphs
					.Where(g => g.WorkflowId == workflowId)
					.ToHashSet();
				if (graphs == null)
					return Result<HashSet<Graph>>.Failure("No graphs found for workflow");

				return Result<HashSet<Graph>>.Success(graphs);
			}
			catch (Exception ex)
			{
				return Result<HashSet<Graph>>.Failure(ex.Message);
			}
		}


		//Intialization
		public static Result<HashSet<Graph>> CreateAllGraphsForFeed(Guid workflowId, in GraphDataDTO networkGraphData, in Dictionary<Guid, GraphDataDTO> routeGraphData)
		{
			var graphs = new HashSet<Graph>();

			var result = GenerateFullNetworkGraph(networkGraphData, workflowId);

			if (result.IsFailure)
				return Result<HashSet<Graph>>.Failure(result.Error);

			graphs.Add(result.Value);

			foreach (var rgd in routeGraphData)
			{
				result = GenerateGraphForRoute(workflowId, rgd.Key, rgd.Value);

				if (result.IsFailure)
					return Result<HashSet<Graph>>.Failure(result.Error);

				graphs.Add(result.Value);
			}

			return Result<HashSet<Graph>>.Success(graphs);
		}

		public static Result<HashSet<Graph>> SafeGraphGeneration(Guid workflowId, in HashSet<Graph> existingGraphs, in GraphDataDTO networkGraphData, in Dictionary<Guid, GraphDataDTO> routeGraphData)
		{
			var graphs = new HashSet<Graph>(existingGraphs);

			var networkGraph = existingGraphs.FirstOrDefault(g => g.Type == EGraphType.Network);

			if (networkGraph == null)
			{
				var result = GenerateFullNetworkGraph(networkGraphData, workflowId);

				if (result.IsFailure)
					return Result<HashSet<Graph>>.Failure(result.Error);

				graphs.Add(result.Value);
			}

			foreach (var rgd in routeGraphData)
			{
				var routeGraph = existingGraphs
					.FirstOrDefault(g =>
						g.Type == EGraphType.Route &&
						g.RouteId == rgd.Key);

				if (routeGraph != null)
					continue;

				var result = GenerateGraphForRoute(workflowId, rgd.Key, rgd.Value);

				if (result.IsFailure)
					return Result<HashSet<Graph>>.Failure(result.Error);

				graphs.Add(result.Value);
			}

			return Result<HashSet<Graph>>.Success(graphs);
		}

		// Getting graph data
		public static Result<Graph> GetNetWorkGraphData(in HashSet<Graph> graphs)
		{
			var graph = graphs.Where(g => g.Type == EGraphType.Network).FirstOrDefault();
			if (graph == null)
				throw new Exception("No network graph for this workflow has been generated");
			return graph;
		}

		// GTFS interactions
		private static Result<Graph> GenerateFullNetworkGraph(in GraphDataDTO networkGraphData, Guid workflowId)
		{
			return CreateGraph(workflowId, networkGraphData, "Network graph", EGraphType.Network);
		}

		private static Result<Graph> GenerateGraphForRoute(Guid workflowId, Guid routeId, in GraphDataDTO routeGraphData)
		{
			return CreateGraph(workflowId, routeGraphData, routeGraphData.GraphName, EGraphType.Route, routeId.ToString());
		}

		// Graph methods
		private static Result<Graph> CreateGraph(Guid workflowId, in GraphDataDTO graphData, string name, EGraphType type, string routeId = "")
		{
			try
			{
				Graph newGraph = type == EGraphType.Route
					? new Graph(workflowId, name, type, routeId)
					: new Graph(workflowId, name, type);

				// Add nodes
				foreach (var nodeData in graphData.NodeData)
				{
					var node = new Node(nodeData.Stop);
					newGraph.AddNode(node);
				}

				// Add edges
				foreach (var edgeData in graphData.EdgesData)
				{
					var fromNode = newGraph.GetNodeByStopId(edgeData.FromStopId);
					var toNode = newGraph.GetNodeByStopId(edgeData.ToStopId);

					if (fromNode == null || toNode == null)
						return Result<Graph>.Failure("Invalid edge reference: node not found.");

					var edge = new Edge(fromNode.Id, toNode.Id, edgeData.TravelTimeMinutes);
					newGraph.AddEdge(edge);
				}

				newGraph.SaveGraph();

				return Result<Graph>.Success(newGraph);
			}
			catch (Exception ex)
			{
				return Result<Graph>.Failure(ex.Message);
			}
		} 

	}
}
