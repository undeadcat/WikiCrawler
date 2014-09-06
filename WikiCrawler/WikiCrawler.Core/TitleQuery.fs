namespace WikiCrawler.Core

open System.Collections.Generic
open System.Collections

type TitleQuery private (titles : string seq) = 
    member __.Titles() = titles
    
    interface IEnumerable<string> with
        member __.GetEnumerator() : IEnumerator<string> = titles.GetEnumerator()
        member __.GetEnumerator() : IEnumerator = titles.GetEnumerator() :> IEnumerator
    
    static member Single(title : string) = TitleQuery([ title ])
    
    static member Create(titles : string seq) = 
        let titleLimit = 50
        let queryStringLimit = 4096
        
        let rec inner rem current count charCount acc = 
            let newQuery() = TitleQuery(List.rev current)
            
            let returnResult() = 
                if List.isEmpty current then acc
                else newQuery() :: acc
                     |> List.rev
            match rem with
            | [] -> returnResult()
            | x :: xs -> 
                if count = titleLimit || ((charCount + String.length x) > queryStringLimit && List.length current > 0) then 
                    inner xs [ x ] 1 (String.length x) (newQuery() :: acc)
                else inner xs (x :: current) (count + 1) (charCount + String.length x) acc
        inner (List.ofSeq titles) [] 0 0 []
    
    static member Titles(query : TitleQuery) = query.Titles()
