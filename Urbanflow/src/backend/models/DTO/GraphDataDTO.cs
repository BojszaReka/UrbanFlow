using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.DTO
{
	public class GraphDataDTO
	{
		public List<EdgeDataDTO> EdgesData {  get; set; }
		public List<NodeDataDTO> NodeData { get; set; }
		public string GraphName { get; set; }

		public GraphDataDTO() { }

		public GraphDataDTO(List<EdgeDataDTO> edgesData, List<NodeDataDTO> nodeData, string graphName = "") { 
			EdgesData = edgesData;
			NodeData = nodeData;
			GraphName = graphName;
		}
	}
}
