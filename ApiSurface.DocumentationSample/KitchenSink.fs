namespace ApiSurface.DocumentationSample


/// A DU which holds no data.
type EnumLikeDu =
    /// case
    | Foo
    /// case
    | Bar

/// A DU whose constructors are internal and hence hidden.
type DuWithInternalConstructors =
    internal
    /// This shouldn't be classed as a difference!
    | Foo
    | Bar

/// A DU whose constructors are private and hence hidden.
type DuWithPrivateConstructors =
    private
    | Foo
    /// This shouldn't be classed as a difference!
    | Bar

type private PrivateDu =
    | Foo
    /// This shouldn't be classed as a difference!
    | Bar

/// This shouldn't be classed as a difference!
type private PrivateRecord =
    {
        /// This shouldn't be classed as a difference!
        PrivateField : bool
    }

/// A module
module SomeModule =
    /// A *nested* module
    module SomeNestedModule =
        /// A member of a nested module
        let foo = 15

    /// For some reason, literal fields seem to be documented as properties; check that we handle them correctly
    [<Literal>]
    let Foo = true

    /// ... and non-literals are handled correctly too
    let Bar = true

/// bar
type MyTinyType =
    /// foo
    | MyTinyType of string

/// A general DU
type MyDu =
    /// A case with arguments
    | SomeCase of int * string
    /// A case with just one argument
    | AnotherCase of bool
    /// A case with no arguments
    | Fieldless

    /// A property
    member _.SomeProperty = "hi"

// The module specifically does not need a comment!
// The type is already required to be commented.
module MyDu =

    /// A property inside a module which has a type of the same name
    let x = 15

/// A record
type SomeRecord =
    {
        /// A field in a record
        SomeField : int

        /// A mutable field
        mutable SomeMutableField : bool
    }

    /// A static member on a record
    static member Parse _ = failwith ""

/// An interface with a generic type parameter
type 'a IGenericInterface =
    /// An abstract member on the interface
    abstract ConsumesGenericParameter : 'a -> int

    /// An abstract member *with a generic type parameter* on the interface
    abstract HasGenericArgument<'b> : 'a -> 'b -> bool

/// A module
module MyModule =

    /// A member on the module
    let myValue = 15

    /// A function on the module
    let someFunc (x : int) =
        // Lambdas compile down to nested types (with fields)
        // The type itself is private, but the Invoke method is public
        // Make sure we don't expect doc comments here
        let someInnerLambda bar = x + bar

        someInnerLambda x > 0

    /// A unit function on the module
    let unitFunc () = "hi"

    // This shouldn't be classed as a difference!
    let internal skipMe () = ""

    // This shouldn't be classed as a difference!
    let private somePrivateFunc () = ""

/// A class
type Class1 () =

    /// A public mutable field
    [<DefaultValue>]
    val mutable public myField : int

    /// A property
    member this.X = "F#"

/// A generic DU
type SomeGenericType<'a, 'c> =
    /// A case of the generic DU
    | SomeGenericType of 'a

    /// A member on the generic DU
    member _.SomeGenericMethod<'b> (_ : 'a) (_ : SomeGenericType<'a, 'b>) : int = failwith ""

/// A module
module ConsumingModule =

    /// A function with generic type parameters on the return type
    let somethingGeneric<'a, 'b> () : SomeGenericType<'a, 'b> = failwith ""

    /// A function with generic type parameters on the input type
    let somethingElse<'a, 'b> (_ : SomeGenericType<'a, 'b> * 'b) : int = failwith ""

/// A type with another `new` constructor
type TypeWithMultipleConstructors (_x : int) =

    // Don't expect constructors to be documented
    new (_ : string) = TypeWithMultipleConstructors (15)

/// A module
module ModuleWithType =

    /// A DU contained within a module
    type DU =
        /// A case
        | Case1
        /// A case
        | Case2

    /// A type with a generic type parameter, contained in a module
    type 'a NestedClass =
        /// An abstract method on a type contained in a module
        abstract MethodOnANestedClass : 'a NestedClass -> unit

    /// Function in a module, consuming the DU also contained in the module
    let thing (_ : DU) = failwith ""

/// Abstract class with methods
[<AbstractClass>]
type GenericClassWithMethod<'T> =

    /// Abstract method
    abstract M' : GenericClassWithMethod<'T> -> unit

    /// Abstract method which takes an IEnumerable byref
    abstract IEnumerableByRef : System.Collections.Generic.IEnumerable<'T> byref -> unit

    /// Abstract method which takes an array byref
    abstract ArrayByRef : 'T array byref -> unit

/// Type with a generic type parameter and a module of the same name
type 'a MyGenericType =
    /// Case of the generic type
    | SomeCase

// This shouldn't be documented, as the type is documented
module MyGenericType =

    /// But we do require the member to be documented
    let myValue = 15

type internal MomentActionsId =
    // Shouldn't need to be documented
    | MomentActionsIdConstructor of int option

/// Type containing properties with getters
type IndexedProperties<'a, 'b> =
    /// Simple getter with one argument
    member this.Item
        with get (_ : int) : string = failwith ""

    /// Overloaded getter with two arguments
    member this.Overloaded
        with get (_ : int, _ : int) : string = failwith ""

    /// Overloaded getter with two arguments
    member this.Overloaded
        with get (_ : string, _ : int) : string = failwith ""

    /// Getter with one argument
    member this.GenericItem
        with get (_ : int * (int * int)) : string = failwith ""

    /// Getter with multiple arguments
    member this.GenericParamItem
        with get (_ : 'b, _ : 'a, _ : int) : string = failwith ""

/// A module containing operators
[<AutoOpen>]
module MyOperators =

    /// An operator
    let (~~) (x : int) = x * 2

/// An exception
exception SomeExceptionType

/// Exception which has args
exception SomeExceptionTypeWithArgs of hello : int

/// Module containing active patterns
module SomeActivePatterns =

    /// An active pattern
    let (|ActivePatternName|_|) _ = None
