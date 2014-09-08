namespace WikiCrawler.Core

//TODO. can get different case.
//TODO. can get duplicates from api.
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
        async.Return (Graph(""))
    
    let GetWikiGraph() = async.Return (Graph(""))
