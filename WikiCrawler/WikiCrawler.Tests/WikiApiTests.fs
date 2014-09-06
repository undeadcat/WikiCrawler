namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core
open WikiCrawler.Core.WikiApi
open System.Net
open System.IO
open System.Text
open System

[<TestFixture>]
type WikiApiTests() = 
    
    let doTest setup titles = 
        let matchSetup req = 
            match setup req with
            | None -> failwith (sprintf "Unmatched invocation with parameters: %s" (req.ToString()))
            | Some(v) -> async.Return(v)
        WikiApi(matchSetup).GetLinks(TitleQuery.Create titles |> Seq.nth 0) |> Async.RunSynchronously
    
    let getResponse (statusCode, stringBody : string) = 
        Some({ StatusCode = statusCode
               Content = new MemoryStream(Encoding.UTF8.GetBytes(stringBody)) })
    
    let queryStringContains key expectedValue response (req : HttpWebRequest) = 
        if req.RequestUri.Query.IndexOf(key + "=" + expectedValue, StringComparison.OrdinalIgnoreCase) > 0 then response
        else None
    
    [<Test>]
    member __.InvalidStatusCode_ThrowException() = 
        Assert.Throws<WebException>
            (fun () -> doTest (fun _ -> getResponse (HttpStatusCode.BadGateway, "")) [ "a" ] |> ignore) |> ignore
    
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
        let setup = queryStringContains "titles" "cat|dog" (getResponse (HttpStatusCode.OK, content))
        Assert.That(doTest setup [ "Cat"; "Dog" ], Is.EqualTo [ "Fat cat" ])
    
    [<Test>]
    member __.EmptyJsonArrayReturned_ReturnEmpty() = 
        let setup _ = getResponse (HttpStatusCode.OK, "[]")
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
        let setup _ = getResponse (HttpStatusCode.OK, sprintf @"{""warnings"":%s }" warnings)
        Assert.Throws
            (Exception.OfType<WikiApiException>().WithMessage("Warnings: one; two"), 
             (fun () -> doTest setup [ "a" ] |> ignore)) |> ignore
    
    [<Test>]
    member __.Error_ThrowException() = 
        let errors = @"{
                            ""error"": {
                                ""code"": ""code"",
                                ""info"": ""info""
                            }
                        }"
        let setup _ = getResponse (HttpStatusCode.OK, errors)
        Assert.Throws
            (Exception.OfType<WikiApiException>().WithMessage("Error: code info"), 
             (fun () -> doTest setup [ "a" ] |> ignore)) |> ignore
    
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
        Assert.That(doTest (fun _ -> getResponse (HttpStatusCode.OK, res)) [ "a" ], Is.Empty)
