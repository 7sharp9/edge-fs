namespace global
//#r "/Library/Frameworks/Mono.framework/Versions/3.0.10/lib/mono/4.0/System.Core.dll"
open System.Threading.Tasks
type Startup() =
    let addSeven v =  
        v + 7
    member x.Invoke(input:obj) =
        let v = input :?> int
        async.Return (addSeven v :> obj) |> Async.StartAsTask
