namespace WikiCrawler.Tests

open NUnit.Framework.Constraints

module Helpers = 
    type ExceptionConstraint(constraints : Constraint list) = 
        
        interface IResolveConstraint with
            member __.Resolve() : Constraint = 
                List.fold (fun x y -> new AndConstraint(x, y) :> Constraint) (List.head constraints) 
                    (List.tail constraints)
        
        member __.WithMessage(message) = 
            ExceptionConstraint
                (new PropertyConstraint("Message", new EqualConstraint(message)) :> Constraint :: constraints)
    
    type Exception() = 
        static member OfType<'T>() = 
            new ExceptionConstraint([ new ExactTypeConstraint(typeof<'T>)
                                      new NotConstraint(new NullConstraint()) ])
    
    type Assert() = 
        inherit NUnit.Framework.Assert()
        static member Throws<'TExn, 'TRes when 'TExn :> exn>(f : unit -> 'TRes) = 
            Assert.Throws<'TExn>(fun () -> f() |> ignore) |> ignore
        static member Throws(exnConstraint : IResolveConstraint, f : unit -> 'TRes) = 
            Assert.Throws(exnConstraint, fun () -> f() |> ignore) |> ignore
