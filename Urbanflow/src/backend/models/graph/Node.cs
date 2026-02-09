using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.enums;
using Urbanflow.src.backend.models.gtfs;

namespace Urbanflow.src.backend.models.graph
{
	[Table("Nodes")]
	public class Node
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public ENodeType Type { get; set; } = ENodeType.Default;
		public Guid StopId { get; set; } = Guid.Empty;
		public double Latitude { get; set; }
		public double Longitude { get; set; } 

		public Node()
		{
			
		}

		public Node(Stop s)
		{
			Id = Guid.NewGuid();
			Name = s.Name;
			Type = s.NodeType;
			StopId = s.Id;
			Latitude = s.Latitude;
			Longitude = s.Longitude;
		}

		public Node(Guid id)
		{
			Id = id;
			DatabaseContext context = new();
			var node = context.Nodes?.Find(id);
			if (node is not null)
			{
				Name = node.Name;
				Type = node.Type;
				StopId = node.StopId;
				Latitude = node.Latitude;
				Longitude = node.Longitude;
			}
		}

		public void UpdateFromStop(Stop s)
		{
			Name = s.Name;
			Type = s.NodeType;
			StopId = s.Id;
			Latitude = s.Latitude;
			Longitude = s.Longitude;
		}

		public override string ToString()
		{
			return $"Node: {Name} ({Id})\n"
				 + $"Type: {Type}\n"
				 + $"Stop ID: {StopId}\n"
				 + $"Latitude: {Latitude}\n"
				 + $"Longitude: {Longitude}\n";
		}

		public void SaveToDatabase()
		{
			DatabaseContext context = new();
			var existingNode = context.Nodes?.Find(Id);
			if (existingNode is null)
			{
				context.Nodes?.Add(this);
			}
			else
			{
				existingNode.Name = Name;
				existingNode.Type = Type;
				existingNode.StopId = StopId;
				existingNode.Latitude = Latitude;
				existingNode.Longitude = Longitude;
				context.Nodes?.Update(existingNode);
			}
			context.SaveChanges();
		}


	}
}
