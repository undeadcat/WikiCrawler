namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core

[<TestFixture>]
type Class1() = 
    [<Test>]
    member __.Test1() = 
        let res = Graph.GetWikiGraph "Totoro" 2 |> Async.RunSynchronously
        res |> ignore
