namespace WikiCrawler.Tests

open NUnit.Framework
open WikiCrawler.Core
open WikiCrawler.Core.WikiApi
open System.Net
open WikiCrawler.Tests.Helpers

[<TestFixture>]
type WikiApiTests() = 
    
    let doTest (mockHttp : MockHttp) titles = 
        WikiApi(mockHttp.Moq.Create()).GetLinks(TitleQuery.Create titles |> Seq.nth 0)
        |> WikiApi.RunToCompletion
        |> Async.RunSynchronously
    
    [<Test>]
    member __.InvalidStatusCode_ThrowException() = 
        let setup = MockHttp().Setup(It.IsAny().Returns(HttpStatusCode.BadGateway, ""))
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
        let setup = MockHttp().Setup(It.IsQueryContaining("titles", "cat|dog").Returns(HttpStatusCode.OK, content))
        let actual = doTest setup [ "Cat"; "Dog" ]
        
        let expected = [ Page( "Cat",[ { Title = "Fat cat" } ]); Page("Dog",[ { Title = "Bad wolf" } ]) ]
        Assert.That(actual, Is.EqualTo(expected))
    
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
        let setup = MockHttp().Setup(It.IsAny().Returns(HttpStatusCode.OK, sprintf @"{""warnings"":%s }" warnings))
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
        let setup = MockHttp().Setup(It.IsAny().Returns(HttpStatusCode.OK, errors))
        Assert.Throws
            (Exception.OfType<WikiApiException>().WithMessage("Error: code info"), (fun () -> doTest setup [ "a" ]))
    
    [<Test>]
    member __.PageNotFound_ReturnNotFound() = 
        let res = @"{
                    ""query"": {
                        ""pages"": {
                            ""-1"": {
                                ""ns"": 0,
                                ""title"": ""a"",
                            }
                        }
                    }
                }"
        let setup = MockHttp().Setup(It.IsAny().Returns(HttpStatusCode.OK, res))
        let actual = doTest setup [ "a" ]
        Assert.That(actual, Is.EqualTo([NotFound("a")]))
    
    [<Test>]
    member __.Continuation() = 
        let request1 = @"{
                        ""query-continue"":{
                            ""links"":{
                                ""continueKey"":""continueValue""
                            }
                        },
                        ""query"": {
                            ""pages"": {
                                ""6678"": {
                                    ""pageid"": 6678,
                                    ""ns"": 0,
                                    ""title"": ""firstRequest"",
                                    ""links"": [
                                        {
                                            ""ns"": 0,
                                            ""title"": ""link1""
                                        }
                                    ]
                                }
                            }
                        }
                    }"
        let request2 = @"{
            ""query"": {
                ""pages"": {
                    ""6678"": {
                        ""pageid"": 6678,
                        ""ns"": 0,
                        ""title"": ""secondRequest"",
                        ""links"": [
                            {
                                ""ns"": 0,
                                ""title"": ""link2""
                            }
                        ]
                    }
                }
            }
        }"
        let query = It.IsQueryContaining("titles", "cat")
        let setup = 
            MockHttp().Setup(query.QueryNotContains("continueKey").Returns(HttpStatusCode.OK, request1))
                .Setup(query.QueryContains("continueKey", "continueValue").Returns(HttpStatusCode.OK, request2))
        let actual = doTest setup [ "cat" ]
        
        let expected = 
            [ Page("firstRequest",[ { Title = "link1" } ]);Page("secondRequest",[ { Title = "link2" } ]) ]
        Assert.That(actual, Is.EqualTo(expected))
