namespace WikiCrawler.Core

open System.Collections.Generic
open System

type Comparable<'T>(value : 'T, comparer : IComparer<'T>) = 
    member __.Value = value
    override this.Equals(obj) = 
        (this :> IComparable).CompareTo(obj) = 0
    
    interface IComparable with
        member this.CompareTo(obj : obj) : int = 
            match obj with
            | :? Comparable<'T> as t -> (this :> IComparable<Comparable<'T>>).CompareTo(t)
            | _ -> -1
    
    interface IComparable<Comparable<'T>> with
        member this.CompareTo(obj : Comparable<'T>) : int = comparer.Compare(this.Value, obj.Value)
