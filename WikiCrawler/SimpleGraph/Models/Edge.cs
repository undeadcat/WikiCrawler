using GraphX;

namespace SimpleGraph.Models
{
	public class Edge : EdgeBase<Vertex>
	{
		public Edge(Vertex source, Vertex target)
			: base(source, target)
		{
		}

		public Edge()
			: base(null, null, 1)
		{
		}

		public string Text { get; set; }

		public override string ToString()
		{
			return Text;
		}
	}
}
