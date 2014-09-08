namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core
open WikiCrawler.Core.WikiApi
open System.Net
open System.IO
open System.Text
open System
open WikiCrawler.Tests.Helpers
open System.Collections.Generic

type SetupBuilder(conditions : (HttpWebRequest -> bool) list, result : HttpWebResponseWrapper Option) = 
    
    let parseQuery (query : String) = 
        let nvc = new Dictionary<string, string>()
        WebUtility.UrlDecode(query).TrimStart('?').Split('&')
        |> Array.map (fun x -> x.Split('='))
        |> Array.iter (fun x -> nvc.Add(x.[0], x.[1]))
        nvc
    
    let getResponse (statusCode, stringBody : string) = 
        Some({ StatusCode = statusCode
               Content = new MemoryStream(Encoding.UTF8.GetBytes(stringBody)) })
    
    new() = SetupBuilder([], None)
    
    member __.QueryContains(key, value) = 
        let condition (x : HttpWebRequest) = 
            let parsed = parseQuery (x.RequestUri.Query)
            let (success, actual) = parsed.TryGetValue(key)
            success && actual.EqualsIgnoringCase(value)
        new SetupBuilder(condition :: conditions, result)
    
    member __.Returns(statusCode, content) = SetupBuilder(conditions, getResponse (statusCode, content))
    member __.ToExecuteHttpRequest() = 
        fun (req : HttpWebRequest) -> 
            if Option.isNone result then failwith ("Setup does not have result configured")
            elif List.forall (fun x -> x req) conditions then async.Return(Option.get result)
            else failwith (sprintf "Unmatched invocation with parameters: %s" (req.ToString()))

[<TestFixture>]
type WikiApiTests() = 
    
    let doTest (setup : SetupBuilder) titles = 
        WikiApi(setup.ToExecuteHttpRequest()).GetLinks(TitleQuery.Create titles |> Seq.nth 0)
        |> Async.sequence
        |> Async.RunSynchronously
        |> List.collect (Seq.toList)
    
    [<Test>]
    member __.InvalidStatusCode_ThrowException() = 
        let setup = SetupBuilder().Returns(HttpStatusCode.BadGateway, "")
        Assert.Throws<WebException, _>(fun () -> doTest setup [ "a" ])
    
    [<Test>]
    member __.SimpleGetLinks() = 
        let content = @"{
                        ""query"": {
                            ""pages"": {
                                ""6678"": {
                                    ""pageid"": 6678,
                                    ""ns"": 0,
                                    ""title"": ""Cat"",
                                    ""links"": [
                                        {
                                            ""ns"": 0,
                                            ""title"": ""Fat cat""
                                        }
                                    ]
                                },
                                ""6679"": {
                                    ""pageid"": 6679,
                                    ""ns"": 0,
                                    ""title"": ""Dog"",
                                    ""links"": [
                                        {
                                            ""ns"": 0,
                                            ""title"": ""Bad wolf""
                                        }
                                    ]
                                }
                            }
                        }
                    }"
        let setup = SetupBuilder().QueryContains("titles", "cat|dog").Returns(HttpStatusCode.OK, content)
        let actual = doTest setup [ "Cat"; "Dog" ]
        
        let expected = 
            [ { Title = "Cat"
                Links = [ { Title = "Fat cat" } ] }
              { Title = "Dog"
                Links = [ { Title = "Bad wolf" } ] } ]
        Assert.That(actual, Is.EqualTo(expected))
    
    [<Test>]
    member __.EmptyJsonArrayReturned_ReturnEmpty() = 
        let setup = SetupBuilder().Returns(HttpStatusCode.OK, "[]")
        Assert.That(doTest setup [ "a" ], Is.Empty)
    
    [<Test>]
    member __.Warning_ThrowException() = 
        let warnings = @"{
                            ""main"": {
                                ""*"": ""one""
                            },
                            ""query"": {
                                ""*"": ""two""
                            }
                        }"
        let setup = SetupBuilder().Returns(HttpStatusCode.OK, sprintf @"{""warnings"":%s }" warnings)
        Assert.Throws
            (Exception.OfType<WikiApiException>().WithMessage("Warnings: one; two"), (fun () -> doTest setup [ "a" ]))
    
    [<Test>]
    member __.Error_ThrowException() = 
        let errors = @"{
                            ""error"": {
                                ""code"": ""code"",
                                ""info"": ""info""
                            }
                        }"
        let setup = SetupBuilder().Returns(HttpStatusCode.OK, errors)
        Assert.Throws
            (Exception.OfType<WikiApiException>().WithMessage("Error: code info"), (fun () -> doTest setup [ "a" ]))
    
    [<Test>]
    member __.PageNotFound_ReturnEmpty() = 
        let res = @"{
                    ""query"": {
                        ""pages"": {
                            ""-1"": {
                                ""ns"": 0,
                                ""title"": ""Aaaaa"",
                                ""missing"": """"
                            }
                        }
                    }
                }"
        let setup = SetupBuilder().Returns(HttpStatusCode.OK, res)
        Assert.That(doTest setup [ "a" ], Is.Empty)
