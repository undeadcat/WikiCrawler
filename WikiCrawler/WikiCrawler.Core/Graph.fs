namespace WikiCrawler.Core

//TODO. can get different case. test
//TODO. can get duplicates from api. dict values need to be sets.
//TODO. first page doesn't exist
//TODO. first page is null or whitespace
open System.Collections.Concurrent
open WikiCrawler.Core.WikiApi
open System.Collections.Generic
open System

type NodeWithEdges<'T> = 
    { Node : 'T
      Edges : 'T list }

type Graph<'T when 'T : comparison>(root : 'T, keyComparer : IEqualityComparer<'T>) = 
    let adjacent = new ConcurrentDictionary<'T, 'T list>(keyComparer)
    
    member __.AddLink one two = 
        adjacent.AddOrUpdate(one, [ two ], fun _ oldValue -> two :: oldValue) |> ignore
        adjacent.AddOrUpdate(two, [], fun _ oldValue -> oldValue) |> ignore
    
    member __.Adjacent = 
        adjacent
        |> Seq.map (fun pair -> (pair.Key, List.rev pair.Value))
        |> List.ofSeq
    
    member __.Root = root

module GraphModule = 
    let GetGraph<'T when 'T : comparison> (getNodes : 'T list -> NodeWithEdges<'T> list Async) (nodeComparer) 
        (startNode : 'T) (maxDepth : int) = 
        let graph = new Graph<_>(startNode, nodeComparer)
        let visited = new HashSet<'T>(nodeComparer : IEqualityComparer<'T>)
        
        let rec inner nodes depth = 
            if depth > maxDepth then async.Return(graph)
            else 
                List.iter (fun x -> visited.Add(x) |> ignore) nodes
                let asyncResult = nodes |> getNodes
                async { 
                    let! nodes = asyncResult
                    //TODO. chain method.
                    List.iter (fun x -> x.Edges |> List.iter (fun y -> graph.AddLink x.Node y)) nodes
                    let newFront = 
                        nodes
                        |> List.collect (fun x -> x.Edges)
                        |> List.filter (visited.Contains >> not)
                    return! inner newFront (depth + 1)
                }
        inner [ startNode ] 1
    
    let GetWikiGraph (startPage : String) maxDepth = 
        let api = WikiApi(HttpProxy())
        
        let ToNode(page : JsonPage) = 
            { Node = page.Title
              Edges = page.Links |> List.map (fun x -> x.Title) }
        
        let getNodes nodes = 
            let result = 
                nodes
                |> Seq.filter (String.IsNullOrWhiteSpace >> not)
                |> TitleQuery.Create
                |> Seq.map (api.GetLinks >> WikiApi.RunToCompletion)
                |> Async.Parallel
                |> Async.map (Seq.collect id
                              >> Seq.map ToNode
                              >> List.ofSeq)
            result
        
        GetGraph<String> getNodes StringComparer.InvariantCultureIgnoreCase startPage maxDepth
