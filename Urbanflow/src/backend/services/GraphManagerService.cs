using NetTopologySuite.GeometriesGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.services
{
	public class GraphManagerService
	{
		public List<Graph> Graphs { get; set; } = [];
		

		public GraphManagerService() { }

		public GraphManagerService(List<Node> nodes, List<Edge> edges)
		{

		}

		public GraphManagerService(Guid workflowId) {
			using var db = new DatabaseContext();
			Graphs = [.. db.Graphs?.Where(g => g.WorkflowId == workflowId)];
		}	

	}
}
