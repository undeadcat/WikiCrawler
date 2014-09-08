namespace WikiCrawler.Tests

open Foq
open NUnit.Framework
open System

type IFoo = 
    abstract Bar : int -> int

[<TestFixture>]
type Class1() = 
    [<Test>]
    member this.Test1() = 
        let mock = Mock<IFoo>(MockMode.Strict)
        let obj = 
            mock.Setup(fun x -> <@ x.Bar(It.Is((<) 10)) @>).Returns(5)
                .Setup(fun x -> <@ x.Bar(It.Is((=) 0)) @>).Returns(0).Create()
        Console.WriteLine(obj.Bar(5))
        Console.WriteLine(obj.Bar(0))
