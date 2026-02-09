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

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for OptimizationPage.xaml
	/// </summary>
	public partial class OptimizationPage : Page
	{
		private readonly Workflow workflow;

		public OptimizationPage(Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;
		}
	}
}
