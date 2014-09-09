namespace WikiCrawler.Core

open System.Net
open System
open System.Collections.Generic
open Newtonsoft.Json
open System.IO

module WikiApi = 
    type UriBuilder = WikiCrawler.Core.UriBuilder
    
    type JsonLink() = 
        [<JsonProperty("title")>]
        member val Title = "" with get, set
    
    type JsonPage() = 
        
        [<JsonProperty("title")>]
        member val Title = "" with get, set
        
        [<JsonProperty("links")>]
        member val Links = [] : JsonLink list with get, set
    
    type private JsonContinueData() = 
        [<JsonProperty("links")>]
        member val Links = new Dictionary<string, string>() with get, set
    
    type private JsonQuery() = 
        [<JsonProperty("pages")>]
        member val Pages = new Dictionary<int, JsonPage>() with get, set
    
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
        
        [<JsonProperty("query-continue")>]
        member val ContinueData = new JsonContinueData() with get, set
        
        [<JsonProperty("query")>]
        member val Query = new JsonQuery() with get, set
    
    type WikiApiException(message : string) = 
        inherit Exception(message)
    
    type IResultCursor = 
        abstract GetBatch : unit -> (JsonPage list * IResultCursor Option) Async
    
    type private ResultCursor(continuations : (String * String) list, getNextBatch : (String * String) Option -> (JsonContinueData * JsonPage list) Async) = 
        
        interface IResultCursor with
            member __.GetBatch() : Async<JsonPage list * Option<IResultCursor>> = 
                let (resultAsync, oldContinuations) = 
                    match continuations with
                    | [] -> (getNextBatch None, [])
                    | (contKey, contValue) :: rest -> (getNextBatch (Some(contKey, contValue)), rest)
                async { 
                    let! (continueData, pages) = resultAsync
                    let continuations = 
                        continueData.Links
                        |> Seq.map (fun x -> (x.Key, x.Value))
                        |> List.ofSeq
                        |> (@) oldContinuations
                    
                    return if Seq.isEmpty continuations then (pages, None)
                           else (pages, Some(ResultCursor(continuations, getNextBatch) :> IResultCursor))
                }
    
    type WikiApi(executeRequest : IExecuteHttpRequest) = 
        
        let baseUri = 
            UriBuilder("http://en.wikipedia.org/w/api.php", 
                       [ ("action", "query")
                         ("prop", "links")
                         ("format", "json")
                         ("plnamespace", "0")
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
            req.KeepAlive <- true
            req.Accept <- "*/*"
            req.Headers.Item("Cache-Control")<-"max-age=5"
            req.AutomaticDecompression <- DecompressionMethods.Deflate ||| DecompressionMethods.GZip
            req

        let performRequest (pageTitle : TitleQuery) (continuation : (String * String) option) = 
            async { 
                let req = createWebRequest pageTitle continuation
                let! res = executeRequest.Execute req
                if res.StatusCode <> HttpStatusCode.OK then 
                    return raise (new WebException(sprintf "Invalid status code: %s" (res.StatusCode.ToString())))
                let response = 
                    serializer.Deserialize<JsonResponse>(new JsonTextReader(new StreamReader(res.Content)))
                if not (String.IsNullOrWhiteSpace response.Error.Code) then 
                    raise (new WikiApiException(sprintf "Error: %s %s" response.Error.Code response.Error.Info))
                if response.Warnings.Values.Count > 0 then 
                    response.Warnings.Values
                    |> Seq.collect (fun x -> x.Values)
                    |> fun x -> raise (new WikiApiException("Warnings: " + String.Join("; ", x)))
                let continueData = response.ContinueData
                let pages = response.Query.Pages 
                                |> Seq.filter (fun x->x.Key> 0) 
                                |> Seq.map (fun x->x.Value)
                                |> List.ofSeq
                return (continueData,pages)
            }
        
        member __.GetLinks(pageTitle : TitleQuery) = new ResultCursor([], performRequest pageTitle) :> IResultCursor
        static member RunToCompletion cursor = 
            let rec inner (cursor : IResultCursor) (res : JsonPage seq) = 
                async { 
                    let! (pages, continuation) = cursor.GetBatch()
                    let newPages = Seq.append res pages
                    match continuation with
                    | None -> return newPages
                    | Some(cursor) -> return! inner cursor newPages
                }
            inner cursor []
