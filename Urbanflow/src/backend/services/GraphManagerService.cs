using Urbanflow.src.backend.db;
using Urbanflow.src.backend.enums;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.graph;

namespace Urbanflow.src.backend.services
{
	public class GraphManagerService
	{
		public Guid WorkflowId { get; set; }
		public List<Graph> Graphs { get; set; } = [];


		public GraphManagerService() { }

		public GraphManagerService(Guid workflowId)
		{
			WorkflowId = workflowId;
			using var db = new DatabaseContext();
			Graphs = [.. db.Graphs?.Where(g => g.WorkflowId == workflowId)];
		}

		//Intialization
		public void CreateAllGraphsForFeed()
		{
			GtfsManagerService gtfsManager = new();
			List<Guid> routeIds = gtfsManager.GetRouteIds(WorkflowId);

			GenerateFullNetworkGraph();
			foreach (var routeId in routeIds) { 
				GenerateGraphForRoute(routeId);
			}
		}

		public void SafeGraphGeneration()
		{
			var networkgraph = Graphs.Where(g => g.Type == EGraphType.Network).FirstOrDefault();
			if(networkgraph == null)
			{
				GenerateFullNetworkGraph();
			}

			GtfsManagerService gtfsManager = new();
			List<Guid> routeIds = gtfsManager.GetRouteIds(WorkflowId);
			GenerateFullNetworkGraph();
			foreach (var routeId in routeIds)
			{
				var routeGraph = Graphs.Where(g => g.Type == EGraphType.Route || g.RouteId == routeId).FirstOrDefault();
				if(routeGraph == null)
					GenerateGraphForRoute(routeId);
			}
		}

		// GTFS interactions
		private void GenerateFullNetworkGraph()
		{
			GtfsManagerService gtfsManager = new();
			var graphData = gtfsManager.GetDataForNetworkGraph(WorkflowId);

			CreateGraph(graphData, "Network graph", EGraphType.Network);
		}

		private void GenerateGraphForRoute(Guid routeId)
		{
			GtfsManagerService gtfsManager = new();
			GraphDataDTO graphData = gtfsManager.GetDataForRouteGraph(WorkflowId, routeId);


			CreateGraph(graphData, graphData.GraphName, EGraphType.Route, routeId.ToString());
		}

		// Graph methods
		private void CreateGraph(GraphDataDTO graphData, string name, EGraphType type, string routeId = "")
		{
			Graph newGraph;
			if (EGraphType.Route == type)
			{
				newGraph = new Graph(WorkflowId, name, type, routeId);
			}
			else
			{
				newGraph = new Graph(WorkflowId, name, type);
			}
			foreach (var NodeDataDTO in graphData.NodeData)
			{
				var n = new Node(NodeDataDTO.Stop);
				newGraph.AddNode(n);
			}
			foreach (var EdgeDataDTO in graphData.EdgesData)
			{
				Node fromNode = newGraph.GetNodeByStopId(EdgeDataDTO.FromStopId);
				Node toNode = newGraph.GetNodeByStopId(EdgeDataDTO.ToStopId);
				var e = new Edge(fromNode.Id, toNode.Id, EdgeDataDTO.TravelTimeMinutes);
				newGraph.AddEdge(e);
			}
			newGraph.SaveGraph();
			Graphs.Add(newGraph);
		} 

	}
}
