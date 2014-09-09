﻿namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core
open System.Collections.Generic

[<TestFixture>]
type GraphTests() = 
    
    let getGraph getEdges (node) depth = 
        let getNodeWithEdges nodes = 
            nodes
            |> List.map (fun x -> 
                   { Node = x
                     Edges = getEdges (x) })
            |> async.Return
        GraphModule.GetGraph getNodeWithEdges EqualityComparer.Default node depth |> Async.RunSynchronously
    
    [<Test>]
    member __.SingleNode() = 
        let graph = getGraph (fun _ -> []) 1 3
        Assert.That(graph.Root, Is.EqualTo 1)
        Assert.That(graph.Adjacent, Is.EqualTo([ (1, List.empty<int>) ]))
    
    [<Test>]
    member __.CanHandleCycles() = 
        let getLinks s = [ 1; 2; 3 ] |> List.filter ((<>) s)
        let graph = getGraph getLinks 1 3
        Assert.That(graph.Root, Is.EqualTo(1))
        let expected = 
            [ (1, [ 2; 3 ])
              (2, [ 1; 3 ])
              (3, [ 1; 2 ]) ]
        Assert.That(List.toArray graph.Adjacent, Is.EqualTo(expected))
    
    [<Test>]
    member __.MaxDepth() = 
        let getLinks s = [ s + 1 ]
        let getAdjacent depth = (getGraph getLinks 1 depth).Adjacent
        let actual = getAdjacent 1
        Assert.That(actual, 
                    Is.EqualTo([ (1, [ 2 ])
                                 (2, []) ]))
        let actual = getAdjacent 2
        Assert.That(actual, 
                    Is.EqualTo([ (1, [ 2 ])
                                 (2, [ 3 ])
                                 (3, []) ]))
        let expected = 
            [ (1, [ 2 ])
              (2, [ 3 ])
              (3, [ 4 ])
              (4, []) ]
        
        let actual = getAdjacent 3
        Assert.That(actual, Is.EqualTo(expected))
    
    [<Test>]
    member this.Simple() = 
        let getLinks = 
            function 
            | 0 -> [ 1; 2; 3 ]
            | 1 -> [ 11; 12; 13 ]
            | 2 -> [ 21; 22; 23 ]
            | 3 -> [ 31; 32; 33 ]
            | v -> failwith ("Unexpected node " + v.ToString())
        
        let actual = getGraph (getLinks) 0 2
        
        let expected = 
            [ (0, [ 1; 2; 3 ])
              (1, [ 11; 12; 13 ])
              (2, [ 21; 22; 23 ])
              (3, [ 31; 32; 33 ]) ]
        Assert.That(actual.Adjacent |> List.filter (snd
                                                    >> List.isEmpty
                                                    >> not), Is.EqualTo(expected))
