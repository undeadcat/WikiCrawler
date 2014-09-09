namespace WikiCrawler.Core

open System

[<AutoOpen>]
module Common = 
    let inline flip f a b = f b a
    let inline curry f a b = f (a, b)
    let inline uncurry f (a, b) = f a b
    let inline swap (a, b) = (b, a)
    let inline toString x = x.ToString()
    
    type String with
        member this.EqualsIgnoringCase(other) = this.Equals(other, StringComparison.InvariantCultureIgnoreCase)

[<AutoOpen>]
module Async = 
    let map f x = async.Bind(x, fun value -> async.Return(f value))
