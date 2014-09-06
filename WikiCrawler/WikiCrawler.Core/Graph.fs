namespace WikiCrawler.Core

//TODO. Async.Parallel?
//TODO. update partial graph
module Graph = 
    type Graph<'T when 'T : comparison>(root, adjacent : Map<'T, 'T list>) = 
        
        let getValue value = 
            match Map.tryFind value adjacent with
            | None -> []
            | Some(v) -> v
        
        member __.AddLink one two = 
            let oneAdj = getValue one
            let twoAdj = getValue two
            Graph(root, adjacent.Add(one, two :: oneAdj).Add(two, twoAdj))
        
        member __.Adjacent = 
            adjacent
            |> Map.toList
            |> List.map (fun (x, y) -> (x, List.rev y))
        
        member __.Root = root
        new(root) = Graph(root, Map([ (root, []) ]))
    
    let GetGraph (getLinks : 'T -> 'T list Async) (startPage : 'T) (maxDepth : int) = 
        let rec inner depth (graph : Graph<_>) (visited : Set<_>) page = 
            if Set.contains page visited || depth > maxDepth then async.Return(graph, visited)
            else 
                async { 
                    let! links = getLinks (page)
                    let newGraph = links |> List.fold (fun (g : Graph<_>) i -> g.AddLink page i) graph
                    let folder (graph, visited) item = inner (depth + 1) graph visited item
                    return! Async.foldM folder (newGraph, visited.Add(page)) links
                }
        inner 1 (Graph<'T>(startPage)) (Set<'T>([])) startPage |> Async.map fst
    
    let GetWikiGraph = GetGraph(TitleQuery.Single >> WikiApi.WikiApi().GetLinks)
