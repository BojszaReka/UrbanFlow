using System.Windows;
using Urbanflow.src.backend.db;
using Urbanflow.src.frontend.pages;

namespace Urbanflow
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			InitializeDatabase();
			MainFrame.Content = new WelcomePage();
		}

		private static void InitializeDatabase()
		{
			using var db = new DatabaseContext();
			db.Database.EnsureCreated();
		}
	}
}
