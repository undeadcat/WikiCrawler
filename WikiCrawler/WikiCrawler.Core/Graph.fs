namespace WikiCrawler.Core

//TODO. can get duplicates from api. dict values need to be sets.
//TODO. Redirects.
open System.Collections.Concurrent
open WikiCrawler.Core.WikiApi
open System.Collections.Generic
open System

type NodeWithEdges<'T> = 
    { Node : 'T
      Edges : 'T list }

type Graph<'T when 'T : comparison> private (adjacent : Map<Comparable<'T>, Comparable<'T> Set>, comparer : IComparer<'T>) = 
    
    let getValue key = 
        match Map.tryFind key adjacent with
        | None -> Set([])
        | Some(v) -> v
    
    new(comparer) = Graph(Map<_, _>([]), comparer)
    
    member __.AddNode(node) = 
        let node = Comparable(node, comparer)
        Graph(adjacent.Add(node, Set([])), comparer)
    
    member __.AddLink(one : 'T, two : 'T) = 
        let one = Comparable(one, comparer)
        let two = Comparable(two, comparer)
        let oneAdj = getValue one
        let twoAdj = getValue two
        Graph(adjacent.Add(one, oneAdj.Add(two)).Add(two, twoAdj), comparer)
    
    member __.Adjacent = 
        adjacent
        |> Seq.map (fun pair -> 
               (pair.Key.Value, 
                pair.Value
                |> Set.toList
                |> List.map (fun x -> x.Value)))
        |> List.ofSeq

module GraphModule = 
    let GetGraph<'T when 'T : comparison> (getNodes : 'T list -> NodeWithEdges<'T> list Async) (nodeComparer) 
        (startNode : 'T) (maxDepth : int) = 
        let rec inner (graph : Graph<'T>) (visited : Set<'T>) nodes depth = 
            if depth > maxDepth || Seq.isEmpty nodes then async.Return(graph)
            else 
                async { 
                    let! nodes = nodes |> getNodes
                    let processNode (graph : Graph<'T>) (node : NodeWithEdges<'T>) = 
                        node.Edges 
                        |> List.fold (fun g i -> (g : Graph<'T>).AddLink(node.Node, i)) (graph.AddNode(node.Node))
                    let newGraph = List.fold processNode graph nodes
                    let newVisited = nodes |> List.fold (fun v i -> Set.add i.Node v) visited
                    
                    let newFront = 
                        nodes
                        |> List.collect (fun x -> x.Edges)
                        |> List.filter (newVisited.Contains >> not)
                    return! inner newGraph newVisited newFront (depth + 1)
                }
        inner (Graph(nodeComparer)) (Set([])) [ startNode ] 1
    
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
