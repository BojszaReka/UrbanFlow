using NetTopologySuite.GeometriesGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.services
{
	public class GraphManagerService
	{
		public List<Node> Nodes { get; set; } = [];
		public List<Edge> Edges { get; set; } = [];

		public GraphManagerService() { }
	}
}
