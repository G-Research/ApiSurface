namespace ApiSurface.Test

open System
open System.Collections.Generic
open NUnit.Framework
open FsUnit

open ApiSurface
open System.Reflection

type R2 =
    {
        Foo : int
    }

type DescendingComparer<'T when 'T :> IComparable<'T>> () =
    interface IComparer<'T> with
        member __.Compare (x : 'T, y : 'T) : int = y.CompareTo x

module private TestFunctions =
    type Hack = Hack

    let simpleParameterConstraint (_ : #(int seq) list) = ()

    let multipleParameterConstraint<'a when 'a :> int seq and 'a :> IDisposable> (_ : 'a list) = ()


[<TestFixture>]
module TypeTests =

    [<Test>]
    let ``Test toString - fullname`` () =
        typeof<int -> (string * char) -> (int -> unit) -> R2 list -> Map<string, int>>
        |> Type.toFullName
        |> should equal "int -> (string * char) -> (int -> unit) -> ApiSurface.Test.R2 list -> Map<string, int>"

    [<Test>]
    let ``Test toString - short name`` () =
        typeof<int -> (string * char) -> (int -> unit) -> R2 list -> Map<string, int>>
        |> Type.toString
        |> should equal "int -> (string * char) -> (int -> unit) -> R2 list -> Map<string, int>"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString (nested functions)`` (fullName : bool) =
        typeof<string -> (int -> unit) -> (string -> bool -> (char -> string) -> float) -> unit>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "string -> (int -> unit) -> (string -> bool -> (char -> string) -> float) -> unit"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString (functions in tuples)`` (fullName : bool) =
        typeof<string * (int -> unit) * float>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "(string * (int -> unit) * float)"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString (functions in struct tuples)`` (fullName : bool) =
        typeof<struct (string * (int -> unit) * float)>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "struct (string * (int -> unit) * float)"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString (Map)`` (fullName : bool) =
        typeof<Map<string, int>>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "Map<string, int>"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString (arrays)`` (fullName : bool) =
        typeof<bool[]>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "bool []"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString (composite template functions)`` (fullName : bool) =
        typeof<obj list seq>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "obj list seq"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (int)`` (fullName : bool) =
        typeof<int>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "int"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (bool seq)`` (fullName : bool) =
        typeof<bool seq>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "bool seq"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (bigint)`` (fullName : bool) =
        typeof<bigint>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "bigint"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (float)`` (fullName : bool) =
        typeof<float>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "float"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (single)`` (fullName : bool) =
        typeof<single>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "single"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (obj)`` (fullName : bool) =
        typeof<obj>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "obj"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (ref)`` (fullName : bool) =
        typeof<int ref>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "int ref"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (option)`` (fullName : bool) =
        typeof<bool option>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "bool option"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString short names (handler)`` (fullName : bool) =
        typeof<bool Handler>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "bool Handler"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString with a function option`` (fullName : bool) =
        typeof<(unit -> bool) option>
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "(unit -> bool) option"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString with parameter type constraints`` (fullName : bool) =
        let mi =
            typeof<TestFunctions.Hack>.DeclaringType
                .GetMethod ("simpleParameterConstraint", BindingFlags.Static ||| BindingFlags.NonPublic)

        let p = mi.GetParameters().[0]

        p.ParameterType
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "#(int seq) list"

    [<TestCase(false)>]
    [<TestCase(true)>]
    let ``Test toString with multiple parameter type constraints`` (fullName : bool) =
        let mi =
            typeof<TestFunctions.Hack>.DeclaringType
                .GetMethod ("multipleParameterConstraint", BindingFlags.Static ||| BindingFlags.NonPublic)

        let p = mi.GetParameters().[0]

        p.ParameterType
        |> if not fullName then Type.toString else Type.toFullName
        |> should equal "('a :> int seq and 'a :> IDisposable) list"

    [<Test>]
    let ``Test toString with recursively defined interfaces`` () =
        let t = typedefof<DescendingComparer<int>>
        Type.toString t |> should equal "#(... IComparable) DescendingComparer"
