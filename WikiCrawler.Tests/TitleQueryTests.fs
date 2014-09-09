namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core
open Helpers
open System

[<TestFixture>]
type TitleQueryTests() = 
    let getQueryTitles list = TitleQuery.Create list |> List.map (TitleQuery.Titles)
    
    [<Test>]
    member __.EmptyEnumerable_NoQueries() = Assert.That(getQueryTitles [], Is.Empty)
    
    [<Test>]
    member __.BelowLimit_OneQuery() = 
        let list = [ "1"; "2"; "3" ]
        Assert.That(getQueryTitles list, Is.EqualTo [ list ])
    
    [<Test>]
    member __.AtLimit_OneQuery() = 
        let list = List.init 50 toString
        Assert.That(getQueryTitles list, Is.EqualTo [ list ])
    
    [<Test>]
    member __.AboveLimit_QueriesSplit() = 
        let list = List.init 55 toString
        Assert.That(getQueryTitles list, 
                    Is.EqualTo [ Seq.take 50 list
                                 Seq.skip 50 list ])
    
    [<Test>]
    member __.AboveQueryStringLimit_QueriesSplit() = 
        let str = String.replicate 3000 "a"
        let list = str |> List.replicate 3
        Assert.That(getQueryTitles list, 
                    Is.EqualTo([ [ str ]
                                 [ str ]
                                 [ str ] ]))
    
    [<Test>]
    member __.SingleItemAboveQueryStringLimit_AllowIt() = 
        let str = String.replicate 9000 "a"
        let list = str |> List.replicate 2
        let actual = getQueryTitles list
        Assert.That(actual, 
                    Is.EqualTo([ [ str ]
                                 [ str ] ]))
    
    [<Test>]
    member __.WhitespaceTitle_ThrowException() = 
        Assert.Throws(Exception.OfType<InvalidOperationException>(), fun () -> getQueryTitles [ "  "; "a" ])
