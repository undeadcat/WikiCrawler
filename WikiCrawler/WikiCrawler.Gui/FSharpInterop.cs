using System;
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

		public static Func<T1, T2, TRes> ToCsharpFunc<T1, T2, TRes>(this FSharpFunc<T1, FSharpFunc<T2, TRes>> fSharpFunc)
		{
			return (s, i) => FSharpFunc<T1, T2>.InvokeFast(fSharpFunc, s, i);
		}
	}
}
