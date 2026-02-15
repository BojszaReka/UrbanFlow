using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.enums;

namespace Urbanflow.src.backend.models.graph
{
	[Table("Graphs")]
	public class Graph
	{
		public Guid Id { get; internal set; } = Guid.NewGuid();
		public Guid WorkflowId { get; internal set; } = Guid.Empty;
		public Guid RouteId { get; internal set; } = Guid.Empty;
		public string Name { get; internal set; } = string.Empty;
		public EGraphType Type { get; internal set; } = EGraphType.Default;

		[NotMapped]
		public List<Node> Nodes { get; internal set; } = [];
		[NotMapped]
		public List<Edge> Edges { get; internal set; } = [];

		// Constructors
		public Graph()
		{

		}

		public Graph(Guid workflowId, string name, EGraphType type, string routeId = "")
		{
			WorkflowId = workflowId;
			Name = name;
			Type = type;

			if(routeId != "")
			{
				RouteId = Guid.Parse(routeId);
			}
			else
			{
				RouteId = Guid.Empty;
			}
			
		}

		public Graph(Guid id)
		{
			Id = id;
			using var db = new DatabaseContext();
			var graph = db.Graphs?.Find(id);
			if (graph is not null)
			{
				WorkflowId = graph.WorkflowId;
				RouteId = graph.RouteId;
				Name = graph.Name;
				Type = graph.Type;
				LoadNodesAndEdgesFromDatabase();
			}
			else
			{
				throw new Exception("Graph not found in database.");
			}
		}

		// Database interactions
		public void LoadNodesAndEdgesFromDatabase()
		{
			using var db = new DatabaseContext();
			List<GraphNode> GraphNodes = [.. db.GraphNodes?.Where(gn => gn.GraphId == Id)];
			List<GraphEdge> GraphEdges = [.. db.GraphEdges?.Where(ge => ge.GraphId == Id)];
			if (GraphEdges is not null)
			{
				foreach (GraphEdge ge in GraphEdges)
				{
					if (ge is not null && ge.EdgeId != Guid.Empty)
					{
						Edges.Add(new Edge(ge.EdgeId));
					}
					else
					{
						continue;
					}
				}
			}
			if(GraphNodes is not null)
			{
				foreach (GraphNode gn in GraphNodes)
				{
					if (gn is not null && gn.NodeId != Guid.Empty)
					{
						Nodes.Add(new Node(gn.NodeId));
					}
					else
					{
						continue;
					}
				}
			}
		}

		public void SaveGraph()
		{
			using var db = new DatabaseContext();
			var graph = db.Graphs.Where(g => g.Id == Id).FirstOrDefault();
			if (graph is null) {
				db.Graphs.Add(this);
			}
			else
			{
				db.Graphs.Update(this);
			}
			db.SaveChanges();	
		}

		public void AddNode(Node node)
		{
			if(node is null || node.Id == Guid.Empty)
			{
				throw new ArgumentException("Node cannot be null or have an empty Id.");
			}
			if(Nodes.Any(n => n.Id == node.Id))
			{
				throw new InvalidOperationException("Node with the same Id already exists in the graph.");
			}
			Nodes.Add(node);
			using var db = new DatabaseContext();
			db.GraphNodes?.Add(new GraphNode(Id, node.Id));
			db.SaveChanges();
		}

		public void RemoveNode(Node node) {
			if (node is null || node.Id == Guid.Empty)
			{
				throw new ArgumentException("Node cannot be null or have an empty Id.");
			}
			if (!Nodes.Any(n => n.Id == node.Id))
			{
				throw new InvalidOperationException("Node does not exist in the graph.");
			}
			Nodes.RemoveAll(n => n.Id == node.Id);
			using var db = new DatabaseContext();
			var graphNodesToRemove = db.GraphNodes?.Where(gn => gn.GraphId == Id && gn.NodeId == node.Id).ToList();
			if (graphNodesToRemove is not null)
			{
				db.GraphNodes?.RemoveRange(graphNodesToRemove);
				db.SaveChanges();
			}
		}

		public void AddEdge(Edge edge)
		{
			if (edge is null || edge.Id == Guid.Empty)
			{
				throw new ArgumentException("Edge cannot be null or have an empty Id.");
			}
			if (Edges.Any(e => e.Id == edge.Id))
			{
				return;
				throw new InvalidOperationException("Edge with the same Id already exists in the graph.");
			}
			Edges.Add(edge);
			using var db = new DatabaseContext();
			db.GraphEdges?.Add(new GraphEdge(Id, edge.Id));
			db.SaveChanges();
		}

		public void RemoveEdge(Edge edge)
		{
			if (edge is null || edge.Id == Guid.Empty)
			{
				throw new ArgumentException("Edge cannot be null or have an empty Id.");
			}
			if (!Edges.Any(e => e.Id == edge.Id))
			{
				throw new InvalidOperationException("Edge does not exist in the graph.");
			}
			Edges.RemoveAll(e => e.Id == edge.Id);
			using var db = new DatabaseContext();
			var graphEdgesToRemove = db.GraphEdges?.Where(ge => ge.GraphId == Id && ge.EdgeId == edge.Id).ToList();
			if (graphEdgesToRemove is not null)
			{
				db.GraphEdges?.RemoveRange(graphEdgesToRemove);
				db.SaveChanges();
			}
		}


		// Other methods
		public override string ToString()
		{
			return $"Graph: {Name} (WorkflowId: {WorkflowId}, Type: {Type}, Nodes: {Nodes.Count}, Edges: {Edges.Count})";
		}

		internal Node GetNodeByStopId(Guid StopId)
		{
			return Nodes.Where(n => n.StopId == StopId).FirstOrDefault();
		}
	}
}
