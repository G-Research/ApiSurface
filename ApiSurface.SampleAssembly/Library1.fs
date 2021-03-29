namespace ApiSurface.SampleAssembly

type SampleDU =
    | Foo
    | Bar of int

type Class1 (something : int) =

    let another = something

    member private _.PrivateProperty = another

    member _.X = "F#"

    member _.AddTwo s = s + 2

    member val Y = "hi" with get, set

type SampleInterface =
    abstract ImplementMe : unit -> unit
    abstract InterfaceProperty : int with get, set

type InterfaceInheriter =
    inherit SampleInterface
    abstract AnotherMethod : unit -> int

module Module1 =

    let private privateFunction x = not x

    let publicAnonymousRecord =
        {|
            Foo = 1
        |}

    let publicFunction () =

        let privateAnonymousRecord =
            {|
                Bar = 2
            |}

        ()

    let typeConstrainedMethodSimple (_ : #(int seq)) = ()
    let typeConstrainedMethodNested (_ : #(int seq) list) = ()

    module Nested =

        let foo = 5

    let publicArgumentFunction (prov : int -> int) : int = prov 0

    let publicArgumentPartial (i : int) : int -> int =
        let i = i + 1
        fun j -> i + j

    let publicArgumentCurry (i : int) (j : int) : int = i + j

    let publicArgumentTupleImplicit (i : int, j : int) : int = i + j

    let publicArgumentTupleExplicit (ij : int * int) : int = let (i, j) = ij in i + j

    let publicArgumentFunctionTuple (i : int, f : int -> int) : int = f i

    [<Literal>]
    let someLiteral = 15

    [<Literal>]
    let someEmptyLiteral = ""

module ValueTupleOperations =
    let fst struct (x, _) = x
