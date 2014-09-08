namespace WikiCrawler.Core

open Newtonsoft.Json

type Link = 
    { [<JsonProperty("title")>]
      Title : string }

type Page = 
    { [<JsonProperty("title")>]
      Title : string
      [<JsonProperty("links")>]
      Links : Link list }
