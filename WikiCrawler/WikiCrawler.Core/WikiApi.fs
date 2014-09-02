namespace WikiCrawler.Core

//TODO. batch
//TODO. batch url length.
//TODO. max results count.
module WikiApi = 
    open System.Net
    open System
    open System.Collections.Generic
    open Newtonsoft.Json
    open System.IO
    
    type UriBuilder(baseUri : String, query : (string * string) list) = 
        
        let queryToString() = 
            if List.isEmpty query then String.Empty
            else 
                query
                |> List.map (fun (key, value) -> sprintf "%s=%s" key value)
                |> fun x -> String.Join("&", x)
        
        member __.With(pair) = new UriBuilder(baseUri, (pair :: query))
        
        member __.ToUri() = 
            let (_, uri) = Uri.TryCreate(sprintf "%s?%s" baseUri (queryToString()), UriKind.RelativeOrAbsolute)
            uri
        
        new(baseUri) = UriBuilder(baseUri, [])
    
    type JsonLink() = 
        member val title = "" with get, set
    
    type JsonPage() = 
        member val links = ([||] : JsonLink []) with get, set
    
    type JsonQuery() = 
        member val pages = new Dictionary<string, JsonPage>() with get, set
    
    type JsonResponse() = 
        member val query = new JsonQuery() with get, set
    
    let baseUri = 
        UriBuilder("http://en.wikipedia.org/w/api.php", 
                   [ ("action", "query")
                     ("prop", "links")
                     ("redirects", "true")
                     ("format", "json")
                     ("pllimit", "500") ])
    
    let getWebRequest pageTitle = WebRequest.Create(baseUri.With("titles", pageTitle).ToUri()) :?> HttpWebRequest
    
    let getSerializer() = 
        let res = new JsonSerializer()
        res
    
    let serializer = getSerializer()
    
    let getLinks (response : Stream) = 
        serializer.Deserialize<JsonResponse>(new JsonTextReader(new StreamReader(response))).query.pages
        |> Seq.map (fun x -> x.Value)
        |> Seq.collect (fun x -> x.links)
        |> Seq.map (fun x -> x.title)
        |> Seq.toList
    
    let GetLinks(pageTitle : string) = 
        //TODO. catch exceptions
        //TODO. status code.
        async { 
            let req = getWebRequest pageTitle
            let! res = req.AsyncGetResponse()
            return res.GetResponseStream() |> getLinks
        }
