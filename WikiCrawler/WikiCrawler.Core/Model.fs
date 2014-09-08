namespace WikiCrawler.Core

type Link = 
    { Title : string }

type Page = 
    | NotFound of string
    | Page of string * Link list
