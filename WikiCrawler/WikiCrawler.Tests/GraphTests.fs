namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core

[<TestFixture>]
type GraphTests() = 
    let getGraph f node depth = Graph.GetGraph f node depth |> Async.RunSynchronously
    
    [<Test>]
    member __.SingleNode() = 
        let graph = getGraph (fun _ -> async.Return []) 1 3
        Assert.That(graph.Root, Is.EqualTo 1)
        Assert.That(graph.Adjacent, Is.EqualTo([ (1, List.empty<int>) ]))
    
    [<Test>]
    member __.CanHandleCycles() = 
        let getLinks s = 
            [ 1; 2; 3 ]
            |> List.filter ((<>) s)
            |> async.Return
        
        let graph = getGraph getLinks 1 3
        Assert.That(graph.Root, Is.EqualTo(1))
        let expected = 
            [ (1, [ 2; 3 ])
              (2, [ 1; 3 ])
              (3, [ 1; 2 ]) ]
        Assert.That(List.toArray graph.Adjacent, Is.EqualTo(expected))
    
    [<Test>]
    member __.MaxDepth() = 
        let getLinks s = async.Return [ s + 1 ]
        let getAdjacent depth = (getGraph getLinks 1 depth).Adjacent
        let actual = getAdjacent 1
        Assert.That(actual, Is.EqualTo([ (1, [ 2 ]);(2,[]); ]))
        let expected = 
            [ (1, [ 2 ])
              (2, [ 3 ])
              (3, [ 4 ]);
              (4, []) ]
        
        let actual = getAdjacent 3
        Assert.That(actual, Is.EqualTo(expected))
