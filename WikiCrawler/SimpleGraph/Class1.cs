using GraphX;
using GraphX.Logic;
using QuickGraph;

namespace SimpleGraph
{
	public static class SomeExtensions
	{
		public static TAlg CreateLayoutParameters<TV, TE, TG, TAlg>(this GXLogicCore<TV, TE, TG> logicCore, LayoutAlgorithmTypeEnum algorithmType)
			where TE: class, IGraphXEdge<TV> where TG: class, IMutableBidirectionalGraph<TV, TE> where TV: class, IGraphXVertex where TAlg: class
		{
			return logicCore.AlgorithmFactory.CreateLayoutParameters(algorithmType) as TAlg;
		}
	}
}
