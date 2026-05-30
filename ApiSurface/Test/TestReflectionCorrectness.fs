namespace ApiSurface.Test

open System
open System.Reflection
open System.Reflection.Emit
open NUnit.Framework
open FsUnitTyped
open ApiSurface

[<TestFixture>]
module TestReflectionCorrectness =

    let private reflectionFixture =
        lazy
            (let assemblyName = AssemblyName "ApiSurface.Test.ReflectionFixture"

             let assembly =
                 AssemblyBuilder.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Run)

             let moduleBuilder = assembly.DefineDynamicModule assemblyName.Name

             let typeBuilder =
                 moduleBuilder.DefineType ("ReflectionFixture", TypeAttributes.Public)

             typeBuilder.DefineDefaultConstructor MethodAttributes.Public |> ignore

             let emitReturn (methodBuilder : MethodBuilder) =
                 methodBuilder.GetILGenerator().Emit OpCodes.Ret

             let propertySetter =
                 typeBuilder.DefineMethod (
                     "set_WriteOnly",
                     MethodAttributes.Public
                     ||| MethodAttributes.SpecialName
                     ||| MethodAttributes.HideBySig,
                     typeof<Void>,
                     [| typeof<int> |]
                 )

             emitReturn propertySetter

             let property =
                 typeBuilder.DefineProperty ("WriteOnly", PropertyAttributes.None, typeof<int>, Type.EmptyTypes)

             property.SetSetMethod propertySetter

             let eventRemover =
                 typeBuilder.DefineMethod (
                     "remove_RemoveOnly",
                     MethodAttributes.Public
                     ||| MethodAttributes.SpecialName
                     ||| MethodAttributes.HideBySig,
                     typeof<Void>,
                     [| typeof<EventHandler> |]
                 )

             emitReturn eventRemover

             let event =
                 typeBuilder.DefineEvent ("RemoveOnly", EventAttributes.None, typeof<EventHandler>)

             event.SetRemoveOnMethod eventRemover

             let literal =
                 typeBuilder.DefineField (
                     "HiddenLiteral",
                     typeof<int>,
                     FieldAttributes.Private ||| FieldAttributes.Static ||| FieldAttributes.Literal
                 )

             literal.SetConstant 42

             let nestedType =
                 typeBuilder.DefineNestedType ("Nested", TypeAttributes.NestedPublic)

             nestedType.CreateTypeInfo () |> ignore

             typeBuilder.CreateTypeInfo().AsType ())

    [<Test>]
    let ``isPublic handles constructors`` () =
        reflectionFixture.Value.GetConstructors().[0]
        |> DocCoverage.isPublic
        |> shouldEqual true

    [<Test>]
    let ``isPublic handles setter-only properties`` () =
        reflectionFixture.Value.GetProperty "WriteOnly"
        |> DocCoverage.isPublic
        |> shouldEqual true

    [<Test>]
    let ``isPublic handles remove-only events`` () =
        reflectionFixture.Value.GetEvent "RemoveOnly"
        |> DocCoverage.isPublic
        |> shouldEqual true

    [<Test>]
    let ``isPublic handles nested public types`` () =
        reflectionFixture.Value.GetNestedType "Nested"
        |> DocCoverage.isPublic
        |> shouldEqual true

    [<Test>]
    let ``print handles private literals`` () =
        reflectionFixture.Value.GetField ("HiddenLiteral", BindingFlags.NonPublic ||| BindingFlags.Static)
        |> ApiMember.ofMemberInfo
        |> ApiMember.print
        |> shouldEqual "ReflectionFixture.HiddenLiteral [static field]: int = 42"
