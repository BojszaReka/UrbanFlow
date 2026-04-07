using ClosedXML.Excel;
using System.IO;
using System.Net;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.util;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.backend.test_automater
{
	public class Main
	{
		private int TestIterations = 10;

		private readonly string CityName = "Veszprém";
		private readonly string WorkflowName = $"{DateTime.Now} Genetic Algorithm test";
		private readonly string WorkflowDescription = "Automatically generated workflow by test_automater to test Genetic Algorithm";

		private readonly string SaveFolder = "C:/ADATOK/Tanulmányok/Diplomamunka/RunResults/";

		private readonly OptimizationSettings settings = new()
		{
			PopulationSize = 100,
			IterationNumber = 150,
			UserOptimizationParameters = new OptimizationParameters
			{
				Genome_RouteCount = 30,
				Genome_HubNumberInRoute = 1,
				Genome_AllowOneWayRoutes = true,

				Fitness_RedundancyPercentParameter = 50,
				Fitness_RouteLengthParameter = 18,
				Fitness_MaximalAllowedChangeParameter = 2,
				Fitness_FleetCapacityParameter = 100,
				Fitness_MaximumWaitingMinutesParameter = 30,
				Fitness_MinimalWaitingMinutesParameter = 5,
				Fitness_MaximumTravelTimeParameter = 120
			}
		};

		private Workflow workflow;
		public List<(string, int, RunResults)> NetworkRunResults = [];
		public List<(string, int, RunResults)> NewWayRunResults = [];
		public List<(string, int, RunResults)> OldWayRunResults = [];


		public async Task RunGeneticAlgorithm() {
			await SetupWorkFlow();
			CheckOriginalNetwork();

			Console.WriteLine($"\n\n--------------------------------------------------------------\n" +
							  $"   Starting tests, running {TestIterations} iterations...\n" +
							  $"--------------------------------------------------------------\n\n");
			try
			{
				int i = 1;
				//var tasks = new List<Task>();
				while (TestIterations > 0)
				{
					//tasks.Add(RunIterationTask(i));

					string descriptor = $"{i} iteration test run";
					Console.WriteLine($"\n\n == Running: {descriptor}... == \n\n");
					var result = RunGA(descriptor);
					if (result.IsFailure) throw new Exception($"Genetic algorith failed becasue: {result.Error}");
					NewWayRunResults.Add((descriptor, i, result.Value[0]));
					OldWayRunResults.Add((descriptor, i, result.Value[1]));
					TestIterations =  0;
					Console.WriteLine($"\n\n == {descriptor} FINISHED! == \n\n");

					i += 1;
				}
				//await Task.WhenAll(tasks);
			}
			catch (Exception e) {
				Console.WriteLine($"\n\n ==== ERROR ====\n > Test iteration ran into error: {e.Message}");
				throw new Exception($"ERROR: {e.Message}");
			}
			
			Console.WriteLine("\n\n ===== Running tests finished ===== ");

			//ExportBestGenomeRunResultsToExcel(SaveFolder, WorkflowName.Replace(':', '-'));
			ExportRunResultsToExcel(SaveFolder, WorkflowName.Replace(':', '-'));
			Console.WriteLine();
		}

		public async Task RunIterationTask(int i)
		{
			string descriptor = $"{i} iteration test run";
			Console.WriteLine($"\n\n == Running: {descriptor}... == \n\n");
			var result = RunGA(descriptor);
			if (result.IsFailure) throw new Exception($"Genetic algorith failed becasue: {result.Error}");
			NewWayRunResults.Add((descriptor, i, result.Value[0]));
			OldWayRunResults.Add((descriptor, i, result.Value[1]));
			TestIterations--;
			Console.WriteLine($"\n\n == {descriptor} FINISHED! == \n\n");
		}

		private async Task SetupWorkFlow()
		{
			Console.WriteLine("\n\n------------------------\n" +
							  "  Setting up Workflow...\n" +
							  "------------------------\n\n");
			try
			{
				await MenuManagerService.AddNewWorkflow(WorkflowName, CityName, WorkflowDescription);
				var tempWorkflowResult = MenuManagerService.GetWorkflowByName(WorkflowName);
				if(tempWorkflowResult.IsFailure)
					throw new Exception(tempWorkflowResult.Error);
				workflow = new Workflow(tempWorkflowResult.Value.Id);
				await workflow.SetStopTypes();
				workflow.SetNetworkinformationFromInnerGtfsFeed();
				workflow.CreateGAOptimizationService(settings);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Workflow setup ran into error: {e.Message}");
				throw new Exception($"ERROR: {e.Message}");
			}
			Console.WriteLine(" ===== Workflow setup done! ===== ");
		}

		private Result<List<RunResults>> RunGA(string descriptor)
		{
			return workflow.RunGA(descriptor);
		}

		public void ExportRunResultsToExcel(string folderPath, string fileName)
		{
			// Ensure the directory exists
			if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

			string fullPath = Path.Combine(folderPath, fileName.EndsWith(".xlsx") ? fileName : fileName + ".xlsx");

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Fitness Results");

			// 1. Create Headers
			worksheet.Cell(1, 1).Value = "Iteration";
			worksheet.Cell(1, 2).Value = "Generation Number";
			worksheet.Cell(1, 3).Value = "Best Fitness Value";
			worksheet.Cell(1, 4).Value = "Avarage Fitness Value";
			worksheet.Cell(1, 5).Value = "Worst Fitness Value";
			worksheet.Cell(1, 6).Value = "Type";

			// Apply some basic styling to headers
			var headerRow = worksheet.Row(1);
			headerRow.Style.Font.Bold = true;
			headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

			int currentRow = 2;

			// 2. Helper local function to process the lists
			void ProcessList(List<(string name, int iteration, RunResults results)> list, string typeLabel)
			{
				foreach (var (name, iteration, results) in list)
				{
					foreach (var (genNum, (best, avg, worst)) in results.FitnessValuesPerGenerations)
					{
						worksheet.Cell(currentRow, 1).Value = iteration;
						worksheet.Cell(currentRow, 2).Value = genNum;
						worksheet.Cell(currentRow, 3).Value = best;
						worksheet.Cell(currentRow, 4).Value = avg;
						worksheet.Cell(currentRow, 5).Value = worst;
						worksheet.Cell(currentRow, 6).Value = typeLabel;
						currentRow++;
					}
				}
			}

			// 3. Populate Data
			ProcessList(NetworkRunResults, "network");
			ProcessList(OldWayRunResults, "old");
			ProcessList(NewWayRunResults, "new");

			// 4. Final touches: Auto-fit columns and save
			worksheet.Columns().AdjustToContents();
			workbook.SaveAs(fullPath);
		}

		public void ExportBestGenomeRunResultsToExcel(string folderPath, string fileName)
		{
			// Ensure the directory exists
			if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

			string fullPath = Path.Combine(folderPath, fileName.EndsWith(".xlsx") ? fileName : fileName + ".xlsx");

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Fitness Results");

			// 1. Create Headers
			worksheet.Cell(1, 1).Value = "Iteration";
			worksheet.Cell(1, 2).Value = "Generation Number";
			worksheet.Cell(1, 3).Value = "Genome Number";
			worksheet.Cell(1, 4).Value = "Fitness Value";
			worksheet.Cell(1, 5).Value = "Type";

			// Apply some basic styling to headers
			var headerRow = worksheet.Row(1);
			headerRow.Style.Font.Bold = true;
			headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

			int currentRow = 2;

			// 2. Helper local function to process the lists
			void ProcessList(List<(string name, int iteration, RunResults results)> list, string typeLabel)
			{
				foreach (var (name, iteration, results) in list)
				{
					foreach (var genome in results.BestGeneratedGenomes)
					{
						worksheet.Cell(currentRow, 1).Value = iteration;
						worksheet.Cell(currentRow, 2).Value = genome.GenerationID;
						worksheet.Cell(currentRow, 3).Value = genome.GenomeID;
						worksheet.Cell(currentRow, 4).Value = genome.FitnessValue;
						worksheet.Cell(currentRow, 5).Value = typeLabel;
						currentRow++;
					}
				}
			}

			// 3. Populate Data
			ProcessList(NetworkRunResults, "network");
			ProcessList(OldWayRunResults, "old");
			ProcessList(NewWayRunResults, "new");

			// 4. Final touches: Auto-fit columns and save
			worksheet.Columns().AdjustToContents();
			workbook.SaveAs(fullPath);
		}

		private void CheckOriginalNetwork()
		{
			Genome g = workflow.GetGenomeForNetwork(settings.UserOptimizationParameters);
			
			RunResults runRes = new([g], [(g.GenerationID, (g.FitnessValue, g.FitnessValue, g.FitnessValue))], "Network fitness value");
			NetworkRunResults.Add(("Network run result",0,runRes));
		}
	}
}
