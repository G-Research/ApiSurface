namespace ApiSurface

open System

/// Simplified model of a .NET type.
type PublicType =
    {
        /// Full name of the type
        FullName : string
        /// Base class (if any)
        BaseClass : Type option
        /// Interfaces implemented directly by this type (not including interfaces implemented by base classes)
        Interfaces : Type list
        /// API members
        Members : ApiMember list
        /// The type that this public type was constructed from
        UnderlyingType : Type
    }

[<RequireQualifiedAccess>]
module PublicType =

    /// Prints a type and all of its members
    [<CompiledName "Print">]
    val print : pt : PublicType -> string list

    /// Convert a .NET Type to a PublicType
    [<CompiledName "FromType">]
    val ofType : Type -> PublicType
