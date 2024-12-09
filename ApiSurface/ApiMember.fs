namespace ApiSurface

open System.Reflection
open FSharp.Reflection

type MemberTypeInfo =
    | Constructor of parameters : System.Type list
    | Method of parameters : System.Type list list * returns : System.Type
    | Property of propertyType : System.Type * readonly : bool
    | Field of fieldType : System.Type * fieldInfo : FieldInfo
    | Event

[<RequireQualifiedAccess>]
module MemberTypeInfo =

    let printMemberType =
        function
        | Constructor _ -> MemberTypes.Constructor
        | Method _ -> MemberTypes.Method
        | Property _ -> MemberTypes.Property
        | Field _ -> MemberTypes.Field
        | Event -> MemberTypes.Event
        >> fun x -> x.ToString().ToLower ()

type ApiMember =
    {
        Name : string
        MemberTypeInfo : MemberTypeInfo
        DeclaringType : System.Type
        IsStatic : bool
    }

[<RequireQualifiedAccess>]
module ApiMember =

    [<CompiledName "Print">]
    let print m =
        /// adds parentheses to function types
        let prettyType t =
            let fullName = Type.toFullName t

            if FSharpType.IsFunction t then
                sprintf "(%s)" fullName
            else
                fullName

        let formatParameters (ps : System.Type list) =
            match ps with
            | [] -> "unit"
            | [ x ] -> prettyType x
            | xs -> xs |> List.map Type.toFullName |> String.concat ", " |> sprintf "(%s)"

        let memberType =
            let str = m.MemberTypeInfo |> MemberTypeInfo.printMemberType
            if m.IsStatic then sprintf "static %s" str else str

        match m.MemberTypeInfo with
        | Constructor parameters -> formatParameters parameters
        | Method (parameters, returnType) ->
            let formattedParameters =
                parameters |> List.map formatParameters |> String.concat " -> "

            returnType |> prettyType |> sprintf "%s -> %s" formattedParameters
        | Property (propertyType, readOnly) ->
            let accessibility = if readOnly then "[read-only] " else ""
            propertyType |> Type.toFullName |> sprintf "%s%s" accessibility
        | Field (fieldType, fieldInfo) ->
            let typeString = fieldType |> Type.toFullName

            if fieldInfo.IsLiteral then
                let value = m.DeclaringType.GetField(m.Name).GetValue null |> string<obj>

                // Don't print `= ` for empty strings, as many editors/Git hooks trim trailing whitespace
                if value = "" then
                    typeString
                else
                    sprintf "%s = %s" typeString value
            else
                typeString
        | Event -> sprintf "[%O]" MemberTypes.Event
        |> sprintf "%s.%s [%s]: %O" m.DeclaringType.FullName m.Name memberType

    [<CompiledName "FromMemberInfo">]
    let ofMemberInfo (m : MemberInfo) =
        let isStatic =
            match m with
            | :? FieldInfo as info -> info.IsStatic
            | :? MethodInfo as info -> info.IsStatic
            | :? EventInfo as info ->
                (info.AddMethod <> null && info.AddMethod.IsStatic)
                || (info.RemoveMethod <> null && info.RemoveMethod.IsStatic)
            | :? PropertyInfo as info ->
                (info.SetMethod <> null && info.SetMethod.IsStatic)
                || (info.GetMethod <> null && info.GetMethod.IsStatic)
            | :? ConstructorInfo as info -> info.IsStatic
            | _ -> failwithf "Unrecognized MemberInfo %O" m

        let memberInfo =
            match m with
            | :? FieldInfo as info -> Field (info.FieldType, info)
            | :? MethodInfo as info ->
                let rawParameters =
                    info.GetParameters () |> List.ofArray |> List.map (fun p -> p.ParameterType)

                let argumentCountsAttribute =
                    info.GetCustomAttribute<CompilationArgumentCountsAttribute> ()

                let counts =
                    if obj.ReferenceEquals (argumentCountsAttribute, null) then
                        [ rawParameters.Length ]
                    else
                        argumentCountsAttribute.Counts |> Seq.toList

                // Combine the raw parameter list in group of sizes given by the counts from the CompilationArgumentCountsAttribute
                // e.g. A function (int, int) -> int -> int would have CompilationArgumentCountsAttribute.Count = [2; 1] and the below code
                // would make a list [[int; int]; [int]]
                let _, parameters =
                    counts
                    |> List.fold
                        (fun (input, output) count ->
                            input |> List.skip count, List.append output [ input |> List.take count ]
                        )
                        (rawParameters, [])

                Method (parameters, info.ReturnType)
            | :? EventInfo -> Event
            | :? PropertyInfo as info -> Property (info.PropertyType, not info.CanWrite)
            | :? ConstructorInfo as info ->
                let parameters =
                    info.GetParameters () |> List.ofArray |> List.map (fun p -> p.ParameterType)

                Constructor parameters
            | _ -> failwithf "Unrecognized MemberInfo %O" m

        {
            Name = m.Name
            MemberTypeInfo = memberInfo
            IsStatic = isStatic
            DeclaringType = m.DeclaringType
        }
