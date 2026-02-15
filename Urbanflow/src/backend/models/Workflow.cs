using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.models.gtfs;

namespace Urbanflow.src.backend.models
{
	[Table("Workflows")]
	public class Workflow
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid GtfsFeedId { get; set; } = Guid.Empty;
		public string Name { get; set; }
		public Guid CityId { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string Description { get; set; }
		public DateTime LastModified { get; set; } = DateTime.UtcNow;
		public bool IsActive { get; set; } = true;

		[ForeignKey("CityId")]
		public City City { get; set; }

		[ForeignKey("GtfsFeedId")]
		public GtfsFeed GtfsFeed { get; internal set; }

		public Workflow() { }

		public Workflow(string name, Guid cityId, string description, Guid feedid)
		{
			Name = name;
			CityId = cityId;
			Description = description;
			GtfsFeedId = feedid;
		}
	}
}
