namespace ApiSurface

open System
open System.Text.RegularExpressions
open System.Reflection
open System.Runtime.CompilerServices
open System.Xml.Linq
open System.IO
open FSharp.Reflection

[<RequireQualifiedAccess>]
type Members =
    | UnknownAccess of (string * string) Set
    | MixedAccess of publicMembers : (string * string) Set * nonPublic : (string * string) Set

type DocCoverage = | DocCoverage of source : string * members : Members

[<RequireQualifiedAccess>]
module DocCoverage =

    let typeFullName (t : Type) : string =
        // FullName precedes nested types with '+' rather than a '.'. However,
        // the F# compiler writes nested types into XML documentation with a period prefix.
        // Standardise on the F# compiler's formatting.
        t.FullName.Replace ('+', '.')

    let isPublic (memberInfo : MemberInfo) : bool =
        (isNull memberInfo.DeclaringType || memberInfo.DeclaringType.IsVisible)
        && match memberInfo.MemberType with
           | MemberTypes.Method
           | MemberTypes.Constructor -> let i = memberInfo :?> MethodInfo in i.IsPublic
           | MemberTypes.Event -> let i = memberInfo :?> EventInfo in i.AddMethod.IsPublic
           | MemberTypes.Field -> let i = memberInfo :?> FieldInfo in i.IsPublic
           | MemberTypes.TypeInfo
           | MemberTypes.NestedType -> let i = memberInfo :?> TypeInfo in i.IsPublic
           | MemberTypes.Property -> let i = memberInfo :?> PropertyInfo in i.GetMethod.IsPublic
           | memberType -> failwithf "Unrecognised MemberType: %O" memberType

    let paramInfoToString (pi : ParameterInfo) : string =
        let rec parameterTypeToString (t : Type) =
            if t.IsGenericParameter then
                let prefix = if isNull t.DeclaringMethod then "`" else "``"
                prefix + t.GenericParameterPosition.ToString ()
            elif t.HasElementType then
                let formatter : Printf.StringFormat<_> =
                    if t.IsArray then "%s[]"
                    elif t.IsByRef then "%s@"
                    elif t.IsPointer then "%s*"
                    else failwithf "Unexpected type '%O' with element type" t

                sprintf formatter (parameterTypeToString (t.GetElementType ()))
            elif t.IsGenericType then
                let rec nestedNames (t : Type) =
                    match t with
                    | null -> []
                    | _ -> t.Name.Split('`').[0] :: (nestedNames t.DeclaringType)

                let simpleName = nestedNames t |> Seq.rev |> String.concat "."

                t.GetGenericArguments ()
                |> Seq.map parameterTypeToString
                |> String.concat ","
                |> sprintf "%s.%s{%s}" t.Namespace simpleName
            else
                typeFullName t

        parameterTypeToString pi.ParameterType

    let paramInfosToString (parameters : ParameterInfo[]) : string =
        match parameters with
        | [||] -> ""
        | _ -> parameters |> Seq.map paramInfoToString |> String.concat "," |> sprintf "(%s)"

    let methodInfoName (methInfo : MethodInfo) : string =
        let suffix =
            if methInfo.IsGenericMethod then
                methInfo.GetGenericArguments().Length |> sprintf "``%d"
            else
                ""

        // op_xyz$W methods are written to the xml file without the $W suffix
        let trimmedName =
            if methInfo.Name.EndsWith ("$W", StringComparison.Ordinal) then
                methInfo.Name.Substring (0, methInfo.Name.Length - 2)
            else
                methInfo.Name

        methInfo.GetParameters ()
        |> paramInfosToString
        |> sprintf "%s%s%s" trimmedName suffix

    let propertyInfoName (propInfo : PropertyInfo) : string =
        propInfo.GetIndexParameters ()
        |> paramInfosToString
        |> sprintf "%s%s" propInfo.Name

    [<CompiledName "FromAssemblySurface">]
    let ofAssemblySurface (assembly : Assembly) : DocCoverage =
        let types =
            assembly.GetTypes ()
            |> Array.filter (fun ty ->
                obj.ReferenceEquals (ty.GetCustomAttribute<CompilerGeneratedAttribute> (), null)
                // A version of the F# compiler in 7.0.400 emitted some generated types which were not marked
                // as compiler-generated. Skip those.
                // https://github.com/dotnet/fsharp/issues/16141
                && not (ty.FullName.StartsWith ("System.Diagnostics.CodeAnalysis.Dynamic", StringComparison.Ordinal))
            )

        let getSourceConstructFlags (compilationMapping : CompilationMappingAttribute) =
            if obj.ReferenceEquals (compilationMapping, null) then
                SourceConstructFlags.None
            else
                compilationMapping.SourceConstructFlags

        let typeWithNonGenericNameExists exclude name =
            types
            |> Array.exists (fun t -> t <> exclude && t.FullName.Split('`').[0] = name)

        let allMembers =
            types
            |> Seq.filter (fun t ->
                match t.DeclaringType with
                // Skip nested types in union types (i.e. the 'Tags' type)
                | declaring when declaring <> null && FSharpType.IsUnion (declaring, true) -> false
                | _ -> true
            )
            |> Seq.map (fun t ->
                let isUnion = FSharpType.IsUnion (t, true)

                let typeConstruct =
                    t.GetCustomAttribute<CompilationMappingAttribute> () |> getSourceConstructFlags

                t,
                t.GetMembers (
                    BindingFlags.Public
                    ||| BindingFlags.NonPublic
                    ||| BindingFlags.Static
                    ||| BindingFlags.Instance
                    ||| BindingFlags.DeclaredOnly
                )
                |> Seq.filter (fun mi ->
                    not (mi.IsDefined typeof<CompilerGeneratedAttribute>)
                    &&

                    // Exception fields are impossible to document - skip them all
                    typeConstruct <> SourceConstructFlags.Exception
                    &&

                    // Skip code generated members
                    // [FS1104] Identifiers containing '@' are reserved for use in F# code generation
                    not (mi.Name.Contains "@")
                    &&

                    match mi.MemberType with
                    // Skip constructors, the type will be documented anyway
                    | MemberTypes.Constructor -> false

                    // Only allow some methods with special names.
                    // We explicitly ban 'get_' and 'set_' methods as we already
                    // crawl over the corresponding property members.
                    // Special names include active patterns and operator
                    // overloads.
                    | MemberTypes.Method ->
                        let methInfo = mi :?> MethodInfo

                        not methInfo.IsSpecialName
                        || (not (mi.Name.StartsWith ("get_", StringComparison.Ordinal))
                            && not (mi.Name.StartsWith ("set_", StringComparison.Ordinal)))

                    // Skip nested types of F# unions - they're compiler
                    // generated (so cannot be commented), although not
                    // annotated as such.
                    | MemberTypes.NestedType
                    | MemberTypes.TypeInfo when isUnion -> false

                    | _ -> true
                )
            )
            |> Seq.collect (fun (t, mis) ->
                let isUnion = FSharpType.IsUnion (t, true)

                let typeConstruct =
                    t.GetCustomAttribute<CompilationMappingAttribute> () |> getSourceConstructFlags

                // Skip modules which have a corresponding type
                let optionalTypeDoc =
                    let searchName =
                        if t.FullName.EndsWith ("Module", StringComparison.Ordinal) then
                            t.FullName.Substring (0, t.FullName.Length - 6)
                        else
                            t.FullName

                    if
                        typeConstruct = SourceConstructFlags.Module
                        && typeWithNonGenericNameExists t searchName
                    then
                        []
                    else
                        [ typeFullName t, "T", t.IsPublic ]

                mis
                |> Seq.collect (fun mi ->
                    let methodConstruct =
                        mi.GetCustomAttribute<CompilationMappingAttribute> () |> getSourceConstructFlags

                    // This might be op_xyz but there's also an op_xyz$W, in which case that $W is
                    // what's actually written to the XML file (but without the $W part in the name)
                    if mis |> Seq.exists (fun other -> other.Name = mi.Name + "$W") then
                        []
                    else

                    match mi.MemberType with
                    | MemberTypes.Method when isUnion && mi.Name.StartsWith ("New", StringComparison.Ordinal) ->
                        // Strip the 'New' prefix and make this member a nested
                        // type instead
                        [
                            sprintf "%s.%s" (typeFullName mi.DeclaringType) (mi.Name.Substring 3), "T", isPublic mi
                        ]

                    // Field-less DU cases are property-like methods, with the
                    // CompilationMappingAttribute specifying that the method
                    // relates to a union case.
                    // Represent it as a type instead, as this is how the XML
                    // represents it.
                    | MemberTypes.Method when isUnion && methodConstruct = SourceConstructFlags.UnionCase ->
                        [
                            sprintf "%s.%s" (typeFullName mi.DeclaringType) (mi.Name.Substring 4), "T", isPublic mi
                        ]

                    // Literal fields are documented as properties, for some reason.
                    // Match that particular error here.
                    | MemberTypes.Field when (mi :?> FieldInfo).Attributes.HasFlag FieldAttributes.Literal ->
                        [ sprintf "%s.%s" (typeFullName mi.DeclaringType) mi.Name, "P", isPublic mi ]

                    | _ ->
                        let name, memberCode =
                            match mi.MemberType with
                            | MemberTypes.Constructor -> sprintf "%s.#ctor" (typeFullName mi.DeclaringType), "M"
                            | MemberTypes.Event -> sprintf "%s.%s" (typeFullName mi.DeclaringType) mi.Name, "E"
                            | MemberTypes.Field -> sprintf "%s.%s" (typeFullName mi.DeclaringType) mi.Name, "F"
                            | MemberTypes.Method ->
                                mi :?> MethodInfo
                                |> methodInfoName
                                |> sprintf "%s.%s" (typeFullName mi.DeclaringType),
                                "M"
                            | MemberTypes.TypeInfo -> sprintf "%s.%s" (typeFullName mi.DeclaringType) mi.Name, "T"
                            | MemberTypes.NestedType -> sprintf "%s.%s" (typeFullName mi.DeclaringType) mi.Name, "T"
                            | MemberTypes.Property ->
                                mi :?> PropertyInfo
                                |> propertyInfoName
                                |> sprintf "%s.%s" (typeFullName mi.DeclaringType),
                                "P"
                            | memberType -> failwithf "Unrecognised MemberType: %O" memberType

                        [ name, memberCode, isPublic mi ]
                )
                |> Seq.append optionalTypeDoc
            )

        let publicMembers, nonPublicMembers =
            allMembers |> List.ofSeq |> List.partition (fun (_, _, isPublic) -> isPublic)

        let publicMembers = publicMembers |> Seq.map (fun (n, c, _) -> n, c) |> Set.ofSeq

        let nonPublicMembers =
            nonPublicMembers |> Seq.map (fun (n, c, _) -> n, c) |> Set.ofSeq

        DocCoverage (Path.GetFileName assembly.Location, Members.MixedAccess (publicMembers, nonPublicMembers))

    let toXmlName (name : string, prefix : string) : string = sprintf "%s:%s" prefix name

    [<CompiledName "ToString">]
    let toString (DocCoverage (_, members)) =
        match members with
        | Members.UnknownAccess set -> set
        | Members.MixedAccess (publicMembers, nonPublic) -> publicMembers |> Set.union nonPublic
        |> Seq.map toXmlName
        |> String.concat "\n"

    let private fixedPoint<'a when 'a : equality> (f : 'a -> 'a) (x : 'a) : 'a =
        let mutable t1, t2 = x, f x
        let mutable counter = 0

        while t1 <> t2 do
            t1 <- t2
            t2 <- f t2
            counter <- counter + 1

            if counter > 1000 then
                failwithf "We appear to have entered a loop in the fixed-point function (%+A)" x

        t1

    [<CompiledName "Compare">]
    let compare (DocCoverage (baselineSrc, baselineMembers)) (DocCoverage (targetSrc, targetMembers)) =
        let removeNonPublic other this =
            match this, other with
            | Members.MixedAccess (publicBase, _), _ -> publicBase
            | Members.UnknownAccess members, Members.MixedAccess (_, nonPublic) -> Set.difference members nonPublic
            | Members.UnknownAccess members, _ -> members

        let baselineSet = baselineMembers |> removeNonPublic targetMembers

        let targetSet = targetMembers |> removeNonPublic baselineMembers

        // There is a bug of unknown provenance in the handling of byref between .NET Core and Framework.
        // We should revisit it when we are on a later version of .NET.
        // The following two are equivalent:
        //(ApiSurface.DocumentationSample.GenericClassWithMethod`1.ArrayByRef(`0[]@), M)
        //(ApiSurface.DocumentationSample.GenericClassWithMethod`1.ArrayByRef(Microsoft.FSharp.Core.byref{`0[],Microsoft.FSharp.Core.ByRefKinds.InOut}), M)
        let fixOneByref (s : string) : string =
            Regex.Replace (
                s,
                @"Microsoft\.FSharp\.Core\.byref{(?<type>.+),Microsoft\.FSharp\.Core\.ByRefKinds\.InOut}",
                @"${type}@"
            )

        let transformByrefs = fixedPoint fixOneByref

        let baselineChanged, baselineSet =
            ((false, Set.empty), baselineSet)
            ||> Set.fold (fun (didChange, building) (elt, memberType) ->
                let newElt = transformByrefs elt
                (didChange || (newElt <> elt)), Set.add (newElt, memberType) building
            )

        let targetChanged, targetSet =
            ((false, Set.empty), targetSet)
            ||> Set.fold (fun (didChange, building) (elt, memberType) ->
                let newElt = transformByrefs elt
                (didChange || (newElt <> elt)), Set.add (newElt, memberType) building
            )

        if targetChanged || baselineChanged then
            printfn
                "WARNING: we corrected at least one use of byref. Byref support is not guaranteed to work correctly in the documentation checker."

        // (end of the hack)

        let added = Set.difference targetSet baselineSet |> Set.toList
        let removed = Set.difference baselineSet targetSet |> Set.toList

        match added, removed with
        | [], [] -> SurfaceComparison.identical
        | _, _ ->
            SurfaceComparison.different
                (sprintf "are only in '%s'" targetSrc)
                (sprintf "are only in '%s'" baselineSrc)
                (added |> List.map toXmlName)
                (removed |> List.map toXmlName)

    [<CompiledName "FromXml">]
    let ofXml (source : string) (xml : string) =
        let doc = XDocument.Parse xml

        let members =
            doc.Descendants (XName.op_Implicit "member")
            |> Seq.map (fun element ->
                let name = element.Attribute(XName.op_Implicit "name").Value
                let parts = name.Split ([| ':' |], 2, StringSplitOptions.None)
                parts.[1], parts.[0]
            )
            |> Seq.filter (fun (memberName, _prefix) ->
                // Skip backing-fields of class properties.
                // These are private, but the F# compiler emits them in the XML
                // documentation anyway.
                not (memberName.EndsWith ("@", StringComparison.Ordinal))
            )
            |> Set.ofSeq
            |> Members.UnknownAccess

        DocCoverage (source, members)

    [<CompiledName "FromAssemblyXml">]
    let ofAssemblyXml (assembly : Assembly) =
        let path = Path.ChangeExtension (assembly.Location, ".xml")
        File.ReadAllText path |> ofXml (Path.GetFileName path)

    /// Checks to make sure that the assembly is fully documented.
    /// Assumes the xml file is the source of truth, and anything that is found in the xml file
    /// that isn't found in the assembly is deemed fine, as it is just extra documentation.
    [<CompiledName "AssertFullyDocumented">]
    let assertFullyDocumented (assembly : Assembly) =
        ofAssemblyXml assembly
        |> compare (ofAssemblySurface assembly)
        |> SurfaceComparison.assertNoneRemoved false
