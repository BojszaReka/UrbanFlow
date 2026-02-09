using System.Windows;
using Urbanflow.src.backend.models;
using Urbanflow.src.frontend.pages;

namespace Urbanflow.src.frontend.windows
{
	/// <summary>
	/// Interaction logic for SidebarWindow.xaml
	/// </summary>
	public partial class SidebarWindow : Window
	{
		private Workflow workflow;

		private const string TabSelectedButtonResourceKey = "TabSelectedButton";
		private const string TabUnselectedButtonResourceKey = "TabUnselectedButton";

		public SidebarWindow(Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;
			MainFrame.Content = new GraphPage(workflow);
		}

		private void unselectTabButtons()
		{
			btn_tabgraph.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
			btn_tabai.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
			btn_tabmap.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
			btn_tabnetwork.Style = Application.Current.Resources[TabUnselectedButtonResourceKey] as Style;
		}

		private void openGraphView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new GraphPage(workflow);
			unselectTabButtons();
			btn_tabgraph.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void openAiView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new OptimizationPage(workflow);
			unselectTabButtons();
			btn_tabai.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void openMapView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new MapPage(workflow);
			unselectTabButtons();
			btn_tabmap.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void openNetworkView(object sender, RoutedEventArgs e)
		{
			MainFrame.Content = new NetworkPage(workflow);
			unselectTabButtons();
			btn_tabnetwork.Style = Application.Current.Resources[TabSelectedButtonResourceKey] as Style;
		}

		private void BackToWelcome_Click(object sender, RoutedEventArgs e)
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			this.Close();
		}
	}
}
