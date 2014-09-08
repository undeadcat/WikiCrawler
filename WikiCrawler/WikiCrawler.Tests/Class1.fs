namespace WikiCrawler.Tests

open Foq
open NUnit.Framework
open System
open WikiCrawler.Core.WikiApi
open WikiCrawler.Core

type IFoo = 
    abstract Bar : int -> int

[<TestFixture>]
type Class1() = 
    [<Test>]
    member this.Test1() = 
        let mock = Mock<IFoo>(MockMode.Strict)
        let obj = 
            mock.Setup(fun x -> <@ x.Bar(It.Is((<) 10)) @>).Returns(5)
                .Setup(fun x -> <@ x.Bar(It.Is((=) 0)) @>).Returns(0).Create()
        Console.WriteLine(obj.Bar(5))
        Console.WriteLine(obj.Bar(0))

    [<Test>]
    member this.TestGet() = 
        let wikiApi = new WikiApi(new HttpProxy())
        let res = wikiApi.GetLinks(TitleQuery.Create(["Cat"]).Head) |> WikiApi.RunToCompletion |> Async.RunSynchronously
        res |> ignore
