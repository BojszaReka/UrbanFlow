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

namespace Urbanflow.src.frontend.pages
{
	/// <summary>
	/// Interaction logic for GraphPage.xaml
	/// </summary>
	public partial class GraphPage : Page
	{
		private Workflow workflow;
		private GraphManagerService graphManager;
		private Graph graphOnDisplay;

		#region Constructor
		public GraphPage(Workflow workflow)
		{
			InitializeComponent();
			this.workflow = workflow;
			graphManager = new GraphManagerService(workflow.Id);

			CreateGraph();
		}
		#endregion

		private void CreateGraph()
		{
			graphManager.CreateAllGraphsForFeed();

			graphOnDisplay = graphManager.GetNetWorkGraphData();
		}

		public async void OnLoaded(object source, EventArgs args)
		{
			ConfigureGroupNodeStyles();
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
				CornerRadius = 8,
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
			List<(Guid nodeId, INode graphNode)> nodeToNodeList = new List<(Guid nodeId, INode graphNode)>();

			foreach(var Edge in graphOnDisplay.Edges)
			{
				var fromNodeDB = graphOnDisplay.Nodes.Where(n => n.Id == Edge.FromNodeId).FirstOrDefault();
				var toNodeDB = graphOnDisplay.Nodes.Where(n => n.Id == Edge.ToNodeId).FirstOrDefault();

				if (nodeToNodeList == null || !nodeToNodeList.Where(n => n.nodeId == Edge.FromNodeId).Any())
				{
					INode node = Graph.CreateNode(new PointD(50, 50));
					(Guid nodeId, INode graphNode) item = (Edge.FromNodeId, node);
					nodeToNodeList.Add(item);
					Graph.AddLabel(node, fromNodeDB.Name);
				}
				if (nodeToNodeList == null || !nodeToNodeList.Where(n => n.nodeId == Edge.ToNodeId).Any())
				{
					INode node = Graph.CreateNode(new PointD(50, 50));
					(Guid nodeId, INode graphNode) item = (Edge.ToNodeId, node);
					nodeToNodeList.Add(item);
					Graph.AddLabel(node, toNodeDB.Name);
				}

				
				var fromNodeGraph = nodeToNodeList.Where(n => n.nodeId == Edge.FromNodeId).FirstOrDefault().graphNode;
				var toNodeGraph = nodeToNodeList.Where(n => n.nodeId == Edge.ToNodeId).FirstOrDefault().graphNode;

				var edge = Graph.CreateEdge(fromNodeGraph, toNodeGraph);
				Graph.AddLabel(edge, Edge.Weight.ToString());
			}			
			
			await graphControl.ApplyLayoutAnimated(new OrthogonalLayout(), TimeSpan.FromSeconds(0));
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
				Pen = new Pen(new SolidColorBrush(Color.FromRgb(102, 43, 0)), 1.5)
			};

			#endregion

			#region Default Edge Style

			// Sets the default style for edges:
			// Creates a PolylineEdgeStyle which will be used as default for all edges
			// that don't have another style assigned explicitly
			var defaultEdgeStyle = new PolylineEdgeStyle
			{
				Pen = (Pen)new Pen(new SolidColorBrush(Color.FromRgb(102, 43, 0)), 1.5).GetAsFrozen(),
				TargetArrow = new Arrow(ArrowType.Triangle,
					(Brush)new SolidColorBrush(Color.FromRgb(102, 43, 0)).GetAsFrozen())
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

			// Sets the default size explicitly to 40x40
			Graph.NodeDefaults.Size = new SizeD(40, 40);

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
