namespace WikiCrawler.Core

open System.Net
open System
open System.Collections.Generic
open Newtonsoft.Json
open System.IO

module WikiApi = 
    //TODO. continuations
    //TODO. compression. how to enable
    //TODO. Canellation token.
    //TODO. return choice?
    type UriBuilder = WikiCrawler.Core.UriBuilder
    
    type JsonLink() = 
        [<JsonProperty("title")>]
        member val Title = "" with get, set
    
    type JsonPage() = 
        [<JsonProperty("links")>]
        member val Links = ([||] : JsonLink []) with get, set
    
    type private JsonQuery() = 
        [<JsonProperty("pages")>]
        member val Pages = new Dictionary<string, JsonPage>() with get, set
    
    type private JsonError() = 
        
        [<JsonProperty("code")>]
        member val Code = "" with get, set
        
        [<JsonProperty("info")>]
        member val Info = "" with get, set
    
    type private JsonResponse() = 
        
        [<JsonProperty("error")>]
        member val Error = new JsonError() with get, set
        
        [<JsonProperty("warnings")>]
        member val Warnings = new Dictionary<string, Dictionary<string, string>>() with get, set
        
        [<JsonProperty("query")>]
        member val Query = new JsonQuery() with get, set
    
    type HttpWebResponseWrapper = 
        { StatusCode : HttpStatusCode
          Content : Stream }
    
    type WikiApiException(message : string) = 
        inherit Exception(message)
    
    type WikiApi(executeRequest : HttpWebRequest -> HttpWebResponseWrapper Async) = 
        
        let baseUri = 
            UriBuilder("http://en.wikipedia.org/w/api.php", 
                       [ ("action", "query")
                         ("prop", "links")
                         ("redirects", "true")
                         ("format", "json")
                         ("pllimit", "500") ])
        
        let serializer = new JsonSerializer()
        let cookieContainer = new CookieContainer()
        
        let createWebRequest (pageTitles : TitleQuery) = 
            let req = WebRequest.Create(baseUri.With("titles", String.Join("|", pageTitles)).ToUri()) :?> HttpWebRequest
            req.UserAgent <- "WikiCrawler/0.1 (WikiCrawler)"
            req.CookieContainer <- cookieContainer
            req.AutomaticDecompression <- DecompressionMethods.Deflate ||| DecompressionMethods.GZip
            req
        
        let deserialize (response : Stream) = 
            serializer.Deserialize<JsonResponse>(new JsonTextReader(new StreamReader(response)))
        
        member __.GetLinks(pageTitle : TitleQuery) = 
            async { 
                let req = createWebRequest pageTitle
                let! res = executeRequest req
                if res.StatusCode <> HttpStatusCode.OK then 
                    return raise (new WebException(sprintf "Invalid status code: %s" (res.StatusCode.ToString())))
                let deserialized = res.Content |> deserialize
                if not (String.IsNullOrWhiteSpace deserialized.Error.Code) then 
                    raise (new WikiApiException(sprintf "Error: %s %s" deserialized.Error.Code deserialized.Error.Info))
                if deserialized.Warnings.Values.Count > 0 then 
                    deserialized.Warnings.Values
                    |> Seq.collect (fun x -> x.Values)
                    |> fun x -> raise (new WikiApiException("Warnings: " + String.Join("; ", x)))
                return deserialized.Query.Pages.Values
                       |> Seq.toList
            }
        
        new() = 
            let executeHttpRequest (req : HttpWebRequest) = 
                req.AsyncGetResponse()
                |> Async.map (fun x -> x :?> HttpWebResponse)
                |> Async.map (fun x -> 
                       { StatusCode = x.StatusCode
                         Content = x.GetResponseStream() })
            new WikiApi(executeHttpRequest)
