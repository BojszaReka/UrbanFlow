using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("FeedInfo")]
	public class FeedInfo
	{
		[Key]
		public Guid Id { get; internal set; }
	}
}
