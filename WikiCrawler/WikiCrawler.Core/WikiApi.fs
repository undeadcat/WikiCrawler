namespace WikiCrawler.Core

open System.Net
open System
open System.Collections.Generic
open Newtonsoft.Json
open System.IO

module WikiApi = 
    //TODO. compression. how to enable
    //TODO. Canellation token.
    //TODO. return choice instead of throwing
    type UriBuilder = WikiCrawler.Core.UriBuilder
    
    type private JsonContinueData() = 
        [<JsonProperty("links")>]
        member val Links = new Dictionary<string, string>() with get, set
    
    type private JsonQuery() = 
        
        [<JsonProperty("query-continue")>]
        member val ContinueData = new JsonContinueData() with get, set
        
        [<JsonProperty("pages")>]
        member val Pages = new Dictionary<string, Page>() with get, set
    
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
    
    type private Cursor(getNextBatch : (string * string) Option -> JsonQuery Async) = 
        let mutable isFirstBatch = true
        let mutable continuations : (string * string) list = []
        
        let getCurrent() : Page seq Async = 
            let getPages (x : JsonQuery) = x.Pages.Values :> Page seq
            isFirstBatch <- false
            match continuations with
            | [] -> getNextBatch None |> Async.map getPages
            | (contKey, contValue) :: rest -> 
                async { 
                    let! queryResult = getNextBatch (Some(contKey, contValue))
                    continuations <- queryResult.ContinueData.Links
                                     |> Seq.map (fun x -> (x.Key, x.Value))
                                     |> List.ofSeq
                                     |> (@) rest
                    return getPages queryResult
                }
        
        let hasMore() = 
            isFirstBatch || not (List.isEmpty continuations)
        interface IEnumerator<Page seq Async> with
            member __.MoveNext() = hasMore()
            member __.Current : IEnumerable<Page> Async = getCurrent()
            member __.Current : obj = getCurrent() :> obj
            member __.Reset() : unit = raise (new NotSupportedException())
            member __.Dispose() = ignore()
    
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
        
        let createWebRequest (pageTitles : TitleQuery) (continuation : (String * String) option) = 
            let uri = 
                match continuation with
                | None -> baseUri
                | Some(key, value) -> baseUri.With(key, value)
                |> fun x -> x.With("titles", String.Join("|", pageTitles))
            
            let req = WebRequest.Create(uri.ToUri()) :?> HttpWebRequest
            req.UserAgent <- "WikiCrawler/0.1 (WikiCrawler)"
            req.CookieContainer <- cookieContainer
            req.AutomaticDecompression <- DecompressionMethods.Deflate ||| DecompressionMethods.GZip
            req
        
        let performRequest (pageTitle : TitleQuery) (continuation : (String * String) option) = 
            async { 
                let req = createWebRequest pageTitle continuation
                let! res = executeRequest req
                if res.StatusCode <> HttpStatusCode.OK then 
                    return raise (new WebException(sprintf "Invalid status code: %s" (res.StatusCode.ToString())))
                let deserialized = 
                    serializer.Deserialize<JsonResponse>(new JsonTextReader(new StreamReader(res.Content)))
                if not (String.IsNullOrWhiteSpace deserialized.Error.Code) then 
                    raise (new WikiApiException(sprintf "Error: %s %s" deserialized.Error.Code deserialized.Error.Info))
                if deserialized.Warnings.Values.Count > 0 then 
                    deserialized.Warnings.Values
                    |> Seq.collect (fun x -> x.Values)
                    |> fun x -> raise (new WikiApiException("Warnings: " + String.Join("; ", x)))
                return deserialized.Query
            }
        
        member __.GetLinks(pageTitle : TitleQuery) = 
            { new IEnumerable<Page seq Async> with
                  member this.GetEnumerator() : Collections.IEnumerator = 
                      (this :> IEnumerable<_>).GetEnumerator() :> Collections.IEnumerator
                  member __.GetEnumerator() : IEnumerator<Page seq Async> = 
                      new Cursor(performRequest pageTitle) :> IEnumerator<_> }
        
        new() = 
            let executeHttpRequest (req : HttpWebRequest) = 
                req.AsyncGetResponse()
                |> Async.map (fun x -> x :?> HttpWebResponse)
                |> Async.map (fun x -> 
                       { StatusCode = x.StatusCode
                         Content = x.GetResponseStream() })
            new WikiApi(executeHttpRequest)
