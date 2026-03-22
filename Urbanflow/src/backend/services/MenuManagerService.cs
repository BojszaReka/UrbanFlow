using System.Collections;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.frontend.pages;

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

		public static Result<List<string>> GetCityNames()
		{
			try
			{
				using var db = new DatabaseContext();
				if (db.Cities == null)
				{
					return Result<List<string>>.Success([]);
				}
				return Result<List<string>>.Success([.. db.Cities.Select(c => c.Name)]);
			}catch(Exception ex)
			{
				return Result<List<string>>.Failure(ex.Message);
			}			
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

		public static async Task<Result<Guid>> AddCity(string Name, string Description, string GtfsPath)
		{
			if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(GtfsPath))
			{
				return Result<Guid>.Failure("City name and GTFS path cannot be empty.");
			}

			var result = GetCityNames();
			if (result.IsFailure)
			{
				return Result<Guid>.Failure(result.Error);
			}
			List<string> citynames = result.Value;
			if (citynames.Contains(Name))
			{
				return Result<Guid>.Failure("City with the same name already exists.");
			}

			using var db = new DatabaseContext();
			var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				Guid feedId = GtfsManagerService.UploadGtfsData(GtfsPath, db);

				City city = new(Name, Description, feedId);
				db.Cities?.Add(city);
				db.SaveChanges();
				await transaction.CommitAsync();
				return city.Id;
				
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return Result<Guid>.Failure($"Adding new City failed: {ex.InnerException}");
			}
			finally
			{
				await transaction.DisposeAsync();
			}
		}

		public static void UpdateCity(in City city)
		{
			using var db = new DatabaseContext();
			Guid cityId = city.Id;
			var existingCity = db.Cities?.FirstOrDefault(c => c.Id == cityId);
			if (existingCity != null)
			{
				existingCity.Name = city.Name;
				existingCity.Description = city.Description;
				existingCity.GtfsFeedId = city.GtfsFeedId;
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

			Workflow workflow = new(Name, CityId, Description, city.GtfsFeedId);

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

		public static void UpdateWorkflow(in Workflow workflow)
		{
			using var db = new DatabaseContext();
			Guid workflowId = workflow.Id;
			var existingWorkflow = db.Workflows?.FirstOrDefault(w => w.Id == workflowId);
			if (existingWorkflow != null)
			{
				existingWorkflow.Name = workflow.Name;
				existingWorkflow.Description = workflow.Description;
				existingWorkflow.GtfsFeedId= workflow.GtfsFeedId;
				existingWorkflow.LastModified = DateTime.UtcNow;
				db.SaveChanges();
			}
			else
			{
				throw new Exception("Workflow not found.");
			}
		}

		public static async Task AddNewWorkflow(string workflowName, string cityName, string workflowDescription)
		{
			using var db = new DatabaseContext();
			var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				var existingCity = (db.Cities?.FirstOrDefault(c => c.Name == cityName)) ?? throw new Exception("City not found.");
				var newWorkflow = new Workflow(workflowName, existingCity, workflowDescription, existingCity.GtfsFeedId);
				db.Workflows?.Add(newWorkflow);
				db.SaveChanges();
				await transaction.CommitAsync();
			}
			catch(Exception ex)
			{
				await transaction.RollbackAsync();
				throw new Exception($"Adding new workflow failed: {ex.Message}");
			}
			finally
			{
				await transaction.DisposeAsync();
			}
			
		}

		public static List<Workflow>? GetWorkflowsByCityName(string cityName)
		{
			using var db = new DatabaseContext();
			var city = db.Cities?.FirstOrDefault(c => c.Name == cityName);
			return city == null ? throw new Exception("City not found.") : (db.Workflows?.Where(w => w.CityId == city.Id && w.IsActive).ToList());
		}

		public static void UpdateWorkflow(Guid workflowId, string workflowName, string workflowDescription)
		{
			using var db = new DatabaseContext();
			var existingWorkflow = db.Workflows?.FirstOrDefault(w => w.Id == workflowId);
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
