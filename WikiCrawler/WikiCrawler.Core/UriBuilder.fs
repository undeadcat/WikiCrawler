namespace WikiCrawler.Core

open System

type UriBuilder(baseUri : String, query : (string * string) list) = 
    
    let queryToString() = 
        if List.isEmpty query then String.Empty
        else 
            query
            |> List.map (fun (key, value) -> sprintf "%s=%s" key value)
            |> fun x -> String.Join("&", x)
    
    member __.With(pair) = new UriBuilder(baseUri, (pair :: query))
    
    member __.ToUri() = 
        let (success, uri) = Uri.TryCreate(sprintf "%s?%s" baseUri (queryToString()), UriKind.RelativeOrAbsolute)
        if not success then raise (new Exception "Could not create Uri from string")
        uri
    
    new(baseUri) = UriBuilder(baseUri, [])
