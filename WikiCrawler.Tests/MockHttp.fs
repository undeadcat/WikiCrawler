namespace WikiCrawler.Tests

open System.Net
open System.IO
open System.Text
open System
open WikiCrawler.Core
open System.Collections.Generic
open Foq

type HttpSetupBuilder(condition, result : HttpWebResponseWrapper option) = 
    
    let parseQuery (query : String) = 
        let nvc = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        query.TrimStart('?').Split('&')
        |> Array.map (fun x -> x.Split('='))
        |> Array.iter (fun x -> nvc.Add(WebUtility.UrlDecode(x.[0]), WebUtility.UrlDecode(x.[1])))
        nvc
    
    let getResponse (statusCode, stringBody : string) = 
        { StatusCode = statusCode
          Content = new MemoryStream(Encoding.UTF8.GetBytes(stringBody)) }
    
    member __.Condition = condition
    member __.Result = result
    
    member __.QueryContains(key, value) = 
        let newCondition (req : HttpWebRequest) = 
            let (success, actual) = parseQuery(req.RequestUri.Query).TryGetValue(key)
            let res = success && actual.EqualsIgnoringCase(value)
            res
        HttpSetupBuilder((fun x -> condition x && newCondition x), result)
    
    member __.QueryNotContains(key) = 
        let newCondition (req : HttpWebRequest) = not (parseQuery(req.RequestUri.Query).ContainsKey(key))
        HttpSetupBuilder((fun x -> condition x && newCondition x), result)
    
    member __.Returns(statusCode, responseBodu) = 
        HttpSetupBuilder(condition, Some(getResponse (statusCode, responseBodu)))
    member this.ReturnsOk(body) = this.Returns(HttpStatusCode.OK, body)

module It = 
    let IsAny() = HttpSetupBuilder((fun _ -> true), None)
    let IsQueryContaining(tuple) = IsAny().QueryContains(tuple)

type MockHttp(moq : Mock<IExecuteHttpRequest>) = 
    member __.Moq = moq
    
    member __.Setup(httpSetupBuilder : HttpSetupBuilder) = 
        match httpSetupBuilder.Result with
        | None -> raise (new Exception "No result setup")
        | Some(res) -> 
            MockHttp(moq.Setup(fun x -> <@ x.Execute(It.Is(httpSetupBuilder.Condition)) @>).Returns(async.Return(res)))
    
    new() = MockHttp(Mock<IExecuteHttpRequest>(MockMode.Strict))
