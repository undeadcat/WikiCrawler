using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace WikiCrawler.Gui
{
	public static class FSharpInterop
	{
		public static Task<T> ToTask<T>(this FSharpAsync<T> fSharpAsync)
		{
			return FSharpAsync.StartAsTask(fSharpAsync, FSharpOption<TaskCreationOptions>.None, FSharpOption<CancellationToken>.None);
		}
	}
}
