namespace ApiSurface

open System
open FSharp.Reflection

/// Associated helper functions for handling System.Type objects.
[<RequireQualifiedAccess>]
module internal Type =
    let private fullNameWithFallback (t : Type) : string =
        if String.IsNullOrEmpty t.FullName then
            t.Name
        else
            t.FullName

    let private toNiceName (t : Type) : string option =
        // RuntimeTypes may have null FullName but non-null Name; fall back to the Name if the FullName is null.
        t.FullName
        |> Option.ofObj
        |> Option.bind (
            function
            | "System.Collections.Generic.IEnumerable`1" -> "seq" |> Some
            | "System.Numerics.BigInteger" -> "bigint" |> Some
            | "System.Boolean" -> "bool" |> Some
            | "System.Char" -> "char" |> Some
            | "System.Double" -> "float" |> Some
            | "Microsoft.FSharp.Control.FSharpHandler`1" -> "Handler" |> Some
            | "Microsoft.FSharp.Core.FSharpRef`1" -> "ref" |> Some
            | "Microsoft.FSharp.Core.FSharpOption`1" -> "option" |> Some
            | "Microsoft.FSharp.Collections.FSharpList`1" -> "list" |> Some
            | "Microsoft.FSharp.Collections.FSharpMap`2" -> "Map" |> Some
            | "System.Single" -> "single" |> Some
            | "System.Int32" -> "int" |> Some
            | "System.Object" -> "obj" |> Some
            | "System.String" -> "string" |> Some
            | "System.Void"
            | "Microsoft.FSharp.Core.Unit" -> "unit" |> Some
            | "System.IDisposable" -> "IDisposable" |> Some
            | _ -> None
        )

    let private readableShortName (t : Type) : string =
        toNiceName t |> Option.defaultWith (fun () -> t.Name.Split([| '`' |], 2).[0])

    let private readableLongName (t : Type) : string =
        toNiceName t
        |> Option.defaultWith (fun () ->
            let fallback = fullNameWithFallback t
            fallback.Split([| '`' |], 2).[0]
        )

    let rec private flexibleToString (seenTypes : Type list) (renderLeaf : Type -> string) (t : Type) : string =
        if seenTypes |> List.contains t then
            "..."
        else

        let toString = flexibleToString seenTypes renderLeaf

        let toName (inner : Type) =
            let name = renderLeaf inner

            if not inner.IsGenericParameter then
                name
            else

            match inner.GetGenericParameterConstraints () with
            | [||] -> "'" + name
            | [| c |] ->
                let s = flexibleToString (t :: seenTypes) renderLeaf c
                sprintf "#(%s)" s
            | constraints ->
                let name = "'" + name

                constraints
                |> Array.map (flexibleToString (t :: seenTypes) renderLeaf)
                |> Array.map (sprintf "%s :> %s" name)
                |> String.concat " and "
                |> sprintf "(%s)"

        if FSharpType.IsFunction t then
            let fromType, toType = FSharpType.GetFunctionElements t

            let fromTypeStr =
                toString fromType
                |> match FSharpType.IsFunction fromType with
                   | true -> sprintf "(%s)"
                   | false -> id

            sprintf "%s -> %s" fromTypeStr (toString toType)
        elif FSharpType.IsTuple t then
            FSharpType.GetTupleElements t
            |> Seq.map (fun t ->
                toString t
                |> match FSharpType.IsFunction t with
                   | true -> sprintf "(%s)"
                   | false -> id
            )
            |> String.concat " * "
            |> sprintf "(%s)"
            |> sprintf
                "%s%s"
                (if
                     t.Name.StartsWith ("ValueTuple", StringComparison.Ordinal)
                     && t.Namespace = "System"
                 then
                     "struct "
                 else
                     "")
        elif t.IsGenericType then
            let gen = t.GetGenericTypeDefinition ()
            let genName = toName gen

            match t.GetGenericArguments () with
            | [| funcType |] when FSharpType.IsFunction funcType -> sprintf "(%s) %s" (toString funcType) genName
            | [| genericType |] -> sprintf "%s %s" (toString genericType) genName
            | genericTypes ->
                let args = genericTypes |> Seq.map toString |> String.concat ", "
                sprintf "%s<%s>" genName args
        elif t.IsArray then
            let elem = t.GetElementType ()
            sprintf "%s []" (toString elem)
        else
            toName t

    /// Renders a type in the way you would idiomatically declare them in F#
    let toString (a : Type) : string = flexibleToString [] readableShortName a

    /// Renders a type in a more verbose way than toString
    let toFullName (a : Type) : string = flexibleToString [] readableLongName a
