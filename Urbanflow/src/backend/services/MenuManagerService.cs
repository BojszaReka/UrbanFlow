using System.Collections;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.gtfs;

namespace Urbanflow.src.backend.services
{
	public class MenuManagerService
	{
		// *** City Management ***
		public static List<City> GetCities()
		{
			using var db = new DatabaseContext();
			return [.. db.Cities];
		}

		public static List<string> GetCityNames()
		{
			using var db = new DatabaseContext();
			if (db.Cities == null)
			{
				return [];
			}
			return [.. db.Cities.Select(c => c.Name)];
		}

		public static City GetCityByName(string name)
		{
			using var db = new DatabaseContext();
			return db.Cities?.FirstOrDefault(c => c.Name == name);
		}

		public static City GetCityById(Guid id)
		{
			using var db = new DatabaseContext();
			return db.Cities?.FirstOrDefault(c => c.Id == id);
		}

		public static Guid AddCity(string Name, string Description, string GtfsPath)
		{
			if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(GtfsPath))
			{
				throw new Exception("City name and GTFS path cannot be empty.");
			}

			List<string> citynames = GetCityNames();
			if (citynames.Contains(Name))
			{
				throw new Exception("City with the same name already exists.");
			}

			string gtfsVersion = GtfsManagerService.UploadGtfsData(GtfsPath);

			City city = new(Name, Description, gtfsVersion);

			using (var db = new DatabaseContext())
			{
				db.Cities?.Add(city);
				db.SaveChanges();
			}

			return city.Id;
		}

		public static void UpdateCity(City city)
		{
			using var db = new DatabaseContext();
			var existingCity = db.Cities?.FirstOrDefault(c => c.Id == city.Id);
			if (existingCity != null)
			{
				existingCity.Name = city.Name;
				existingCity.Description = city.Description;
				existingCity.DefaultGtfsVersion = city.DefaultGtfsVersion;
				existingCity.LastUpdatedAt = DateTime.UtcNow;
				db.SaveChanges();
			}
			else
			{
				throw new Exception("City not found.");
			}
		}
		// *** End City Management ***


		// *** Workflow Management ***
		public static List<Workflow> GetWorkflows()
		{
			using var db = new DatabaseContext();
			return [.. db.Workflows?.Where(w => w.IsActive)];
		}

		public static List<string> GetWorkflowNames()
		{
			using var db = new DatabaseContext();
			return [.. db.Workflows?.Where(w => w.IsActive).Select(w => w.Name)];
		}

		public static Workflow GetWorkflowByName(string name)
		{
			using var db = new DatabaseContext();
			return db.Workflows?.FirstOrDefault(w => w.Name == name && w.IsActive);
		}

		public static Workflow GetWorkflowById(Guid id)
		{
			using var db = new DatabaseContext();
			return db.Workflows?.FirstOrDefault(w => w.Id == id && w.IsActive);
		}

		public static Guid AddWorkflow(string Name, Guid CityId, string Description)
		{
			if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Description))
			{
				throw new Exception("Workflow name and description cannot be empty.");
			}

			List<string> workflownames = GetWorkflowNames();
			if (workflownames.Contains(Name))
			{
				throw new Exception("Workflow with the same name already exists.");
			}

			City city = GetCityById(CityId);

			Workflow workflow = new(Name, CityId, Description, city.DefaultGtfsVersion);

			using (var db = new DatabaseContext())
			{
				db.Workflows?.Add(workflow);
				db.SaveChanges();
			}

			return workflow.Id;
		}

		public static void DeleteWorkflow(Guid workflowId)
		{
			using var db = new DatabaseContext();
			var workflow = db.Workflows?.FirstOrDefault(w => w.Id == workflowId);
			workflow?.IsActive = false;
			if (workflow != null)
			{
				db.Workflows?.Update(workflow);
				db.SaveChanges();
			}
			else
			{
				throw new Exception("Workflow not found.");
			}
		}

		public static void UpdateWorkflow(Workflow workflow)
		{
			using var db = new DatabaseContext();
			var existingWorkflow = db.Workflows?.FirstOrDefault(w => w.Id == workflow.Id);
			if (existingWorkflow != null)
			{
				existingWorkflow.Name = workflow.Name;
				existingWorkflow.Description = workflow.Description;
				existingWorkflow.GtfsVersion = workflow.GtfsVersion;
				existingWorkflow.LastModified = DateTime.UtcNow;
				db.SaveChanges();
			}
			else
			{
				throw new Exception("Workflow not found.");
			}
		}

		public static void AddNewWorkflow(string workflowName, string cityName, string workflowDescription)
		{
			using var db = new DatabaseContext();
			var existingCity = (db.Cities?.FirstOrDefault(c => c.Name == cityName)) ?? throw new Exception("City not found.");
			var newWorkflow = new Workflow(workflowName, existingCity.Id, workflowDescription, existingCity.DefaultGtfsVersion);
			db.Workflows?.Add(newWorkflow);
			db.SaveChanges();
		}

		public static List<Workflow>? GetWorkflowsByCityName(string cityName)
		{
			using var db = new DatabaseContext();
			var city = db.Cities?.FirstOrDefault(c => c.Name == cityName);
			return city == null ? throw new Exception("City not found.") : (db.Workflows?.Where(w => w.CityId == city.Id && w.IsActive).ToList());
		}

		public static void UpdateWorkflow(Workflow workflow, string workflowName, string workflowDescription)
		{
			using var db = new DatabaseContext();
			var existingWorkflow = db.Workflows?.FirstOrDefault(w => w.Id == workflow.Id);
			if (existingWorkflow != null)
			{
				existingWorkflow.Name = workflowName;
				existingWorkflow.Description = workflowDescription;
				existingWorkflow.LastModified = DateTime.UtcNow;
				db.SaveChanges();
			}
			else
			{
				throw new Exception("Workflow not found.");
			}
		}
		// *** End Workflow Management ***
	}
}
