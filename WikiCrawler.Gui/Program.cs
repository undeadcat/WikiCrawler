using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Glee.Drawing;
using Microsoft.Glee.GraphViewerGdi;
using WikiCrawler.Core;

namespace WikiCrawler.Gui
{
	public class Program
	{
		[STAThread]
		public static void Main()
		{
			var form = CreateForm();
			var subscription = FromTextChanged(form.DepthText)
				.Where(CanParseInt)
				.Select(int.Parse)
				.Where(x => x > 0)
				.CombineLatest(FromTextChanged(form.StartPageText), (d, p) => new { Page = p, Depth = d })
				.DistinctUntilChanged()
				.Select(x => GetGraph(x.Page, x.Depth))
				.Switch()
				.ObserveOn(SynchronizationContext.Current)
				.Select(ConvertGraph)
				.Subscribe(x => form.Viewer.Graph = x);

			using (subscription)
				Application.Run(form.Form);
		}

		private static IObservable<Graph<string>> GetGraph(string page, int depth)
		{
			return Observable.FromAsync(token => GraphModule.GetWikiGraph(page, depth).ToTask(token))
							 .Catch<Graph<string>, Exception>(ex =>
																  {
																	  MessageBox.Show(string.Format("An exception occured: {0}, {1}", ex.Message, ex.StackTrace));
																	  return Observable.Empty<Graph<string>>();
																  });
		}

		private static IObservable<string> FromTextChanged(TextBox textBox)
		{
			return Observable.FromEventPattern(
											   x => textBox.TextChanged += x,
											   x => textBox.TextChanged -= x)
							 .Select(x => ((TextBox) x.Sender).Text)
							 .Where(x => !String.IsNullOrWhiteSpace(x));
		}

		private static Graph ConvertGraph(Graph<string> arg)
		{
			var graph = new Graph("graph")
							{
								GraphAttr = new GraphAttr
												{
													NodeAttribute = new NodeAttr { Shape = Shape.Plaintext },
													LayerDirection = LayerDirection.TB,
													LayerSep = 500
												},
								Directed = true,
							};
			foreach (var tuple in arg.Adjacent.SelectMany(x => x.Item2.Select(y => new { One = x.Item1, Two = y })))
				graph.AddEdge(tuple.One, tuple.Two);
			return graph;
		}

		private static bool CanParseInt(string s)
		{
			int _;
			return int.TryParse(s, out _);
		}

		private static FormContent CreateForm()
		{
			var startPageText = new TextBox { Name = "StartPage", Width = 150 };
			var depthText = new TextBox { Name = "Depth", Width = 50 };
			var viewer = new GViewer { Dock = DockStyle.Fill, Graph = new Graph("someGraph") };
			var layoutPanel = new TableLayoutPanel
								  {
									  Dock = DockStyle.Fill,
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
							   Controls = { layoutPanel },
							   Text = Assembly.GetExecutingAssembly().GetName().Name
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