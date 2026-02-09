using System.ComponentModel.DataAnnotations.Schema;

namespace Urbanflow.src.backend.models
{
	[Table("Workflows")]
	public class Workflow
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; }
		public Guid CityId { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string Description { get; set; }
		public string GtfsVersion { get; set; }
		public DateTime LastModified { get; set; } = DateTime.UtcNow;
		public bool IsActive { get; set; } = true;

		[ForeignKey("CityId")]
		public City City { get; set; }

		public Workflow() { }

		public Workflow(string name, Guid cityId, string description, string defaultGtfsVersion)
		{
			Name = name;
			CityId = cityId;
			Description = description;
			GtfsVersion = defaultGtfsVersion;
		}
	}
}
