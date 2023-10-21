namespace ApiSurface.Test

open System
open System.IO
open System.Text
open System.Threading

type RedirectOutput () =
    let mutable original = Unchecked.defaultof<_>
    let substitute = new MemoryStream ()
    let writer = new StreamWriter (substitute)

    do
        let isLocked = Interlocked.Increment (RedirectOutput.IsLocked : int ref)

        if isLocked = 1 then
            original <- Console.Out
            Console.SetOut writer
        else
            failwith "Attempted to use RedirectOutput twice in parallel"

    member this.GetText () =
        substitute.ToArray () |> Encoding.UTF8.GetString

    static member val private IsLocked = ref 0

    interface IDisposable with
        member _.Dispose () =
            Console.SetOut original
            writer.Dispose ()
            substitute.Dispose ()
            let isUnlocked = Interlocked.Decrement RedirectOutput.IsLocked

            if isUnlocked <> 0 then
                failwithf "RedirectOutput lock was somehow still held after disposal: %i" isUnlocked
