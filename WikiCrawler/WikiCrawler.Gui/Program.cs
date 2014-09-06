using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Glee.Drawing;
using Microsoft.Glee.GraphViewerGdi;

namespace WikiCrawler.Gui
{
	//TODO. make ui responsive.
	//TODO. update graph with async feedback.
	public class Program
	{
		public static void Main()
		{
			var form = CreateForm();
			var subscription = FromTextChanged(form.DepthText)
				.Where(CanParseInt)
				.Select(int.Parse)
				.CombineLatest(FromTextChanged(form.StartPageText), (d, p) => new { Page = p, Depth = d })
				.DistinctUntilChanged()
				.Select(x => Observable.FromAsync(() => Core.Graph.GetWikiGraph.ToCsharpFunc()(x.Page, x.Depth).ToTask()))
				.Switch()
				.ObserveOn(SynchronizationContext.Current)
				.Select(ConvertGraph)
				.Subscribe(x => form.Viewer.Graph = x);

			using (subscription)
				Application.Run(form.Form);
		}

		private static Graph ConvertGraph(Core.Graph.Graph<string> arg)
		{
			var res = new Graph("graph");
			foreach (var tuple in arg.Adjacent.SelectMany(x => x.Item2.Select(y => new { One = x.Item1, Two = y })))
				res.AddEdge(tuple.One, tuple.Two);
			return res;
		}

		private static IObservable<string> FromTextChanged(TextBox textBox)
		{
			return Observable.FromEventPattern(
											   x => textBox.TextChanged += x,
											   x => textBox.TextChanged -= x)
							 .Select(x => ((TextBox) x.Sender).Text);
		}

		private static bool CanParseInt(string s)
		{
			int _;
			return int.TryParse(s, out _);
		}

		private static Graph CreateGraph()
		{
			var graph = new Graph("graph")
							{
								GraphAttr = new GraphAttr { NodeAttribute = new NodeAttr { Shape = Shape.Plaintext } },
								Directed = true
							};
			var random = new Random();
			const int count = 100;
			var nodes = Enumerable.Range(0, count).Select(x => x.ToString()).ToArray();
			for (var i = 0; i < count; i++)
				graph.AddEdge(nodes[random.Next(count)], nodes[random.Next(count)]);
			return graph;
		}

		private static FormContent CreateForm()
		{
			var startPageText = new TextBox { Name = "StartPage", Width = 150 };
			var depthText = new TextBox { Name = "Depth", Width = 50 };
			var viewer = new GViewer { Dock = DockStyle.Fill, Graph = CreateGraph() };
			var layoutPanel = new TableLayoutPanel
								  {
									  Dock = DockStyle.Fill,
									  BorderStyle = BorderStyle.FixedSingle,
									  CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
									  Controls =
										  {
											  { FlowLayout(Label("Start Page:"), startPageText), 0, 0 },
											  { FlowLayout(Label("Depth:"), depthText), 1, 0 },
											  { viewer, 0, 1 }
										  }
								  };
			layoutPanel.SetColumnSpan(viewer, 2);
			var form = new Form
						   {
							   WindowState = FormWindowState.Maximized,
							   Controls = { layoutPanel }
						   };
			return new FormContent { Form = form, DepthText = depthText, StartPageText = startPageText, Viewer = viewer };
		}

		private static Label Label(string text)
		{
			return new Label { Text = text, AutoSize = true, TextAlign = ContentAlignment.BottomLeft };
		}

		private static Control FlowLayout(params Control[] controls)
		{
			var panel = new FlowLayoutPanel
							{
								Dock = DockStyle.Fill,
								FlowDirection = FlowDirection.LeftToRight,
								AutoSize = true,
								AutoSizeMode = AutoSizeMode.GrowAndShrink,
							};
			foreach (var control in controls)
				panel.Controls.Add(control);
			return panel;
		}

		private class FormContent
		{
			public Form Form { get; set; }
			public TextBox StartPageText { get; set; }
			public TextBox DepthText { get; set; }
			public GViewer Viewer { get; set; }
		}
	}
}