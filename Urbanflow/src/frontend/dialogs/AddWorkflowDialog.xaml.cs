using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Urbanflow.src.frontend.dialogs
{
	/// <summary>
	/// Interaction logic for AddWorkflowDialog.xaml
	/// </summary>
	public partial class AddWorkflowDialog : Window
	{
		public string WorkflowName { get; private set; }
		public string WorkflowDescription { get; private set; }

		public AddWorkflowDialog()
		{
			InitializeComponent();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			WorkflowName = NameTextBox.Text.Trim();
			WorkflowDescription = DescriptionTextBox.Text.Trim();

			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
