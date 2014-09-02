namespace WikiCrawler.Core

[<AutoOpen>]
module Common = 
    let inline flip f a b = f b a
    let inline curry f a b = f (a, b)
    let inline uncurry f (a, b) = f a b
    let inline swap (a, b) = (b, a)
