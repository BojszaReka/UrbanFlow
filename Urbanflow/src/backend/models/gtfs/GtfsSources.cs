using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("GtfsSources")]
	public class GtfsSources
	{
		[Key]
		public string Id { get; set; }
		public string CityName { get; set; }
		public string SourceUrl { get; set; }
		public string GtfsName { get; set; }
		public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
		public string Version { get; set; }
		public string Description { get; set; }

		public GtfsSources(string cityname, string srcurl, string ver, string desc)
		{
			Id = cityname + "_v" + ver;
			CityName = cityname;
			SourceUrl = srcurl;
			Version = ver;
			Description = desc;
			GtfsName = SourceUrl.Split('/').Last();
		}
	}
}
