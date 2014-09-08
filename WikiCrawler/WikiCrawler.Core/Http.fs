namespace WikiCrawler.Core

open System.Net
open System.IO

type HttpWebResponseWrapper = 
    { StatusCode : HttpStatusCode
      Content : Stream }

type IExecuteHttpRequest = 
    abstract Execute : HttpWebRequest -> HttpWebResponseWrapper Async

type HttpProxy() = 
    interface IExecuteHttpRequest with
        member __.Execute(req : HttpWebRequest) : Async<HttpWebResponseWrapper> = 
            req.AsyncGetResponse()
            |> Async.map (fun x -> x :?> HttpWebResponse)
            |> Async.map (fun x -> 
                   { StatusCode = x.StatusCode
                     Content = x.GetResponseStream() })
