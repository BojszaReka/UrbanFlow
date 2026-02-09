using GTFS;
using GTFS.Fields;
using GTFS.IO;
using Urbanflow.src.backend.models.gtfs;

namespace Urbanflow.src.backend.services
{
	public class GtfsManagerService
	{
		protected GtfsManagerService()
		{
		}

		public static string UploadGtfsData(string gtfsPath)
		{
			//gets a gtfs data feed
			//parses the data up to the database
			//returns the GTFS version string
			GTFSFeed feed = ParseGtfsData(gtfsPath);
			GtfsFeed gtfsFeed = new(feed);

			return gtfsFeed.Version; //placeholder
		}

		private static GTFSFeed ParseGtfsData(string gtfsPath)
		{
			var reader = new GTFSReader<GTFSFeed>
			{
				LinePreprocessor = delegate (string s) { return s.Replace(", ", " ").Replace("\"\",3", "\"700\",\"700\"").Replace("\"", ""); }
			};
			var feed = null as GTFSFeed;
			try
			{
				feed = reader.Read(gtfsPath);
			}catch (GTFS.Exceptions.GTFSParseException ex)
			{
				throw new("Failed to parse GTFS data: " + ex.Message);
			}			
			return feed;
		}

		internal static List<Route> GetRoutesForWorkflow(Guid id)
		{
			throw new NotImplementedException();
		}
	}
}
