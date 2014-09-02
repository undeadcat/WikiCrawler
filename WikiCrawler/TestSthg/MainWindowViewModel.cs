using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TestSthg
{
	public class MainWindowViewModel: INotifyPropertyChanged
	{
		private PocGraph graph;

		public MainWindowViewModel()
		{
			Graph = new PocGraph(true);
			var verts = Enumerable.Range(1, 1000).Select(x => new PocVertex(x.ToString(), true)).ToArray();
			foreach (var vert in verts)
				Graph.AddVertex(vert);
			verts.Skip(1).Concat(new []{verts.First()}).Aggregate(verts.First(), (v1, v2) =>
												   {
													   AddNewGraphEdge(v1, v2);
													   return v2;
												   });

			return;
			var existingVertices = new List<PocVertex>();
			existingVertices.Add(new PocVertex("Sacha Barber", true)); //0
			existingVertices.Add(new PocVertex("Sarah Barber", false)); //1
			existingVertices.Add(new PocVertex("Marlon Grech", true)); //2
			existingVertices.Add(new PocVertex("Daniel Vaughan", true)); //3
			existingVertices.Add(new PocVertex("Bea Costa", false)); //4

			foreach (var vertex in existingVertices)
				Graph.AddVertex(vertex);

			//add some edges to the graph
			AddNewGraphEdge(existingVertices[0], existingVertices[1]);
			AddNewGraphEdge(existingVertices[0], existingVertices[2]);
			AddNewGraphEdge(existingVertices[0], existingVertices[3]);
			AddNewGraphEdge(existingVertices[0], existingVertices[4]);

			AddNewGraphEdge(existingVertices[1], existingVertices[0]);
			AddNewGraphEdge(existingVertices[1], existingVertices[2]);
			AddNewGraphEdge(existingVertices[1], existingVertices[3]);

			AddNewGraphEdge(existingVertices[2], existingVertices[0]);
			AddNewGraphEdge(existingVertices[2], existingVertices[1]);
			AddNewGraphEdge(existingVertices[2], existingVertices[3]);
			AddNewGraphEdge(existingVertices[2], existingVertices[4]);

			AddNewGraphEdge(existingVertices[3], existingVertices[0]);
			AddNewGraphEdge(existingVertices[3], existingVertices[1]);
			AddNewGraphEdge(existingVertices[3], existingVertices[3]);
			AddNewGraphEdge(existingVertices[3], existingVertices[4]);

			AddNewGraphEdge(existingVertices[4], existingVertices[0]);
			AddNewGraphEdge(existingVertices[4], existingVertices[2]);
			AddNewGraphEdge(existingVertices[4], existingVertices[3]);

			AddNewGraphEdge(existingVertices[0], existingVertices[1]);
		}

		private void AddNewGraphEdge(PocVertex @from, PocVertex to)
		{
			var edgeString = string.Format("{0}-{1} Connected", from.ID, to.ID);
			var newEdge = new PocEdge(edgeString, from, to);
			Graph.AddEdge(newEdge);
		}

		public PocGraph Graph
		{
			get { return graph; }
			set
			{
				graph = value;
				NotifyPropertyChanged("Graph");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}