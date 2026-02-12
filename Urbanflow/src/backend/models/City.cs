using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Urbanflow.src.backend.models
{
	[Table("Cities")]
	public class City
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid GtfsFeedId { get; set; } = Guid.Empty;
		public string Name { get; set; } 
		public string Description { get; set; } 
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public List<Workflow> Workflows { get; set; } 

		public City() { }

		public City(string name, string description, Guid feedId)
		{
			Name = name;
			Description = description;
			GtfsFeedId= feedId;
		}

		public override string ToString()
		{
			return $"City: {Name} ({Id})\n"
				 + $"Description: {Description}\n"
				 + $"Created At: {CreatedAt}\n"
				 + $"Last Updated At: {LastUpdatedAt}\n";
		}
	}
}
