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

type Graph<'T when 'T : comparison> private (root : 'T, adjacent : Map<'T, 'T Set>, keyComparer : IEqualityComparer<'T>) = 
    
    let getValue key = 
        match Map.tryFind key adjacent with
        | None -> Set([])
        | Some(v) -> v
    
    new(root, comparer) = Graph(root, Map([]), comparer)
    
    member __.AddLink(one, two) = 
        let oneValue = getValue one
        let twoValue = getValue two
        Graph(root, adjacent.Add(one, oneValue.Add(two)).Add(two, twoValue), keyComparer)
    
    member __.Adjacent = 
        adjacent
        |> Seq.map (fun pair -> (pair.Key, Set.toList pair.Value))
        |> List.ofSeq
    
    member __.Root = root

module GraphModule = 
    let GetGraph<'T when 'T : comparison> (getNodes : 'T list -> NodeWithEdges<'T> list Async) (nodeComparer) 
        (startNode : 'T) (maxDepth : int) = 
        let rec inner (graph : Graph<'T>) (visited : Set<'T>) nodes depth = 
            if depth > maxDepth || Seq.isEmpty nodes then async.Return(graph)
            else 
                async { 
                    let! nodes = nodes |> getNodes
                    let newGraph = 
                        nodes
                        |> List.collect (fun x -> List.map (fun e -> (x.Node, e)) x.Edges)
                        |> List.fold (fun g i -> (g : Graph<'T>).AddLink(i)) graph
                    
                    let newVisited = nodes |> List.fold (fun v i -> Set.add i.Node v) visited
                    
                    let newFront = 
                        nodes
                        |> List.collect (fun x -> x.Edges)
                        |> List.filter (newVisited.Contains >> not)
                    return! inner newGraph newVisited newFront (depth + 1)
                }
        inner (Graph(startNode, nodeComparer)) (Set([])) [ startNode ] 1
    
    let GetWikiGraph (startPage : String) maxDepth = 
        let api = WikiApi(HttpProxy())
        
        let ToNode(page : JsonPage) = 
            { Node = page.Title
              Edges = page.Links |> List.map (fun x -> x.Title) }
        
        let getNodes nodes = 
            nodes
            |> Seq.filter (String.IsNullOrWhiteSpace >> not)
            |> TitleQuery.Create
            |> Seq.map (api.GetLinks >> WikiApi.RunToCompletion)
            |> Async.Parallel
            |> Async.map (Seq.collect id
                          >> Seq.map ToNode
                          >> List.ofSeq)
        
        GetGraph<String> getNodes StringComparer.InvariantCultureIgnoreCase startPage maxDepth
