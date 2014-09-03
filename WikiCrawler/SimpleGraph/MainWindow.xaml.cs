using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphX;
using GraphX.Controls;
using GraphX.GraphSharp.Algorithms.Layout;
using GraphX.GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphX.GraphSharp.Algorithms.Layout.Simple.Tree;
using GraphX.GraphSharp.Algorithms.OverlapRemoval;
using GraphX.Logic;
using QuickGraph;
using SimpleGraph.Models;
using WikiCrawler.Core;

namespace SimpleGraph
{
	public partial class MainWindow : IDisposable
	{
		private readonly IDisposable controlSubscription;

		public MainWindow()
		{
			InitializeComponent();

			ZoomControl.SetViewFinderVisibility(zoomctrl, Visibility.Visible);
			zoomctrl.ZoomToFill();
			var nameChanged = FromTextChangedEvent(PageName);
			var depthChanged = FromTextChangedEvent(SearchDepth);
			controlSubscription = depthChanged
				.Where(x => Common.ParseIntOrNull(x) != null)
				.CombineLatest(nameChanged, (x, y) =>
					new
					{
						Name = y,
						Depth = int.Parse(x)
					})
				.DistinctUntilChanged()
				.Select(x => Observable.FromAsync(() => Graph.GetWikiGraph.ToCsharpFunc()(x.Name, x.Depth).ToTask()))
				.Switch()
				.ObserveOnDispatcher()
				.Select(ConvertGraph)
				.Subscribe(x =>
						   {
							   Area.LogicCore.Graph = x;
							   DisplayGraph();
						   });
			Loaded += (o, e) => DisplayGraph();

			Area.LogicCore = new GXLogicCore<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
							 {
								 Graph = new BidirectionalGraph<Vertex, Edge>(),
								 DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK,
								 DefaultLayoutAlgorithmParams = new KKLayoutParameters { MaxIterations = 100 },
								 DefaultOverlapRemovalAlgorithmParams = new OverlapRemovalParameters { HorizontalGap = 50, VerticalGap = 50 },
								 DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA,
								 DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER,
								 AsyncAlgorithmCompute = false
							 };
		}

		private static BidirectionalGraph<Vertex, Edge> ConvertGraph(Graph.Graph<string> arg)
		{
			var g = new BidirectionalGraph<Vertex, Edge>();
			var verts = arg.Adjacent.Select((x, i) => new Vertex { Text = x.Item1, ID = i }).ToDictionary(x => x.Text, x => x);
			var edges = arg.Adjacent.SelectMany(x => x.Item2.Select(y => new Edge(verts[x.Item1], verts[y])));
			g.AddVertexRange(verts.Values);
			g.AddEdgeRange(edges);
			return g;
		}


		private static IObservable<string> FromTextChangedEvent(TextBox control)
		{
			return Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
				x => new TextChangedEventHandler(x),
				x => control.TextChanged += x,
				x => control.TextChanged -= x).Select(x => control.Text);
		}

		private void DisplayGraph()
		{
			//Note that you can't create it in class constructor as there will be problems with visuals
			Area.GenerateGraph(true);
			Area.SetEdgesDashStyle(EdgeDashStyle.Solid);
			Area.ShowAllEdgesArrows();
			zoomctrl.ZoomToFill();
		}

		public void Dispose()
		{
			controlSubscription.Dispose();
			Area.Dispose();
		}
	}
}