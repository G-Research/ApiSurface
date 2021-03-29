namespace ApiSurface

open System
open System.Reflection

/// Simplified model of .NET Reflection hierarchy.
/// This subset of information reflects the amount of information that ApiSurface chooses to include in its baseline.
type MemberTypeInfo =
    /// A constructor has a number of parameters, optionality/naming is ignored.
    | Constructor of parameters : Type list
    /// A method has a number of curried parameters, in/out/optionality/naming is ignored.
    | Method of parameters : Type list list * returns : Type
    /// A property has an associated type and mutability.
    | Property of propertyType : Type * readonly : bool
    /// A field has an associated type.
    | Field of fieldType : Type * fieldInfo : FieldInfo
    /// An event.
    | Event

/// Simplified model of .NET Reflection hierarchy.
type ApiMember =
    {
        /// Name (from the `MemberInfo.Name` property)
        Name : string
        /// Type information
        MemberTypeInfo : MemberTypeInfo
        /// Type that this member is defined on
        DeclaringType : Type
        /// Whether this member is a static member
        IsStatic : bool
    }

[<RequireQualifiedAccess>]
module ApiMember =

    /// Default printer for an ApiMember
    [<CompiledName "Print">]
    val print : ApiMember -> string

    /// Extract the simplified ApiMember information from a .NET member
    [<CompiledName "FromMemberInfo">]
    val ofMemberInfo : MemberInfo -> ApiMember
