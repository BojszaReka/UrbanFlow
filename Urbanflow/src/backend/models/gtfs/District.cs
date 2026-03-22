using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Districts")]
	public class District
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public Guid GtfsFeedId { get; set; }
		public bool IsCollectorDistrict { get; set; } = false;

		//helperlist
		public List<Stop> Stops { get; set; } = [];

		[ForeignKey("GtfsFeedId")]
		public GtfsFeed GtfsFeed { get; set; }

		public District() { }

		public District(string name, Guid feedId, bool collectorDistrict = false)
		{
			Name = name;
			GtfsFeedId = feedId;
			IsCollectorDistrict = collectorDistrict;
			using var context = new DatabaseContext();
			context.Districts?.Add(this);
			context.SaveChanges();
		}

		public District(string name, Guid feedId, bool collectorDistrict = false, bool wihtoutdb = true)
		{
			Name = name;
			GtfsFeedId = feedId;
			IsCollectorDistrict = collectorDistrict;
		}

		public District(string name, Guid feedId, in DatabaseContext db, bool collectorDistrict = false)
		{
			Name = name;
			GtfsFeedId = feedId;
			IsCollectorDistrict = collectorDistrict;
			db.Districts?.Add(this);
			db.SaveChanges();
		}

		public District(Guid id)
		{
			using var context = new DatabaseContext();
			var tempDistrict = context.Districts?.Where(x => x.Id == id).FirstOrDefault();
			if (tempDistrict != null) { 
				Id = tempDistrict.Id;
				Name = tempDistrict.Name;
				GtfsFeedId = tempDistrict.GtfsFeedId;
			}
			throw new Exception($"District (Guid: {id}) not found in the database");
		}

	}
}
