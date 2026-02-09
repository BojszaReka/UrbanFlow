using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Urbanflow.src.backend.models.graph
{
	[Table("GraphNodes")]
	public class GraphNode
	{
		public Guid GraphId { get; set; } = Guid.Empty;
		public Guid NodeId { get; set; } = Guid.Empty;

		public GraphNode() { }
		public GraphNode(Guid graphid, Guid nodeid)
		{
			GraphId = graphid;
			NodeId = nodeid;
		}

		
	}
}
