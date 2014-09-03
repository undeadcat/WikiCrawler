using GraphX;

namespace SimpleGraph.Models
{
	public class Vertex: VertexBase
	{
		public string Text { get; set; }

		public override string ToString()
		{
			return Text;
		}
	}
}