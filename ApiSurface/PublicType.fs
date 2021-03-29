namespace ApiSurface

open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Reflection

type PublicType =
    {
        FullName : string
        BaseClass : Type option
        Interfaces : Type list
        Members : ApiMember list
        UnderlyingType : Type
    }

[<RequireQualifiedAccess>]
module PublicType =

    [<CompiledName "Print">]
    let print (pt : PublicType) =
        let inheritStr =
            pt.BaseClass
            |> Option.map (fun bc -> sprintf " inherit %s" (Type.toFullName bc))
            |> Option.defaultValue ""

        let interfacesStr =
            match pt.Interfaces with
            | [] -> ""
            | interfaces ->
                interfaces
                |> Seq.map Type.toFullName
                |> String.concat ", "
                |> sprintf ", implements %s"

        let unionCountString =
            let isDu = FSharpType.IsUnion pt.UnderlyingType

            let isDuCase =
                pt.BaseClass |> Option.map FSharpType.IsUnion |> Option.defaultValue false

            if isDu && not isDuCase then
                let unionCount = FSharpType.GetUnionCases(pt.UnderlyingType).Length
                sprintf " - union type with %i cases" unionCount
            else
                ""

        let interfaceMembersString =
            if pt.UnderlyingType.IsInterface then
                let interfaceMembers = pt.UnderlyingType.GetMembers().Length
                sprintf " - interface with %i member(s)" interfaceMembers
            else
                ""

        [
            yield
                pt.FullName
                + inheritStr
                + interfacesStr
                + unionCountString
                + interfaceMembersString
            yield! pt.Members |> List.map ApiMember.print
        ]

    [<CompiledName "FromType">]
    let ofType (t : Type) : PublicType =
        let baseClass = Option.ofObj t.BaseType

        let interfaces =
            let allInterfaces = t.GetInterfaces () |> HashSet

            let baseInterfaces =
                baseClass
                |> Option.map (fun t -> t.GetInterfaces () |> HashSet)
                |> Option.defaultWith HashSet

            allInterfaces.ExceptWith baseInterfaces
            List.ofSeq allInterfaces

        let members =
            let interfaceImplementations =
                if t.IsInterface then
                    HashSet ()
                else
                    t.GetInterfaces ()
                    |> Seq.collect (fun intf -> t.GetInterfaceMap(intf).TargetMethods)
                    |> HashSet

            t.GetMembers (
                BindingFlags.Public
                ||| BindingFlags.Static
                ||| BindingFlags.Instance
                ||| BindingFlags.DeclaredOnly
            )
            |> Seq.filter (fun m ->
                m.MemberType <> MemberTypes.NestedType
                && match m with
                   | :? MethodInfo as methInfo ->
                       // Skip overridden methods - these are already represented in the base class
                       methInfo.GetBaseDefinition () = methInfo
                       && not (interfaceImplementations.Contains methInfo)
                   | _ -> true
            )
            |> Seq.map ApiMember.ofMemberInfo
            |> List.ofSeq

        {
            FullName = t.FullName
            BaseClass = Option.ofObj t.BaseType
            Interfaces = interfaces
            Members = members
            UnderlyingType = t
        }
