using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Urbanflow.src.backend.models;
using yWorks.Geometry;
using yWorks.Graph;  
using yWorks.Graph.LabelModels;
using yWorks.Graph.Styles; 
using yWorks.Layout.Orthogonal;
using Urbanflow.src.backend.services;
using Urbanflow.src.backend.models.graph;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for GraphPage.xaml
	/// </summary>
	public partial class GraphPage : Page
	{
		private Workflow workflow;
		private Graph graphOnDisplay;

		#region Constructor
		public GraphPage(in Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;

			CreateGraph();
		}
		#endregion

		private void CreateGraph()
		{
			Result<Graph> result = workflow.GetNetWorkGraphData();
			if (result.IsSuccess)
			{
				graphOnDisplay = result.Value;
			}
			else
			{
				MessageBox.Show(result.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}

		public async void OnLoaded(object source, EventArgs args)
		{
			//ConfigureGroupNodeStyles();
			SetDefaultStyles();
			// Populates the graph and overrides some styles and label models
			await PopulateGraph();
			UpdateViewport();
		}

		private void ConfigureGroupNodeStyles()
		{
			// GroupNodeStyle is a style especially suited to group nodes
			var groupNodeDefaults = Graph.GroupNodeDefaults;
			groupNodeDefaults.Style = new GroupNodeStyle
			{
				GroupIcon = GroupNodeStyleIconType.ChevronDown,
				FolderIcon = GroupNodeStyleIconType.ChevronUp,
				IconSize = 14,
				IconBackgroundShape = GroupNodeStyleIconBackgroundShape.Circle,
				IconForegroundBrush = Brushes.White,
				TabBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x22, 0x65)),
				TabPosition = GroupNodeStyleTabPosition.TopTrailing,
				Pen = new Pen(new SolidColorBrush(Color.FromRgb(0x24, 0x22, 0x65)), 2),
				CornerRadius = 4,
				TabWidth = 70,
				ContentAreaPadding = new InsetsD(8),
				HitTransparentContentArea = true
			};

			// Sets a label style with right-aligned text
			groupNodeDefaults.Labels.Style =
				new LabelStyle { TextAlignment = TextAlignment.Right, TextBrush = Brushes.White };

			// Places the label inside the tab.
			groupNodeDefaults.Labels.LayoutParameter = new GroupNodeLabelModel().CreateTabParameter();
		}

		private async Task PopulateGraph()
		{
			var nodeLookup = graphOnDisplay.Nodes.ToDictionary(n => n.Id);
			var graphNodeMap = new Dictionary<Guid, INode>();

			foreach (var edge in graphOnDisplay.Edges)
			{
				if (!graphNodeMap.TryGetValue(edge.FromNodeId, out var fromNodeGraph))
				{
					var fromNodeDB = nodeLookup[edge.FromNodeId];

					fromNodeGraph = Graph.CreateNode(new PointD(50, 50));
					Graph.AddLabel(fromNodeGraph, fromNodeDB.Name);

					graphNodeMap.Add(edge.FromNodeId, fromNodeGraph);
				}

				if (!graphNodeMap.TryGetValue(edge.ToNodeId, out var toNodeGraph))
				{
					var toNodeDB = nodeLookup[edge.ToNodeId];

					toNodeGraph = Graph.CreateNode(new PointD(50, 50));
					Graph.AddLabel(toNodeGraph, toNodeDB.Name);

					graphNodeMap.Add(edge.ToNodeId, toNodeGraph);
				}

				// Create edge
				Graph.CreateEdge(fromNodeGraph, toNodeGraph);
			}

			var layout = new OrthogonalLayout
			{
				PreferParallelRoutes = true,
				ChainSubstructureStyle = ChainSubstructureStyle.Straight,
				QualityTimeRatio = 0.8,
				LayoutMode = OrthogonalLayoutMode.Strict,
				GridSpacing = 10
			};

			await graphControl.ApplyLayoutAnimated(layout, TimeSpan.Zero);
		}


		/// <summary>
		///   Sets up default styles for graph elements.
		/// </summary>
		/// <remarks>
		///   Default styles apply only to elements created after the default style has been set,
		///   so typically, you'd set these as early as possible in your application.
		/// </remarks>
		private void SetDefaultStyles()
		{
			#region Default Node Style

			// Sets this style as the default for all nodes that don't have another
			// style assigned explicitly
			Graph.NodeDefaults.Style = new ShapeNodeStyle
			{
				Shape = ShapeNodeShape.RoundRectangle,
				Brush = new SolidColorBrush(Color.FromRgb(255, 108, 0)),
				Pen = new Pen(new SolidColorBrush(Color.FromRgb(102, 43, 0)), 2)
			};

			#endregion

			#region Default Edge Style

			// Sets the default style for edges:
			// Creates a PolylineEdgeStyle which will be used as default for all edges
			// that don't have another style assigned explicitly
			var defaultEdgeStyle = new PolylineEdgeStyle
			{
				Pen = (Pen)new Pen(new SolidColorBrush(Color.FromRgb(102, 43, 0)), 2).GetAsFrozen(),
				TargetArrow = new Arrow(ArrowType.Triangle,
					(Brush)new SolidColorBrush(Color.FromRgb(102, 43, 0)).GetAsFrozen()),
				OrthogonalEditing = true,
				SmoothingLength = 5,
			};

			// Sets the defined edge style as the default for all edges that don't have
			// another style assigned explicitly:
			Graph.EdgeDefaults.Style = defaultEdgeStyle;

			#endregion

			#region Default Label Styles

			// Sets the default style for labels
			// Creates a label style with the label text color set to dark red
			ILabelStyle defaultLabelStyle =
				new LabelStyle { Typeface = new Typeface("Tahoma"), TextSize = 12, TextBrush = Brushes.Black };

			// Sets the defined style as the default for both edge and node labels:
			Graph.EdgeDefaults.Labels.Style = Graph.NodeDefaults.Labels.Style = defaultLabelStyle;

			#endregion

			#region Default Node size

			Graph.NodeDefaults.Size = new SizeD(50, 50);

			#endregion
		}

		private void UpdateViewport()
		{
			graphControl.FitGraphBounds();
		}

		#region Convenience Properties

		public IGraph Graph
		{
			get { return graphControl.Graph; }
		}

		#endregion

	}
}
