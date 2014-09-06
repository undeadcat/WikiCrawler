namespace WikiCrawler.Tests

open NUnit.Framework.Constraints

module Exception = 
    type ExceptionConstraint(constraints : Constraint list) = 
        
        interface IResolveConstraint with
            member __.Resolve() : Constraint = 
                match List.rev constraints with
                    | [] -> failwith "Impossible"
                    | x::xs -> List.fold (fun x y -> new AndConstraint(x, y) :> Constraint) x xs
        
        member __.WithMessage(message) = 
            ExceptionConstraint
                (new PropertyConstraint("Message", new EqualConstraint(message)) :> Constraint :: constraints)
    
    let OfType<'T>() = 
        new ExceptionConstraint([ new ExactTypeConstraint(typeof<'T>)
                                  new NotConstraint(new NullConstraint()) ])
