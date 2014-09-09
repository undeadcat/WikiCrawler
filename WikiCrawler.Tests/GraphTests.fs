namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core
open System.Collections.Generic
open System

[<TestFixture>]
type GraphTests() = 
    
    let getNodesFunc getEdgesByNode nodes = 
        nodes
        |> List.map (fun x -> 
               { Node = x
                 Edges = getEdgesByNode (x) })
        |> async.Return
    
    let getGraph getNodes node depth = 
        GraphModule.GetGraph getNodes Comparer.Default node depth |> Async.RunSynchronously
    
    [<Test>]
    member __.SingleNode() = 
        let graph = getGraph (getNodesFunc (fun _ -> [])) 1 3
        Assert.That(graph.Adjacent, Is.EqualTo([ (1, List.empty<int>) ]))
    
    [<Test>]
    member __.CanHandleCycles() = 
        let linksByNode s = [ 1; 2; 3 ] |> List.filter ((<>) s)
        let graph = getGraph (getNodesFunc linksByNode) 1 3
        
        let expected = 
            [ (1, [ 2; 3 ])
              (2, [ 1; 3 ])
              (3, [ 1; 2 ]) ]
        Assert.That(List.toArray graph.Adjacent, Is.EqualTo(expected))
    
    [<Test>]
    member __.MaxDepth() = 
        let linksByNode s = [ s + 1 ]
        let getAdjacent depth = (getGraph (getNodesFunc linksByNode) 1 depth).Adjacent
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
    member __.Simple() = 
        let linksByNode = 
            function 
            | 0 -> [ 1; 2; 3 ]
            | 1 -> [ 11; 12; 13 ]
            | 2 -> [ 21; 22; 23 ]
            | 3 -> [ 31; 32; 33 ]
            | v -> failwith ("Unexpected node " + v.ToString())
        
        let actual = getGraph (getNodesFunc linksByNode) 0 2
        
        let expected = 
            [ (0, [ 1; 2; 3 ])
              (1, [ 11; 12; 13 ])
              (2, [ 21; 22; 23 ])
              (3, [ 31; 32; 33 ]) ]
        
        let actualNodes = 
            actual.Adjacent |> List.filter (snd
                                            >> Seq.isEmpty
                                            >> not)
        
        Assert.That(actualNodes, Is.EqualTo(expected))
    
    [<Test>]
    member __.StartNodeDoesntExist_GraphIsEmpty() = 
        let getNodes _ = async.Return []
        let actual = getGraph (getNodes) 0 2
        Assert.That(actual.Adjacent, Is.Empty)
    
    [<Test>]
    member __.CreateGraphWithCustomComparer_UsedForComparison() = 
        let linksByNode = 
            function 
            | "start" -> [ "one"; "two" ]
            | "one" -> [ "TWO"; "three" ]
            | "two" -> [ "ThrEE" ]
            | _ -> failwith "unmatched setup"
        
        let graph = 
            GraphModule.GetGraph (getNodesFunc linksByNode) StringComparer.InvariantCultureIgnoreCase "start" 2 
            |> Async.RunSynchronously
        
        let check (key, values) = 
            Assert.That(graph.Adjacent
                        |> List.find (fun x -> (fst x).EqualsIgnoringCase(key))
                        |> snd
                        |> List.map (fun x -> x.ToLower()), Is.EquivalentTo values)
        Assert.That(graph.Adjacent.Length, Is.EqualTo(4))
        check ("start", [ "one"; "two" ])
        check ("one", [ "two"; "three" ])
        check ("two", [ "three" ])
        check ("three", [])
    
    [<Test>]
    member __.DuplicateLinksReturned_NotDuplicatedInGraph() = 
        let linksByNode = 
            function 
            | "start" -> [ "one"; "two"; "two"; "one" ]
            | "one" -> []
            | "two" -> []
            | _ -> failwith "unmatched setup"
        
        let graph = getGraph (getNodesFunc linksByNode) "start" 2
        Assert.That(graph.Adjacent, 
                    Is.EquivalentTo([ ("start", [ "one"; "two" ])
                                      ("one", [])
                                      ("two", []) ]))
