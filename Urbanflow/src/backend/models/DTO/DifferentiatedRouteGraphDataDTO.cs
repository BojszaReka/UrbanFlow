using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.DTO
{
	public class DifferentiatedRouteGraphDataDTO
	{
		public List<List<EdgeDataDTO>> EdgesDataGrouppedByRoutes { get; set; }
		public List<NodeDataDTO> NodeData { get; set; }
		public string GraphName { get; set; }
		public List<(byte r, byte g, byte b)> Colors { get; set; }

		public DifferentiatedRouteGraphDataDTO() { }

		public DifferentiatedRouteGraphDataDTO(in List<List<EdgeDataDTO>> edgesData, in List<NodeDataDTO> nodeData, in List<(byte r, byte g, byte b)> colors, string graphName = "")
		{
			EdgesDataGrouppedByRoutes = edgesData;
			NodeData = nodeData;
			GraphName = graphName;
			Colors = colors;
		}
	}
}
