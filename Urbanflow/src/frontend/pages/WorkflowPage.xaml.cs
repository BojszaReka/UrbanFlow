using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Urbanflow.src.backend.models;
using Urbanflow.src.backend.services;
using Urbanflow.src.frontend.dialogs;
using Urbanflow.src.frontend.windows;

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for WorkflowPage.xaml
	/// </summary>
	public partial class WorkflowPage : Page
	{
		private readonly string CityName;

		public WorkflowPage(string cityName)
		{
			CityName = cityName;
			InitializeComponent();
			PageTitle.Text = $"Munkafolyamatok - {CityName}";
			LoadWorkflowTable();
		}

		private void AddNewWorkFlow_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new AddWorkflowDialog
			{
				Owner = Application.Current.MainWindow
			};

			if (dialog.ShowDialog() == true)
			{
				MenuManagerService.AddNewWorkflow(dialog.WorkflowName, CityName, dialog.WorkflowDescription);

				// Refresh DataGrid (LoadWorkflowTable should filter by CityName)
				LoadWorkflowTable();
			}
		}

		private void LoadWorkflowTable()
		{
			List<Workflow>? workflows = MenuManagerService.GetWorkflowsByCityName(CityName);
			if (workflows == null) return;

			WorkflowDataGrid.ItemsSource = workflows;
		}


		private static Workflow? GetWorkflowFromSender(object sender)
		{
			if (sender is not Button btn) return null;

			Workflow? workflow = btn.DataContext as Workflow;
			return workflow;
		}

		private void EditWorkflow_Click(object sender, RoutedEventArgs e)
		{
			Workflow? workflow = GetWorkflowFromSender(sender);
			if (workflow == null) return;

			var dialog = new EditWorkflowDialog(workflow.Name, workflow.Description)
			{
				Owner = Application.Current.MainWindow
			};

			if (dialog.ShowDialog() == true)
			{
				MenuManagerService.UpdateWorkflow(workflow, dialog.WorkflowName, dialog.WorkflowDescription);

				LoadWorkflowTable();
			}
		}

		private void DeleteWorkflow_Click(object sender, RoutedEventArgs e)
		{
			Workflow? workflow = GetWorkflowFromSender(sender);
			if (workflow == null) return;

			if (MessageBox.Show(
				$"Biztos törli a(z) '{workflow.Name}' munkafolyamatot?",
				"Törlés megerősítése",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning) == MessageBoxResult.Yes)
			{
				MenuManagerService.DeleteWorkflow(workflow.Id);
				LoadWorkflowTable();
			}
		}

		private void RunWorkflow_Click(object sender, RoutedEventArgs e)
		{
			Workflow? workflow = GetWorkflowFromSender(sender);
			if (workflow == null) return;

			SidebarWindow w = new(workflow);
			w.Show();

			Application.Current.MainWindow.Close();
		}



		private void BackToWelcome_Click(object sender, RoutedEventArgs e)
		{
			WelcomePage welcomePage = new();

			// Get MainWindow and navigate
			((MainWindow)Application.Current.MainWindow).MainFrame.Navigate(welcomePage);
		}
	}
}
