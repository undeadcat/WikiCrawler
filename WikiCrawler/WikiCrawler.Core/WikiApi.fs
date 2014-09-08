namespace WikiCrawler.Core

open System.Net
open System
open System.Collections.Generic
open Newtonsoft.Json
open System.IO

module WikiApi = 
    //TODO. Canellation token.
    //TODO. return choice instead of throwing?
    type UriBuilder = WikiCrawler.Core.UriBuilder
    
    type private JsonLink() = 
        [<JsonProperty("title")>]
        member val Title = "" with get, set
    
    type private JsonPage() = 
        
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
        abstract GetBatch : unit -> (Page list * IResultCursor Option) Async
    
    type private ResultCursor(continuations : (String * String) list, getNextBatch : (String * String) Option -> JsonResponse Async) = 
        
        let ToPage(pair : KeyValuePair<int, JsonPage>) = 
            if pair.Key < 0 then NotFound(pair.Value.Title)
            else Page(pair.Value.Title, pair.Value.Links |> List.map (fun x -> { Title = x.Title }))
        
        interface IResultCursor with
            member __.GetBatch() : Async<Page list * Option<IResultCursor>> = 
                let (resultAsync, oldContinuations) = 
                    match continuations with
                    | [] -> (getNextBatch None, [])
                    | (contKey, contValue) :: rest -> (getNextBatch (Some(contKey, contValue)), rest)
                async { 
                    let! result = resultAsync
                    let continuations = 
                        result.ContinueData.Links
                        |> Seq.map (fun x -> (x.Key, x.Value))
                        |> List.ofSeq
                        |> (@) oldContinuations
                    
                    let items = 
                        result.Query.Pages
                        |> Seq.map ToPage
                        |> List.ofSeq
                    
                    return if Seq.isEmpty continuations then (items, None)
                           else (items, Some(ResultCursor(continuations, getNextBatch) :> IResultCursor))
                }
    
    type WikiApi(executeRequest : IExecuteHttpRequest) = 
        
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
                let deserialized = 
                    serializer.Deserialize<JsonResponse>(new JsonTextReader(new StreamReader(res.Content)))
                if not (String.IsNullOrWhiteSpace deserialized.Error.Code) then 
                    raise (new WikiApiException(sprintf "Error: %s %s" deserialized.Error.Code deserialized.Error.Info))
                if deserialized.Warnings.Values.Count > 0 then 
                    deserialized.Warnings.Values
                    |> Seq.collect (fun x -> x.Values)
                    |> fun x -> raise (new WikiApiException("Warnings: " + String.Join("; ", x)))
                return deserialized
            }
        
        member __.GetLinks(pageTitle : TitleQuery) = new ResultCursor([], performRequest pageTitle) :> IResultCursor
        static member RunToCompletion cursor = 
            let rec inner (cursor : IResultCursor) (res : Page seq) = 
                async { 
                    let! (pages, continuation) = cursor.GetBatch()
                    let newPages = Seq.append res pages
                    match continuation with
                    | None -> return newPages
                    | Some(cursor) -> return! inner cursor newPages
                }
            inner cursor []
