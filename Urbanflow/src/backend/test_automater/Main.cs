using Urbanflow.src.backend.models;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.services;

namespace Urbanflow.src.backend.test_automater
{
	public class Main
	{
		private int TestIterations = 1;

		private readonly string CityName = "Veszprém";
		private readonly string WorkflowName = $"{DateTime.Now} Genetic Algorithm test";
		private readonly string WorkflowDescription = "Automatically generated workflow by test_automater to test Genetic Algorithm";

		private readonly OptimizationSettings settings = new OptimizationSettings
		{
			PopulationSize = 100,
			IterationNumber = 100,
			UserOptimizationParameters = new OptimizationParameters
			{
				Genome_RouteCount = 20,
				Genome_HubNumberInRoute = 2,
				Genome_AllowOneWayRoutes = true,

				Fitness_RedundancyPercentParameter = 50,
				Fitness_RouteLengthParameter = 30,
				Fitness_MaximalAllowedChangeParameter = 1,
				Fitness_FleetCapacityParameter = 100,
				Fitness_PreferedWaitingMinutesParameter = 30
			}
		};

		private Workflow workflow;
		private GAStatistics statisticsCollector;

		public void RunGeneticAlgorithm() {
			SetupWorkFlow();

			Console.WriteLine($"\n\n--------------------------------------------------------------\n" +
							  $"   Starting tests, running {TestIterations} iterations...\n" +
							  $"--------------------------------------------------------------\n\n");
			try
			{
				int i = 1;
				while (TestIterations > 0)
				{
					string descriptor = $"{i} iteration test run";
					Console.WriteLine($"\n\n == Running: {descriptor}... == \n\n");
					RunGA(descriptor);
					TestIterations--;
					Console.WriteLine($"\n\n == {descriptor} FINISHED! == \n\n");
				}
			}
			catch (Exception e) {
				Console.WriteLine($"\n\n ==== ERROR ====\n > Test iteration ran into error: {e.Message}");
				throw new Exception($"ERROR: {e.Message}");
			}
			
			Console.WriteLine("\n\n ===== Running tests finished ===== ");
		}


		private void SetupWorkFlow()
		{
			Console.WriteLine("\n\n------------------------\n" +
							  "  Setting up Workflow...\n" +
							  "------------------------\n\n");
			try
			{
				MenuManagerService.AddNewWorkflow(WorkflowName, CityName, WorkflowDescription);
				var tempWorkflow = MenuManagerService.GetWorkflowByName(WorkflowName);
				workflow = new Workflow(tempWorkflow.Id);
				workflow.SetNetworkinformationFromInnerGtfsFeed();
				workflow.CreateGAOptimizationService(settings, statisticsCollector);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Workflow setup ran into error: {e.Message}");
				throw new Exception($"ERROR: {e.Message}");
			}
			Console.WriteLine(" ===== Workflow setup done! ===== ");
		}

		private void RunGA(string descriptor)
		{
			workflow.RunGA(descriptor);
		}
	}
}
